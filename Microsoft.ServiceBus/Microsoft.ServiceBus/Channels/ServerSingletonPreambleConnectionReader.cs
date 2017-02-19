using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerSingletonPreambleConnectionReader : Microsoft.ServiceBus.Channels.InitialServerConnectionReader
	{
		private Microsoft.ServiceBus.Channels.ServerSingletonDecoder decoder;

		private Microsoft.ServiceBus.Channels.ServerSingletonPreambleCallback callback;

		private WaitCallback onAsyncReadComplete;

		private Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings transportSettings;

		private Microsoft.ServiceBus.Channels.TransportSettingsCallback transportSettingsCallback;

		private SecurityMessageProperty security;

		private Uri via;

		private Microsoft.ServiceBus.Channels.IConnection rawConnection;

		private byte[] connectionBuffer;

		private bool isReadPending;

		private int offset;

		private int size;

		private Microsoft.ServiceBus.Common.TimeoutHelper receiveTimeoutHelper;

		private OnViaDelegate viaDelegate;

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

		public Microsoft.ServiceBus.Channels.ServerSingletonDecoder Decoder
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

		public SecurityMessageProperty Security
		{
			get
			{
				return this.security;
			}
		}

		public Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings TransportSettings
		{
			get
			{
				return this.transportSettings;
			}
		}

		public Uri Via
		{
			get
			{
				return this.via;
			}
		}

		public ServerSingletonPreambleConnectionReader(Microsoft.ServiceBus.Channels.IConnection connection, Action connectionDequeuedCallback, long streamPosition, int offset, int size, Microsoft.ServiceBus.Channels.TransportSettingsCallback transportSettingsCallback, Microsoft.ServiceBus.Channels.ConnectionClosedCallback closedCallback, Microsoft.ServiceBus.Channels.ServerSingletonPreambleCallback callback) : base(connection, closedCallback)
		{
			this.decoder = new Microsoft.ServiceBus.Channels.ServerSingletonDecoder(streamPosition, base.MaxViaSize, base.MaxContentTypeSize);
			this.offset = offset;
			this.size = size;
			this.callback = callback;
			this.transportSettingsCallback = transportSettingsCallback;
			this.rawConnection = connection;
			base.ConnectionDequeuedCallback = connectionDequeuedCallback;
		}

		public Microsoft.ServiceBus.Channels.IConnection CompletePreamble(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			if (!this.transportSettings.MessageEncoderFactory.Encoder.IsContentTypeSupported(this.decoder.ContentType))
			{
				this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/ContentTypeInvalid", ref timeoutHelper);
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string contentTypeMismatch = Resources.ContentTypeMismatch;
				object[] contentType = new object[] { this.decoder.ContentType, this.transportSettings.MessageEncoderFactory.Encoder.ContentType };
				throw exceptionUtility.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(contentTypeMismatch, contentType)));
			}
			StreamUpgradeAcceptor streamUpgradeAcceptor = null;
			StreamUpgradeProvider upgrade = this.transportSettings.Upgrade;
			if (upgrade != null)
			{
				streamUpgradeAcceptor = upgrade.CreateUpgradeAcceptor();
			}
			Microsoft.ServiceBus.Channels.IConnection connection = base.Connection;
			while (true)
			{
				if (this.size == 0)
				{
					this.offset = 0;
					this.size = connection.Read(this.connectionBuffer, 0, (int)this.connectionBuffer.Length, timeoutHelper.RemainingTime());
					if (this.size == 0)
					{
						break;
					}
				}
				do
				{
					int num = this.decoder.Decode(this.connectionBuffer, this.offset, this.size);
					if (num > 0)
					{
						Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader serverSingletonPreambleConnectionReader = this;
						serverSingletonPreambleConnectionReader.offset = serverSingletonPreambleConnectionReader.offset + num;
						Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader serverSingletonPreambleConnectionReader1 = this;
						serverSingletonPreambleConnectionReader1.size = serverSingletonPreambleConnectionReader1.size - num;
					}
					switch (this.decoder.CurrentState)
					{
						case Microsoft.ServiceBus.Channels.ServerSingletonDecoder.State.UpgradeRequest:
						{
							if (streamUpgradeAcceptor == null)
							{
								this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/UpgradeInvalid", ref timeoutHelper);
								ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
								string upgradeRequestToNonupgradableService = Resources.UpgradeRequestToNonupgradableService;
								object[] objArray = new object[] { this.decoder.Upgrade };
								throw exceptionUtility1.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(upgradeRequestToNonupgradableService, objArray)));
							}
							if (!streamUpgradeAcceptor.CanUpgrade(this.decoder.Upgrade))
							{
								this.SendFault("http://schemas.microsoft.com/ws/2006/05/framing/faults/UpgradeInvalid", ref timeoutHelper);
								ExceptionUtility exceptionUtility2 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
								string upgradeProtocolNotSupported = Resources.UpgradeProtocolNotSupported;
								object[] upgrade1 = new object[] { this.decoder.Upgrade };
								throw exceptionUtility2.ThrowHelperError(new ProtocolException(Microsoft.ServiceBus.SR.GetString(upgradeProtocolNotSupported, upgrade1)));
							}
							connection.Write(Microsoft.ServiceBus.Channels.ServerSingletonEncoder.UpgradeResponseBytes, 0, (int)Microsoft.ServiceBus.Channels.ServerSingletonEncoder.UpgradeResponseBytes.Length, true, timeoutHelper.RemainingTime());
							Microsoft.ServiceBus.Channels.IConnection preReadConnection = connection;
							if (this.size > 0)
							{
								preReadConnection = new Microsoft.ServiceBus.Channels.PreReadConnection(preReadConnection, this.connectionBuffer, this.offset, this.size);
							}
							try
							{
								connection = Microsoft.ServiceBus.Channels.InitialServerConnectionReader.UpgradeConnection(preReadConnection, streamUpgradeAcceptor, this.transportSettings);
								this.connectionBuffer = connection.AsyncReadBuffer;
								continue;
							}
							catch (Exception exception1)
							{
								Exception exception = exception1;
								if (Fx.IsFatal(exception))
								{
									throw;
								}
								this.WriteAuditFailure(streamUpgradeAcceptor as StreamSecurityUpgradeAcceptor, exception);
								throw;
							}
							break;
						}
						case Microsoft.ServiceBus.Channels.ServerSingletonDecoder.State.Start:
						{
							this.SetupSecurityIfNecessary(streamUpgradeAcceptor);
							connection.Write(Microsoft.ServiceBus.Channels.ServerSessionEncoder.AckResponseBytes, 0, (int)Microsoft.ServiceBus.Channels.ServerSessionEncoder.AckResponseBytes.Length, true, timeoutHelper.RemainingTime());
							return connection;
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

		private TimeSpan GetRemainingTimeout()
		{
			return this.receiveTimeoutHelper.RemainingTime();
		}

		private void HandleReadComplete()
		{
			this.offset = 0;
			this.size = base.Connection.EndRead();
			this.isReadPending = false;
			if (this.size == 0)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(this.decoder.CreatePrematureEOFException());
			}
		}

		private void OnAsyncReadComplete(object state)
		{
			bool flag = false;
			try
			{
				try
				{
					this.HandleReadComplete();
					this.ReadAndDispatch();
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

		private void ReadAndDispatch()
		{
			bool flag = false;
			try
			{
				try
				{
					while ((this.size > 0 || !this.isReadPending) && !base.IsClosed)
					{
						if (this.size == 0)
						{
							this.isReadPending = true;
							if (this.onAsyncReadComplete == null)
							{
								this.onAsyncReadComplete = new WaitCallback(this.OnAsyncReadComplete);
							}
							if (base.Connection.BeginRead(0, (int)this.connectionBuffer.Length, this.GetRemainingTimeout(), this.onAsyncReadComplete, null) == AsyncReadResult.Queued)
							{
								break;
							}
							this.HandleReadComplete();
						}
						int num = this.decoder.Decode(this.connectionBuffer, this.offset, this.size);
						if (num > 0)
						{
							Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader serverSingletonPreambleConnectionReader = this;
							serverSingletonPreambleConnectionReader.offset = serverSingletonPreambleConnectionReader.offset + num;
							Microsoft.ServiceBus.Channels.ServerSingletonPreambleConnectionReader serverSingletonPreambleConnectionReader1 = this;
							serverSingletonPreambleConnectionReader1.size = serverSingletonPreambleConnectionReader1.size - num;
						}
						if (this.decoder.CurrentState != Microsoft.ServiceBus.Channels.ServerSingletonDecoder.State.PreUpgradeStart)
						{
							continue;
						}
						this.via = this.decoder.Via;
						if (this.viaDelegate != null)
						{
							this.viaDelegate(this.via);
						}
						this.transportSettings = this.transportSettingsCallback(this.via);
						if (this.transportSettings != null)
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

		public void SendFault(string faultString)
		{
			this.SendFault(faultString, ref this.receiveTimeoutHelper);
		}

		private void SendFault(string faultString, ref Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper)
		{
			Microsoft.ServiceBus.Channels.InitialServerConnectionReader.SendFault(base.Connection, faultString, this.connectionBuffer, timeoutHelper.RemainingTime(), 65536);
		}

		private void SetupSecurityIfNecessary(StreamUpgradeAcceptor upgradeAcceptor)
		{
			StreamSecurityUpgradeAcceptor streamSecurityUpgradeAcceptor = upgradeAcceptor as StreamSecurityUpgradeAcceptor;
			if (streamSecurityUpgradeAcceptor != null)
			{
				this.security = streamSecurityUpgradeAcceptor.GetRemoteSecurity();
				if (this.security == null)
				{
					string remoteSecurityNotNegotiatedOnStreamUpgrade = Resources.RemoteSecurityNotNegotiatedOnStreamUpgrade;
					object[] via = new object[] { this.Via };
					Exception protocolException = new ProtocolException(Microsoft.ServiceBus.SR.GetString(remoteSecurityNotNegotiatedOnStreamUpgrade, via));
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(protocolException);
				}
				this.WriteAuditEvent(streamSecurityUpgradeAcceptor, AuditLevel.Success, null);
			}
		}

		public void StartReading(OnViaDelegate viaDelegate, TimeSpan timeout)
		{
			this.viaDelegate = viaDelegate;
			this.receiveTimeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
			this.connectionBuffer = base.Connection.AsyncReadBuffer;
			this.ReadAndDispatch();
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
	}
}