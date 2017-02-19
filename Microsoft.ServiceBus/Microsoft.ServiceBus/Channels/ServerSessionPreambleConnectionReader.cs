using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Xml;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerSessionPreambleConnectionReader : Microsoft.ServiceBus.Channels.InitialServerConnectionReader
	{
		private Microsoft.ServiceBus.Channels.ServerSessionDecoder decoder;

		private byte[] connectionBuffer;

		private int offset;

		private int size;

		private Microsoft.ServiceBus.Channels.TransportSettingsCallback transportSettingsCallback;

		private Microsoft.ServiceBus.Channels.ServerSessionPreambleCallback callback;

		private static WaitCallback readCallback;

		private Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings;

		private Uri via;

		private OnViaDelegate viaDelegate;

		private Microsoft.ServiceBus.Common.TimeoutHelper receiveTimeoutHelper;

		private Microsoft.ServiceBus.Channels.IConnection rawConnection;

		public int BufferOffset
		{
			get
			{
				return this.offset;
			}
		}

		public int BufferSize
		{
			get
			{
				return this.size;
			}
		}

		public Microsoft.ServiceBus.Channels.ServerSessionDecoder Decoder
		{
			get
			{
				return this.decoder;
			}
		}

		public Microsoft.ServiceBus.Channels.IConnection RawConnection
		{
			get
			{
				return this.rawConnection;
			}
		}

		public Uri Via
		{
			get
			{
				return this.via;
			}
		}

		public ServerSessionPreambleConnectionReader(Microsoft.ServiceBus.Channels.IConnection connection, Action connectionDequeuedCallback, long streamPosition, int offset, int size, Microsoft.ServiceBus.Channels.TransportSettingsCallback transportSettingsCallback, Microsoft.ServiceBus.Channels.ConnectionClosedCallback closedCallback, Microsoft.ServiceBus.Channels.ServerSessionPreambleCallback callback) : base(connection, closedCallback)
		{
			this.rawConnection = connection;
			this.decoder = new Microsoft.ServiceBus.Channels.ServerSessionDecoder(streamPosition, base.MaxViaSize, base.MaxContentTypeSize);
			this.offset = offset;
			this.size = size;
			this.transportSettingsCallback = transportSettingsCallback;
			this.callback = callback;
			base.ConnectionDequeuedCallback = connectionDequeuedCallback;
		}

		private void ContinueReading()
		{
			bool flag = false;
			try
			{
				try
				{
					while (true)
					{
						if (this.size == 0)
						{
							if (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.readCallback == null)
							{
								Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.readCallback = new WaitCallback(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ReadCallback);
							}
							if (base.Connection.BeginRead(0, (int)this.connectionBuffer.Length, this.GetRemainingTimeout(), Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.readCallback, this) == AsyncReadResult.Queued)
							{
								break;
							}
							this.GetReadResult();
						}
						int num = this.decoder.Decode(this.connectionBuffer, this.offset, this.size);
						if (num > 0)
						{
							Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader serverSessionPreambleConnectionReader = this;
							serverSessionPreambleConnectionReader.offset = serverSessionPreambleConnectionReader.offset + num;
							Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader serverSessionPreambleConnectionReader1 = this;
							serverSessionPreambleConnectionReader1.size = serverSessionPreambleConnectionReader1.size - num;
						}
						if (this.decoder.CurrentState == Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.PreUpgradeStart)
						{
							this.via = this.decoder.Via;
							if (base.Connection.Validate(this.via))
							{
								if (this.viaDelegate != null)
								{
									try
									{
										this.viaDelegate(this.via);
									}
									catch (ServiceActivationException serviceActivationException1)
									{
										ServiceActivationException serviceActivationException = serviceActivationException1;
										if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
										{
											Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(serviceActivationException, TraceEventType.Information);
										}
										this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/ServiceActivationFailed");
										break;
									}
								}
								this.settings = this.transportSettingsCallback(this.via);
								if (this.settings != null)
								{
									this.callback(this);
									break;
								}
								else
								{
									string endpointNotFound = Resources.EndpointNotFound;
									object[] via = new object[] { this.decoder.Via };
									EndpointNotFoundException endpointNotFoundException = new EndpointNotFoundException(Microsoft.ServiceBus.SR.GetString(endpointNotFound, via));
									if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
									{
										Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(endpointNotFoundException, TraceEventType.Information);
									}
									this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/EndpointNotFound");
									return;
								}
							}
							else
							{
								return;
							}
						}
					}
					flag = true;
				}
				catch (CommunicationException communicationException1)
				{
					CommunicationException communicationException = communicationException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, TraceEventType.Information);
					}
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					if (!ExceptionHandler.HandleTransportExceptionHelper(exception))
					{
						throw;
					}
				}
			}
			finally
			{
				if (!flag)
				{
					base.Abort();
				}
			}
		}

		public IDuplexSessionChannel CreateDuplexSessionChannel(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener channelListener, EndpointAddress localAddress, bool exposeConnectionProperty, Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer)
		{
			return new Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel(channelListener, this, localAddress, exposeConnectionProperty, connectionDemuxer);
		}

		private void GetReadResult()
		{
			this.offset = 0;
			this.size = base.Connection.EndRead();
			if (this.size == 0)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
			}
		}

		private TimeSpan GetRemainingTimeout()
		{
			return this.receiveTimeoutHelper.RemainingTime();
		}

		private static void ReadCallback(object state)
		{
			Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader serverSessionPreambleConnectionReader = (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader)state;
			bool flag = false;
			try
			{
				try
				{
					serverSessionPreambleConnectionReader.GetReadResult();
					serverSessionPreambleConnectionReader.ContinueReading();
					flag = true;
				}
				catch (CommunicationException communicationException1)
				{
					CommunicationException communicationException = communicationException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(communicationException, TraceEventType.Information);
					}
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					if (!ExceptionHandler.HandleTransportExceptionHelper(exception))
					{
						throw;
					}
				}
			}
			finally
			{
				if (!flag)
				{
					serverSessionPreambleConnectionReader.Abort();
				}
			}
		}

		public void SendFault(string faultString)
		{
			Microsoft.ServiceBus.Channels.InitialServerConnectionReader.SendFault(base.Connection, faultString, this.connectionBuffer, this.GetRemainingTimeout(), 65536);
			base.Close(this.GetRemainingTimeout());
		}

		public void StartReading(OnViaDelegate viaDelegate, TimeSpan receiveTimeout)
		{
			this.viaDelegate = viaDelegate;
			this.receiveTimeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(receiveTimeout);
			this.connectionBuffer = base.Connection.AsyncReadBuffer;
			this.ContinueReading();
		}

		private class ServerFramingDuplexSessionChannel : Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel
		{
			private Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener channelListener;

			private Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer;

			private Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.ServerSessionConnectionReader sessionReader;

			private Microsoft.ServiceBus.Channels.ServerSessionDecoder decoder;

			private Microsoft.ServiceBus.Channels.IConnection rawConnection;

			private byte[] connectionBuffer;

			private int offset;

			private int size;

			private StreamUpgradeAcceptor upgradeAcceptor;

			public ServerFramingDuplexSessionChannel(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener channelListener, Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader preambleReader, EndpointAddress localAddress, bool exposeConnectionProperty, Microsoft.ServiceBus.Channels.ConnectionDemuxer connectionDemuxer) : base(channelListener, localAddress, preambleReader.Via, exposeConnectionProperty)
			{
				this.channelListener = channelListener;
				this.connectionDemuxer = connectionDemuxer;
				base.Connection = preambleReader.Connection;
				this.decoder = preambleReader.Decoder;
				this.connectionBuffer = preambleReader.connectionBuffer;
				this.offset = preambleReader.BufferOffset;
				this.size = preambleReader.BufferSize;
				this.rawConnection = preambleReader.RawConnection;
				StreamUpgradeProvider upgrade = channelListener.Upgrade;
				if (upgrade != null)
				{
					this.upgradeAcceptor = upgrade.CreateUpgradeAcceptor();
				}
				MessagingClientEtwProvider.Provider.RelayChannelConnectionTransfer(base.Activity, base.Connection.Activity);
			}

			private void AcceptUpgradedConnection(Microsoft.ServiceBus.Channels.IConnection upgradedConnection)
			{
				base.Connection = upgradedConnection;
				this.connectionBuffer = base.Connection.AsyncReadBuffer;
			}

			private void DecodeBytes()
			{
				int num = this.decoder.Decode(this.connectionBuffer, this.offset, this.size);
				if (num > 0)
				{
					Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel serverFramingDuplexSessionChannel = this;
					serverFramingDuplexSessionChannel.offset = serverFramingDuplexSessionChannel.offset + num;
					Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel serverFramingDuplexSessionChannel1 = this;
					serverFramingDuplexSessionChannel1.size = serverFramingDuplexSessionChannel1.size - num;
				}
			}

			protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult(this, timeout, callback, state);
			}

			protected override void OnEndOpen(IAsyncResult result)
			{
				Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.End(result);
			}

			protected override void OnOpen(TimeSpan timeout)
			{
				bool flag = false;
				try
				{
					Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
					this.ValidateContentType(ref timeoutHelper);
					while (true)
					{
						if (this.size == 0)
						{
							this.offset = 0;
							this.size = base.Connection.Read(this.connectionBuffer, 0, (int)this.connectionBuffer.Length, timeoutHelper.RemainingTime());
							if (this.size == 0)
							{
								break;
							}
						}
						do
						{
							this.DecodeBytes();
							switch (this.decoder.CurrentState)
							{
								case Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.UpgradeRequest:
								{
									this.ProcessUpgradeRequest(ref timeoutHelper);
									base.Connection.Write(Microsoft.ServiceBus.Channels.ServerSessionEncoder.UpgradeResponseBytes, 0, (int)Microsoft.ServiceBus.Channels.ServerSessionEncoder.UpgradeResponseBytes.Length, true, timeoutHelper.RemainingTime());
									Microsoft.ServiceBus.Channels.IConnection connection = base.Connection;
									if (this.size > 0)
									{
										connection = new Microsoft.ServiceBus.Channels.PreReadConnection(connection, this.connectionBuffer, this.offset, this.size);
									}
									try
									{
										base.Connection = Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnection(connection, this.upgradeAcceptor, this);
										this.connectionBuffer = base.Connection.AsyncReadBuffer;
										continue;
									}
									catch (Exception exception1)
									{
										Exception exception = exception1;
										if (Fx.IsFatal(exception))
										{
											throw;
										}
										this.WriteAuditFailure(this.upgradeAcceptor as StreamSecurityUpgradeAcceptor, exception);
										throw;
									}
									break;
								}
								case Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.Start:
								{
									this.SetupSecurityIfNecessary();
									base.Connection.Write(Microsoft.ServiceBus.Channels.ServerSessionEncoder.AckResponseBytes, 0, (int)Microsoft.ServiceBus.Channels.ServerSessionEncoder.AckResponseBytes.Length, true, timeoutHelper.RemainingTime());
									this.SetupSessionReader();
									flag = true;
									return;
								}
								default:
								{
									continue;
								}
							}
						}
						while (this.size != 0);
					}
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
				}
				finally
				{
					if (!flag)
					{
						base.Connection.Abort();
					}
				}
			}

			protected override void PrepareMessage(Message message)
			{
				this.channelListener.RaiseMessageReceived();
				base.PrepareMessage(message);
			}

			private void ProcessUpgradeRequest(ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
			{
				if (this.upgradeAcceptor == null)
				{
					this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/UpgradeInvalid", ref timeoutHelper);
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string upgradeRequestToNonupgradableService = Resources.UpgradeRequestToNonupgradableService;
					object[] upgrade = new object[] { this.decoder.Upgrade };
					throw exceptionUtility.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(upgradeRequestToNonupgradableService, upgrade)));
				}
				if (!this.upgradeAcceptor.CanUpgrade(this.decoder.Upgrade))
				{
					this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/UpgradeInvalid", ref timeoutHelper);
					ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string upgradeProtocolNotSupported = Resources.UpgradeProtocolNotSupported;
					object[] objArray = new object[] { this.decoder.Upgrade };
					throw exceptionUtility1.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(upgradeProtocolNotSupported, objArray)));
				}
			}

			protected override void ReturnConnectionIfNecessary(bool abort, TimeSpan timeout)
			{
				Microsoft.ServiceBus.Channels.IConnection rawConnection = null;
				if (this.sessionReader != null)
				{
					lock (base.ThisLock)
					{
						rawConnection = this.sessionReader.GetRawConnection();
					}
				}
				if (rawConnection != null)
				{
					if (!abort)
					{
						this.connectionDemuxer.ReuseConnection(rawConnection, timeout);
					}
					else
					{
						rawConnection.Abort();
					}
					this.connectionDemuxer = null;
				}
			}

			private void SendFault(string faultString, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
			{
				Microsoft.ServiceBus.Channels.InitialServerConnectionReader.SendFault(base.Connection, faultString, this.connectionBuffer, timeoutHelper.RemainingTime(), 65536);
			}

			private void SetupSecurityIfNecessary()
			{
				StreamSecurityUpgradeAcceptor streamSecurityUpgradeAcceptor = this.upgradeAcceptor as StreamSecurityUpgradeAcceptor;
				if (streamSecurityUpgradeAcceptor != null)
				{
					base.RemoteSecurity = streamSecurityUpgradeAcceptor.GetRemoteSecurity();
					if (base.RemoteSecurity == null)
					{
						string remoteSecurityNotNegotiatedOnStreamUpgrade = Resources.RemoteSecurityNotNegotiatedOnStreamUpgrade;
						object[] via = new object[] { this.Via };
						Exception protocolException = new ProtocolException(Microsoft.ServiceBus.SR.GetString(remoteSecurityNotNegotiatedOnStreamUpgrade, via));
						this.WriteAuditFailure(streamSecurityUpgradeAcceptor, protocolException);
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(protocolException);
					}
					this.WriteAuditEvent(streamSecurityUpgradeAcceptor, AuditLevel.Success, null);
				}
			}

			private void SetupSessionReader()
			{
				this.sessionReader = new Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.ServerSessionConnectionReader(this);
				base.SetMessageSource(this.sessionReader);
			}

			private void ValidateContentType(ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
			{
				base.MessageEncoder = this.channelListener.MessageEncoderFactory.CreateSessionEncoder();
				if (!base.MessageEncoder.IsContentTypeSupported(this.decoder.ContentType))
				{
					this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/ContentTypeInvalid", ref timeoutHelper);
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string contentTypeMismatch = Resources.ContentTypeMismatch;
					object[] contentType = new object[] { this.decoder.ContentType, base.MessageEncoder.ContentType };
					throw exceptionUtility.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(contentTypeMismatch, contentType)));
				}
			}

			private void WriteAuditEvent(StreamSecurityUpgradeAcceptor securityUpgradeAcceptor, AuditLevel auditLevel, Exception exception)
			{
			}

			private void WriteAuditFailure(StreamSecurityUpgradeAcceptor securityUpgradeAcceptor, Exception exception)
			{
				try
				{
					this.WriteAuditEvent(securityUpgradeAcceptor, AuditLevel.Failure, exception);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(exception1, TraceEventType.Error);
				}
			}

			private class OpenAsyncResult : AsyncResult
			{
				private Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel channel;

				private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

				private static WaitCallback readCallback;

				private static AsyncCallback onWriteAckResponse;

				private static AsyncCallback onWriteUpgradeResponse;

				private static AsyncCallback onUpgradeConnection;

				public OpenAsyncResult(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
				{
					this.channel = channel;
					this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
					bool flag = false;
					bool flag1 = false;
					try
					{
						channel.ValidateContentType(ref this.timeoutHelper);
						flag = this.ContinueReading();
						flag1 = true;
					}
					finally
					{
						if (!flag1)
						{
							this.CleanupOnError();
						}
					}
					if (flag)
					{
						base.Complete(true);
					}
				}

				private void CleanupOnError()
				{
					this.channel.Connection.Abort();
				}

				private bool ContinueReading()
				{
					while (true)
					{
						if (this.channel.size == 0)
						{
							if (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.readCallback == null)
							{
								Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.readCallback = new WaitCallback(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.ReadCallback);
							}
							if (this.channel.Connection.BeginRead(0, (int)this.channel.connectionBuffer.Length, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.readCallback, this) == AsyncReadResult.Queued)
							{
								break;
							}
							this.GetReadResult();
						}
						do
						{
							this.channel.DecodeBytes();
							switch (this.channel.decoder.CurrentState)
							{
								case Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.UpgradeRequest:
								{
									this.channel.ProcessUpgradeRequest(ref this.timeoutHelper);
									if (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onWriteUpgradeResponse == null)
									{
										Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onWriteUpgradeResponse = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.OnWriteUpgradeResponse));
									}
									IAsyncResult asyncResult = this.channel.Connection.BeginWrite(Microsoft.ServiceBus.Channels.ServerSessionEncoder.UpgradeResponseBytes, 0, (int)Microsoft.ServiceBus.Channels.ServerSessionEncoder.UpgradeResponseBytes.Length, true, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onWriteUpgradeResponse, this);
									if (!asyncResult.CompletedSynchronously)
									{
										return false;
									}
									if (this.HandleWriteUpgradeResponseComplete(asyncResult))
									{
										continue;
									}
									return false;
								}
								case Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.Start:
								{
									this.channel.SetupSecurityIfNecessary();
									if (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onWriteAckResponse == null)
									{
										Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onWriteAckResponse = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.OnWriteAckResponse));
									}
									IAsyncResult asyncResult1 = this.channel.Connection.BeginWrite(Microsoft.ServiceBus.Channels.ServerSessionEncoder.AckResponseBytes, 0, (int)Microsoft.ServiceBus.Channels.ServerSessionEncoder.AckResponseBytes.Length, true, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onWriteAckResponse, this);
									if (!asyncResult1.CompletedSynchronously)
									{
										return false;
									}
									return this.HandleWriteAckComplete(asyncResult1);
								}
								default:
								{
									continue;
								}
							}
						}
						while (this.channel.size != 0);
					}
					return false;
				}

				public static new void End(IAsyncResult result)
				{
					AsyncResult.End<Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult>(result);
				}

				private void GetReadResult()
				{
					this.channel.offset = 0;
					this.channel.size = this.channel.Connection.EndRead();
					if (this.channel.size == 0)
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.channel.decoder.CreatePrematureEOFException());
					}
				}

				private bool HandleUpgradeConnectionComplete(IAsyncResult result)
				{
					this.channel.AcceptUpgradedConnection(Microsoft.ServiceBus.Channels.InitialServerConnectionReader.EndUpgradeConnection(result));
					return true;
				}

				private bool HandleWriteAckComplete(IAsyncResult result)
				{
					this.channel.Connection.EndWrite(result);
					this.channel.SetupSessionReader();
					return true;
				}

				private bool HandleWriteUpgradeResponseComplete(IAsyncResult result)
				{
					bool flag;
					this.channel.Connection.EndWrite(result);
					Microsoft.ServiceBus.Channels.IConnection connection = this.channel.Connection;
					if (this.channel.size > 0)
					{
						connection = new Microsoft.ServiceBus.Channels.PreReadConnection(connection, this.channel.connectionBuffer, this.channel.offset, this.channel.size);
					}
					if (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onUpgradeConnection == null)
					{
						Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onUpgradeConnection = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.OnUpgradeConnection));
					}
					try
					{
						IAsyncResult asyncResult = Microsoft.ServiceBus.Channels.InitialServerConnectionReader.BeginUpgradeConnection(connection, this.channel.upgradeAcceptor, this.channel, Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult.onUpgradeConnection, this);
						flag = (asyncResult.CompletedSynchronously ? this.HandleUpgradeConnectionComplete(asyncResult) : false);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.channel.WriteAuditFailure(this.channel.upgradeAcceptor as StreamSecurityUpgradeAcceptor, exception);
						throw;
					}
					return flag;
				}

				private static void OnUpgradeConnection(IAsyncResult result)
				{
					if (result.CompletedSynchronously)
					{
						return;
					}
					Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult)result.AsyncState;
					bool flag = false;
					Exception exception = null;
					try
					{
						flag = asyncState.HandleUpgradeConnectionComplete(result);
						if (flag)
						{
							flag = asyncState.ContinueReading();
						}
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
						flag = true;
						asyncState.CleanupOnError();
						asyncState.channel.WriteAuditFailure(asyncState.channel.upgradeAcceptor as StreamSecurityUpgradeAcceptor, exception1);
					}
					if (flag)
					{
						asyncState.Complete(false, exception);
					}
				}

				private static void OnWriteAckResponse(IAsyncResult result)
				{
					if (result.CompletedSynchronously)
					{
						return;
					}
					Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult)result.AsyncState;
					bool flag = false;
					Exception exception = null;
					try
					{
						flag = asyncState.HandleWriteAckComplete(result);
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
						flag = true;
						asyncState.CleanupOnError();
					}
					if (flag)
					{
						asyncState.Complete(false, exception);
					}
				}

				private static void OnWriteUpgradeResponse(IAsyncResult result)
				{
					if (result.CompletedSynchronously)
					{
						return;
					}
					Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult asyncState = (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult)result.AsyncState;
					bool flag = false;
					Exception exception = null;
					try
					{
						flag = asyncState.HandleWriteUpgradeResponseComplete(result);
						if (flag)
						{
							flag = asyncState.ContinueReading();
						}
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						exception = exception1;
						flag = true;
						asyncState.CleanupOnError();
						asyncState.channel.WriteAuditFailure(asyncState.channel.upgradeAcceptor as StreamSecurityUpgradeAcceptor, exception1);
					}
					if (flag)
					{
						asyncState.Complete(false, exception);
					}
				}

				private static void ReadCallback(object state)
				{
					Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult openAsyncResult = (Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.OpenAsyncResult)state;
					bool flag = false;
					Exception exception = null;
					try
					{
						openAsyncResult.GetReadResult();
						flag = openAsyncResult.ContinueReading();
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
						openAsyncResult.CleanupOnError();
					}
					if (flag)
					{
						openAsyncResult.Complete(false, exception);
					}
				}
			}

			private class ServerSessionConnectionReader : Microsoft.ServiceBus.Channels.SessionConnectionReader
			{
				private Microsoft.ServiceBus.Channels.ServerSessionDecoder decoder;

				private int maxBufferSize;

				private BufferManager bufferManager;

				private MessageEncoder messageEncoder;

				private string contentType;

				private Microsoft.ServiceBus.Channels.IConnection rawConnection;

				public ServerSessionConnectionReader(Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel channel) : base(channel.Connection, channel.rawConnection, channel.offset, channel.size, channel.RemoteSecurity)
				{
					this.decoder = channel.decoder;
					this.contentType = this.decoder.ContentType;
					this.maxBufferSize = channel.channelListener.MaxBufferSize;
					this.bufferManager = channel.channelListener.BufferManager;
					this.messageEncoder = channel.MessageEncoder;
					this.rawConnection = channel.rawConnection;
				}

				protected override Message DecodeMessage(byte[] buffer, ref int offset, ref int size, ref bool isAtEof, TimeSpan timeout)
				{
					Message message;
					ServiceModelActivity serviceModelActivity;
					while (!isAtEof && size > 0)
					{
						int num = this.decoder.Decode(buffer, offset, size);
						if (num > 0)
						{
							if (base.EnvelopeBuffer != null)
							{
								if (!object.ReferenceEquals(buffer, base.EnvelopeBuffer))
								{
									Buffer.BlockCopy(buffer, offset, base.EnvelopeBuffer, base.EnvelopeOffset, num);
								}
								Microsoft.ServiceBus.Channels.ServerSessionPreambleConnectionReader.ServerFramingDuplexSessionChannel.ServerSessionConnectionReader envelopeOffset = this;
								envelopeOffset.EnvelopeOffset = envelopeOffset.EnvelopeOffset + num;
							}
							offset = offset + num;
							size = size - num;
						}
						switch (this.decoder.CurrentState)
						{
							case Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.EnvelopeStart:
							{
								int envelopeSize = this.decoder.EnvelopeSize;
								if (envelopeSize > this.maxBufferSize)
								{
									base.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/MaxMessageSizeExceededFault", timeout);
									throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(Microsoft.ServiceBus.Channels.MaxMessageSizeStream.CreateMaxReceivedMessageSizeExceededException((long)this.maxBufferSize));
								}
								base.EnvelopeBuffer = this.bufferManager.TakeBuffer(envelopeSize);
								base.EnvelopeOffset = 0;
								base.EnvelopeSize = envelopeSize;
								continue;
							}
							case Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.EnvelopeEnd:
							{
								if (base.EnvelopeBuffer == null)
								{
									continue;
								}
								if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldUseActivity)
								{
									serviceModelActivity = ServiceModelActivity.CreateBoundedActivity(true);
								}
								else
								{
									serviceModelActivity = null;
								}
								using (serviceModelActivity)
								{
									Message message1 = null;
									try
									{
										message1 = this.messageEncoder.ReadMessage(new ArraySegment<byte>(base.EnvelopeBuffer, 0, base.EnvelopeSize), this.bufferManager, this.contentType);
									}
									catch (XmlException xmlException1)
									{
										XmlException xmlException = xmlException1;
										throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(Resources.MessageXmlProtocolError, new object[0]), xmlException));
									}
									base.EnvelopeBuffer = null;
									message = message1;
								}
								return message;
							}
							case Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.End:
							{
								isAtEof = true;
								continue;
							}
							default:
							{
								continue;
							}
						}
					}
					return null;
				}

				protected override void EnsureDecoderAtEof()
				{
					if (this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.End && this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ServerSessionDecoder.State.EnvelopeEnd)
					{
						throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
					}
				}

				protected override void PrepareMessage(Message message)
				{
					base.PrepareMessage(message);
					IPEndPoint remoteIPEndPoint = this.rawConnection.RemoteIPEndPoint;
					if (remoteIPEndPoint != null)
					{
						RemoteEndpointMessageProperty remoteEndpointMessageProperty = new RemoteEndpointMessageProperty(remoteIPEndPoint.Address.ToString(), remoteIPEndPoint.Port);
						message.Properties.Add(RemoteEndpointMessageProperty.Name, remoteEndpointMessageProperty);
					}
				}
			}
		}
	}
}