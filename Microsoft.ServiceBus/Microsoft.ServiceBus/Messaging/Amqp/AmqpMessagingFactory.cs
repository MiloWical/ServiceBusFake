using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Messaging.Amqp.Transport;
using Microsoft.ServiceBus.Messaging.AmqpClient;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class AmqpMessagingFactory : MessagingFactory
	{
		private readonly List<Uri> addresses;

		private readonly string containerId;

		private readonly AmqpTransportSettings settings;

		private readonly FaultTolerantObject<AmqpConnection> connection;

		private readonly AmqpMessagingFactory.ConnectionManager connectionManager;

		private AmqpMessagingFactory.ConnectInfo connectInfo;

		private ConcurrentDictionary<string, AmqpMessagingFactory.RedirectionInfo> redirectedConnections;

		internal bool DirectMode
		{
			get
			{
				return this.settings.DirectMode;
			}
		}

		internal string RemoteContainerId
		{
			get
			{
				AmqpConnection unsafeInnerObject = this.connection.UnsafeInnerObject;
				if (unsafeInnerObject == null)
				{
					return null;
				}
				return unsafeInnerObject.Settings.RemoteContainerId;
			}
		}

		internal override IServiceBusSecuritySettings ServiceBusSecuritySettings
		{
			get
			{
				return this.settings;
			}
		}

		internal AmqpTransportSettings TransportSettings
		{
			get
			{
				return this.settings;
			}
		}

		static AmqpMessagingFactory()
		{
			AmqpCodec.RegisterKnownTypes(AmqpSqlFilter.Name, AmqpSqlFilter.Code, () => new AmqpSqlFilter());
			AmqpCodec.RegisterKnownTypes(AmqpTrueFilter.Name, AmqpTrueFilter.Code, () => new AmqpTrueFilter());
			AmqpCodec.RegisterKnownTypes(AmqpFalseFilter.Name, AmqpFalseFilter.Code, () => new AmqpFalseFilter());
			AmqpCodec.RegisterKnownTypes(AmqpCorrelationFilter.Name, AmqpCorrelationFilter.Code, () => new AmqpCorrelationFilter());
		}

		public AmqpMessagingFactory(IEnumerable<Uri> baseAddresses, AmqpTransportSettings settings)
		{
			this.containerId = Guid.NewGuid().ToString("N");
			this.connection = new FaultTolerantObject<AmqpConnection>(this, new Action<AmqpConnection>(this.CloseConnection), new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginCreateConnection), new Func<IAsyncResult, AmqpConnection>(this.EndCreateConnection));
			this.connectionManager = new AmqpMessagingFactory.ConnectionManager(this);
			this.settings = settings;
			this.PrefetchCount = 0;
			this.addresses = new List<Uri>();
			this.redirectedConnections = new ConcurrentDictionary<string, AmqpMessagingFactory.RedirectionInfo>();
			foreach (Uri baseAddress in baseAddresses)
			{
				if (base.Address == null)
				{
					base.Address = baseAddress;
				}
				UriBuilder uriBuilder = new UriBuilder(baseAddress);
				if (!settings.DirectMode && string.Compare(uriBuilder.Scheme, "sb", StringComparison.OrdinalIgnoreCase) != 0)
				{
					ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
					string invalidUriScheme = Resources.InvalidUriScheme;
					object[] scheme = new object[] { uriBuilder.Scheme, "sb" };
					throw exception.AsError(new ArgumentException(Microsoft.ServiceBus.SR.GetString(invalidUriScheme, scheme)), null);
				}
				MessagingUtilities.EnsureTrailingSlash(uriBuilder);
				this.addresses.Add(uriBuilder.Uri);
			}
			base.Address = this.addresses[0];
		}

		private void BaseClose(TimeSpan timeout)
		{
			base.OnClose(timeout);
		}

		private IAsyncResult BaseOnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.OnBeginClose(timeout, callback, state);
		}

		private void BaseOnEndClose(IAsyncResult result)
		{
			base.OnEndClose(result);
		}

		internal IAsyncResult BeginAcceptSessionInternal(string entityName, MessagingEntityType? entityType, string sessionId, Microsoft.ServiceBus.RetryPolicy retryPolicy, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpMessagingFactory.AcceptSessionReceiverAsyncResult(this, entityName, entityType, sessionId, retryPolicy, receiveMode, (serverWaitTime < timeout ? serverWaitTime : timeout), callback, state);
		}

		internal IAsyncResult BeginCloseEntity(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpMessagingFactory.CloseEntityAsyncResult(link, timeout, callback, state);
		}

		private IAsyncResult BeginCreateConnection(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpMessagingFactory.ConnectAsyncResult(this, (this.connectInfo.Redirected ? this.connectInfo : new AmqpMessagingFactory.ConnectInfo()), this.addresses, timeout, callback, state);
		}

		internal IAsyncResult BeginCreateManagementLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpMessagingFactory.CreateManagementLinkAsyncResult(this, timeout, callback, state);
		}

		internal IAsyncResult BeginOpenControlEntity(string entityName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpMessagingFactory.OpenControlEntityAsyncResult(this, entityName, timeout, callback, state);
		}

		internal IAsyncResult BeginOpenEntity(MessageClientEntity clientEntity, string entityName, MessagingEntityType? entityType, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpMessagingFactory.OpenSendEntityAsyncResult(this, clientEntity, entityName, entityType, timeout, callback, state);
		}

		internal IAsyncResult BeginOpenEntity(MessageClientEntity clientEntity, string entityName, MessagingEntityType? entityType, int prefetchCount, string sessionId, bool sessionReceiver, ReceiveMode receiveMode, IList<AmqpDescribed> filters, long? epoch, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new AmqpMessagingFactory.OpenReceiveEntityAsyncResult(this, clientEntity, entityName, entityType, prefetchCount, sessionId, sessionReceiver, receiveMode, filters, epoch, timeout, callback, state)).Start();
		}

		private void CloseConnection(AmqpConnection connection)
		{
			connection.SafeClose();
		}

		internal MessageSession EndAcceptSessionInternal(IAsyncResult result)
		{
			return AsyncResult<AmqpMessagingFactory.AcceptSessionReceiverAsyncResult>.End(result).MessageSession;
		}

		internal void EndCloseEntity(IAsyncResult result)
		{
			AsyncResult<AmqpMessagingFactory.CloseEntityAsyncResult>.End(result);
		}

		private AmqpConnection EndCreateConnection(IAsyncResult result)
		{
			AmqpConnection amqpConnection;
			this.connectInfo = AmqpMessagingFactory.ConnectAsyncResult.End(result, out amqpConnection);
			return amqpConnection;
		}

		internal AmqpManagementLink EndCreateManagementLink(IAsyncResult result)
		{
			return AsyncResult<AmqpMessagingFactory.CreateManagementLinkAsyncResult>.End(result).ManagementLink;
		}

		internal ActiveClientLink EndOpenEntity(IAsyncResult result)
		{
			return AsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>.End(result).ActiveClientLink;
		}

		protected override void OnAbort()
		{
			this.redirectedConnections.Clear();
			base.OnAbort();
			this.connectionManager.Abort();
			AmqpConnection amqpConnection = null;
			if (this.connection.TryGetOpenedObject(out amqpConnection))
			{
				amqpConnection.Abort();
			}
		}

		protected override IAsyncResult OnBeginAcceptMessageSession(ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		protected override IAsyncResult OnBeginAcceptSessionReceiver(string entityName, string sessionId, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			MessagingEntityType? nullable = null;
			return this.BeginAcceptSessionInternal(entityName, nullable, sessionId, null, receiveMode, timeout, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpMessagingFactory.CloseAsyncResult(this, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginCreateMessageReceiver(string entityName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			MessagingEntityType? nullable = null;
			return new CompletedAsyncResult<AmqpMessageReceiver>(new AmqpMessageReceiver(this, entityName, nullable, null, receiveMode), callback, state);
		}

		protected override IAsyncResult OnBeginCreateMessageSender(string transferDestinationEntityName, string viaEntityName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (transferDestinationEntityName != null)
			{
				throw new NotSupportedException();
			}
			MessagingEntityType? nullable = null;
			return new CompletedAsyncResult<AmqpMessageSender>(new AmqpMessageSender(this, viaEntityName, nullable, null), callback, state);
		}

		protected override IAsyncResult OnBeginCreateMessageSender(string entityName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			MessagingEntityType? nullable = null;
			return new CompletedAsyncResult<AmqpMessageSender>(new AmqpMessageSender(this, entityName, nullable, null), callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object asyncState)
		{
			this.connectionManager.Open(timeout);
			return new CompletedAsyncResult(callback, asyncState);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.OnEndClose(this.OnBeginClose(timeout, null, null));
		}

		protected override EventHubClient OnCreateEventHubClient(string path)
		{
			MessagingUtilities.ThrowIfContainsSubQueueName(path);
			return new AmqpEventHubClient(this, path);
		}

		protected override QueueClient OnCreateQueueClient(string path, ReceiveMode receiveMode)
		{
			return new AmqpQueueClient(this, path, receiveMode);
		}

		protected override SubscriptionClient OnCreateSubscriptionClient(string topicPath, string name, ReceiveMode receiveMode)
		{
			MessagingUtilities.ThrowIfContainsSubQueueName(topicPath);
			return new AmqpSubscriptionClient(this, topicPath, name, receiveMode);
		}

		protected override SubscriptionClient OnCreateSubscriptionClient(string subscriptionPath, ReceiveMode receiveMode)
		{
			return new AmqpSubscriptionClient(this, subscriptionPath, receiveMode);
		}

		protected override TopicClient OnCreateTopicClient(string path)
		{
			MessagingUtilities.ThrowIfContainsSubQueueName(path);
			return new AmqpTopicClient(this, path);
		}

		internal override VolatileTopicClient OnCreateVolatileTopicClient(string path, string clientId, Filter filter)
		{
			return new AmqpVolatileTopicClient(this, path, clientId, base.RetryPolicy, filter);
		}

		protected override MessageSession OnEndAcceptMessageSession(IAsyncResult result)
		{
			throw new NotSupportedException();
		}

		protected override MessageSession OnEndAcceptSessionReceiver(IAsyncResult result)
		{
			return this.EndAcceptSessionInternal(result);
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<AmqpMessagingFactory.CloseAsyncResult>.End(result);
		}

		protected override MessageReceiver OnEndCreateMessageReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageReceiver>.End(result);
		}

		protected override MessageSender OnEndCreateMessageSender(IAsyncResult result)
		{
			return CompletedAsyncResult<AmqpMessageSender>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		public override string ToString()
		{
			return "factory";
		}

		private sealed class AcceptSessionReceiverAsyncResult : IteratorAsyncResult<AmqpMessagingFactory.AcceptSessionReceiverAsyncResult>
		{
			private readonly AmqpMessagingFactory factory;

			private AmqpMessageReceiver receiver;

			private AmqpMessageSession messageSession;

			public MessageSession MessageSession
			{
				get
				{
					return this.messageSession;
				}
			}

			public AcceptSessionReceiverAsyncResult(AmqpMessagingFactory factory, string entityName, MessagingEntityType? entityType, string sessionId, Microsoft.ServiceBus.RetryPolicy retryPolicy, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				this.receiver = new AmqpMessageReceiver(this.factory, entityName, entityType, sessionId, retryPolicy, receiveMode);
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpMessagingFactory.AcceptSessionReceiverAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				AmqpMessagingFactory.AcceptSessionReceiverAsyncResult acceptSessionReceiverAsyncResult = this;
				IteratorAsyncResult<AmqpMessagingFactory.AcceptSessionReceiverAsyncResult>.BeginCall beginCall = (AmqpMessagingFactory.AcceptSessionReceiverAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.receiver.BeginOpen(t, c, s);
				IteratorAsyncResult<AmqpMessagingFactory.AcceptSessionReceiverAsyncResult>.EndCall endCall = (AmqpMessagingFactory.AcceptSessionReceiverAsyncResult thisPtr, IAsyncResult r) => thisPtr.receiver.EndOpen(r);
				yield return acceptSessionReceiverAsyncResult.CallAsync(beginCall, endCall, (AmqpMessagingFactory.AcceptSessionReceiverAsyncResult thisPtr, TimeSpan t) => thisPtr.receiver.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				Exception lastAsyncStepException = base.LastAsyncStepException;
				if (lastAsyncStepException == null)
				{
					this.messageSession = new AmqpMessageSession(this.receiver.Mode, this.receiver.SessionId, this.receiver);
				}
				else
				{
					this.receiver.Abort();
					base.Complete(ExceptionHelper.ToMessagingContract(lastAsyncStepException, this.factory.RemoteContainerId));
				}
			}
		}

		private sealed class CloseAsyncResult : IteratorAsyncResult<AmqpMessagingFactory.CloseAsyncResult>
		{
			private readonly AmqpMessagingFactory factory;

			private AmqpConnection amqpConnection;

			public CloseAsyncResult(AmqpMessagingFactory factory, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpMessagingFactory.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				AmqpMessagingFactory.CloseAsyncResult closeAsyncResult = this;
				IteratorAsyncResult<AmqpMessagingFactory.CloseAsyncResult>.BeginCall beginCall = (AmqpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.BaseOnBeginClose(t, c, s);
				IteratorAsyncResult<AmqpMessagingFactory.CloseAsyncResult>.EndCall endCall = (AmqpMessagingFactory.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.factory.BaseOnEndClose(r);
				yield return closeAsyncResult.CallAsync(beginCall, endCall, (AmqpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t) => thisPtr.factory.BaseClose(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.factory.redirectedConnections.Clear();
				AmqpMessagingFactory.CloseAsyncResult closeAsyncResult1 = this;
				IteratorAsyncResult<AmqpMessagingFactory.CloseAsyncResult>.BeginCall beginCall1 = (AmqpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.connectionManager.BeginClose(t, c, s);
				IteratorAsyncResult<AmqpMessagingFactory.CloseAsyncResult>.EndCall endCall1 = (AmqpMessagingFactory.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.factory.connectionManager.EndClose(r);
				yield return closeAsyncResult1.CallAsync(beginCall1, endCall1, (AmqpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t) => thisPtr.factory.connectionManager.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (this.factory.connection.TryGetOpenedObject(out this.amqpConnection))
				{
					AmqpMessagingFactory.CloseAsyncResult closeAsyncResult2 = this;
					IteratorAsyncResult<AmqpMessagingFactory.CloseAsyncResult>.BeginCall beginCall2 = (AmqpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.amqpConnection.BeginClose(t, c, s);
					yield return closeAsyncResult2.CallAsync(beginCall2, (AmqpMessagingFactory.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpConnection.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			}
		}

		private sealed class CloseEntityAsyncResult : IteratorAsyncResult<AmqpMessagingFactory.CloseEntityAsyncResult>
		{
			private readonly AmqpLink link;

			public CloseEntityAsyncResult(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.link = link;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpMessagingFactory.CloseEntityAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				AmqpMessagingFactory.CloseEntityAsyncResult closeEntityAsyncResult = this;
				IteratorAsyncResult<AmqpMessagingFactory.CloseEntityAsyncResult>.BeginCall beginCall = (AmqpMessagingFactory.CloseEntityAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.link.Session.BeginClose(t, c, s);
				yield return closeEntityAsyncResult.CallAsync(beginCall, (AmqpMessagingFactory.CloseEntityAsyncResult thisPtr, IAsyncResult r) => thisPtr.link.Session.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class ConnectAsyncResult : IteratorAsyncResult<AmqpMessagingFactory.ConnectAsyncResult>
		{
			private readonly AmqpMessagingFactory messagingFactory;

			private readonly List<Uri> addresses;

			private AmqpMessagingFactory.ConnectInfo connectInfo;

			private AmqpTransportInitiator initiator;

			private TransportBase transport;

			private AmqpConnection amqpConnection;

			private Exception completeException;

			public ConnectAsyncResult(AmqpMessagingFactory messagingFactory, AmqpMessagingFactory.ConnectInfo directConnect, List<Uri> addresses, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.messagingFactory = messagingFactory;
				this.connectInfo = directConnect;
				this.addresses = addresses;
				base.Start();
			}

			public static AmqpMessagingFactory.ConnectInfo End(IAsyncResult result, out AmqpConnection connection)
			{
				AmqpMessagingFactory.ConnectAsyncResult connectAsyncResult = AsyncResult.End<AmqpMessagingFactory.ConnectAsyncResult>(result);
				connection = connectAsyncResult.amqpConnection;
				return connectAsyncResult.connectInfo;
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpMessagingFactory.ConnectAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				int num;
				bool networkHost = this.connectInfo.NetworkHost != null;
				bool flag = false;
				AmqpSettings amqpSetting = null;
				ServiceBusUriManager serviceBusUriManager = null;
				if (this.addresses != null && this.addresses.Count > 0)
				{
					serviceBusUriManager = new ServiceBusUriManager(this.addresses, false);
				}
				while (true)
				{
					if (!networkHost && !flag && (serviceBusUriManager == null || !serviceBusUriManager.MoveNextUri()) || !(base.RemainingTime() > TimeSpan.Zero))
					{
						goto Label0;
					}
					this.completeException = null;
					if (!networkHost && !flag)
					{
						Uri current = serviceBusUriManager.Current;
						this.connectInfo = new AmqpMessagingFactory.ConnectInfo(current.Host, current.Host, current.Port);
						if (current.LocalPath.Length > 1)
						{
							string localPath = current.LocalPath;
							char[] chrArray = new char[] { '/' };
							this.connectInfo.Path = localPath.TrimStart(chrArray);
							if (!this.connectInfo.Path.EndsWith("/", StringComparison.Ordinal))
							{
								this.connectInfo.Path = string.Concat(this.connectInfo.Path, "/");
							}
						}
					}
					amqpSetting = this.messagingFactory.settings.CreateAmqpSettings(this.connectInfo.HostName);
					TcpTransportSettings tcpTransportSetting = new TcpTransportSettings()
					{
						Host = this.connectInfo.NetworkHost
					};
					TcpTransportSettings tcpTransportSetting1 = tcpTransportSetting;
					num = (this.connectInfo.Port < 0 ? this.messagingFactory.settings.DefaultPort : this.connectInfo.Port);
					tcpTransportSetting1.Port = num;
					Microsoft.ServiceBus.Messaging.Amqp.Transport.TransportSettings transportSetting = tcpTransportSetting;
					if (this.messagingFactory.settings.UseSslStreamSecurity && !this.messagingFactory.settings.SslStreamUpgrade)
					{
						TlsTransportSettings tlsTransportSetting = new TlsTransportSettings(tcpTransportSetting);
						TlsTransportSettings tlsTransportSetting1 = tlsTransportSetting;
						string sslHostName = this.messagingFactory.settings.SslHostName;
						if (sslHostName == null)
						{
							sslHostName = AmqpMessagingFactory.ConnectAsyncResult.GetDomainName(this.connectInfo.HostName);
						}
						tlsTransportSetting1.TargetHost = sslHostName;
						if (networkHost || flag)
						{
							tlsTransportSetting.CertificateValidationCallback = new RemoteCertificateValidationCallback(this.ValidateCertificate);
						}
						else
						{
							tlsTransportSetting.CertificateValidationCallback = this.messagingFactory.settings.CertificateValidationCallback;
						}
						transportSetting = tlsTransportSetting;
					}
					this.initiator = new AmqpTransportInitiator(amqpSetting, transportSetting);
					AmqpMessagingFactory.ConnectAsyncResult connectAsyncResult = this;
					IteratorAsyncResult<AmqpMessagingFactory.ConnectAsyncResult>.BeginCall beginCall = (AmqpMessagingFactory.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.initiator.BeginConnect(t, c, s);
					yield return connectAsyncResult.CallAsync(beginCall, (AmqpMessagingFactory.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.transport = thisPtr.initiator.EndConnect(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						this.completeException = null;
						AmqpConnectionSettings amqpConnectionSetting = new AmqpConnectionSettings()
						{
							MaxFrameSize = new uint?((uint)this.messagingFactory.settings.MaxFrameSize),
							ContainerId = this.messagingFactory.containerId
						};
						AmqpConnectionSettings amqpConnectionSetting1 = amqpConnectionSetting;
						string openHostName = this.messagingFactory.settings.OpenHostName;
						if (openHostName == null)
						{
							openHostName = this.connectInfo.HostName;
						}
						amqpConnectionSetting1.HostName = openHostName;
						this.amqpConnection = new AmqpConnection(this.transport, amqpSetting, amqpConnectionSetting);
						AmqpMessagingFactory.ConnectAsyncResult connectAsyncResult1 = this;
						IteratorAsyncResult<AmqpMessagingFactory.ConnectAsyncResult>.BeginCall beginCall1 = (AmqpMessagingFactory.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.amqpConnection.BeginOpen(t, c, s);
						yield return connectAsyncResult1.CallAsync(beginCall1, (AmqpMessagingFactory.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpConnection.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						AmqpException amqpException = null;
						if (networkHost || flag)
						{
							break;
						}
						AmqpException lastAsyncStepException = base.LastAsyncStepException as AmqpException;
						AmqpException amqpException1 = lastAsyncStepException;
						amqpException = lastAsyncStepException;
						if (amqpException1 == null || !amqpException.Error.Condition.Equals(AmqpError.ConnectionRedirect.Condition))
						{
							break;
						}
						this.amqpConnection.SafeClose();
						this.amqpConnection = null;
						Fields info = amqpException.Error.Info;
						string str = null;
						string str1 = null;
						int num1 = -1;
						if (info != null)
						{
							info.TryGetValue<string>(AmqpConstants.HostName, out str);
							info.TryGetValue<string>(AmqpConstants.NetworkHost, out str1);
							info.TryGetValue<int>(AmqpConstants.Port, out num1);
						}
						if (str == null || str1 == null || num1 <= 0)
						{
							break;
						}
						this.connectInfo = new AmqpMessagingFactory.ConnectInfo(str, str1, num1)
						{
							Redirected = true
						};
						flag = true;
					}
					else
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(this.messagingFactory, "Connect", base.LastAsyncStepException.Message));
						this.completeException = base.LastAsyncStepException;
						if (!networkHost)
						{
							if (flag)
							{
								goto Label0;
							}
							this.connectInfo = new AmqpMessagingFactory.ConnectInfo();
						}
						else
						{
							networkHost = false;
							this.connectInfo = new AmqpMessagingFactory.ConnectInfo();
						}
					}
				}
				this.completeException = base.LastAsyncStepException;
				if (this.completeException != null)
				{
					this.amqpConnection.SafeClose();
					this.amqpConnection = null;
				}
			Label0:
				if (this.amqpConnection == null && base.RemainingTime() == TimeSpan.Zero && this.completeException == null)
				{
					this.completeException = new TimeoutException(SRAmqp.AmqpTimeout(base.OriginalTimeout, base.GetType().Name));
				}
				base.Complete(this.completeException);
			}

			private static string GetDomainName(string hostName)
			{
				string str = hostName;
				int num = str.IndexOf(':');
				if (num > 0)
				{
					str = str.Substring(0, num);
				}
				return str;
			}

			private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
			{
				if (this.connectInfo.HostName == null)
				{
					return true;
				}
				return SecureSocketUtil.CustomizedCertificateValidator(sender, certificate, chain, sslPolicyErrors, AmqpMessagingFactory.ConnectAsyncResult.GetDomainName(this.connectInfo.HostName));
			}
		}

		private struct ConnectInfo
		{
			private readonly string hostName;

			private readonly string networkHost;

			private readonly int port;

			private bool redirected;

			private string path;

			public string HostName
			{
				get
				{
					return this.hostName;
				}
			}

			public string NetworkHost
			{
				get
				{
					return this.networkHost;
				}
			}

			public string Path
			{
				get
				{
					return this.path;
				}
				set
				{
					this.path = value;
				}
			}

			public int Port
			{
				get
				{
					return this.port;
				}
			}

			public bool Redirected
			{
				get
				{
					return this.redirected;
				}
				set
				{
					this.redirected = value;
				}
			}

			public ConnectInfo(string hostName, string networkHost, int port)
			{
				this.hostName = hostName;
				this.networkHost = networkHost;
				this.port = port;
				this.redirected = false;
				this.path = null;
			}

			public override bool Equals(object obj)
			{
				if (obj == null || !(obj is AmqpMessagingFactory.ConnectInfo))
				{
					return false;
				}
				AmqpMessagingFactory.ConnectInfo connectInfo = (AmqpMessagingFactory.ConnectInfo)obj;
				if (!string.Equals(this.hostName, connectInfo.hostName, StringComparison.OrdinalIgnoreCase) || !string.Equals(this.networkHost, connectInfo.networkHost, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				return this.port == connectInfo.port;
			}

			public override int GetHashCode()
			{
				int hashCode = this.hostName.ToUpper(CultureInfo.InvariantCulture).GetHashCode();
				int num = this.networkHost.ToUpper(CultureInfo.InvariantCulture).GetHashCode();
				int num1 = this.port;
				return HashCode.CombineHashCodes(hashCode, num, num1.GetHashCode());
			}
		}

		private sealed class ConnectionManager : SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>
		{
			private const string ConnectInfoName = "connect-info";

			private readonly static AsyncCallback unloadCallback;

			private readonly AmqpMessagingFactory factory;

			static ConnectionManager()
			{
				AmqpMessagingFactory.ConnectionManager.unloadCallback = new AsyncCallback(AmqpMessagingFactory.ConnectionManager.UnloadCallback);
			}

			public ConnectionManager(AmqpMessagingFactory factory)
			{
				this.factory = factory;
			}

			public IAsyncResult BeginGetConnection(AmqpMessagingFactory.ConnectInfo connectInfo, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return base.BeginLoadInstance(connectInfo, null, timeout, callback, state);
			}

			public AmqpConnection EndGetConnection(IAsyncResult result)
			{
				return base.EndLoadInstance(result);
			}

			protected override void OnAbortInstance(SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>.SingletonContext singletonContext, AmqpMessagingFactory.ConnectInfo key, AmqpConnection instance, object unloadingContext)
			{
				instance.Abort();
			}

			protected override IAsyncResult OnBeginCloseInstance(SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>.SingletonContext singletonContext, AmqpMessagingFactory.ConnectInfo key, AmqpConnection instance, object unloadingContext, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult(instance, timeout, callback, state);
			}

			protected override IAsyncResult OnBeginCreateInstance(SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>.SingletonContext singletonContext, AmqpMessagingFactory.ConnectInfo key, object loadingContext, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new AmqpMessagingFactory.ConnectAsyncResult(this.factory, key, null, timeout, callback, state);
			}

			protected override IAsyncResult OnBeginOpenInstance(SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>.SingletonContext singletonContext, AmqpMessagingFactory.ConnectInfo key, AmqpConnection instance, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new CompletedAsyncResult(callback, (object)timeout);
			}

			protected override void OnCloseInstance(SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>.SingletonContext singletonContext, AmqpMessagingFactory.ConnectInfo key, AmqpConnection instance, object unloadingContext, TimeSpan timeout)
			{
				instance.Close(timeout);
			}

			private void OnConnectionClosed(object sender, EventArgs e)
			{
				AmqpMessagingFactory.ConnectInfo connectInfo;
				AmqpConnection amqpConnection = (AmqpConnection)sender;
				if (base.State == CommunicationState.Opened && amqpConnection.Settings.Properties != null && amqpConnection.Settings.Properties.TryGetValue<AmqpMessagingFactory.ConnectInfo>("connect-info", out connectInfo))
				{
					try
					{
						base.BeginUnloadInstance(connectInfo, null, true, TimeSpan.FromSeconds(10), AmqpMessagingFactory.ConnectionManager.unloadCallback, this);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteExceptionAsWarning(exception.ToString()));
					}
				}
			}

			protected override AmqpConnection OnCreateInstance(SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>.SingletonContext singletonContext, AmqpMessagingFactory.ConnectInfo key, object loadingContext, TimeSpan timeout)
			{
				IAsyncResult asyncResult = this.OnBeginCreateInstance(singletonContext, key, loadingContext, timeout, null, null);
				return this.OnEndCreateInstance(asyncResult);
			}

			protected override void OnEndCloseInstance(IAsyncResult result)
			{
				AsyncResult<AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult>.End(result);
			}

			protected override AmqpConnection OnEndCreateInstance(IAsyncResult result)
			{
				AmqpConnection amqpConnection;
				AmqpMessagingFactory.ConnectInfo connectInfo = AmqpMessagingFactory.ConnectAsyncResult.End(result, out amqpConnection);
				amqpConnection.Settings.AddProperty("connect-info", connectInfo);
				amqpConnection.Closed += new EventHandler(this.OnConnectionClosed);
				if (amqpConnection.State != AmqpObjectState.Opened)
				{
					this.OnConnectionClosed(amqpConnection, EventArgs.Empty);
				}
				return amqpConnection;
			}

			protected override void OnEndOpenInstance(IAsyncResult result)
			{
				CompletedAsyncResult.End(result);
			}

			protected override void OnOpenInstance(SingletonDictionaryManager<AmqpMessagingFactory.ConnectInfo, AmqpConnection>.SingletonContext singletonContext, AmqpMessagingFactory.ConnectInfo key, AmqpConnection instance, TimeSpan timeout)
			{
			}

			private static void UnloadCallback(IAsyncResult result)
			{
				AmqpMessagingFactory.ConnectionManager asyncState = (AmqpMessagingFactory.ConnectionManager)result.AsyncState;
				try
				{
					asyncState.EndUnloadInstance(result);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteExceptionAsWarning(exception.ToString()));
				}
			}

			private sealed class CloseInstanceAsyncResult : IteratorAsyncResult<AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult>
			{
				private readonly AmqpConnection connection;

				public CloseInstanceAsyncResult(AmqpConnection connection, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.connection = connection;
					base.Start();
				}

				protected override IEnumerator<IteratorAsyncResult<AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult closeInstanceAsyncResult = this;
					IteratorAsyncResult<AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult>.BeginCall beginCall = (AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.connection.BeginClose(t, c, s);
					yield return closeInstanceAsyncResult.CallAsync(beginCall, (AmqpMessagingFactory.ConnectionManager.CloseInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.connection.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			}
		}

		private sealed class CreateManagementLinkAsyncResult : IteratorAsyncResult<AmqpMessagingFactory.CreateManagementLinkAsyncResult>
		{
			private readonly AmqpMessagingFactory factory;

			public AmqpManagementLink ManagementLink
			{
				get;
				private set;
			}

			public CreateManagementLinkAsyncResult(AmqpMessagingFactory factory, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpMessagingFactory.CreateManagementLinkAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				AmqpConnection amqpConnection = null;
				if (!this.factory.connection.TryGetOpenedObject(out amqpConnection))
				{
					AmqpMessagingFactory.CreateManagementLinkAsyncResult createManagementLinkAsyncResult = this;
					IteratorAsyncResult<AmqpMessagingFactory.CreateManagementLinkAsyncResult>.BeginCall beginCall = (AmqpMessagingFactory.CreateManagementLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.connection.BeginGetInstance(t, c, s);
					yield return createManagementLinkAsyncResult.CallAsync(beginCall, (AmqpMessagingFactory.CreateManagementLinkAsyncResult thisPtr, IAsyncResult r) => amqpConnection = thisPtr.factory.connection.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				this.ManagementLink = amqpConnection.Extensions.Find<AmqpManagementLink>();
				if (this.ManagementLink == null)
				{
					this.ManagementLink = new AmqpManagementLink(amqpConnection);
				}
			}
		}

		private sealed class OpenControlEntityAsyncResult : AmqpMessagingFactory.OpenEntityAsyncResult
		{
			public OpenControlEntityAsyncResult(AmqpMessagingFactory factory, string entityName, TimeSpan timeout, AsyncCallback callback, object state) : base(factory, null, entityName, null, new string[] { "Listen", "Manage" }, timeout, callback, state)
			{
				base.Start();
			}

			protected override void UpdateLinkSettings(AmqpLinkSettings settings, string address)
			{
				settings.Role = new bool?(false);
				settings.InitialDeliveryCount = new uint?(0);
				settings.Target = new CommandTarget()
				{
					Entity = address
				};
			}
		}

		private abstract class OpenEntityAsyncResult : IteratorAsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>
		{
			private readonly AmqpMessagingFactory factory;

			private readonly MessageClientEntity clientEntity;

			private readonly MessagingEntityType? entityType;

			private readonly TokenProvider tokenProvider;

			private readonly string[] requiredClaims;

			private AmqpMessagingFactory.ConnectInfo connectInfo;

			private string entityName;

			private string audience;

			private string entityEndpointUri;

			private ActiveClientLink activeClientLink;

			private AmqpConnection amqpConnection;

			private AmqpCbsLink cbsLink;

			private DateTime authorizationValidToUtc;

			public ActiveClientLink ActiveClientLink
			{
				get
				{
					return this.activeClientLink;
				}
			}

			protected OpenEntityAsyncResult(AmqpMessagingFactory factory, MessageClientEntity clientEntity, string entityName, MessagingEntityType? entityType, string[] requiredClaims, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				this.clientEntity = clientEntity;
				this.entityName = entityName;
				this.entityType = entityType;
				this.requiredClaims = requiredClaims;
				this.tokenProvider = this.factory.ServiceBusSecuritySettings.TokenProvider;
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				string str;
				Exception lastAsyncStepException;
				AmqpLink sendingAmqpLink;
				string redirectCacheKey = AmqpMessagingFactory.OpenEntityAsyncResult.GetRedirectCacheKey(this.factory.connectInfo, this.entityName);
				AmqpMessagingFactory.RedirectionInfo redirectionInfo = null;
				bool flag = false;
				if (this.factory.redirectedConnections.TryGetValue(redirectCacheKey, out redirectionInfo))
				{
					AmqpMessagingFactory.ConnectInfo connectInfo = this.connectInfo;
					string str1 = this.entityName;
					this.entityName = redirectionInfo.EntityName;
					this.connectInfo = redirectionInfo.ConnectionInfo;
					AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult = this;
					IteratorAsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>.BeginCall beginCall = (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.connectionManager.BeginGetConnection(thisPtr.connectInfo, t, c, s);
					yield return openEntityAsyncResult.CallAsync(beginCall, (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpConnection = thisPtr.factory.connectionManager.EndGetConnection(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						flag = true;
					}
					else
					{
						this.factory.redirectedConnections.TryRemove(redirectCacheKey, out redirectionInfo);
						this.entityName = str1;
						this.connectInfo = connectInfo;
					}
				}
				if (!flag)
				{
					if (!this.factory.connection.TryGetOpenedObject(out this.amqpConnection))
					{
						AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult1 = this;
						IteratorAsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>.BeginCall beginCall1 = (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.connection.BeginGetInstance(t, c, s);
						yield return openEntityAsyncResult1.CallAsync(beginCall1, (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpConnection = thisPtr.factory.connection.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							goto Label1;
						}
						base.Complete(ExceptionHelper.ToMessagingContract(base.LastAsyncStepException, null));
						goto Label0;
					}
				Label1:
					redirectCacheKey = AmqpMessagingFactory.OpenEntityAsyncResult.GetRedirectCacheKey(this.factory.connectInfo, this.entityName);
				}
				AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult2 = this;
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] hostName = new object[] { "amqp", this.factory.connectInfo.HostName, redirectCacheKey };
				openEntityAsyncResult2.entityEndpointUri = string.Format(invariantCulture, "{0}://{1}/{2}", hostName);
				AmqpSession amqpSession = null;
				while (true)
				{
					if (base.RemainingTime() > TimeSpan.Zero)
					{
						TimeSpan zero = TimeSpan.Zero;
						if (this.clientEntity != null)
						{
							MessageSender messageSender = this.clientEntity as MessageSender;
							if (messageSender == null)
							{
								MessageReceiver messageReceiver = this.clientEntity as MessageReceiver;
								if (messageReceiver != null)
								{
									zero = messageReceiver.BatchFlushInterval;
								}
							}
							else
							{
								zero = messageSender.BatchFlushInterval;
							}
						}
						try
						{
							AmqpSessionSettings amqpSessionSetting = new AmqpSessionSettings()
							{
								Properties = new Fields()
							};
							amqpSessionSetting.Properties.Add(ClientConstants.BatchFlushIntervalName, (uint)zero.TotalMilliseconds);
							amqpSession = this.amqpConnection.CreateSession(amqpSessionSetting);
						}
						catch (InvalidOperationException invalidOperationException1)
						{
							InvalidOperationException invalidOperationException = invalidOperationException1;
							base.Complete(new MessagingException(invalidOperationException.Message, false, invalidOperationException));
							goto Label0;
						}
						AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult3 = this;
						IteratorAsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>.BeginCall beginCall2 = (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => amqpSession.BeginOpen(t, c, s);
						yield return openEntityAsyncResult3.CallAsync(beginCall2, (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, IAsyncResult r) => amqpSession.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						lastAsyncStepException = base.LastAsyncStepException;
						if (lastAsyncStepException == null)
						{
							AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings();
							AmqpLinkSettings amqpLinkSetting1 = amqpLinkSetting;
							AmqpSymbol timeoutName = ClientConstants.TimeoutName;
							TimeSpan timeSpan = base.RemainingTime();
							amqpLinkSetting1.AddProperty(timeoutName, (uint)timeSpan.TotalMilliseconds);
							if (this.entityType.HasValue)
							{
								AmqpLinkSettings amqpLinkSetting2 = amqpLinkSetting;
								AmqpSymbol entityTypeName = ClientConstants.EntityTypeName;
								MessagingEntityType? nullable = this.entityType;
								amqpLinkSetting2.AddProperty(entityTypeName, (int)nullable.Value);
							}
							bool flag1 = false;
							this.authorizationValidToUtc = DateTime.MaxValue;
							if (this.tokenProvider != null)
							{
								flag1 = true;
								this.cbsLink = this.amqpConnection.Extensions.Find<AmqpCbsLink>();
								if (this.cbsLink == null)
								{
									this.cbsLink = new AmqpCbsLink(this.amqpConnection);
								}
								AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult4 = this;
								str = (this.connectInfo.Redirected ? this.entityName : this.entityEndpointUri);
								openEntityAsyncResult4.audience = str;
								AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult5 = this;
								IteratorAsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>.BeginCall beginCall3 = (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.cbsLink.BeginSendToken(thisPtr.tokenProvider, thisPtr.factory.Address, thisPtr.audience, thisPtr.entityEndpointUri, thisPtr.requiredClaims, t, c, s);
								yield return openEntityAsyncResult5.CallAsync(beginCall3, (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, IAsyncResult r) => thisPtr.authorizationValidToUtc = thisPtr.cbsLink.EndSendToken(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
							}
							string str2 = this.entityName;
							if (!this.connectInfo.Redirected && !string.IsNullOrEmpty(this.factory.connectInfo.Path))
							{
								str2 = string.Concat(this.factory.connectInfo.Path, this.entityName);
							}
							this.UpdateLinkSettings(amqpLinkSetting, str2);
							if (!amqpLinkSetting.Role.HasValue || !amqpLinkSetting.Role.Value)
							{
								sendingAmqpLink = new SendingAmqpLink(amqpLinkSetting);
							}
							else
							{
								sendingAmqpLink = new ReceivingAmqpLink(amqpLinkSetting);
							}
							AmqpLinkSettings amqpLinkSetting3 = amqpLinkSetting;
							CultureInfo cultureInfo = CultureInfo.InvariantCulture;
							object[] identifier = new object[] { this.factory.containerId, this.amqpConnection.Identifier, amqpSession.Identifier, sendingAmqpLink.Identifier };
							amqpLinkSetting3.LinkName = string.Format(cultureInfo, "{0};{1}:{2}:{3}", identifier);
							sendingAmqpLink.AttachTo(amqpSession);
							yield return base.CallAsync((AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => sendingAmqpLink.BeginOpen(t, c, s), (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, IAsyncResult r) => sendingAmqpLink.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							lastAsyncStepException = base.LastAsyncStepException;
							if (lastAsyncStepException == null)
							{
								MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntitySucceeded(this.factory, sendingAmqpLink, sendingAmqpLink.Name, this.entityName));
								this.activeClientLink = new ActiveClientLink(sendingAmqpLink, this.audience, this.entityEndpointUri, this.requiredClaims, flag1, this.authorizationValidToUtc);
								goto Label0;
							}
							else
							{
								MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntityFailed(this.factory, sendingAmqpLink, sendingAmqpLink.Name, this.entityName, lastAsyncStepException.Message));
								amqpSession.SafeClose();
								AmqpException amqpException = lastAsyncStepException as AmqpException;
								if (amqpException == null)
								{
									break;
								}
								if (!amqpException.Error.Condition.Equals(AmqpError.NotFound.Condition))
								{
									if (!amqpException.Error.Condition.Equals(AmqpError.LinkRedirect.Condition))
									{
										break;
									}
									Fields field = amqpException.Error.Info;
									string str3 = null;
									string str4 = null;
									int num = -1;
									string str5 = null;
									if (field != null)
									{
										field.TryGetValue<string>(AmqpConstants.HostName, out str3);
										field.TryGetValue<string>(AmqpConstants.NetworkHost, out str4);
										field.TryGetValue<int>(AmqpConstants.Port, out num);
										field.TryGetValue<string>(AmqpConstants.Address, out str5);
									}
									if (str3 == null || str4 == null || num <= 0 || str5 == null)
									{
										break;
									}
									this.entityName = str5;
									AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult6 = this;
									AmqpMessagingFactory.ConnectInfo connectInfo1 = new AmqpMessagingFactory.ConnectInfo(str3, str4, num)
									{
										Redirected = true
									};
									openEntityAsyncResult6.connectInfo = connectInfo1;
									AmqpMessagingFactory.OpenEntityAsyncResult openEntityAsyncResult7 = this;
									IteratorAsyncResult<AmqpMessagingFactory.OpenEntityAsyncResult>.BeginCall beginCall4 = (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.connectionManager.BeginGetConnection(thisPtr.connectInfo, t, c, s);
									yield return openEntityAsyncResult7.CallAsync(beginCall4, (AmqpMessagingFactory.OpenEntityAsyncResult thisPtr, IAsyncResult r) => thisPtr.amqpConnection = thisPtr.factory.connectionManager.EndGetConnection(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									if (base.LastAsyncStepException == null)
									{
										ConcurrentDictionary<string, AmqpMessagingFactory.RedirectionInfo> strs = this.factory.redirectedConnections;
										string str6 = redirectCacheKey;
										AmqpMessagingFactory.RedirectionInfo redirectionInfo1 = new AmqpMessagingFactory.RedirectionInfo()
										{
											EntityName = str5,
											ConnectionInfo = this.connectInfo
										};
										strs[str6] = redirectionInfo1;
									}
									else
									{
										base.Complete(ExceptionHelper.GetClientException(base.LastAsyncStepException, null));
										goto Label0;
									}
								}
								else
								{
									this.factory.redirectedConnections.TryRemove(redirectCacheKey, out redirectionInfo);
									break;
								}
							}
						}
						else
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntityFailed(this.factory, amqpSession, string.Empty, this.entityName, lastAsyncStepException.Message));
							amqpSession.Abort();
							base.Complete(ExceptionHelper.ToMessagingContract(lastAsyncStepException, this.factory.RemoteContainerId));
							goto Label0;
						}
					}
					else
					{
						if (amqpSession != null)
						{
							amqpSession.SafeClose();
						}
						base.Complete(new TimeoutException(SRAmqp.AmqpTimeout(base.OriginalTimeout, this.entityName)));
						goto Label0;
					}
				}
				base.Complete(ExceptionHelper.ToMessagingContract(lastAsyncStepException, this.factory.RemoteContainerId));
			Label0:
				yield break;
			}

			private static string GetRedirectCacheKey(AmqpMessagingFactory.ConnectInfo connectInfo, string entityName)
			{
				if (connectInfo.Path == null)
				{
					return entityName;
				}
				return string.Concat(connectInfo.Path, entityName);
			}

			protected abstract void UpdateLinkSettings(AmqpLinkSettings settings, string address);
		}

		private sealed class OpenReceiveEntityAsyncResult : AmqpMessagingFactory.OpenEntityAsyncResult
		{
			private readonly int prefetchCount;

			private readonly string sessionId;

			private readonly ReceiveMode receiveMode;

			private readonly bool sessionReceiver;

			private readonly IList<AmqpDescribed> filters;

			private readonly long? epoch;

			public OpenReceiveEntityAsyncResult(AmqpMessagingFactory factory, MessageClientEntity clientEntity, string entityName, MessagingEntityType? entityType, int prefetchCount, string sessionId, bool sessionReceiver, ReceiveMode receiveMode, IList<AmqpDescribed> filters, long? epoch, TimeSpan timeout, AsyncCallback callback, object state) : base(factory, clientEntity, entityName, entityType, new string[] { "Listen" }, timeout, callback, state)
			{
				this.prefetchCount = prefetchCount;
				this.sessionId = sessionId;
				this.sessionReceiver = sessionReceiver;
				this.receiveMode = receiveMode;
				this.filters = filters;
				this.epoch = epoch;
			}

			protected override void UpdateLinkSettings(AmqpLinkSettings settings, string address)
			{
				FilterSet filterSet = null;
				if (this.sessionReceiver)
				{
					filterSet = new FilterSet()
					{
						{ ClientConstants.SessionFilterName, this.sessionId }
					};
				}
				if (this.filters != null && this.filters.Count > 0)
				{
					if (filterSet == null)
					{
						filterSet = new FilterSet();
					}
					foreach (AmqpDescribed filter in this.filters)
					{
						filterSet.Add(filter.DescriptorName, filter);
					}
				}
				settings.Role = new bool?(true);
				settings.TotalLinkCredit = (uint)this.prefetchCount;
				settings.AutoSendFlow = this.prefetchCount > 0;
				Source source = new Source()
				{
					Address = address,
					FilterSet = filterSet
				};
				settings.Source = source;
				settings.SettleType = (this.receiveMode == ReceiveMode.PeekLock ? SettleMode.SettleOnDispose : SettleMode.SettleOnSend);
				if (this.epoch.HasValue)
				{
					settings.AddProperty(ClientConstants.AttachEpoch, this.epoch);
				}
			}
		}

		private sealed class OpenSendEntityAsyncResult : AmqpMessagingFactory.OpenEntityAsyncResult
		{
			public OpenSendEntityAsyncResult(AmqpMessagingFactory factory, MessageClientEntity clientEntity, string entityName, MessagingEntityType? entityType, TimeSpan timeout, AsyncCallback callback, object state) : base(factory, clientEntity, entityName, entityType, new string[] { "Send" }, timeout, callback, state)
			{
				base.Start();
			}

			protected override void UpdateLinkSettings(AmqpLinkSettings settings, string address)
			{
				settings.Role = new bool?(false);
				settings.InitialDeliveryCount = new uint?(0);
				settings.Target = new Target()
				{
					Address = address
				};
			}
		}

		private class RedirectionInfo
		{
			public string EntityName;

			public AmqpMessagingFactory.ConnectInfo ConnectionInfo;

			public RedirectionInfo()
			{
			}
		}
	}
}