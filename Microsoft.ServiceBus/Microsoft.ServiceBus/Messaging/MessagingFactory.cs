using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging.Configuration;
using Microsoft.ServiceBus.PerformanceCounters;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public abstract class MessagingFactory : MessageClientEntity
	{
		internal TrackingContext instanceTrackingContext;

		private int prefetchCount;

		private MessagingFactorySettings settings;

		private IPairedNamespaceFactory pairedNamespaceFactory;

		private bool hasPrefetchCountChanged;

		public Uri Address
		{
			get;
			internal set;
		}

		internal Microsoft.ServiceBus.Messaging.FaultInjectionInfo FaultInjectionInfo
		{
			get;
			set;
		}

		internal TrackingContext InstanceTrackingContext
		{
			get
			{
				if (this.instanceTrackingContext == null)
				{
					this.instanceTrackingContext = TrackingContext.GetInstance(Guid.NewGuid());
				}
				return this.instanceTrackingContext;
			}
		}

		private bool IsPaired
		{
			get;
			set;
		}

		public IEnumerable<Uri> NamespaceEndpoints
		{
			get;
			internal set;
		}

		internal Microsoft.ServiceBus.Messaging.OpenOnceManager OpenOnceManager
		{
			get;
			private set;
		}

		internal override TimeSpan OperationTimeout
		{
			get
			{
				return this.settings.OperationTimeout;
			}
		}

		internal PairedNamespaceOptions Options
		{
			get;
			set;
		}

		internal IPairedNamespaceFactory PairedNamespaceFactory
		{
			get
			{
				return this.pairedNamespaceFactory;
			}
			private set
			{
				this.pairedNamespaceFactory = value;
			}
		}

		public virtual int PrefetchCount
		{
			get
			{
				return this.prefetchCount;
			}
			set
			{
				if (value < 0)
				{
					throw FxTrace.Exception.ArgumentOutOfRange("PrefetchCount", value, SRClient.ArgumentOutOfRange(0, 2147483647));
				}
				this.prefetchCount = value;
				this.hasPrefetchCountChanged = true;
			}
		}

		internal abstract IServiceBusSecuritySettings ServiceBusSecuritySettings
		{
			get;
		}

		internal MessagingFactory()
		{
			this.settings = new MessagingFactorySettings();
			this.OpenOnceManager = new Microsoft.ServiceBus.Messaging.OpenOnceManager(this);
			base.ClientEntityManager = new MessageClientEntityManager();
		}

		internal MessageSession AcceptMessageSession(string entityName, string sessionId, ReceiveMode receiveMode)
		{
			return this.AcceptMessageSession(entityName, sessionId, receiveMode, this.OperationTimeout, this.OperationTimeout);
		}

		internal MessageSession AcceptMessageSession(string entityName, string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout)
		{
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			MessagingFactory.AcceptMessageSessionAsyncResult acceptMessageSessionAsyncResult = new MessagingFactory.AcceptMessageSessionAsyncResult(this, entityName, sessionId, receiveMode, serverWaitTime, timeout, null, null);
			try
			{
				acceptMessageSessionAsyncResult.RunSynchronously();
			}
			finally
			{
				if (acceptMessageSessionAsyncResult.IsScaledReceive)
				{
					MessagingPerformanceCounters.DecrementPendingAcceptMessageSessionByNamespaceCount(this.Address, 1);
				}
			}
			return acceptMessageSessionAsyncResult.MessageSession;
		}

		public MessageSession AcceptMessageSession()
		{
			return this.AcceptMessageSession(this.OperationTimeout);
		}

		public MessageSession AcceptMessageSession(TimeSpan serverWaitTime)
		{
			return this.AcceptMessageSession(null, null, ReceiveMode.PeekLock, serverWaitTime, (serverWaitTime > this.OperationTimeout ? serverWaitTime : this.OperationTimeout));
		}

		public Task<MessageSession> AcceptMessageSessionAsync()
		{
			return TaskHelpers.CreateTask<MessageSession>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginAcceptMessageSession), new Func<IAsyncResult, MessageSession>(this.EndAcceptMessageSession));
		}

		public Task<MessageSession> AcceptMessageSessionAsync(TimeSpan timeout)
		{
			return TaskHelpers.CreateTask<MessageSession>((AsyncCallback c, object s) => this.BeginAcceptMessageSession(timeout, c, s), new Func<IAsyncResult, MessageSession>(this.EndAcceptMessageSession));
		}

		public IAsyncResult BeginAcceptMessageSession(AsyncCallback callback, object state)
		{
			return this.BeginAcceptMessageSession(this.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginAcceptMessageSession(TimeSpan serverWaitTime, AsyncCallback callback, object state)
		{
			return this.BeginAcceptMessageSession(null, null, ReceiveMode.PeekLock, serverWaitTime, (serverWaitTime > this.OperationTimeout ? serverWaitTime : this.OperationTimeout), callback, state);
		}

		internal IAsyncResult BeginAcceptMessageSession(string entityName, string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (!this.OpenOnceManager.ShouldOpen)
			{
				return (new MessagingFactory.AcceptMessageSessionAsyncResult(this, entityName, sessionId, receiveMode, serverWaitTime, timeout, callback, state)).Start();
			}
			return this.OpenOnceManager.Begin<MessageSession>(callback, state, (AsyncCallback c, object s) => (new MessagingFactory.AcceptMessageSessionAsyncResult(this, entityName, sessionId, receiveMode, serverWaitTime, timeout, c, s)).Start(), new Func<IAsyncResult, MessageSession>(MessagingFactory.AcceptMessageSessionAsyncResult.End));
		}

		public static IAsyncResult BeginCreate(string address, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(new Uri(address), callback, state);
		}

		public static IAsyncResult BeginCreate(IEnumerable<string> addresses, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(addresses, callback, state);
		}

		public static IAsyncResult BeginCreate(Uri address, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(address, (TokenProvider)null, callback, state);
		}

		public static IAsyncResult BeginCreate(IEnumerable<Uri> addresses, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(addresses, (TokenProvider)null, callback, state);
		}

		public static IAsyncResult BeginCreate(string address, TokenProvider tokenProvider, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(new Uri(address), tokenProvider, callback, state);
		}

		public static IAsyncResult BeginCreate(IEnumerable<string> addresses, TokenProvider tokenProvider, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(MessagingUtilities.GetUriList(addresses), tokenProvider, callback, state);
		}

		public static IAsyncResult BeginCreate(Uri address, TokenProvider tokenProvider, AsyncCallback callback, object state)
		{
			MessagingFactorySettings messagingFactorySetting = new MessagingFactorySettings()
			{
				TokenProvider = tokenProvider
			};
			return MessagingFactory.BeginCreate(address, messagingFactorySetting, callback, state);
		}

		public static IAsyncResult BeginCreate(IEnumerable<Uri> addresses, TokenProvider tokenProvider, AsyncCallback callback, object state)
		{
			MessagingFactorySettings messagingFactorySetting = new MessagingFactorySettings()
			{
				TokenProvider = tokenProvider
			};
			return MessagingFactory.BeginCreate(addresses, messagingFactorySetting, callback, state);
		}

		public static IAsyncResult BeginCreate(string address, MessagingFactorySettings factorySettings, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(new Uri(address), factorySettings, callback, state);
		}

		public static IAsyncResult BeginCreate(IEnumerable<string> addresses, MessagingFactorySettings factorySettings, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(MessagingUtilities.GetUriList(addresses), factorySettings, callback, state);
		}

		public static IAsyncResult BeginCreate(Uri address, MessagingFactorySettings factorySettings, AsyncCallback callback, object state)
		{
			if (factorySettings == null)
			{
				throw FxTrace.Exception.ArgumentNull("factorySettings");
			}
			return MessagingFactory.BeginCreate(address, factorySettings, factorySettings.OperationTimeout, callback, state);
		}

		public static IAsyncResult BeginCreate(IEnumerable<Uri> addresses, MessagingFactorySettings factorySettings, AsyncCallback callback, object state)
		{
			if (factorySettings == null)
			{
				throw FxTrace.Exception.ArgumentNull("factorySettings");
			}
			return MessagingFactory.BeginCreate(addresses, factorySettings, factorySettings.OperationTimeout, callback, state);
		}

		internal static IAsyncResult BeginCreate(Uri address, MessagingFactorySettings factorySettings, TimeSpan timeout, AsyncCallback callback, object state)
		{
			List<Uri> uris = new List<Uri>()
			{
				address
			};
			return MessagingFactory.BeginCreate(uris, factorySettings, timeout, callback, state);
		}

		internal static IAsyncResult BeginCreate(IEnumerable<string> addresses, MessagingFactorySettings factorySettings, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return MessagingFactory.BeginCreate(MessagingUtilities.GetUriList(addresses), factorySettings, timeout, callback, state);
		}

		internal static IAsyncResult BeginCreate(IEnumerable<Uri> addresses, MessagingFactorySettings factorySettings, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (factorySettings == null)
			{
				throw FxTrace.Exception.ArgumentNull("factorySettings");
			}
			if (!factorySettings.NetMessagingTransportSettings.GatewayMode)
			{
				MessagingUtilities.ThrowIfNullAddressesOrPathExists(addresses, "addresses");
			}
			else if (addresses == null)
			{
				throw FxTrace.Exception.ArgumentNull("addresses");
			}
			return new MessagingFactory.CreateMessagingFactoryAsyncResult(factorySettings, addresses, callback, state);
		}

		public IAsyncResult BeginCreateMessageReceiver(string entityPath, AsyncCallback callback, object state)
		{
			return this.BeginCreateMessageReceiver(entityPath, ReceiveMode.PeekLock, callback, state);
		}

		public IAsyncResult BeginCreateMessageReceiver(string entityPath, ReceiveMode receiveMode, AsyncCallback callback, object state)
		{
			if (!this.OpenOnceManager.ShouldOpen)
			{
				MessagingFactory.CreateMessageReceiverAsyncResult createMessageReceiverAsyncResult = new MessagingFactory.CreateMessageReceiverAsyncResult(this, entityPath, receiveMode, this.settings.OperationTimeout, callback, state);
				return createMessageReceiverAsyncResult.Start();
			}
			return this.OpenOnceManager.Begin<MessagingFactory.CreateMessageReceiverAsyncResult>(callback, state, (AsyncCallback c, object s) => (new MessagingFactory.CreateMessageReceiverAsyncResult(this, entityPath, receiveMode, this.settings.OperationTimeout, c, s)).Start(), new Func<IAsyncResult, MessagingFactory.CreateMessageReceiverAsyncResult>(AsyncResult<MessagingFactory.CreateMessageReceiverAsyncResult>.End));
		}

		internal IAsyncResult BeginCreateMessageReceiver(string entityPath, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (!this.OpenOnceManager.ShouldOpen)
			{
				return (new MessagingFactory.CreateMessageReceiverAsyncResult(this, entityPath, receiveMode, timeout, callback, state)).Start();
			}
			return this.OpenOnceManager.Begin<MessagingFactory.CreateMessageReceiverAsyncResult>(callback, state, (AsyncCallback c, object s) => (new MessagingFactory.CreateMessageReceiverAsyncResult(this, entityPath, receiveMode, timeout, c, s)).Start(), new Func<IAsyncResult, MessagingFactory.CreateMessageReceiverAsyncResult>(AsyncResult<MessagingFactory.CreateMessageReceiverAsyncResult>.End));
		}

		public IAsyncResult BeginCreateMessageSender(string entityPath, AsyncCallback callback, object state)
		{
			return this.BeginCreateMessageSender(null, entityPath, this.settings.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginCreateMessageSender(string transferDestinationEntityPath, string viaEntityPath, AsyncCallback callback, object state)
		{
			return this.BeginCreateMessageSender(transferDestinationEntityPath, viaEntityPath, this.settings.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginCreateMessageSender(string entityPath, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginCreateMessageSender(null, entityPath, timeout, callback, state);
		}

		internal IAsyncResult BeginCreateMessageSender(string transferDestinationEntityPath, string viaEntityPath, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.BeginCreateMessageSender(transferDestinationEntityPath, viaEntityPath, true, timeout, callback, state);
		}

		internal IAsyncResult BeginCreateMessageSender(string transferDestinationEntityPath, string viaEntityPath, bool tryPairSender, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			if (!this.OpenOnceManager.ShouldOpen)
			{
				MessagingFactory.CreateMessageSenderAsyncResult createMessageSenderAsyncResult = new MessagingFactory.CreateMessageSenderAsyncResult(this, viaEntityPath, transferDestinationEntityPath, tryPairSender, timeout, callback, state);
				asyncResult = createMessageSenderAsyncResult.Start();
			}
			else
			{
				asyncResult = this.OpenOnceManager.Begin<MessagingFactory.CreateMessageSenderAsyncResult>(callback, state, (AsyncCallback c, object s) => (new MessagingFactory.CreateMessageSenderAsyncResult(this, viaEntityPath, transferDestinationEntityPath, tryPairSender, timeout, c, s)).Start(), new Func<IAsyncResult, MessagingFactory.CreateMessageSenderAsyncResult>(AsyncResult<MessagingFactory.CreateMessageSenderAsyncResult>.End));
			}
			return asyncResult;
		}

		private IAsyncResult BeginCreatePartitionReceiver(string entityPath, string consumerGroupName, ReceiveMode userRequestedMode, string partitionId, string offset, bool offsetInclusive, long? epoch, AsyncCallback callback, object state)
		{
			return (new MessagingFactory.CreatePartitionReceiverAsyncResult(this, entityPath, consumerGroupName, userRequestedMode, partitionId, offset, offsetInclusive, epoch, callback, state)).Start();
		}

		private IAsyncResult BeginCreatePartitionReceiver(string entityPath, string consumerGroupName, ReceiveMode mode, string partitionId, DateTime startTime, long? epoch, AsyncCallback callback, object state)
		{
			return (new MessagingFactory.CreatePartitionReceiverAsyncResult(this, entityPath, consumerGroupName, mode, partitionId, startTime, epoch, callback, state)).Start();
		}

		private IAsyncResult BeginCreatePartitionSender(string entityPath, string paritionId, AsyncCallback callback, object state)
		{
			return (new MessagingFactory.CreatePartitionSenderAsyncResult(this, entityPath, paritionId, callback, state)).Start();
		}

		public IAsyncResult BeginPairNamespace(PairedNamespaceOptions options, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (options == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("options"), null);
			}
			IPairedNamespaceFactory pairedNamespaceFactory = options.CreatePairedNamespaceFactory();
			if (pairedNamespaceFactory == null)
			{
				throw Fx.Exception.AsError(new ArgumentException("options"), null);
			}
			if (options.SecondaryMessagingFactory == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("options.MessagingFactory"), null);
			}
			if (options.SecondaryNamespaceManager == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("options.NamespaceManager"), null);
			}
			if (Uri.Compare(this.Address, options.SecondaryMessagingFactory.Address, UriComponents.HostAndPort, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
			{
				throw Fx.Exception.AsError(new ArgumentException(SRClient.PairedNamespacePrimaryAndSecondaryEqual, "options"), null);
			}
			if (this.Options != null)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.PairedNamespaceOnlyCallOnce), null);
			}
			if (options.SecondaryMessagingFactory != null && options.SecondaryMessagingFactory.Options != null)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.PairedNamespaceMessagingFactoryInOptionsAlreadyPaired), null);
			}
			if (options.SecondaryMessagingFactory != null && options.SecondaryMessagingFactory.IsPaired)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.PairedNamespaceMessagingFactoryAlreadyPaired), null);
			}
			this.Options = options;
			this.PairedNamespaceFactory = pairedNamespaceFactory;
			this.Options.SecondaryMessagingFactory.IsPaired = true;
			this.Options.PrimaryMessagingFactory = this;
			return new MessagingFactory.PairNamespaceAsyncResult(this, timeout, callback, state);
		}

		private static void CheckEventHubValidTransportType(MessagingFactorySettings settings)
		{
			if (settings.TransportType != TransportType.Amqp)
			{
				throw Fx.Exception.AsError(new NotSupportedException(SRClient.EventHubUnsupportedTransport(settings.TransportType)), null);
			}
		}

		internal static void CheckValidEntityName(string entityName)
		{
			MessagingFactory.CheckValidEntityName(entityName, Constants.MaximumEntityNameLength);
		}

		internal static void CheckValidEntityName(string entityName, int maxEntityNameLength)
		{
			MessagingFactory.CheckValidEntityName(entityName, maxEntityNameLength, true);
		}

		internal static void CheckValidEntityName(string entityName, int maxEntityNameLength, bool allowSeparator)
		{
			MessagingFactory.CheckValidEntityName(entityName, maxEntityNameLength, allowSeparator, "entityName");
		}

		internal static void CheckValidEntityName(string entityName, int maxEntityNameLength, bool allowSeparator, string paramName)
		{
			if (string.IsNullOrWhiteSpace(entityName))
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty(paramName);
			}
			string str = entityName.Replace("\\", "/");
			if (str.Length > maxEntityNameLength)
			{
				throw FxTrace.Exception.ArgumentOutOfRange(paramName, str, SRClient.EntityNameLengthExceedsLimit(str, maxEntityNameLength));
			}
			if (str.StartsWith("/", StringComparison.OrdinalIgnoreCase) || str.EndsWith("/", StringComparison.OrdinalIgnoreCase))
			{
				throw FxTrace.Exception.Argument(paramName, SRClient.InvalidEntityNameFormatWithSlash(str));
			}
			if (!allowSeparator && str.Contains("/"))
			{
				throw FxTrace.Exception.Argument(paramName, SRClient.InvalidCharacterInEntityName("/", str));
			}
			MessagingUtilities.CheckUriSchemeKey(entityName, paramName);
		}

		internal void ClearPairing()
		{
			if (this.Options != null)
			{
				this.Options.ClearPairing();
				if (this.Options.SecondaryMessagingFactory != null)
				{
					this.Options.SecondaryMessagingFactory.IsPaired = false;
				}
				this.Options = null;
			}
			this.PairedNamespaceFactory = null;
		}

		public static MessagingFactory Create()
		{
			return (new KeyValueConfigurationManager()).CreateMessagingFactory(false);
		}

		public static MessagingFactory Create(string address)
		{
			return MessagingFactory.Create(new Uri(address));
		}

		public static MessagingFactory Create(IEnumerable<string> addresses)
		{
			return MessagingFactory.Create(addresses, (TokenProvider)null);
		}

		public static MessagingFactory Create(Uri address)
		{
			return MessagingFactory.Create(address, (TokenProvider)null);
		}

		public static MessagingFactory Create(IEnumerable<Uri> addresses)
		{
			return MessagingFactory.Create(addresses, (TokenProvider)null);
		}

		public static MessagingFactory Create(string address, TokenProvider tokenProvider)
		{
			return MessagingFactory.Create(new Uri(address), tokenProvider);
		}

		public static MessagingFactory Create(IEnumerable<string> addresses, TokenProvider tokenProvider)
		{
			return MessagingFactory.EndCreate(MessagingFactory.BeginCreate(addresses, tokenProvider, null, null));
		}

		public static MessagingFactory Create(Uri address, TokenProvider tokenProvider)
		{
			return MessagingFactory.EndCreate(MessagingFactory.BeginCreate(address, tokenProvider, null, null));
		}

		public static MessagingFactory Create(IEnumerable<Uri> addresses, TokenProvider tokenProvider)
		{
			return MessagingFactory.EndCreate(MessagingFactory.BeginCreate(addresses, tokenProvider, null, null));
		}

		public static MessagingFactory Create(string address, MessagingFactorySettings factorySettings)
		{
			return MessagingFactory.Create(new Uri(address), factorySettings);
		}

		public static MessagingFactory Create(IEnumerable<string> addresses, MessagingFactorySettings factorySettings)
		{
			return MessagingFactory.Create(MessagingUtilities.GetUriList(addresses), factorySettings);
		}

		public static MessagingFactory Create(Uri address, MessagingFactorySettings factorySettings)
		{
			if (factorySettings == null)
			{
				throw FxTrace.Exception.ArgumentNull("factorySettings");
			}
			return MessagingFactory.EndCreate(MessagingFactory.BeginCreate(address, factorySettings, factorySettings.OperationTimeout, null, null));
		}

		public static MessagingFactory Create(IEnumerable<Uri> addresses, MessagingFactorySettings factorySettings)
		{
			if (factorySettings == null)
			{
				throw FxTrace.Exception.ArgumentNull("factorySettings");
			}
			return MessagingFactory.EndCreate(MessagingFactory.BeginCreate(addresses, factorySettings, factorySettings.OperationTimeout, null, null));
		}

		internal static MessagingFactory Create(Uri address, MessagingFactorySettings factorySettings, TimeSpan timeout)
		{
			return MessagingFactory.EndCreate(MessagingFactory.BeginCreate(address, factorySettings, timeout, null, null));
		}

		internal static MessagingFactory Create(IEnumerable<Uri> addresses, MessagingFactorySettings factorySettings, TimeSpan timeout)
		{
			return MessagingFactory.EndCreate(MessagingFactory.BeginCreate(addresses, factorySettings, timeout, null, null));
		}

		public static Task<MessagingFactory> CreateAsync(string address)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(address, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(IEnumerable<string> addresses)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(addresses, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(Uri address)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(address, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(IEnumerable<Uri> addresses)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(addresses, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(string address, TokenProvider tokenProvider)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(address, tokenProvider, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(IEnumerable<string> addresses, TokenProvider tokenProvider)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(addresses, tokenProvider, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(Uri address, TokenProvider tokenProvider)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(address, tokenProvider, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(IEnumerable<Uri> addresses, TokenProvider tokenProvider)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(addresses, tokenProvider, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(string address, MessagingFactorySettings factorySettings)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(address, factorySettings, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(IEnumerable<string> addresses, MessagingFactorySettings factorySettings)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(addresses, factorySettings, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(Uri address, MessagingFactorySettings factorySettings)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(address, factorySettings, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public static Task<MessagingFactory> CreateAsync(IEnumerable<Uri> addresses, MessagingFactorySettings factorySettings)
		{
			return TaskHelpers.CreateTask<MessagingFactory>((AsyncCallback c, object s) => MessagingFactory.BeginCreate(addresses, factorySettings, c, s), new Func<IAsyncResult, MessagingFactory>(MessagingFactory.EndCreate));
		}

		public EventHubClient CreateEventHubClient(string path)
		{
			base.ThrowIfDisposed();
			MessagingFactory.CheckEventHubValidTransportType(this.settings);
			MessagingFactory.CheckValidEntityName(path, 260);
			MessagingUtilities.ThrowIfContainsSubQueueName(path);
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			EventHubClient prefetchCount = this.OnCreateEventHubClient(path);
			if (this.hasPrefetchCountChanged)
			{
				prefetchCount.PrefetchCount = this.PrefetchCount;
			}
			base.ClientEntityManager.Add(prefetchCount);
			return prefetchCount;
		}

		public static MessagingFactory CreateFromConnectionString(string connectionString)
		{
			return (new KeyValueConfigurationManager(connectionString)).CreateMessagingFactory(false);
		}

		public MessageReceiver CreateMessageReceiver(string entityPath)
		{
			return this.CreateMessageReceiver(entityPath, ReceiveMode.PeekLock);
		}

		public MessageReceiver CreateMessageReceiver(string entityPath, ReceiveMode receiveMode)
		{
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			MessagingFactory.CreateMessageReceiverAsyncResult createMessageReceiverAsyncResult = new MessagingFactory.CreateMessageReceiverAsyncResult(this, entityPath, receiveMode, this.settings.OperationTimeout, null, null);
			createMessageReceiverAsyncResult.RunSynchronously();
			return createMessageReceiverAsyncResult.Receiver;
		}

		internal MessageReceiver CreateMessageReceiver(string entityPath, ReceiveMode receiveMode, TimeSpan timeout)
		{
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			MessagingFactory.CreateMessageReceiverAsyncResult createMessageReceiverAsyncResult = new MessagingFactory.CreateMessageReceiverAsyncResult(this, entityPath, receiveMode, timeout, null, null);
			createMessageReceiverAsyncResult.RunSynchronously();
			return createMessageReceiverAsyncResult.Receiver;
		}

		public Task<MessageReceiver> CreateMessageReceiverAsync(string entityPath)
		{
			return TaskHelpers.CreateTask<MessageReceiver>((AsyncCallback c, object s) => this.BeginCreateMessageReceiver(entityPath, c, s), new Func<IAsyncResult, MessageReceiver>(this.EndCreateMessageReceiver));
		}

		public Task<MessageReceiver> CreateMessageReceiverAsync(string entityPath, ReceiveMode receiveMode)
		{
			return TaskHelpers.CreateTask<MessageReceiver>((AsyncCallback c, object s) => this.BeginCreateMessageReceiver(entityPath, receiveMode, c, s), new Func<IAsyncResult, MessageReceiver>(this.EndCreateMessageReceiver));
		}

		public MessageSender CreateMessageSender(string entityPath)
		{
			return this.CreateMessageSender(null, entityPath, this.settings.OperationTimeout);
		}

		internal MessageSender CreateMessageSender(string entityPath, TimeSpan timeout)
		{
			return this.CreateMessageSender(null, entityPath, timeout);
		}

		public MessageSender CreateMessageSender(string transferDestinationEntityPath, string viaEntityPath)
		{
			return this.CreateMessageSender(transferDestinationEntityPath, viaEntityPath, this.settings.OperationTimeout);
		}

		internal MessageSender CreateMessageSender(string transfserDestinationEntityPath, string viaEntityPath, TimeSpan timeout)
		{
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			MessagingFactory.CreateMessageSenderAsyncResult createMessageSenderAsyncResult = new MessagingFactory.CreateMessageSenderAsyncResult(this, viaEntityPath, transfserDestinationEntityPath, true, timeout, null, null);
			createMessageSenderAsyncResult.RunSynchronously();
			return createMessageSenderAsyncResult.Sender;
		}

		public Task<MessageSender> CreateMessageSenderAsync(string entityPath)
		{
			return TaskHelpers.CreateTask<MessageSender>((AsyncCallback c, object s) => this.BeginCreateMessageSender(entityPath, c, s), new Func<IAsyncResult, MessageSender>(this.EndCreateMessageSender));
		}

		public Task<MessageSender> CreateMessageSenderAsync(string transferDestinationEntityPath, string viaEntityPath)
		{
			return TaskHelpers.CreateTask<MessageSender>((AsyncCallback c, object s) => this.BeginCreateMessageSender(transferDestinationEntityPath, viaEntityPath, c, s), new Func<IAsyncResult, MessageSender>(this.EndCreateMessageSender));
		}

		public QueueClient CreateQueueClient(string path)
		{
			return this.CreateQueueClient(path, ReceiveMode.PeekLock);
		}

		public QueueClient CreateQueueClient(string path, ReceiveMode receiveMode)
		{
			base.ThrowIfDisposed();
			if (string.IsNullOrWhiteSpace(path))
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty(path);
			}
			string str = Constants.SupportedSubQueueNames.Find((string s) => path.EndsWith(s, StringComparison.OrdinalIgnoreCase));
			int length = 260;
			if (!string.IsNullOrWhiteSpace(str))
			{
				length = length + str.Length + 1;
			}
			MessagingFactory.CheckValidEntityName(path, length);
			MessagingUtilities.ThrowIfInvalidSubQueueNameString(path, "path");
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			QueueClient queueClient = this.OnCreateQueueClient(path, receiveMode);
			base.ClientEntityManager.Add(queueClient);
			return queueClient;
		}

		internal Task<MessageReceiver> CreateReceiverAsync(string entityPath, string consumerGroupName, ReceiveMode userRequestedMode, string partitionId, string offset, bool offsetInclusive, long? epoch)
		{
			return TaskHelpers.CreateTask<MessageReceiver>((AsyncCallback c, object s) => this.BeginCreatePartitionReceiver(entityPath, consumerGroupName, userRequestedMode, partitionId, offset, offsetInclusive, epoch, c, s), new Func<IAsyncResult, MessageReceiver>(this.EndCreatePartitionReceiver));
		}

		internal Task<MessageReceiver> CreateReceiverAsync(string entityPath, string consumerGroupName, ReceiveMode mode, string partitionId, DateTime startTime, long? epoch)
		{
			return TaskHelpers.CreateTask<MessageReceiver>((AsyncCallback c, object s) => this.BeginCreatePartitionReceiver(entityPath, consumerGroupName, mode, partitionId, startTime, epoch, c, s), new Func<IAsyncResult, MessageReceiver>(this.EndCreatePartitionReceiver));
		}

		internal Task<MessageSender> CreateSenderAsync(string entityPath, string paritionId)
		{
			return TaskHelpers.CreateTask<MessageSender>((AsyncCallback c, object s) => this.BeginCreatePartitionSender(entityPath, paritionId, c, s), new Func<IAsyncResult, MessageSender>(this.EndCreatePartitionSender));
		}

		public SubscriptionClient CreateSubscriptionClient(string topicPath, string name)
		{
			return this.CreateSubscriptionClient(topicPath, name, ReceiveMode.PeekLock);
		}

		public SubscriptionClient CreateSubscriptionClient(string topicPath, string name, ReceiveMode receiveMode)
		{
			base.ThrowIfDisposed();
			MessagingFactory.CheckValidEntityName(topicPath, 260, true, "topicPath");
			if (string.IsNullOrWhiteSpace(name))
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty(name);
			}
			MessagingUtilities.ThrowIfContainsSubQueueName(topicPath);
			MessagingUtilities.ThrowIfInvalidSubQueueNameString(name, "name");
			string str = Constants.SupportedSubQueueNames.Find((string s) => name.EndsWith(s, StringComparison.OrdinalIgnoreCase));
			int length = 50;
			bool flag = false;
			if (!string.IsNullOrWhiteSpace(str))
			{
				length = length + str.Length + 1;
				flag = true;
			}
			MessagingFactory.CheckValidEntityName(name, length, flag, "name");
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			SubscriptionClient subscriptionClient = this.OnCreateSubscriptionClient(topicPath, name, receiveMode);
			base.ClientEntityManager.Add(subscriptionClient);
			return subscriptionClient;
		}

		internal SubscriptionClient CreateSubscriptionClient(string subscriptionPath, ReceiveMode receiveMode)
		{
			base.ThrowIfDisposed();
			if (string.IsNullOrWhiteSpace(subscriptionPath))
			{
				throw FxTrace.Exception.ArgumentNullOrEmpty(subscriptionPath);
			}
			MessagingUtilities.ThrowIfInvalidSubQueueNameString(subscriptionPath, "subscriptionPath");
			string str = Constants.SupportedSubQueueNames.Find((string s) => subscriptionPath.EndsWith(s, StringComparison.OrdinalIgnoreCase));
			int length = 310;
			bool flag = false;
			if (!string.IsNullOrWhiteSpace(str))
			{
				length = length + str.Length + 1;
				flag = true;
			}
			MessagingFactory.CheckValidEntityName(subscriptionPath, length, flag, "subscriptionPath");
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			SubscriptionClient subscriptionClient = this.OnCreateSubscriptionClient(subscriptionPath, receiveMode);
			base.ClientEntityManager.Add(subscriptionClient);
			return subscriptionClient;
		}

		public TopicClient CreateTopicClient(string path)
		{
			base.ThrowIfDisposed();
			MessagingFactory.CheckValidEntityName(path, 260);
			MessagingUtilities.ThrowIfContainsSubQueueName(path);
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			TopicClient topicClient = this.OnCreateTopicClient(path);
			base.ClientEntityManager.Add(topicClient);
			return topicClient;
		}

		internal VolatileTopicClient CreateVolatileTopicClient(string path)
		{
			string str = Guid.NewGuid().ToString();
			return this.CreateVolatileTopicClient(path, str, null);
		}

		internal VolatileTopicClient CreateVolatileTopicClient(string path, Filter filter)
		{
			string str = Guid.NewGuid().ToString();
			return this.CreateVolatileTopicClient(path, str, filter);
		}

		internal VolatileTopicClient CreateVolatileTopicClient(string path, string clientId)
		{
			return this.CreateVolatileTopicClient(path, clientId, null);
		}

		internal VolatileTopicClient CreateVolatileTopicClient(string path, string clientId, Filter filter)
		{
			MessagingFactory.CheckValidEntityName(path, 260);
			MessagingUtilities.ThrowIfContainsSubQueueName(path);
			if (this.OpenOnceManager.ShouldOpen)
			{
				this.OpenOnceManager.Open();
			}
			VolatileTopicClient volatileTopicClient = this.OnCreateVolatileTopicClient(path, clientId, filter);
			base.ClientEntityManager.Add(volatileTopicClient);
			return volatileTopicClient;
		}

		public MessageSession EndAcceptMessageSession(IAsyncResult result)
		{
			if (Microsoft.ServiceBus.Messaging.OpenOnceManager.ShouldEnd<MessageSession>(result))
			{
				return Microsoft.ServiceBus.Messaging.OpenOnceManager.End<MessageSession>(result);
			}
			return MessagingFactory.AcceptMessageSessionAsyncResult.End(result);
		}

		public static MessagingFactory EndCreate(IAsyncResult result)
		{
			return MessagingFactory.CreateMessagingFactoryAsyncResult.End(result);
		}

		public MessageReceiver EndCreateMessageReceiver(IAsyncResult result)
		{
			if (Microsoft.ServiceBus.Messaging.OpenOnceManager.ShouldEnd<MessagingFactory.CreateMessageReceiverAsyncResult>(result))
			{
				return Microsoft.ServiceBus.Messaging.OpenOnceManager.End<MessagingFactory.CreateMessageReceiverAsyncResult>(result).Receiver;
			}
			return AsyncResult<MessagingFactory.CreateMessageReceiverAsyncResult>.End(result).Receiver;
		}

		public MessageSender EndCreateMessageSender(IAsyncResult result)
		{
			MessageSender messageSender = null;
			messageSender = (!Microsoft.ServiceBus.Messaging.OpenOnceManager.ShouldEnd<MessagingFactory.CreateMessageSenderAsyncResult>(result) ? AsyncResult<MessagingFactory.CreateMessageSenderAsyncResult>.End(result).Sender : Microsoft.ServiceBus.Messaging.OpenOnceManager.End<MessagingFactory.CreateMessageSenderAsyncResult>(result).Sender);
			return messageSender;
		}

		private MessageReceiver EndCreatePartitionReceiver(IAsyncResult result)
		{
			return AsyncResult<MessagingFactory.CreatePartitionReceiverAsyncResult>.End(result).MessageReceiver;
		}

		private MessageSender EndCreatePartitionSender(IAsyncResult result)
		{
			return AsyncResult<MessagingFactory.CreatePartitionSenderAsyncResult>.End(result).MessageSender;
		}

		public void EndPairNamespace(IAsyncResult result)
		{
			AsyncResult<MessagingFactory.PairNamespaceAsyncResult>.End(result);
		}

		public MessagingFactorySettings GetSettings()
		{
			return this.settings.Clone();
		}

		protected override void OnAbort()
		{
			MessagingPerformanceCounters.DecrementMessagingFactoryCount(this.Address, 1);
			base.ClientEntityManager.Abort();
		}

		protected virtual MessageSession OnAcceptMessageSession(ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout)
		{
			return this.OnEndAcceptMessageSession(this.OnBeginAcceptMessageSession(receiveMode, serverWaitTime, timeout, null, null));
		}

		protected virtual MessageSession OnAcceptSessionReceiver(string entityName, string sessionId, ReceiveMode receiveMode, TimeSpan timeout)
		{
			return this.OnEndAcceptSessionReceiver(this.OnBeginAcceptSessionReceiver(entityName, sessionId, receiveMode, timeout, null, null));
		}

		protected abstract IAsyncResult OnBeginAcceptMessageSession(ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginAcceptSessionReceiver(string entityName, string sessionId, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new MessagingFactory.CloseFactoryAsyncResult(this, timeout, callback, state);
		}

		protected abstract IAsyncResult OnBeginCreateMessageReceiver(string entityName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state);

		protected virtual IAsyncResult OnBeginCreateMessageSender(string entityName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateMessageSender(null, entityName, timeout, callback, state);
		}

		protected abstract IAsyncResult OnBeginCreateMessageSender(string transferDestinationEntityName, string viaEntityName, TimeSpan timeout, AsyncCallback callback, object state);

		protected override void OnClose(TimeSpan timeout)
		{
			this.OnEndClose(this.OnBeginClose(timeout, null, null));
		}

		protected virtual EventHubClient OnCreateEventHubClient(string path)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		protected virtual MessageReceiver OnCreateMessageReceiver(string entityName, ReceiveMode receiveMode, TimeSpan timeout)
		{
			return this.OnEndCreateMessageReceiver(this.OnBeginCreateMessageReceiver(entityName, receiveMode, timeout, null, null));
		}

		protected virtual MessageSender OnCreateMessageSender(string entityName, TimeSpan timeout)
		{
			return this.OnEndCreateMessageSender(this.OnBeginCreateMessageSender(null, entityName, timeout, null, null));
		}

		protected virtual MessageSender OnCreateMessageSender(string transferDestinationEntityName, string viaEntityName, TimeSpan timeout)
		{
			return this.OnEndCreateMessageSender(this.OnBeginCreateMessageSender(transferDestinationEntityName, viaEntityName, timeout, null, null));
		}

		protected virtual QueueClient OnCreateQueueClient(string path, ReceiveMode receiveMode)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		protected virtual SubscriptionClient OnCreateSubscriptionClient(string topicPath, string name, ReceiveMode receiveMode)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		protected virtual SubscriptionClient OnCreateSubscriptionClient(string subscriptionPath, ReceiveMode receiveMode)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		protected virtual TopicClient OnCreateTopicClient(string path)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		internal virtual VolatileTopicClient OnCreateVolatileTopicClient(string path, string clientId, Filter filter)
		{
			throw FxTrace.Exception.AsError(new NotSupportedException(), null);
		}

		protected abstract MessageSession OnEndAcceptMessageSession(IAsyncResult result);

		protected abstract MessageSession OnEndAcceptSessionReceiver(IAsyncResult result);

		protected override void OnEndClose(IAsyncResult result)
		{
			AsyncResult<MessagingFactory.CloseFactoryAsyncResult>.End(result);
		}

		protected abstract MessageReceiver OnEndCreateMessageReceiver(IAsyncResult result);

		protected abstract MessageSender OnEndCreateMessageSender(IAsyncResult result);

		public Task PairNamespaceAsync(PairedNamespaceOptions options)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginPairNamespace(options, TimeSpan.MaxValue, c, s), new Action<IAsyncResult>(this.EndPairNamespace));
		}

		private sealed class AcceptMessageSessionAsyncResult : RetryAsyncResult<MessagingFactory.AcceptMessageSessionAsyncResult>
		{
			private readonly MessagingFactory factory;

			private readonly string entityName;

			private readonly ReceiveMode receiveMode;

			private readonly string sessionId;

			private readonly EventTraceActivity relatedActivity;

			private readonly TimeSpan serverWaitTime;

			private readonly TrackingContext trackingContext;

			private TimeoutHelper retryHelper;

			public bool IsScaledReceive
			{
				get;
				private set;
			}

			public MessageSession MessageSession
			{
				get;
				private set;
			}

			public AcceptMessageSessionAsyncResult(MessagingFactory factory, string entityName, string sessionId, ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				this.sessionId = sessionId;
				this.entityName = entityName;
				this.receiveMode = receiveMode;
				this.serverWaitTime = serverWaitTime;
				this.retryHelper = new TimeoutHelper(serverWaitTime, true);
				this.factory.ThrowIfDisposed();
				if (string.IsNullOrEmpty(entityName))
				{
					this.IsScaledReceive = true;
					MessagingPerformanceCounters.IncrementPendingAcceptMessageSessionByNamespaceCount(this.factory.Address, 1);
				}
				else
				{
					MessagingFactory.CheckValidEntityName(entityName);
					this.ThrowIfContainsSubQueueName(entityName);
				}
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), this.entityName);
			}

			public static new MessageSession End(IAsyncResult r)
			{
				MessageSession messageSession;
				MessagingFactory.AcceptMessageSessionAsyncResult acceptMessageSessionAsyncResult = r as MessagingFactory.AcceptMessageSessionAsyncResult;
				try
				{
					acceptMessageSessionAsyncResult = AsyncResult<MessagingFactory.AcceptMessageSessionAsyncResult>.End(r);
					messageSession = acceptMessageSessionAsyncResult.MessageSession;
				}
				finally
				{
					if (acceptMessageSessionAsyncResult != null && acceptMessageSessionAsyncResult.factory != null && acceptMessageSessionAsyncResult.IsScaledReceive)
					{
						MessagingPerformanceCounters.DecrementPendingAcceptMessageSessionByNamespaceCount(acceptMessageSessionAsyncResult.factory.Address, 1);
					}
				}
				return messageSession;
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.AcceptMessageSessionAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				if (this.relatedActivity != null && this.relatedActivity != EventTraceActivity.Empty)
				{
					MessagingClientEtwProvider.TraceClient(() => {
					});
				}
				MessagingClientEtwProvider.TraceClient(() => {
				});
				if (!this.IsScaledReceive)
				{
					MessagingFactory.AcceptMessageSessionAsyncResult acceptMessageSessionAsyncResult = this;
					IteratorAsyncResult<MessagingFactory.AcceptMessageSessionAsyncResult>.BeginCall beginCall = (MessagingFactory.AcceptMessageSessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.OnBeginAcceptSessionReceiver(thisPtr.entityName, thisPtr.sessionId, thisPtr.receiveMode, t, c, s);
					IteratorAsyncResult<MessagingFactory.AcceptMessageSessionAsyncResult>.EndCall messageSession = (MessagingFactory.AcceptMessageSessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.MessageSession = thisPtr.factory.OnEndAcceptSessionReceiver(r);
					yield return acceptMessageSessionAsyncResult.CallAsync(beginCall, messageSession, (MessagingFactory.AcceptMessageSessionAsyncResult thisPtr, TimeSpan t) => thisPtr.MessageSession = thisPtr.factory.OnAcceptSessionReceiver(thisPtr.entityName, thisPtr.sessionId, thisPtr.receiveMode, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				}
				else
				{
					int num = 0;
					Stopwatch stopwatch = Stopwatch.StartNew();
					try
					{
						timeSpan = (this.factory.RetryPolicy.IsServerBusy ? Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
						TimeSpan timeSpan1 = timeSpan;
						if (!this.factory.RetryPolicy.IsServerBusy || !(Microsoft.ServiceBus.RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
						{
							while (true)
							{
								bool flag1 = false;
								if (timeSpan1 != TimeSpan.Zero)
								{
									yield return base.CallAsyncSleep(timeSpan1);
								}
								MessagingFactory.AcceptMessageSessionAsyncResult acceptMessageSessionAsyncResult1 = this;
								IteratorAsyncResult<MessagingFactory.AcceptMessageSessionAsyncResult>.BeginCall beginCall1 = (MessagingFactory.AcceptMessageSessionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.OnBeginAcceptMessageSession(thisPtr.receiveMode, thisPtr.serverWaitTime, t, c, s);
								IteratorAsyncResult<MessagingFactory.AcceptMessageSessionAsyncResult>.EndCall endCall = (MessagingFactory.AcceptMessageSessionAsyncResult thisPtr, IAsyncResult r) => thisPtr.MessageSession = thisPtr.factory.OnEndAcceptMessageSession(r);
								yield return acceptMessageSessionAsyncResult1.CallAsync(beginCall1, endCall, (MessagingFactory.AcceptMessageSessionAsyncResult thisPtr, TimeSpan t) => thisPtr.MessageSession = thisPtr.factory.OnAcceptMessageSession(thisPtr.receiveMode, thisPtr.serverWaitTime, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException == null)
								{
									MessagingPerformanceCounters.IncrementAcceptMessageSessionByNamespaceSuccessPerSec(this.factory.Address, 1);
									this.factory.RetryPolicy.ResetServerBusy();
								}
								else
								{
									MessagingPerformanceCounters.IncrementAcceptMessageSessionByNamespaceFailurePerSec(this.factory.Address, 1);
									MessagingPerformanceCounters.IncrementExceptionPerSec(this.factory.Address, 1, base.LastAsyncStepException);
									flag = (base.TransactionExists ? false : this.factory.RetryPolicy.ShouldRetry(this.retryHelper.RemainingTime(), num, base.LastAsyncStepException, out timeSpan1));
									flag1 = flag;
									if (flag1)
									{
										MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(this.trackingContext.Activity, this.trackingContext.TrackingId, this.factory.RetryPolicy.GetType().Name, string.Concat("AcceptMessageSessionByNamespace:", this.factory.Address.AbsoluteUri), num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
										num++;
									}
								}
								if (!flag1)
								{
									break;
								}
							}
						}
						else
						{
							string str = this.factory.RetryPolicy.ServerBusyExceptionMessage;
							yield return base.CallAsyncSleep(base.RemainingTime());
							base.Complete(new ServerBusyException(str, this.trackingContext));
							goto Label0;
						}
					}
					finally
					{
						stopwatch.Stop();
						MessagingPerformanceCounters.IncrementAcceptMessageSessionByNamespaceLatency(this.factory.Address, stopwatch.ElapsedTicks);
					}
				}
				if (base.LastAsyncStepException == null)
				{
					MessagingClientEtwProvider.TraceClient(() => {
					});
					this.factory.ClientEntityManager.Add(this.MessageSession);
				}
				else
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAcceptSessionRequestFailed(this.trackingContext.Activity, this.trackingContext.TrackingId, this.trackingContext.SystemTracker, this.entityName ?? string.Empty, this.sessionId ?? string.Empty, base.LastAsyncStepException.ToString()));
					if (!this.factory.IsClosed || base.LastAsyncStepException is OperationCanceledException)
					{
						base.Complete(base.LastAsyncStepException);
					}
					else
					{
						base.Complete(new OperationCanceledException(SRClient.MessageEntityDisposed, base.LastAsyncStepException));
					}
				}
			Label0:
				yield break;
			}

			private void ThrowIfContainsSubQueueName(string path)
			{
				if (path.Contains("$"))
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRClient.CannotCreateMessageSessionForSubQueue), null);
				}
			}
		}

		private class CloseFactoryAsyncResult : IteratorAsyncResult<MessagingFactory.CloseFactoryAsyncResult>
		{
			private readonly MessagingFactory messagingFactory;

			private readonly bool shouldDecrement;

			public CloseFactoryAsyncResult(MessagingFactory factory, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.messagingFactory = factory;
				this.shouldDecrement = !factory.IsClosed;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.CloseFactoryAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				MessagingFactory.CloseFactoryAsyncResult closeFactoryAsyncResult = this;
				IteratorAsyncResult<MessagingFactory.CloseFactoryAsyncResult>.BeginCall beginCall = (MessagingFactory.CloseFactoryAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.messagingFactory.ClientEntityManager.BeginClose(t, c, s);
				yield return closeFactoryAsyncResult.CallAsync(beginCall, (MessagingFactory.CloseFactoryAsyncResult thisPtr, IAsyncResult r) => thisPtr.messagingFactory.ClientEntityManager.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (this.shouldDecrement)
				{
					MessagingPerformanceCounters.DecrementMessagingFactoryCount(this.messagingFactory.Address, 1);
				}
				if (this.messagingFactory.PairedNamespaceFactory != null)
				{
					MessagingFactory.CloseFactoryAsyncResult closeFactoryAsyncResult1 = this;
					IteratorAsyncResult<MessagingFactory.CloseFactoryAsyncResult>.BeginCall closePairedNamesaceFactoryAsyncResult = (MessagingFactory.CloseFactoryAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new MessagingFactory.ClosePairedNamesaceFactoryAsyncResult(thisPtr.messagingFactory, t, c, s);
					yield return closeFactoryAsyncResult1.CallAsync(closePairedNamesaceFactoryAsyncResult, (MessagingFactory.CloseFactoryAsyncResult thisPtr, IAsyncResult r) => AsyncResult<MessagingFactory.ClosePairedNamesaceFactoryAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			}
		}

		private class ClosePairedNamesaceFactoryAsyncResult : IteratorAsyncResult<MessagingFactory.ClosePairedNamesaceFactoryAsyncResult>
		{
			private readonly IPairedNamespaceFactory pairedNamespaceFactory;

			public ClosePairedNamesaceFactoryAsyncResult(MessagingFactory factory, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.pairedNamespaceFactory = factory.PairedNamespaceFactory;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.ClosePairedNamesaceFactoryAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (this.pairedNamespaceFactory != null)
				{
					MessagingFactory.ClosePairedNamesaceFactoryAsyncResult closePairedNamesaceFactoryAsyncResult = this;
					IteratorAsyncResult<MessagingFactory.ClosePairedNamesaceFactoryAsyncResult>.BeginCall beginCall = (MessagingFactory.ClosePairedNamesaceFactoryAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.pairedNamespaceFactory.BeginClose(t, c, s);
					yield return closePairedNamesaceFactoryAsyncResult.CallAsync(beginCall, (MessagingFactory.ClosePairedNamesaceFactoryAsyncResult thisPtr, IAsyncResult r) => thisPtr.pairedNamespaceFactory.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				}
			}
		}

		private sealed class CreateMessageReceiverAsyncResult : IteratorAsyncResult<MessagingFactory.CreateMessageReceiverAsyncResult>
		{
			private readonly MessagingFactory factory;

			private readonly string entityName;

			private readonly ReceiveMode receiveMode;

			public MessageReceiver Receiver
			{
				get;
				private set;
			}

			public CreateMessageReceiverAsyncResult(MessagingFactory factory, string entityName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				this.entityName = entityName;
				this.receiveMode = receiveMode;
				this.factory.ThrowIfDisposed();
				MessagingFactory.CheckValidEntityName(entityName);
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.CreateMessageReceiverAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				MessagingFactory.CreateMessageReceiverAsyncResult createMessageReceiverAsyncResult = this;
				IteratorAsyncResult<MessagingFactory.CreateMessageReceiverAsyncResult>.BeginCall beginCall = (MessagingFactory.CreateMessageReceiverAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.OnBeginCreateMessageReceiver(thisPtr.entityName, thisPtr.receiveMode, t, c, s);
				IteratorAsyncResult<MessagingFactory.CreateMessageReceiverAsyncResult>.EndCall receiver = (MessagingFactory.CreateMessageReceiverAsyncResult thisPtr, IAsyncResult r) => thisPtr.Receiver = thisPtr.factory.OnEndCreateMessageReceiver(r);
				yield return createMessageReceiverAsyncResult.CallAsync(beginCall, receiver, (MessagingFactory.CreateMessageReceiverAsyncResult thisPtr, TimeSpan t) => thisPtr.Receiver = thisPtr.factory.OnCreateMessageReceiver(thisPtr.entityName, thisPtr.receiveMode, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.factory.ClientEntityManager.Add(this.Receiver);
			}
		}

		private sealed class CreateMessageSenderAsyncResult : IteratorAsyncResult<MessagingFactory.CreateMessageSenderAsyncResult>
		{
			private readonly MessagingFactory factory;

			private readonly string entityName;

			private readonly string transferDestinationEntityName;

			private readonly bool tryPairSender;

			public MessageSender Sender
			{
				get;
				private set;
			}

			public CreateMessageSenderAsyncResult(MessagingFactory factory, string entityName, string transferDestinationEntityName, bool tryPairSender, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				this.entityName = entityName;
				this.transferDestinationEntityName = transferDestinationEntityName;
				this.tryPairSender = tryPairSender;
				this.factory.ThrowIfDisposed();
				MessagingFactory.CheckValidEntityName(entityName);
				if (string.IsNullOrEmpty(transferDestinationEntityName))
				{
					MessagingUtilities.ThrowIfContainsSubQueueName(entityName);
				}
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.CreateMessageSenderAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				MessagingFactory.CreateMessageSenderAsyncResult createMessageSenderAsyncResult = this;
				IteratorAsyncResult<MessagingFactory.CreateMessageSenderAsyncResult>.BeginCall beginCall = (MessagingFactory.CreateMessageSenderAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.OnBeginCreateMessageSender(thisPtr.transferDestinationEntityName, thisPtr.entityName, t, c, s);
				IteratorAsyncResult<MessagingFactory.CreateMessageSenderAsyncResult>.EndCall sender = (MessagingFactory.CreateMessageSenderAsyncResult thisPtr, IAsyncResult r) => thisPtr.Sender = thisPtr.factory.OnEndCreateMessageSender(r);
				yield return createMessageSenderAsyncResult.CallAsync(beginCall, sender, (MessagingFactory.CreateMessageSenderAsyncResult thisPtr, TimeSpan t) => thisPtr.Sender = thisPtr.factory.OnCreateMessageSender(thisPtr.transferDestinationEntityName, thisPtr.entityName, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (this.tryPairSender && this.Sender != null)
				{
					IPairedNamespaceFactory pairedNamespaceFactory = this.factory.PairedNamespaceFactory;
					if (pairedNamespaceFactory != null)
					{
						this.Sender = pairedNamespaceFactory.CreateMessageSender(this.Sender);
					}
				}
				this.factory.ClientEntityManager.Add(this.Sender);
			}
		}

		private sealed class CreateMessagingFactoryAsyncResult : AsyncResult
		{
			private readonly static AsyncResult.AsyncCompletion createComplete;

			private readonly MessagingFactorySettings settings;

			private readonly IEnumerable<Uri> addresses;

			private MessagingFactory messagingFactory;

			static CreateMessagingFactoryAsyncResult()
			{
				MessagingFactory.CreateMessagingFactoryAsyncResult.createComplete = new AsyncResult.AsyncCompletion(MessagingFactory.CreateMessagingFactoryAsyncResult.CreateComplete);
			}

			public CreateMessagingFactoryAsyncResult(MessagingFactorySettings settings, IEnumerable<Uri> addresses, AsyncCallback callback, object state) : base(callback, state)
			{
				this.settings = settings.Clone();
				this.addresses = addresses;
				if (base.SyncContinue(this.settings.BeginCreateFactory(this.addresses, base.PrepareAsyncCompletion(MessagingFactory.CreateMessagingFactoryAsyncResult.createComplete), this)))
				{
					base.Complete(true);
				}
			}

			private static bool CreateComplete(IAsyncResult result)
			{
				MessagingFactory.CreateMessagingFactoryAsyncResult asyncState = (MessagingFactory.CreateMessagingFactoryAsyncResult)result.AsyncState;
				asyncState.messagingFactory = asyncState.settings.EndCreateFactory(result);
				asyncState.messagingFactory.hasPrefetchCountChanged = false;
				asyncState.messagingFactory.Address = asyncState.addresses.First<Uri>();
				asyncState.messagingFactory.settings = asyncState.settings.Clone();
				asyncState.messagingFactory.NamespaceEndpoints = new List<Uri>(asyncState.addresses);
				MessagingPerformanceCounters.IncrementMessagingFactoryCount(asyncState.messagingFactory.Address, 1);
				return true;
			}

			public static new MessagingFactory End(IAsyncResult result)
			{
				return AsyncResult.End<MessagingFactory.CreateMessagingFactoryAsyncResult>(result).messagingFactory;
			}
		}

		private class CreatePartitionReceiverAsyncResult : IteratorAsyncResult<MessagingFactory.CreatePartitionReceiverAsyncResult>
		{
			private readonly MessagingFactory factory;

			private readonly string entityName;

			private readonly string consumerGroupName;

			private readonly string offset;

			private readonly string partitionId;

			private readonly bool offsetInclusive;

			private readonly DateTime? startTime;

			private readonly ReceiveMode userRequestedMode;

			private readonly long? epoch;

			public MessageReceiver MessageReceiver
			{
				get;
				set;
			}

			public CreatePartitionReceiverAsyncResult(MessagingFactory factory, string entityName, string consumerGroupName, ReceiveMode userRequestedMode, string partitionId, string offset, bool offsetInclusive, long? epoch, AsyncCallback callback, object state) : this(factory, entityName, consumerGroupName, userRequestedMode, partitionId, epoch, callback, state)
			{
				this.offset = offset;
				this.epoch = epoch;
				this.offsetInclusive = offsetInclusive;
			}

			public CreatePartitionReceiverAsyncResult(MessagingFactory factory, string entityName, string consumerGroupName, ReceiveMode userRequestedMode, string partitionId, DateTime startTime, long? epoch, AsyncCallback callback, object state) : this(factory, entityName, consumerGroupName, userRequestedMode, partitionId, epoch, callback, state)
			{
				this.startTime = new DateTime?(startTime);
				this.epoch = epoch;
			}

			private CreatePartitionReceiverAsyncResult(MessagingFactory factory, string entityName, string consumerGroupName, ReceiveMode userRequestedMode, string partitionId, long? epoch, AsyncCallback callback, object state) : base(Constants.DefaultOperationTimeout, callback, state)
			{
				this.factory = factory;
				this.entityName = entityName;
				this.consumerGroupName = consumerGroupName;
				this.partitionId = partitionId;
				this.userRequestedMode = userRequestedMode;
				this.epoch = epoch;
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.CreatePartitionReceiverAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				string str;
				str = (string.IsNullOrWhiteSpace(this.consumerGroupName) ? this.entityName : EntityNameHelper.FormatConsumerGroupPath(this.entityName, this.consumerGroupName));
				string str1 = str;
				MessagingFactory.CreatePartitionReceiverAsyncResult createPartitionReceiverAsyncResult = this;
				IteratorAsyncResult<MessagingFactory.CreatePartitionReceiverAsyncResult>.BeginCall beginCall = (MessagingFactory.CreatePartitionReceiverAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.BeginCreateMessageReceiver(str1, thisPtr.userRequestedMode, t, c, s);
				yield return createPartitionReceiverAsyncResult.CallAsync(beginCall, (MessagingFactory.CreatePartitionReceiverAsyncResult thisPtr, IAsyncResult r) => thisPtr.MessageReceiver = thisPtr.factory.EndCreateMessageReceiver(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				this.MessageReceiver.EntityType = new MessagingEntityType?(MessagingEntityType.ConsumerGroup);
				if (!string.IsNullOrWhiteSpace(this.offset))
				{
					this.MessageReceiver.StartOffset = this.offset;
					this.MessageReceiver.OffsetInclusive = this.offsetInclusive;
				}
				else if (!this.startTime.HasValue)
				{
					this.MessageReceiver.StartOffset = "-1";
				}
				else
				{
					this.MessageReceiver.ReceiverStartTime = this.startTime;
				}
				if (!string.IsNullOrWhiteSpace(this.partitionId))
				{
					this.MessageReceiver.PartitionId = this.partitionId;
				}
				if (this.epoch.HasValue)
				{
					this.MessageReceiver.Epoch = this.epoch;
				}
			}
		}

		private class CreatePartitionSenderAsyncResult : IteratorAsyncResult<MessagingFactory.CreatePartitionSenderAsyncResult>
		{
			private readonly MessagingFactory factory;

			private readonly string entityName;

			private readonly string partitionId;

			public MessageSender MessageSender
			{
				get;
				set;
			}

			public CreatePartitionSenderAsyncResult(MessagingFactory factory, string entityName, string partitionId, AsyncCallback callback, object state) : base(Constants.DefaultOperationTimeout, callback, state)
			{
				this.factory = factory;
				this.entityName = entityName;
				this.partitionId = partitionId;
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.CreatePartitionSenderAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				MessagingFactory.CreatePartitionSenderAsyncResult createPartitionSenderAsyncResult = this;
				IteratorAsyncResult<MessagingFactory.CreatePartitionSenderAsyncResult>.BeginCall beginCall = (MessagingFactory.CreatePartitionSenderAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.BeginCreateMessageSender(thisPtr.entityName, t, c, s);
				yield return createPartitionSenderAsyncResult.CallAsync(beginCall, (MessagingFactory.CreatePartitionSenderAsyncResult thisPtr, IAsyncResult r) => thisPtr.MessageSender = thisPtr.factory.EndCreateMessageSender(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (!string.IsNullOrWhiteSpace(this.partitionId))
				{
					this.MessageSender.PartitionId = this.partitionId;
				}
			}
		}

		private class PairNamespaceAsyncResult : IteratorAsyncResult<MessagingFactory.PairNamespaceAsyncResult>
		{
			private readonly MessagingFactory messagingFactory;

			public PairNamespaceAsyncResult(MessagingFactory messagingFactory, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.messagingFactory = messagingFactory;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<MessagingFactory.PairNamespaceAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				IPairedNamespaceFactory pairedNamespaceFactory = this.messagingFactory.PairedNamespaceFactory;
				yield return base.CallAsync((MessagingFactory.PairNamespaceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => pairedNamespaceFactory.BeginStart(thisPtr.messagingFactory, t, c, s), (MessagingFactory.PairNamespaceAsyncResult thisPtr, IAsyncResult r) => pairedNamespaceFactory.EndStart(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (base.LastAsyncStepException != null)
				{
					Exception lastAsyncStepException = base.LastAsyncStepException;
					this.messagingFactory.ClearPairing();
					MessagingFactory.PairNamespaceAsyncResult pairNamespaceAsyncResult = this;
					IteratorAsyncResult<MessagingFactory.PairNamespaceAsyncResult>.BeginCall closePairedNamesaceFactoryAsyncResult = (MessagingFactory.PairNamespaceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new MessagingFactory.ClosePairedNamesaceFactoryAsyncResult(thisPtr.messagingFactory, t, c, s);
					yield return pairNamespaceAsyncResult.CallAsync(closePairedNamesaceFactoryAsyncResult, (MessagingFactory.PairNamespaceAsyncResult thisPtr, IAsyncResult r) => AsyncResult<MessagingFactory.ClosePairedNamesaceFactoryAsyncResult>.End(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					base.Complete(lastAsyncStepException);
				}
			}
		}
	}
}