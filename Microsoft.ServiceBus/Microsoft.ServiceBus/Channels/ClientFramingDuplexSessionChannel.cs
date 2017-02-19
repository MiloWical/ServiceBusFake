using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class ClientFramingDuplexSessionChannel : Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel
	{
		private Microsoft.ServiceBus.Channels.IConnectionOrientedTransportChannelFactorySettings settings;

		private Microsoft.ServiceBus.Channels.ClientDuplexDecoder decoder;

		private StreamUpgradeProvider upgrade;

		private Microsoft.ServiceBus.Channels.ConnectionPoolHelper connectionPoolHelper;

		public ClientFramingDuplexSessionChannel(ChannelManagerBase factory, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportChannelFactorySettings settings, EndpointAddress remoteAddresss, Uri via, Microsoft.ServiceBus.Channels.IConnectionInitiator connectionInitiator, Microsoft.ServiceBus.Channels.ConnectionPool connectionPool, bool exposeConnectionProperty) : base(factory, settings, remoteAddresss, via, exposeConnectionProperty)
		{
			this.settings = settings;
			base.MessageEncoder = settings.MessageEncoderFactory.CreateSessionEncoder();
			this.upgrade = settings.Upgrade;
			this.connectionPoolHelper = new Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.DuplexConnectionPoolHelper(this, connectionPool, connectionInitiator);
		}

		private void AcceptConnection(Microsoft.ServiceBus.Channels.IConnection connection)
		{
			MessagingClientEtwProvider.Provider.RelayChannelConnectionTransfer(base.Activity, connection.Activity);
			base.SetMessageSource(new Microsoft.ServiceBus.Channels.ClientDuplexConnectionReader(this, connection, this.decoder, this.settings, base.MessageEncoder));
			lock (base.ThisLock)
			{
				if (base.State != CommunicationState.Opening)
				{
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string duplexChannelAbortedDuringOpen = Resources.DuplexChannelAbortedDuringOpen;
					object[] via = new object[] { this.Via };
					throw exceptionUtility.ThrowHelperError(new CommunicationObjectAbortedException(Microsoft.ServiceBus.SR.GetString(duplexChannelAbortedDuringOpen, via)));
				}
				base.Connection = connection;
			}
		}

		private IAsyncResult BeginSendPreamble(Microsoft.ServiceBus.Channels.IConnection connection, ArraySegment<byte> preamble, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult(this, connection, preamble, ref timeoutHelper, callback, state);
		}

		private ArraySegment<byte> CreatePreamble()
		{
			Microsoft.ServiceBus.Channels.EncodedVia encodedVium = new Microsoft.ServiceBus.Channels.EncodedVia(this.Via.AbsoluteUri);
			Microsoft.ServiceBus.Channels.EncodedContentType encodedContentType = Microsoft.ServiceBus.Channels.EncodedContentType.Create(base.MessageEncoder.ContentType);
			int length = (int)Microsoft.ServiceBus.Channels.ClientDuplexEncoder.ModeBytes.Length + Microsoft.ServiceBus.Channels.SessionEncoder.CalcStartSize(encodedVium, encodedContentType);
			int num = 0;
			if (this.upgrade == null)
			{
				num = length;
				length = length + (int)Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes.Length;
			}
			byte[] numArray = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.Utility.AllocateByteArray(length);
			Buffer.BlockCopy(Microsoft.ServiceBus.Channels.ClientDuplexEncoder.ModeBytes, 0, numArray, 0, (int)Microsoft.ServiceBus.Channels.ClientDuplexEncoder.ModeBytes.Length);
			Microsoft.ServiceBus.Channels.SessionEncoder.EncodeStart(numArray, (int)Microsoft.ServiceBus.Channels.ClientDuplexEncoder.ModeBytes.Length, encodedVium, encodedContentType);
			if (num > 0)
			{
				Buffer.BlockCopy(Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes, 0, numArray, num, (int)Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes.Length);
			}
			return new ArraySegment<byte>(numArray, 0, length);
		}

		private static Microsoft.ServiceBus.Channels.IConnection EndSendPreamble(IAsyncResult result)
		{
			return Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.End(result);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult(this, timeout, callback, state);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult.End(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.IConnection connection;
			try
			{
				connection = this.connectionPoolHelper.EstablishConnection(timeout);
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				ExceptionTrace exception = Fx.Exception;
				string timeoutOnOpen = Resources.TimeoutOnOpen;
				object[] objArray = new object[] { timeout };
				throw exception.AsError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(timeoutOnOpen, objArray), timeoutException), base.Activity);
			}
			bool flag = false;
			try
			{
				this.AcceptConnection(connection);
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

		protected override void PrepareMessage(Message message)
		{
			base.PrepareMessage(message);
			if (base.RemoteSecurity != null)
			{
				message.Properties.Security = (SecurityMessageProperty)base.RemoteSecurity.CreateCopy();
			}
		}

		protected override void ReturnConnectionIfNecessary(bool abort, TimeSpan timeout)
		{
			lock (base.ThisLock)
			{
				if (!abort)
				{
					this.connectionPoolHelper.Close(timeout);
				}
				else
				{
					this.connectionPoolHelper.Abort();
				}
			}
		}

		private Microsoft.ServiceBus.Channels.IConnection SendPreamble(Microsoft.ServiceBus.Channels.IConnection connection, ArraySegment<byte> preamble, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
		{
			this.decoder = new Microsoft.ServiceBus.Channels.ClientDuplexDecoder((long)0);
			byte[] numArray = new byte[1];
			connection.Write(preamble.Array, preamble.Offset, preamble.Count, true, timeoutHelper.RemainingTime());
			if (this.upgrade != null)
			{
				StreamUpgradeInitiator streamUpgradeInitiator = this.upgrade.CreateUpgradeInitiator(this.RemoteAddress, this.Via);
				Type type = streamUpgradeInitiator.GetType();
				object[] objArray = new object[] { timeoutHelper.RemainingTime() };
				InvokeHelper.InvokeInstanceMethod(type, streamUpgradeInitiator, "Open", objArray);
				if (!Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.InitiateUpgrade(streamUpgradeInitiator, ref connection, this.decoder, this, ref timeoutHelper))
				{
					Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFramingFault(this.decoder, connection, this.Via, base.MessageEncoder.ContentType, ref timeoutHelper);
				}
				this.SetRemoteSecurity(streamUpgradeInitiator);
				Type type1 = streamUpgradeInitiator.GetType();
				object[] objArray1 = new object[] { timeoutHelper.RemainingTime() };
				InvokeHelper.InvokeInstanceMethod(type1, streamUpgradeInitiator, "Close", objArray1);
				connection.Write(Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes, 0, (int)Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes.Length, true, timeoutHelper.RemainingTime());
			}
			if (!Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidatePreambleResponse(numArray, connection.Read(numArray, 0, (int)numArray.Length, timeoutHelper.RemainingTime()), this.decoder, this.Via))
			{
				Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.DecodeFramingFault(this.decoder, connection, this.Via, base.MessageEncoder.ContentType, ref timeoutHelper);
			}
			return connection;
		}

		private void SetRemoteSecurity(StreamUpgradeInitiator upgradeInitiator)
		{
			if (upgradeInitiator is StreamSecurityUpgradeInitiator)
			{
				base.RemoteSecurity = ((StreamSecurityUpgradeInitiator)upgradeInitiator).GetRemoteSecurity();
			}
		}

		private class DuplexConnectionPoolHelper : Microsoft.ServiceBus.Channels.ConnectionPoolHelper
		{
			private Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel channel;

			private ArraySegment<byte> preamble;

			public DuplexConnectionPoolHelper(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel channel, Microsoft.ServiceBus.Channels.ConnectionPool connectionPool, Microsoft.ServiceBus.Channels.IConnectionInitiator connectionInitiator) : base(connectionPool, connectionInitiator, channel.Via)
			{
				this.channel = channel;
				this.preamble = channel.CreatePreamble();
			}

			protected override Microsoft.ServiceBus.Channels.IConnection AcceptPooledConnection(Microsoft.ServiceBus.Channels.IConnection connection, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
			{
				return this.channel.SendPreamble(connection, this.preamble, ref timeoutHelper);
			}

			protected override IAsyncResult BeginAcceptPooledConnection(Microsoft.ServiceBus.Channels.IConnection connection, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state)
			{
				return this.channel.BeginSendPreamble(connection, this.preamble, ref timeoutHelper, callback, state);
			}

			protected override TimeoutException CreateNewConnectionTimeoutException(TimeSpan timeout, TimeoutException innerException)
			{
				string openTimedOutEstablishingTransportSession = Resources.OpenTimedOutEstablishingTransportSession;
				object[] objArray = new object[] { timeout, this.channel.Via.AbsoluteUri };
				return new TimeoutException(Microsoft.ServiceBus.SR.GetString(openTimedOutEstablishingTransportSession, objArray), innerException);
			}

			protected override Microsoft.ServiceBus.Channels.IConnection EndAcceptPooledConnection(IAsyncResult result)
			{
				return Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.EndSendPreamble(result);
			}
		}

		private sealed class OpenAsyncResult : AsyncResult
		{
			private readonly static AsyncCallback onEstablishConnection;

			private readonly Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel duplexChannel;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.duplexChannel.Activity;
				}
			}

			protected override TraceEventType TraceEventType
			{
				get
				{
					return TraceEventType.Warning;
				}
			}

			static OpenAsyncResult()
			{
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult.onEstablishConnection = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult.OnEstablishConnection));
			}

			public OpenAsyncResult(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel duplexChannel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				IAsyncResult asyncResult;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				this.duplexChannel = duplexChannel;
				try
				{
					asyncResult = duplexChannel.connectionPoolHelper.BeginEstablishConnection(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult.onEstablishConnection, this);
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string timeoutOnOpen = Resources.TimeoutOnOpen;
					object[] objArray = new object[] { timeout };
					throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(timeoutOnOpen, objArray), timeoutException));
				}
				if (!asyncResult.CompletedSynchronously)
				{
					return;
				}
				if (this.HandleEstablishConnection(asyncResult))
				{
					base.Complete(true);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult>(result);
			}

			private bool HandleEstablishConnection(IAsyncResult result)
			{
				Microsoft.ServiceBus.Channels.IConnection connection;
				try
				{
					connection = Microsoft.ServiceBus.Channels.ConnectionPoolHelper.EndEstablishConnection(result);
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string timeoutOnOpen = Resources.TimeoutOnOpen;
					object[] originalTimeout = new object[] { this.timeoutHelper.OriginalTimeout };
					throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(timeoutOnOpen, originalTimeout), timeoutException));
				}
				this.duplexChannel.AcceptConnection(connection);
				return true;
			}

			private static void OnEstablishConnection(IAsyncResult result)
			{
				bool flag;
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.OpenAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					flag = asyncState.HandleEstablishConnection(result);
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
		}

		private class SendPreambleAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel channel;

			private Microsoft.ServiceBus.Channels.IConnection connection;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			private StreamUpgradeInitiator upgradeInitiator;

			private static WaitCallback onReadPreambleAck;

			private static AsyncCallback onWritePreamble;

			private static AsyncCallback onWritePreambleEnd;

			private static AsyncCallback onUpgrade;

			private static AsyncCallback onUpgradeInitiatorOpen;

			private static AsyncCallback onUpgradeInitiatorClose;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.channel.Activity;
				}
			}

			protected override TraceEventType TraceEventType
			{
				get
				{
					return TraceEventType.Warning;
				}
			}

			static SendPreambleAsyncResult()
			{
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onReadPreambleAck = new WaitCallback(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.OnReadPreambleAck);
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onWritePreamble = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.OnWritePreamble));
			}

			public SendPreambleAsyncResult(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel channel, Microsoft.ServiceBus.Channels.IConnection connection, ArraySegment<byte> preamble, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper, AsyncCallback callback, object state) : base(callback, state)
			{
				this.channel = channel;
				this.timeoutHelper = timeoutHelper;
				this.connection = connection;
				channel.decoder = new Microsoft.ServiceBus.Channels.ClientDuplexDecoder((long)0);
				IAsyncResult asyncResult = connection.BeginWrite(preamble.Array, preamble.Offset, preamble.Count, true, timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onWritePreamble, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return;
				}
				if (this.HandleWritePreamble(asyncResult))
				{
					base.Complete(true);
				}
			}

			public static new Microsoft.ServiceBus.Channels.IConnection End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult>(result).connection;
			}

			private bool HandleInitiatorClose(IAsyncResult result)
			{
				Type type = this.upgradeInitiator.GetType();
				StreamUpgradeInitiator streamUpgradeInitiator = this.upgradeInitiator;
				object[] objArray = new object[] { result };
				InvokeHelper.InvokeInstanceMethod(type, streamUpgradeInitiator, "EndClose", objArray);
				this.upgradeInitiator = null;
				if (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onWritePreambleEnd == null)
				{
					Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onWritePreambleEnd = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.OnWritePreambleEnd));
				}
				IAsyncResult asyncResult = this.connection.BeginWrite(Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes, 0, (int)Microsoft.ServiceBus.Channels.SessionEncoder.PreambleEndBytes.Length, true, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onWritePreambleEnd, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				this.connection.EndWrite(asyncResult);
				return this.ReadAck();
			}

			private bool HandleInitiatorOpen(IAsyncResult result)
			{
				Type type = this.upgradeInitiator.GetType();
				StreamUpgradeInitiator streamUpgradeInitiator = this.upgradeInitiator;
				object[] objArray = new object[] { result };
				InvokeHelper.InvokeInstanceMethod(type, streamUpgradeInitiator, "EndOpen", objArray);
				if (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgrade == null)
				{
					Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgrade = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.OnUpgrade));
				}
				IAsyncResult asyncResult = Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.BeginInitiateUpgrade(this.channel, this.channel.RemoteAddress, this.connection, this.channel.decoder, this.upgradeInitiator, this.channel.MessageEncoder.ContentType, this.timeoutHelper, Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgrade, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleUpgrade(asyncResult);
			}

			private bool HandlePreambleAck()
			{
				int num = this.connection.EndRead();
				if (Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.ValidatePreambleResponse(this.connection.AsyncReadBuffer, num, this.channel.decoder, this.channel.Via))
				{
					return true;
				}
				IAsyncResult asyncResult = Microsoft.ServiceBus.Channels.ConnectionUpgradeHelper.BeginDecodeFramingFault(this.channel.decoder, this.connection, this.channel.Via, this.channel.MessageEncoder.ContentType, ref this.timeoutHelper, Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(this.OnFailedPreamble)), this);
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
				this.channel.SetRemoteSecurity(this.upgradeInitiator);
				if (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgradeInitiatorClose == null)
				{
					Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgradeInitiatorClose = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.OnUpgradeInitiatorClose));
				}
				Type type = this.upgradeInitiator.GetType();
				StreamUpgradeInitiator streamUpgradeInitiator = this.upgradeInitiator;
				object[] objArray = new object[] { this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgradeInitiatorClose, this };
				IAsyncResult asyncResult = InvokeHelper.InvokeInstanceMethod(type, streamUpgradeInitiator, "BeginClose", objArray) as IAsyncResult;
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleInitiatorClose(asyncResult);
			}

			private bool HandleWritePreamble(IAsyncResult result)
			{
				this.connection.EndWrite(result);
				if (this.channel.upgrade == null)
				{
					return this.ReadAck();
				}
				this.upgradeInitiator = this.channel.upgrade.CreateUpgradeInitiator(this.channel.RemoteAddress, this.channel.Via);
				if (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgradeInitiatorOpen == null)
				{
					Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgradeInitiatorOpen = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.OnUpgradeInitiatorOpen));
				}
				Type type = this.upgradeInitiator.GetType();
				StreamUpgradeInitiator streamUpgradeInitiator = this.upgradeInitiator;
				object[] objArray = new object[] { this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onUpgradeInitiatorOpen, this };
				IAsyncResult asyncResult = InvokeHelper.InvokeInstanceMethod(type, streamUpgradeInitiator, "BeginOpen", objArray) as IAsyncResult;
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleInitiatorOpen(asyncResult);
			}

			private void OnFailedPreamble(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
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
				base.Complete(false, exception);
			}

			private static void OnReadPreambleAck(object state)
			{
				bool flag;
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult sendPreambleAsyncResult = (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult)state;
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
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult)result.AsyncState;
				bool flag = false;
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

			private static void OnUpgradeInitiatorClose(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult)result.AsyncState;
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = asyncState.HandleInitiatorClose(result);
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

			private static void OnUpgradeInitiatorOpen(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult)result.AsyncState;
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = asyncState.HandleInitiatorOpen(result);
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
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult)result.AsyncState;
				Exception exception = null;
				bool flag = false;
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
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult)result.AsyncState;
				Exception exception = null;
				bool flag = false;
				try
				{
					asyncState.connection.EndWrite(result);
					flag = asyncState.ReadAck();
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

			private bool ReadAck()
			{
				if (this.connection.BeginRead(0, 1, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ClientFramingDuplexSessionChannel.SendPreambleAsyncResult.onReadPreambleAck, this) == AsyncReadResult.Queued)
				{
					return false;
				}
				return this.HandlePreambleAck();
			}
		}
	}
}