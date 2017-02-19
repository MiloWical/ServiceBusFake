using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class StreamedFramingRequestChannel : Microsoft.ServiceBus.Channels.RequestChannel
	{
		private Microsoft.ServiceBus.Channels.IConnectionInitiator connectionInitiator;

		private Microsoft.ServiceBus.Channels.ConnectionPool connectionPool;

		private MessageEncoder messageEncoder;

		private Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings;

		private byte[] startBytes;

		private StreamUpgradeProvider upgrade;

		private EventTraceActivity Activity
		{
			get;
			set;
		}

		private byte[] Preamble
		{
			get
			{
				return this.startBytes;
			}
		}

		public StreamedFramingRequestChannel(ChannelManagerBase factory, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportChannelFactorySettings settings, EndpointAddress remoteAddresss, Uri via, Microsoft.ServiceBus.Channels.IConnectionInitiator connectionInitiator, Microsoft.ServiceBus.Channels.ConnectionPool connectionPool) : base(factory, remoteAddresss, via, settings.ManualAddressing)
		{
			this.settings = settings;
			this.connectionInitiator = connectionInitiator;
			this.connectionPool = connectionPool;
			this.messageEncoder = settings.MessageEncoderFactory.Encoder;
			this.upgrade = settings.Upgrade;
			this.Activity = new EventTraceActivity();
		}

		protected override Microsoft.ServiceBus.Channels.IAsyncRequest CreateAsyncRequest(Message message, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest(this, callback, state);
		}

		protected override Microsoft.ServiceBus.Channels.IRequest CreateRequest(Message message)
		{
			return new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingRequest(this);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.BeginWaitForPendingRequests(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			base.WaitForPendingRequests(timeout);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.RequestChannel.EndWaitForPendingRequests(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		protected override void OnOpened()
		{
			Microsoft.ServiceBus.Channels.EncodedVia encodedVium = new Microsoft.ServiceBus.Channels.EncodedVia(base.Via.AbsoluteUri);
			Microsoft.ServiceBus.Channels.EncodedContentType encodedContentType = Microsoft.ServiceBus.Channels.EncodedContentType.Create(this.settings.MessageEncoderFactory.Encoder.ContentType);
			int length = (int)Microsoft.ServiceBus.Channels.ClientSingletonEncoder.ModeBytes.Length + Microsoft.ServiceBus.Channels.ClientSingletonEncoder.CalcStartSize(encodedVium, encodedContentType);
			int num = 0;
			if (this.upgrade == null)
			{
				num = length;
				length = length + (int)Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes.Length;
			}
			this.startBytes = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(length);
			Buffer.BlockCopy(Microsoft.ServiceBus.Channels.ClientSingletonEncoder.ModeBytes, 0, this.startBytes, 0, (int)Microsoft.ServiceBus.Channels.ClientSingletonEncoder.ModeBytes.Length);
			Microsoft.ServiceBus.Channels.ClientSingletonEncoder.EncodeStart(this.startBytes, (int)Microsoft.ServiceBus.Channels.ClientSingletonEncoder.ModeBytes.Length, encodedVium, encodedContentType);
			if (num > 0)
			{
				Buffer.BlockCopy(Microsoft.ServiceBus.Channels.ClientSingletonEncoder.PreambleEndBytes, 0, this.startBytes, num, (int)Microsoft.ServiceBus.Channels.ClientSingletonEncoder.PreambleEndBytes.Length);
			}
			base.OnOpened();
		}

		private Microsoft.ServiceBus.Channels.IConnection SendPreamble(Microsoft.ServiceBus.Channels.IConnection connection, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, out SecurityMessageProperty remoteSecurity)
		{
			connection.Write(this.Preamble, 0, (int)this.Preamble.Length, true, timeoutHelper.RemainingTime());
			if (this.upgrade == null)
			{
				remoteSecurity = null;
			}
			else
			{
				StreamUpgradeInitiator streamUpgradeInitiator = this.upgrade.CreateUpgradeInitiator(base.RemoteAddress, base.Via);
				if (!Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgrade(streamUpgradeInitiator, ref connection, decoder, this, ref timeoutHelper))
				{
					Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFramingFault(decoder, connection, base.Via, this.messageEncoder.ContentType, ref timeoutHelper);
				}
				if (!(streamUpgradeInitiator is StreamSecurityUpgradeInitiator))
				{
					remoteSecurity = null;
				}
				else
				{
					remoteSecurity = ((StreamSecurityUpgradeInitiator)streamUpgradeInitiator).GetRemoteSecurity();
				}
				connection.Write(Microsoft.ServiceBus.Channels.ClientSingletonEncoder.PreambleEndBytes, 0, (int)Microsoft.ServiceBus.Channels.ClientSingletonEncoder.PreambleEndBytes.Length, true, timeoutHelper.RemainingTime());
			}
			byte[] numArray = new byte[1];
			if (!Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidatePreambleResponse(numArray, connection.Read(numArray, 0, (int)numArray.Length, timeoutHelper.RemainingTime()), decoder, base.Via))
			{
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFramingFault(decoder, connection, base.Via, this.messageEncoder.ContentType, ref timeoutHelper);
			}
			return connection;
		}

		private class ClientSingletonConnectionReader : Microsoft.ServiceBus.Channels.SingletonConnectionReader
		{
			private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper connectionPoolHelper;

			protected override long StreamPosition
			{
				get
				{
					return this.connectionPoolHelper.Decoder.StreamPosition;
				}
			}

			public ClientSingletonConnectionReader(Microsoft.ServiceBus.Channels.IConnection connection, Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper connectionPoolHelper, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings) : base(connection, 0, 0, connectionPoolHelper.RemoteSecurity, settings, null)
			{
				this.connectionPoolHelper = connectionPoolHelper;
			}

			protected override bool DecodeBytes(byte[] buffer, ref int offset, ref int size, ref bool isAtEof)
			{
				while (size > 0)
				{
					int num = this.connectionPoolHelper.Decoder.Decode(buffer, offset, size);
					if (num > 0)
					{
						offset = offset + num;
						size = size - num;
					}
					Microsoft.ServiceBus.Channels.ClientFramingDecoderState currentState = this.connectionPoolHelper.Decoder.CurrentState;
					if (currentState == Microsoft.ServiceBus.Channels.ClientFramingDecoderState.EnvelopeStart)
					{
						return true;
					}
					if (currentState == Microsoft.ServiceBus.Channels.ClientFramingDecoderState.End)
					{
						isAtEof = true;
						return false;
					}
				}
				return false;
			}

			protected override void OnClose(TimeSpan timeout)
			{
				this.connectionPoolHelper.Close(timeout);
			}
		}

		internal class StreamedConnectionPoolHelper : Microsoft.ServiceBus.Channels.ConnectionPoolHelper
		{
			private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel;

			private Microsoft.ServiceBus.Channels.ClientSingletonDecoder decoder;

			private SecurityMessageProperty remoteSecurity;

			public Microsoft.ServiceBus.Channels.ClientSingletonDecoder Decoder
			{
				get
				{
					return this.decoder;
				}
			}

			public SecurityMessageProperty RemoteSecurity
			{
				get
				{
					return this.remoteSecurity;
				}
			}

			public StreamedConnectionPoolHelper(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel) : base(channel.connectionPool, channel.connectionInitiator, channel.Via)
			{
				this.channel = channel;
			}

			protected override Microsoft.ServiceBus.Channels.IConnection AcceptPooledConnection(Microsoft.ServiceBus.Channels.IConnection connection, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
			{
				this.decoder = new Microsoft.ServiceBus.Channels.ClientSingletonDecoder((long)0);
				return this.channel.SendPreamble(connection, ref timeoutHelper, this.decoder, out this.remoteSecurity);
			}

			protected override IAsyncResult BeginAcceptPooledConnection(Microsoft.ServiceBus.Channels.IConnection connection, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
			{
				this.decoder = new Microsoft.ServiceBus.Channels.ClientSingletonDecoder((long)0);
				return new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult(this.channel, connection, ref timeoutHelper, this.decoder, callback, state);
			}

			protected override TimeoutException CreateNewConnectionTimeoutException(TimeSpan timeout, TimeoutException innerException)
			{
				string requestTimedOutEstablishingTransportSession = Resources.RequestTimedOutEstablishingTransportSession;
				object[] objArray = new object[] { timeout, this.channel.Via.AbsoluteUri };
				return new TimeoutException(Microsoft.ServiceBus.SR.GetString(requestTimedOutEstablishingTransportSession, objArray), innerException);
			}

			protected override Microsoft.ServiceBus.Channels.IConnection EndAcceptPooledConnection(IAsyncResult result)
			{
				return Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.End(result, out this.remoteSecurity);
			}

			private class SendPreambleAsyncResult : AsyncResult
			{
				private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel;

				private Microsoft.ServiceBus.Channels.IConnection connection;

				private Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder;

				private StreamUpgradeInitiator upgradeInitiator;

				private SecurityMessageProperty remoteSecurity;

				private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

				private static AsyncCallback onWritePreamble;

				private static AsyncCallback onWritePreambleEnd;

				private static WaitCallback onReadPreambleAck;

				private static AsyncCallback onUpgrade;

				private static AsyncCallback onFailedUpgrade;

				static SendPreambleAsyncResult()
				{
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onWritePreamble = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.OnWritePreamble));
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onReadPreambleAck = new WaitCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.OnReadPreambleAck);
				}

				public SendPreambleAsyncResult(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel, Microsoft.ServiceBus.Channels.IConnection connection, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, Microsoft.ServiceBus.Channels.ClientFramingDecoder decoder, AsyncCallback callback, object state) : base(callback, state)
				{
					this.channel = channel;
					this.connection = connection;
					this.timeoutHelper = timeoutHelper;
					this.decoder = decoder;
					IAsyncResult asyncResult = connection.BeginWrite(channel.Preamble, 0, (int)channel.Preamble.Length, true, timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onWritePreamble, this);
					if (!asyncResult.CompletedSynchronously)
					{
						return;
					}
					if (this.HandleWritePreamble(asyncResult))
					{
						base.Complete(true);
					}
				}

				public static Microsoft.ServiceBus.Channels.IConnection End(IAsyncResult result, out SecurityMessageProperty remoteSecurity)
				{
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult sendPreambleAsyncResult = AsyncResult.End<Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult>(result);
					remoteSecurity = sendPreambleAsyncResult.remoteSecurity;
					return sendPreambleAsyncResult.connection;
				}

				private bool HandlePreambleAck()
				{
					int num = this.connection.EndRead();
					if (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidatePreambleResponse(this.connection.AsyncReadBuffer, num, this.decoder, this.channel.Via))
					{
						return true;
					}
					if (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onFailedUpgrade == null)
					{
						Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onFailedUpgrade = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.OnFailedUpgrade));
					}
					IAsyncResult asyncResult = Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.BeginDecodeFramingFault(this.decoder, this.connection, this.channel.Via, this.channel.messageEncoder.ContentType, ref this.timeoutHelper, Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onFailedUpgrade, this);
					if (!asyncResult.CompletedSynchronously)
					{
						return false;
					}
					Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.EndDecodeFramingFault(asyncResult);
					return true;
				}

				private bool HandleUpgrade(IAsyncResult result)
				{
					this.connection = Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.EndInitiateUpgrade(result);
					if (this.upgradeInitiator is StreamSecurityUpgradeInitiator)
					{
						this.remoteSecurity = ((StreamSecurityUpgradeInitiator)this.upgradeInitiator).GetRemoteSecurity();
					}
					this.upgradeInitiator = null;
					if (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onWritePreambleEnd == null)
					{
						Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onWritePreambleEnd = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.OnWritePreambleEnd));
					}
					IAsyncResult asyncResult = this.connection.BeginWrite(Microsoft.ServiceBus.Channels.ClientSingletonEncoder.PreambleEndBytes, 0, (int)Microsoft.ServiceBus.Channels.ClientSingletonEncoder.PreambleEndBytes.Length, true, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onWritePreambleEnd, this);
					if (!asyncResult.CompletedSynchronously)
					{
						return false;
					}
					this.connection.EndWrite(asyncResult);
					return this.ReadPreambleAck();
				}

				private bool HandleWritePreamble(IAsyncResult result)
				{
					this.connection.EndWrite(result);
					if (this.channel.upgrade == null)
					{
						return this.ReadPreambleAck();
					}
					this.upgradeInitiator = this.channel.upgrade.CreateUpgradeInitiator(this.channel.RemoteAddress, this.channel.Via);
					if (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onUpgrade == null)
					{
						Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onUpgrade = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.OnUpgrade));
					}
					IAsyncResult asyncResult = Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.BeginInitiateUpgrade(this.channel.settings, this.channel.RemoteAddress, this.connection, this.decoder, this.upgradeInitiator, this.channel.messageEncoder.ContentType, this.timeoutHelper, Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onUpgrade, this);
					if (!asyncResult.CompletedSynchronously)
					{
						return false;
					}
					return this.HandleUpgrade(asyncResult);
				}

				private static void OnFailedUpgrade(IAsyncResult result)
				{
					if (result.CompletedSynchronously)
					{
						return;
					}
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult)result.AsyncState;
					Exception exception = null;
					try
					{
						Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.EndDecodeFramingFault(result);
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
					}
					asyncState.Complete(false, exception);
				}

				private static void OnReadPreambleAck(object state)
				{
					bool flag;
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult sendPreambleAsyncResult = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult)state;
					Exception exception = null;
					try
					{
						flag = sendPreambleAsyncResult.HandlePreambleAck();
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
					if (flag)
					{
						sendPreambleAsyncResult.Complete(false, exception);
					}
				}

				private static void OnUpgrade(IAsyncResult result)
				{
					bool flag;
					if (result.CompletedSynchronously)
					{
						return;
					}
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult)result.AsyncState;
					Exception exception = null;
					try
					{
						flag = asyncState.HandleUpgrade(result);
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
					if (flag)
					{
						asyncState.Complete(false, exception);
					}
				}

				private static void OnWritePreamble(IAsyncResult result)
				{
					bool flag;
					if (result.CompletedSynchronously)
					{
						return;
					}
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult)result.AsyncState;
					Exception exception = null;
					try
					{
						flag = asyncState.HandleWritePreamble(result);
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
					if (flag)
					{
						asyncState.Complete(false, exception);
					}
				}

				private static void OnWritePreambleEnd(IAsyncResult result)
				{
					bool flag;
					if (result.CompletedSynchronously)
					{
						return;
					}
					Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult)result.AsyncState;
					Exception exception = null;
					try
					{
						asyncState.connection.EndWrite(result);
						flag = asyncState.ReadPreambleAck();
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
					if (flag)
					{
						asyncState.Complete(false, exception);
					}
				}

				private bool ReadPreambleAck()
				{
					if (this.connection.BeginRead(0, 1, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper.SendPreambleAsyncResult.onReadPreambleAck, this) == AsyncReadResult.Queued)
					{
						return false;
					}
					return this.HandlePreambleAck();
				}
			}
		}

		private class StreamedFramingAsyncRequest : AsyncResult, Microsoft.ServiceBus.Channels.IAsyncRequest, IAsyncResult, Microsoft.ServiceBus.Channels.IRequestBase
		{
			private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper connectionPoolHelper;

			private Message message;

			private Message replyMessage;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			private static AsyncCallback onEstablishConnection;

			private static AsyncCallback onWriteMessage;

			private static AsyncCallback onReceiveReply;

			private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.ClientSingletonConnectionReader connectionReader;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.channel.Activity;
				}
			}

			static StreamedFramingAsyncRequest()
			{
				Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.onEstablishConnection = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.OnEstablishConnection));
				Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.onWriteMessage = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.OnWriteMessage));
				Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.onReceiveReply = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.OnReceiveReply));
			}

			public StreamedFramingAsyncRequest(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel, AsyncCallback callback, object state) : base(callback, state)
			{
				this.channel = channel;
				this.connectionPoolHelper = new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper(channel);
			}

			public void Abort(Microsoft.ServiceBus.Channels.RequestChannel requestChannel)
			{
				this.Cleanup();
			}

			public void BeginSendRequest(Message message, TimeSpan timeout)
			{
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				this.message = message;
				bool flag = false;
				bool flag1 = false;
				try
				{
					try
					{
						IAsyncResult asyncResult = this.connectionPoolHelper.BeginEstablishConnection(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.onEstablishConnection, this);
						if (asyncResult.CompletedSynchronously)
						{
							flag = this.HandleEstablishConnection(asyncResult);
						}
					}
					catch (TimeoutException timeoutException1)
					{
						TimeoutException timeoutException = timeoutException1;
						ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
						string timeoutOnRequest = Resources.TimeoutOnRequest;
						object[] objArray = new object[] { timeout };
						throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(timeoutOnRequest, objArray), timeoutException));
					}
					flag1 = true;
				}
				finally
				{
					if (!flag1)
					{
						this.Cleanup();
					}
				}
				if (flag)
				{
					base.Complete(true);
				}
			}

			private void Cleanup()
			{
				this.connectionPoolHelper.Abort();
				this.message.Close();
			}

			private bool CompleteReceiveReply(IAsyncResult result)
			{
				this.replyMessage = this.connectionReader.EndReceive(result);
				return true;
			}

			public Message End()
			{
				try
				{
					AsyncResult.End<Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest>(this);
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string timeoutOnRequest = Resources.TimeoutOnRequest;
					object[] originalTimeout = new object[] { this.timeoutHelper.OriginalTimeout };
					throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(timeoutOnRequest, originalTimeout), timeoutException));
				}
				return this.replyMessage;
			}

			public void Fault(Microsoft.ServiceBus.Channels.RequestChannel requestChannel)
			{
				this.Cleanup();
			}

			private bool HandleEstablishConnection(IAsyncResult result)
			{
				this.connection = Microsoft.ServiceBus.Channels.ConnectionPoolHelper.EndEstablishConnection(result);
				IAsyncResult asyncResult = Microsoft.ServiceBus.Channels.StreamingConnectionHelper.BeginWriteMessage(this.message, this.connection, true, this.channel.settings, ref this.timeoutHelper, Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.onWriteMessage, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleWriteMessage(asyncResult);
			}

			private bool HandleWriteMessage(IAsyncResult result)
			{
				Microsoft.ServiceBus.Channels.StreamingConnectionHelper.EndWriteMessage(result);
				this.connectionReader = new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.ClientSingletonConnectionReader(this.connection, this.connectionPoolHelper, this.channel.settings);
				this.connectionReader.DoneSending(TimeSpan.Zero);
				IAsyncResult asyncResult = this.connectionReader.BeginReceive(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest.onReceiveReply, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.CompleteReceiveReply(asyncResult);
			}

			private static void OnEstablishConnection(IAsyncResult result)
			{
				bool flag;
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest asyncState = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest)result.AsyncState;
				Exception exception = null;
				bool flag1 = true;
				try
				{
					try
					{
						flag = asyncState.HandleEstablishConnection(result);
						flag1 = false;
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
				}
				finally
				{
					if (flag1)
					{
						asyncState.Cleanup();
					}
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnReceiveReply(IAsyncResult result)
			{
				bool flag;
				Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest asyncState = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest)result.AsyncState;
				Exception exception = null;
				bool flag1 = true;
				try
				{
					try
					{
						flag = asyncState.CompleteReceiveReply(result);
						flag1 = false;
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
				}
				finally
				{
					if (flag1)
					{
						asyncState.Cleanup();
					}
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnWriteMessage(IAsyncResult result)
			{
				bool flag;
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest asyncState = (Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedFramingAsyncRequest)result.AsyncState;
				Exception exception = null;
				bool flag1 = true;
				try
				{
					try
					{
						flag = asyncState.HandleWriteMessage(result);
						flag1 = false;
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						flag = true;
						exception = exception1;
					}
				}
				finally
				{
					if (flag1)
					{
						asyncState.Cleanup();
					}
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}
		}

		private class StreamedFramingRequest : Microsoft.ServiceBus.Channels.IRequest, Microsoft.ServiceBus.Channels.IRequestBase
		{
			private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel;

			private Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper connectionPoolHelper;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private EventTraceActivity Activity
			{
				get
				{
					return this.channel.Activity;
				}
			}

			public StreamedFramingRequest(Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel channel)
			{
				this.channel = channel;
				this.connectionPoolHelper = new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.StreamedConnectionPoolHelper(channel);
			}

			public void Abort(Microsoft.ServiceBus.Channels.RequestChannel requestChannel)
			{
				this.Cleanup();
			}

			private void Cleanup()
			{
				this.connectionPoolHelper.Abort();
			}

			public void Fault(Microsoft.ServiceBus.Channels.RequestChannel requestChannel)
			{
				this.Cleanup();
			}

			public void SendRequest(Message message, TimeSpan timeout)
			{
				Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				try
				{
					this.connection = this.connectionPoolHelper.EstablishConnection(timeoutHelper.RemainingTime());
					MessagingClientEtwProvider.Provider.RelayChannelConnectionTransfer(this.Activity, this.connection.Activity);
					bool flag = false;
					try
					{
						Microsoft.ServiceBus.Channels.StreamingConnectionHelper.WriteMessage(message, this.connection, true, this.channel.settings, ref timeoutHelper);
						flag = true;
					}
					finally
					{
						if (!flag)
						{
							this.connectionPoolHelper.Abort();
						}
					}
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string timeoutOnRequest = Resources.TimeoutOnRequest;
					object[] objArray = new object[] { timeout };
					throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(timeoutOnRequest, objArray), timeoutException));
				}
			}

			public Message WaitForReply(TimeSpan timeout)
			{
				Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.ClientSingletonConnectionReader clientSingletonConnectionReader = new Microsoft.ServiceBus.Channels.StreamedFramingRequestChannel.ClientSingletonConnectionReader(this.connection, this.connectionPoolHelper, this.channel.settings);
				clientSingletonConnectionReader.DoneSending(TimeSpan.Zero);
				return clientSingletonConnectionReader.Receive(timeout);
			}
		}
	}
}