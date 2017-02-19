using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Configuration;
using Microsoft.ServiceBus.Notifications;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public sealed class NamespaceManager
	{
		public const string ProtocolVersion = "2014-09";

		private readonly static int ForwardToEntityNameMaxLength;

		internal readonly IEnumerable<Uri> addresses;

		private readonly NamespaceManagerSettings settings;

		public Uri Address
		{
			get
			{
				return this.addresses.First<Uri>();
			}
		}

		internal Microsoft.ServiceBus.Messaging.FaultInjectionInfo FaultInjectionInfo
		{
			get
			{
				return this.Settings.FaultInjectionInfo;
			}
			set
			{
				this.Settings.FaultInjectionInfo = value;
			}
		}

		public NamespaceManagerSettings Settings
		{
			get
			{
				return this.settings;
			}
		}

		static NamespaceManager()
		{
			NamespaceManager.ForwardToEntityNameMaxLength = Math.Max(260, 260);
		}

		public NamespaceManager(string address) : this(new Uri(address), (TokenProvider)null)
		{
		}

		public NamespaceManager(IEnumerable<string> addresses) : this(addresses, (TokenProvider)null)
		{
		}

		public NamespaceManager(Uri address) : this(address, (TokenProvider)null)
		{
		}

		public NamespaceManager(IEnumerable<Uri> addresses) : this(addresses, (TokenProvider)null)
		{
		}

		public NamespaceManager(string address, TokenProvider tokenProvider) : this(new Uri(address), tokenProvider)
		{
		}

		public NamespaceManager(IEnumerable<string> addresses, TokenProvider tokenProvider) : this(MessagingUtilities.GetUriList(addresses), tokenProvider)
		{
		}

		public NamespaceManager(Uri address, TokenProvider tokenProvider)
		{
			MessagingUtilities.ThrowIfNullAddressOrPathExists(address, "address");
			this.addresses = new List<Uri>()
			{
				address
			};
			this.settings = new NamespaceManagerSettings()
			{
				TokenProvider = tokenProvider
			};
		}

		public NamespaceManager(IEnumerable<Uri> addresses, TokenProvider tokenProvider)
		{
			MessagingUtilities.ThrowIfNullAddressesOrPathExists(addresses, "addresses");
			this.addresses = addresses.ToList<Uri>();
			this.settings = new NamespaceManagerSettings()
			{
				TokenProvider = tokenProvider
			};
		}

		public NamespaceManager(string address, NamespaceManagerSettings settings) : this(new Uri(address), settings)
		{
		}

		public NamespaceManager(IEnumerable<string> addresses, NamespaceManagerSettings settings) : this(MessagingUtilities.GetUriList(addresses), settings)
		{
		}

		public NamespaceManager(Uri address, NamespaceManagerSettings settings) : this(new List<Uri>()
		{
			address
		}, settings)
		{
		}

		public NamespaceManager(IEnumerable<Uri> addresses, NamespaceManagerSettings settings)
		{
			MessagingUtilities.ThrowIfNullAddressesOrPathExists(addresses, "addresses");
			if (settings == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("settings");
			}
			Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNonPositiveArgument(settings.OperationTimeout);
			this.addresses = addresses.ToList<Uri>();
			this.settings = settings;
		}

		private IAsyncResult BeginCreateConsumerGroup(string eventHubPath, string name, AsyncCallback callback, object state)
		{
			return this.BeginCreateConsumerGroup(new ConsumerGroupDescription(eventHubPath, name), callback, state);
		}

		private IAsyncResult BeginCreateConsumerGroup(ConsumerGroupDescription description, AsyncCallback callback, object state)
		{
			return new NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult(null, this, description, false, callback, state);
		}

		internal IAsyncResult BeginCreateConsumerGroupIfNotExists(string eventHubPath, string name, AsyncCallback callback, object state)
		{
			return this.BeginCreateConsumerGroupIfNotExists(new ConsumerGroupDescription(eventHubPath, name), callback, state);
		}

		internal IAsyncResult BeginCreateConsumerGroupIfNotExists(ConsumerGroupDescription description, AsyncCallback callback, object state)
		{
			return (new NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult(this, description, callback, state)).Start();
		}

		internal IAsyncResult BeginCreateEventHub(string path, AsyncCallback callback, object state)
		{
			return this.BeginCreateEventHub(new EventHubDescription(path), callback, state);
		}

		internal IAsyncResult BeginCreateEventHub(EventHubDescription description, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateEventHub(description, callback, state);
		}

		internal IAsyncResult BeginCreateEventHubIfNotExists(string path, AsyncCallback callback, object state)
		{
			return this.BeginCreateEventHubIfNotExists(new EventHubDescription(path), callback, state);
		}

		internal IAsyncResult BeginCreateEventHubIfNotExists(EventHubDescription description, AsyncCallback callback, object state)
		{
			return (new NamespaceManager.CreateEventHubIfNotExistsAsyncResult(this, description, callback, state)).Start();
		}

		private IAsyncResult BeginCreateNotificationHub(NotificationHubDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateNotificationHubAsyncResult createOrUpdateNotificationHubAsyncResult = new NamespaceManager.CreateOrUpdateNotificationHubAsyncResult(this, description, false, callback, state);
			createOrUpdateNotificationHubAsyncResult.Start();
			return createOrUpdateNotificationHubAsyncResult;
		}

		public IAsyncResult BeginCreateQueue(string path, AsyncCallback callback, object state)
		{
			return this.BeginCreateQueue(new QueueDescription(path), callback, state);
		}

		public IAsyncResult BeginCreateQueue(QueueDescription description, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateQueue(description, callback, state);
		}

		private IAsyncResult BeginCreateRegistration<TRegistrationDescription>(TRegistrationDescription registrationDescription, AsyncCallback callback, object state)
		where TRegistrationDescription : RegistrationDescription
		{
			RegistrationSDKHelper.ValidateRegistration(registrationDescription);
			return new NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>(this, registrationDescription, false, callback, state);
		}

		internal IAsyncResult BeginCreateRule(string ruleName, string subscriptionName, string topicPath, RuleDescription description, AsyncCallback callback, object state)
		{
			return this.BeginCreateRule(ruleName, subscriptionName, topicPath, description, this.settings.InternalOperationTimeout, callback, state);
		}

		internal IAsyncResult BeginCreateRule(string ruleName, string subscriptionName, string topicPath, RuleDescription description, TimeSpan timeout, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateRuleAsyncResult createRuleAsyncResult = new NamespaceManager.CreateRuleAsyncResult(this, ruleName, subscriptionName, topicPath, description, timeout, callback, state);
			createRuleAsyncResult.Start();
			return createRuleAsyncResult;
		}

		public IAsyncResult BeginCreateSubscription(string topicPath, string name, AsyncCallback callback, object state)
		{
			SubscriptionDescription subscriptionDescription = new SubscriptionDescription(topicPath, name);
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = TrueFilter.Default,
				Action = EmptyRuleAction.Default
			};
			return this.BeginCreateSubscription(subscriptionDescription, ruleDescription, callback, state);
		}

		public IAsyncResult BeginCreateSubscription(string topicPath, string name, Filter filter, AsyncCallback callback, object state)
		{
			SubscriptionDescription subscriptionDescription = new SubscriptionDescription(topicPath, name);
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = filter,
				Action = EmptyRuleAction.Default
			};
			return this.BeginCreateSubscription(subscriptionDescription, ruleDescription, callback, state);
		}

		public IAsyncResult BeginCreateSubscription(string topicPath, string name, RuleDescription ruleDescription, AsyncCallback callback, object state)
		{
			return this.BeginCreateSubscription(new SubscriptionDescription(topicPath, name), ruleDescription, callback, state);
		}

		public IAsyncResult BeginCreateSubscription(SubscriptionDescription description, AsyncCallback callback, object state)
		{
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = TrueFilter.Default,
				Action = EmptyRuleAction.Default
			};
			return this.BeginCreateSubscription(description, ruleDescription, callback, state);
		}

		public IAsyncResult BeginCreateSubscription(SubscriptionDescription description, Filter filter, AsyncCallback callback, object state)
		{
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = filter,
				Action = EmptyRuleAction.Default
			};
			return this.BeginCreateSubscription(description, ruleDescription, callback, state);
		}

		public IAsyncResult BeginCreateSubscription(SubscriptionDescription description, RuleDescription ruleDescription, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateSubscription(ruleDescription, description, callback, state);
		}

		public IAsyncResult BeginCreateTopic(string path, AsyncCallback callback, object state)
		{
			return this.BeginCreateTopic(new TopicDescription(path), callback, state);
		}

		public IAsyncResult BeginCreateTopic(TopicDescription description, AsyncCallback callback, object state)
		{
			return this.OnBeginCreateTopic(description, callback, state);
		}

		internal IAsyncResult BeginCreateVolatileTopic(string path, AsyncCallback callback, object state)
		{
			return this.BeginCreateVolatileTopic(new VolatileTopicDescription(path), callback, state);
		}

		internal IAsyncResult BeginCreateVolatileTopic(VolatileTopicDescription description, AsyncCallback callback, object state)
		{
			return (new NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult(this, description, false, callback, state)).Start();
		}

		internal IAsyncResult BeginDeleteConsumerGroup(string eventHubPath, string name, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(eventHubPath, 260, "eventHubPath");
			NamespaceManager.CheckValidEntityName(name, 50, false, "name");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatConsumerGroupPath(eventHubPath, name));
			IResourceDescription[] consumerGroupDescription = new IResourceDescription[] { new ConsumerGroupDescription() };
			string[] strArrays = new string[] { eventHubPath, name };
			return ServiceBusResourceOperations.BeginDelete(instance, consumerGroupDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		internal IAsyncResult BeginDeleteEventHub(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginDeleteEventHub(path, callback, state);
		}

		private IAsyncResult BeginDeleteNotificationHub(string path, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginDelete(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		public IAsyncResult BeginDeleteQueue(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginDeleteQueue(path, callback, state);
		}

		internal IAsyncResult BeginDeleteRegistration(string notificationHubPath, string registrationId, string etag, AsyncCallback callback, object state)
		{
			Dictionary<string, string> strs = new Dictionary<string, string>();
			if (!string.IsNullOrEmpty(etag))
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { etag };
				strs.Add("If-Match", string.Format(invariantCulture, "\"{0}\"", objArray));
			}
			else
			{
				strs.Add("If-Match", "*");
			}
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), notificationHubPath);
			IResourceDescription[] genericDescription = new IResourceDescription[] { new GenericDescription("Registrations") };
			string[] strArrays = new string[] { notificationHubPath, registrationId };
			return ServiceBusResourceOperations.BeginDelete(instance, genericDescription, strArrays, this.addresses, strs, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		internal IAsyncResult BeginDeleteRule(string ruleName, string subscriptionName, string topicPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(ruleName, 50, false, "ruleName");
			NamespaceManager.CheckValidEntityName(subscriptionName, 50, false, "subscriptionName");
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, subscriptionName));
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription(), new RuleDescription() };
			string[] strArrays = new string[] { topicPath, subscriptionName, ruleName };
			return ServiceBusResourceOperations.BeginDelete(instance, subscriptionDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		public IAsyncResult BeginDeleteSubscription(string topicPath, string name, AsyncCallback callback, object state)
		{
			return this.OnBeginDeleteSubscription(name, topicPath, callback, state);
		}

		public IAsyncResult BeginDeleteTopic(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginDeleteTopic(path, callback, state);
		}

		internal IAsyncResult BeginDeleteVolatileTopic(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginDelete(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		internal IAsyncResult BeginEventHubExists(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginEventHubExists(path, callback, state);
		}

		private IAsyncResult BeginGetAllRegistrations(string notificationHubPath, string continuationToken, int top, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), notificationHubPath);
			string empty = string.Empty;
			IResourceDescription[] genericDescription = new IResourceDescription[] { new GenericDescription("Registrations") };
			string[] strArrays = new string[] { notificationHubPath };
			return ServiceBusResourceOperations.BeginGetAll(instance, empty, genericDescription, strArrays, this.addresses, this.settings, continuationToken, top, callback, state);
		}

		internal IAsyncResult BeginGetConsumerGroup(string eventHubPath, string name, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(name, 50, false, "name");
			NamespaceManager.CheckValidEntityName(eventHubPath, 260, "eventHubPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatConsumerGroupPath(eventHubPath, name));
			IResourceDescription[] consumerGroupDescription = new IResourceDescription[] { new ConsumerGroupDescription() };
			string[] strArrays = new string[] { eventHubPath, name };
			return ServiceBusResourceOperations.BeginGet<ConsumerGroupDescription>(instance, consumerGroupDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult BeginGetConsumerGroups(string eventHubPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(eventHubPath, 260, "eventHubPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), eventHubPath);
			IResourceDescription[] consumerGroupDescription = new IResourceDescription[] { new ConsumerGroupDescription() };
			string[] strArrays = new string[] { eventHubPath };
			return new NamespaceManager.GetAllAsyncResult(instance, consumerGroupDescription, strArrays, this.addresses, this.settings, callback, state);
		}

		internal IAsyncResult BeginGetEventHub(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginGetEventHub(path, callback, state);
		}

		internal IAsyncResult BeginGetEventHubPartition(string eventHubPath, string consumerGroupName, string name, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(name, 50, false, "name");
			NamespaceManager.CheckValidEntityName(eventHubPath, 260, "eventHubPath");
			NamespaceManager.CheckValidEntityName(consumerGroupName, 50, "consumerGroupName");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatConsumerGroupPath(eventHubPath, name));
			IResourceDescription[] consumerGroupDescription = new IResourceDescription[] { new ConsumerGroupDescription(), new PartitionDescription() };
			string[] strArrays = new string[] { eventHubPath, consumerGroupName, name };
			return ServiceBusResourceOperations.BeginGet<PartitionDescription>(instance, consumerGroupDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		internal IAsyncResult BeginGetEventHubPartitions(string eventHubPath, string consumerGroupName, AsyncCallback callback, object state)
		{
			return this.OnBeginGetEventHubPartitions(eventHubPath, consumerGroupName, callback, state);
		}

		internal IAsyncResult BeginGetEventHubs(AsyncCallback callback, object state)
		{
			return this.OnBeginGetEventHubs(callback, state);
		}

		private IAsyncResult BeginGetNotificationHub(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<NotificationHubDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult BeginGetNotificationHubs(AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] notificationHubDescription = new IResourceDescription[] { new NotificationHubDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, notificationHubDescription, null, this.addresses, this.settings, callback, state);
		}

		public IAsyncResult BeginGetQueue(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginGetQueue(path, callback, state);
		}

		public IAsyncResult BeginGetQueues(AsyncCallback callback, object state)
		{
			return this.OnBeginGetQueues(callback, state);
		}

		public IAsyncResult BeginGetQueues(string filter, AsyncCallback callback, object state)
		{
			return this.OnBeginGetQueues(filter, callback, state);
		}

		private IAsyncResult BeginGetRegistration<TRegistrationDescription>(string registrationId, string notificationHubPath, AsyncCallback callback, object state)
		where TRegistrationDescription : RegistrationDescription
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), notificationHubPath);
			IResourceDescription[] genericDescription = new IResourceDescription[] { new GenericDescription("Registrations") };
			string[] strArrays = new string[] { notificationHubPath, registrationId };
			return ServiceBusResourceOperations.BeginGet<RegistrationDescription>(instance, genericDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult BeginGetRegistrationsByChannel(string pnsHandle, string notificationHubPath, string continuationToken, int top, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), notificationHubPath);
			string str = string.Concat("ChannelUri eq '", pnsHandle, "'");
			IResourceDescription[] genericDescription = new IResourceDescription[] { new GenericDescription("Registrations") };
			string[] strArrays = new string[] { notificationHubPath };
			return ServiceBusResourceOperations.BeginGetAll(instance, str, genericDescription, strArrays, this.addresses, this.settings, continuationToken, top, callback, state);
		}

		private IAsyncResult BeginGetRegistrationsByTag(string notificationHubPath, string tag, string continuationToken, int top, AsyncCallback callback, object state)
		{
			if (string.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentNullException("tag");
			}
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), notificationHubPath);
			string empty = string.Empty;
			IResourceDescription[] genericDescription = new IResourceDescription[] { new GenericDescription("Tags"), new GenericDescription("Registrations") };
			string[] strArrays = new string[] { notificationHubPath, tag };
			return ServiceBusResourceOperations.BeginGetAll(instance, empty, genericDescription, strArrays, this.addresses, this.settings, continuationToken, top, callback, state);
		}

		internal IAsyncResult BeginGetRule(string ruleName, string subscriptionName, string topicPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(ruleName, 50, false, "ruleName");
			NamespaceManager.CheckValidEntityName(subscriptionName, 50, false, "subscriptionName");
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicName");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, subscriptionName));
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription(), new RuleDescription() };
			string[] strArrays = new string[] { topicPath, subscriptionName, ruleName };
			return ServiceBusResourceOperations.BeginGet<RuleDescription>(instance, subscriptionDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		public IAsyncResult BeginGetRules(string topicPath, string subscriptionName, AsyncCallback callback, object state)
		{
			return this.OnBeginGetRules(subscriptionName, topicPath, callback, state);
		}

		public IAsyncResult BeginGetRules(string topicPath, string subscriptionName, string filter, AsyncCallback callback, object state)
		{
			return this.OnBeginGetRules(subscriptionName, topicPath, filter, callback, state);
		}

		public IAsyncResult BeginGetSubscription(string topicPath, string name, AsyncCallback callback, object state)
		{
			return this.OnBeginGetSubscription(name, topicPath, callback, state);
		}

		public IAsyncResult BeginGetSubscriptions(string topicPath, AsyncCallback callback, object state)
		{
			return this.OnBeginGetSubscriptions(topicPath, callback, state);
		}

		public IAsyncResult BeginGetSubscriptions(string topicPath, string filter, AsyncCallback callback, object state)
		{
			return this.OnBeginGetSubscriptions(topicPath, filter, callback, state);
		}

		public IAsyncResult BeginGetTopic(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginGetTopic(path, callback, state);
		}

		public IAsyncResult BeginGetTopics(AsyncCallback callback, object state)
		{
			return this.OnBeginGetTopics(callback, state);
		}

		public IAsyncResult BeginGetTopics(string filter, AsyncCallback callback, object state)
		{
			return this.OnBeginGetTopics(filter, callback, state);
		}

		public IAsyncResult BeginGetVersionInfo(AsyncCallback callback, object state)
		{
			NamespaceManager.GetVersionInfoAsyncResult getVersionInfoAsyncResult = new NamespaceManager.GetVersionInfoAsyncResult(this, callback, state);
			getVersionInfoAsyncResult.Start();
			return getVersionInfoAsyncResult;
		}

		internal IAsyncResult BeginGetVolatileTopic(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<VolatileTopicDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		internal IAsyncResult BeginGetVolatileTopics(AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] volatileTopicDescription = new IResourceDescription[] { new VolatileTopicDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, volatileTopicDescription, null, this.addresses, this.settings, callback, state);
		}

		internal IAsyncResult BeginGetVolatileTopics(string filter, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] volatileTopicDescription = new IResourceDescription[] { new VolatileTopicDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, filter, volatileTopicDescription, null, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult BeginNotificationHubExists(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<NotificationHubDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		public IAsyncResult BeginQueueExists(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginQueueExists(path, callback, state);
		}

		public IAsyncResult BeginSubscriptionExists(string topicPath, string name, AsyncCallback callback, object state)
		{
			return this.OnBeginSubscriptionExists(name, topicPath, callback, state);
		}

		public IAsyncResult BeginTopicExists(string path, AsyncCallback callback, object state)
		{
			return this.OnBeginTopicExists(path, callback, state);
		}

		private IAsyncResult BeginUpdateConsumerGroup(ConsumerGroupDescription description, AsyncCallback callback, object state)
		{
			return new NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult(null, this, description, true, callback, state);
		}

		internal IAsyncResult BeginUpdateEventHub(EventHubDescription description, AsyncCallback callback, object state)
		{
			return this.OnBeginUpdateEventHub(description, callback, state);
		}

		private IAsyncResult BeginUpdateNotificationHub(NotificationHubDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateNotificationHubAsyncResult createOrUpdateNotificationHubAsyncResult = new NamespaceManager.CreateOrUpdateNotificationHubAsyncResult(this, description, true, callback, state);
			createOrUpdateNotificationHubAsyncResult.Start();
			return createOrUpdateNotificationHubAsyncResult;
		}

		public IAsyncResult BeginUpdateQueue(QueueDescription description, AsyncCallback callback, object state)
		{
			return this.OnBeginUpdateQueue(description, callback, state);
		}

		private IAsyncResult BeginUpdateRegistration<TRegistrationDescription>(TRegistrationDescription registrationDescription, AsyncCallback callback, object state)
		where TRegistrationDescription : RegistrationDescription
		{
			RegistrationSDKHelper.ValidateRegistration(registrationDescription);
			return new NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>(this, registrationDescription, true, callback, state);
		}

		public IAsyncResult BeginUpdateSubscription(SubscriptionDescription description, AsyncCallback callback, object state)
		{
			return this.OnBeginUpdateSubscription(description, callback, state);
		}

		public IAsyncResult BeginUpdateTopic(TopicDescription description, AsyncCallback callback, object state)
		{
			return this.OnBeginUpdateTopic(description, callback, state);
		}

		internal IAsyncResult BeginUpdateVolatileTopic(VolatileTopicDescription description, AsyncCallback callback, object state)
		{
			return (new NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult(this, description, true, callback, state)).Start();
		}

		private static void CheckValidEntityName(string entityName, int maxEntityNameLength, string paramName)
		{
			NamespaceManager.CheckValidEntityName(entityName, maxEntityNameLength, true, paramName);
		}

		private static void CheckValidEntityName(string entityName, int maxEntityNameLength, bool allowSeparator, string paramName)
		{
			if (string.IsNullOrWhiteSpace(entityName))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNullOrEmpty(paramName);
			}
			string str = entityName.Replace("\\", "/");
			if (str.Length > maxEntityNameLength)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange(paramName, str, SRClient.EntityNameLengthExceedsLimit(str, maxEntityNameLength));
			}
			if (str.StartsWith("/", StringComparison.OrdinalIgnoreCase) || str.EndsWith("/", StringComparison.Ordinal))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument(paramName, SRClient.InvalidEntityNameFormatWithSlash(str));
			}
			if (!allowSeparator && str.Contains("/"))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument(paramName, SRClient.InvalidCharacterInEntityName("/", str));
			}
			MessagingUtilities.CheckUriSchemeKey(entityName, paramName);
		}

		public static NamespaceManager Create()
		{
			return (new KeyValueConfigurationManager()).CreateNamespaceManager();
		}

		public ConsumerGroupDescription CreateConsumerGroup(string eventHubPath, string name)
		{
			return this.EndCreateConsumerGroup(this.BeginCreateConsumerGroup(eventHubPath, name, null, null));
		}

		public ConsumerGroupDescription CreateConsumerGroup(ConsumerGroupDescription description)
		{
			return this.EndCreateConsumerGroup(this.BeginCreateConsumerGroup(description, null, null));
		}

		public Task<ConsumerGroupDescription> CreateConsumerGroupAsync(string eventHubPath, string name)
		{
			return TaskHelpers.CreateTask<ConsumerGroupDescription>((AsyncCallback c, object s) => this.BeginCreateConsumerGroup(eventHubPath, name, c, s), new Func<IAsyncResult, ConsumerGroupDescription>(this.EndCreateConsumerGroup));
		}

		public Task<ConsumerGroupDescription> CreateConsumerGroupAsync(ConsumerGroupDescription description)
		{
			return TaskHelpers.CreateTask<ConsumerGroupDescription>((AsyncCallback c, object s) => this.BeginCreateConsumerGroup(description, c, s), new Func<IAsyncResult, ConsumerGroupDescription>(this.EndCreateConsumerGroup));
		}

		public ConsumerGroupDescription CreateConsumerGroupIfNotExists(string eventHubPath, string name)
		{
			return this.EndCreateConsumerGroupIfNotExists(this.BeginCreateConsumerGroupIfNotExists(eventHubPath, name, null, null));
		}

		public ConsumerGroupDescription CreateConsumerGroupIfNotExists(ConsumerGroupDescription description)
		{
			return this.EndCreateConsumerGroupIfNotExists(this.BeginCreateConsumerGroupIfNotExists(description, null, null));
		}

		public Task<ConsumerGroupDescription> CreateConsumerGroupIfNotExistsAsync(string eventHubPath, string name)
		{
			return TaskHelpers.CreateTask<ConsumerGroupDescription>((AsyncCallback c, object s) => this.BeginCreateConsumerGroupIfNotExists(eventHubPath, name, c, s), new Func<IAsyncResult, ConsumerGroupDescription>(this.EndCreateConsumerGroupIfNotExists));
		}

		public Task<ConsumerGroupDescription> CreateConsumerGroupIfNotExistsAsync(ConsumerGroupDescription description)
		{
			return TaskHelpers.CreateTask<ConsumerGroupDescription>((AsyncCallback c, object s) => this.BeginCreateConsumerGroupIfNotExists(description, c, s), new Func<IAsyncResult, ConsumerGroupDescription>(this.EndCreateConsumerGroupIfNotExists));
		}

		public EventHubDescription CreateEventHub(string path)
		{
			return this.EndCreateEventHub(this.BeginCreateEventHub(new EventHubDescription(path), null, null));
		}

		public EventHubDescription CreateEventHub(EventHubDescription description)
		{
			return this.EndCreateEventHub(this.BeginCreateEventHub(description, null, null));
		}

		public Task<EventHubDescription> CreateEventHubAsync(string path)
		{
			return TaskHelpers.CreateTask<EventHubDescription>((AsyncCallback c, object s) => this.BeginCreateEventHub(path, c, s), new Func<IAsyncResult, EventHubDescription>(this.EndCreateEventHub));
		}

		public Task<EventHubDescription> CreateEventHubAsync(EventHubDescription description)
		{
			return TaskHelpers.CreateTask<EventHubDescription>((AsyncCallback c, object s) => this.BeginCreateEventHub(description, c, s), new Func<IAsyncResult, EventHubDescription>(this.EndCreateEventHub));
		}

		public EventHubDescription CreateEventHubIfNotExists(string path)
		{
			return this.EndCreateEventHubIfNotExists(this.BeginCreateEventHubIfNotExists(path, null, null));
		}

		public EventHubDescription CreateEventHubIfNotExists(EventHubDescription description)
		{
			return this.EndCreateEventHubIfNotExists(this.BeginCreateEventHubIfNotExists(description, null, null));
		}

		public Task<EventHubDescription> CreateEventHubIfNotExistsAsync(string path)
		{
			return TaskHelpers.CreateTask<EventHubDescription>((AsyncCallback c, object s) => this.BeginCreateEventHubIfNotExists(path, c, s), new Func<IAsyncResult, EventHubDescription>(this.EndCreateEventHubIfNotExists));
		}

		public Task<EventHubDescription> CreateEventHubIfNotExistsAsync(EventHubDescription description)
		{
			return TaskHelpers.CreateTask<EventHubDescription>((AsyncCallback c, object s) => this.BeginCreateEventHubIfNotExists(description, c, s), new Func<IAsyncResult, EventHubDescription>(this.EndCreateEventHubIfNotExists));
		}

		public static NamespaceManager CreateFromConnectionString(string connectionString)
		{
			return (new KeyValueConfigurationManager(connectionString)).CreateNamespaceManager();
		}

		internal NotificationHubDescription CreateNotificationHub(string path)
		{
			return this.EndCreateNotificationHub(this.BeginCreateNotificationHub(new NotificationHubDescription(path), null, null));
		}

		public NotificationHubDescription CreateNotificationHub(NotificationHubDescription description)
		{
			return this.EndCreateNotificationHub(this.BeginCreateNotificationHub(description, null, null));
		}

		public Task<NotificationHubDescription> CreateNotificationHubAsync(NotificationHubDescription description)
		{
			return TaskHelpers.CreateTask<NotificationHubDescription>((AsyncCallback c, object s) => this.BeginCreateNotificationHub(description, c, s), new Func<IAsyncResult, NotificationHubDescription>(this.EndCreateNotificationHub));
		}

		public QueueDescription CreateQueue(string path)
		{
			return this.EndCreateQueue(this.BeginCreateQueue(new QueueDescription(path), null, null));
		}

		public QueueDescription CreateQueue(QueueDescription description)
		{
			return this.EndCreateQueue(this.BeginCreateQueue(description, null, null));
		}

		public Task<QueueDescription> CreateQueueAsync(string path)
		{
			return TaskHelpers.CreateTask<QueueDescription>((AsyncCallback c, object s) => this.BeginCreateQueue(path, c, s), new Func<IAsyncResult, QueueDescription>(this.EndCreateQueue));
		}

		public Task<QueueDescription> CreateQueueAsync(QueueDescription description)
		{
			return TaskHelpers.CreateTask<QueueDescription>((AsyncCallback c, object s) => this.BeginCreateQueue(description, c, s), new Func<IAsyncResult, QueueDescription>(this.EndCreateQueue));
		}

		internal TRegistrationDescription CreateRegistration<TRegistrationDescription>(TRegistrationDescription registrationDescription)
		where TRegistrationDescription : RegistrationDescription
		{
			return this.EndCreateRegistration<TRegistrationDescription>(this.BeginCreateRegistration<TRegistrationDescription>(registrationDescription, null, null));
		}

		internal Task<TRegistrationDescription> CreateRegistrationAsync<TRegistrationDescription>(TRegistrationDescription registrationDescription)
		where TRegistrationDescription : RegistrationDescription
		{
			return TaskHelpers.CreateTask<TRegistrationDescription>((AsyncCallback c, object s) => this.BeginCreateRegistration<TRegistrationDescription>(registrationDescription, c, s), new Func<IAsyncResult, TRegistrationDescription>(this.EndCreateRegistration<TRegistrationDescription>));
		}

		internal Task<string> CreateRegistrationIdAsync(string notificationHubPath)
		{
			TaskFactory factory = Task.Factory;
			Func<string[], NamespaceManager, AsyncCallback, object, IAsyncResult> func = new Func<string[], NamespaceManager, AsyncCallback, object, IAsyncResult>(ServiceBusResourceOperations.BeginCreateRegistrationId);
			Func<IAsyncResult, string> func1 = new Func<IAsyncResult, string>(ServiceBusResourceOperations.EndCreateRegistrationId);
			string[] strArrays = new string[] { notificationHubPath };
			return factory.FromAsync<string[], NamespaceManager, string>(func, func1, strArrays, this, null);
		}

		public Task<RelayDescription> CreateRelayAsync(string path, RelayType type)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			return this.CreateRelayAsync(new RelayDescription(path, type));
		}

		public Task<RelayDescription> CreateRelayAsync(RelayDescription description)
		{
			if (description == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
			}
			NamespaceManager.CheckValidEntityName(description.Path, 260, "description.Path");
			string[] path = new string[] { description.Path };
			Task<RelayDescription> task = ServiceBusResourceOperations.CreateAsync<RelayDescription>(description, path, this);
			return this.FixupRelayDescriptionPath(description.Path, task);
		}

		internal RuleDescription CreateRule(string ruleName, string subscriptionName, string topicPath, RuleDescription description)
		{
			return this.EndCreateRule(this.BeginCreateRule(ruleName, subscriptionName, topicPath, description, null, null));
		}

		public SubscriptionDescription CreateSubscription(string topicPath, string name)
		{
			SubscriptionDescription subscriptionDescription = new SubscriptionDescription(topicPath, name);
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = TrueFilter.Default,
				Action = EmptyRuleAction.Default
			};
			return this.EndCreateSubscription(this.BeginCreateSubscription(subscriptionDescription, ruleDescription, null, null));
		}

		public SubscriptionDescription CreateSubscription(string topicPath, string name, Filter filter)
		{
			SubscriptionDescription subscriptionDescription = new SubscriptionDescription(topicPath, name);
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = filter,
				Action = EmptyRuleAction.Default
			};
			return this.EndCreateSubscription(this.BeginCreateSubscription(subscriptionDescription, ruleDescription, null, null));
		}

		public SubscriptionDescription CreateSubscription(SubscriptionDescription description)
		{
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = TrueFilter.Default,
				Action = EmptyRuleAction.Default
			};
			return this.EndCreateSubscription(this.BeginCreateSubscription(description, ruleDescription, null, null));
		}

		public SubscriptionDescription CreateSubscription(SubscriptionDescription description, Filter filter)
		{
			RuleDescription ruleDescription = new RuleDescription()
			{
				Filter = filter,
				Action = EmptyRuleAction.Default
			};
			return this.EndCreateSubscription(this.BeginCreateSubscription(description, ruleDescription, null, null));
		}

		public SubscriptionDescription CreateSubscription(string topicPath, string name, RuleDescription ruleDescription)
		{
			return this.EndCreateSubscription(this.BeginCreateSubscription(new SubscriptionDescription(topicPath, name), ruleDescription, null, null));
		}

		public SubscriptionDescription CreateSubscription(SubscriptionDescription description, RuleDescription ruleDescription)
		{
			return this.EndCreateSubscription(this.BeginCreateSubscription(description, ruleDescription, null, null));
		}

		public Task<SubscriptionDescription> CreateSubscriptionAsync(string topicPath, string name)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginCreateSubscription(topicPath, name, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndCreateSubscription));
		}

		public Task<SubscriptionDescription> CreateSubscriptionAsync(string topicPath, string name, Filter filter)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginCreateSubscription(topicPath, name, filter, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndCreateSubscription));
		}

		public Task<SubscriptionDescription> CreateSubscriptionAsync(string topicPath, string name, RuleDescription ruleDescription)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginCreateSubscription(topicPath, name, ruleDescription, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndCreateSubscription));
		}

		public Task<SubscriptionDescription> CreateSubscriptionAsync(SubscriptionDescription description)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginCreateSubscription(description, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndCreateSubscription));
		}

		public Task<SubscriptionDescription> CreateSubscriptionAsync(SubscriptionDescription description, Filter filter)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginCreateSubscription(description, filter, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndCreateSubscription));
		}

		public Task<SubscriptionDescription> CreateSubscriptionAsync(SubscriptionDescription description, RuleDescription ruleDescription)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginCreateSubscription(description, ruleDescription, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndCreateSubscription));
		}

		public TopicDescription CreateTopic(string path)
		{
			return this.EndCreateTopic(this.BeginCreateTopic(new TopicDescription(path), null, null));
		}

		public TopicDescription CreateTopic(TopicDescription description)
		{
			return this.EndCreateTopic(this.BeginCreateTopic(description, null, null));
		}

		public Task<TopicDescription> CreateTopicAsync(string path)
		{
			return TaskHelpers.CreateTask<TopicDescription>((AsyncCallback c, object s) => this.BeginCreateTopic(path, c, s), new Func<IAsyncResult, TopicDescription>(this.EndCreateTopic));
		}

		public Task<TopicDescription> CreateTopicAsync(TopicDescription description)
		{
			return TaskHelpers.CreateTask<TopicDescription>((AsyncCallback c, object s) => this.BeginCreateTopic(description, c, s), new Func<IAsyncResult, TopicDescription>(this.EndCreateTopic));
		}

		internal VolatileTopicDescription CreateVolatileTopic(string path)
		{
			return this.CreateVolatileTopic(new VolatileTopicDescription(path));
		}

		internal VolatileTopicDescription CreateVolatileTopic(VolatileTopicDescription description)
		{
			NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult createOrUpdateVolatileTopicAsyncResult = new NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult(this, description, false, null, null);
			createOrUpdateVolatileTopicAsyncResult.RunSynchronously();
			return createOrUpdateVolatileTopicAsyncResult.Description;
		}

		internal Task<VolatileTopicDescription> CreateVolatileTopicAsync(string path)
		{
			return TaskHelpers.CreateTask<VolatileTopicDescription>((AsyncCallback c, object s) => this.BeginCreateVolatileTopic(path, c, s), new Func<IAsyncResult, VolatileTopicDescription>(this.EndCreateVolatileTopic));
		}

		internal Task<VolatileTopicDescription> CreateVolatileTopicAsync(VolatileTopicDescription description)
		{
			return TaskHelpers.CreateTask<VolatileTopicDescription>((AsyncCallback c, object s) => this.BeginCreateVolatileTopic(description, c, s), new Func<IAsyncResult, VolatileTopicDescription>(this.EndCreateVolatileTopic));
		}

		public void DeleteConsumerGroup(string eventHubPath, string name)
		{
			this.EndDeleteConsumerGroup(this.BeginDeleteConsumerGroup(eventHubPath, name, null, null));
		}

		public Task DeleteConsumerGroupAsync(string eventHubPath, string name)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteConsumerGroup(eventHubPath, name, c, s), new Action<IAsyncResult>(this.EndDeleteConsumerGroup));
		}

		public void DeleteEventHub(string path)
		{
			this.EndDeleteEventHub(this.BeginDeleteEventHub(path, null, null));
		}

		public Task DeleteEventHubAsync(string path)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteEventHub(path, c, s), new Action<IAsyncResult>(this.EndDeleteEventHub));
		}

		public void DeleteNotificationHub(string path)
		{
			this.EndDeleteNotificationHub(this.BeginDeleteNotificationHub(path, null, null));
		}

		public Task DeleteNotificationHubAsync(string path)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteNotificationHub(path, c, s), new Action<IAsyncResult>(this.EndDeleteNotificationHub));
		}

		public void DeleteQueue(string path)
		{
			this.EndDeleteQueue(this.BeginDeleteQueue(path, null, null));
		}

		public Task DeleteQueueAsync(string path)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteQueue(path, c, s), new Action<IAsyncResult>(this.EndDeleteQueue));
		}

		internal void DeleteRegistration(string notificationHubPath, string registrationId, string etag)
		{
			this.EndDeleteRegistration(this.BeginDeleteRegistration(notificationHubPath, registrationId, etag, null, null));
		}

		internal Task DeleteRegistrationAsync(string notificationHubPath, string registrationId, string etag)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteRegistration(notificationHubPath, registrationId, etag, c, s), new Action<IAsyncResult>(this.EndDeleteRegistration));
		}

		public Task DeleteRelayAsync(string path)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			return ServiceBusResourceOperations.DeleteAsync(new string[] { path }, this);
		}

		public void DeleteSubscription(string topicPath, string name)
		{
			this.EndDeleteSubscription(this.BeginDeleteSubscription(topicPath, name, null, null));
		}

		public Task DeleteSubscriptionAsync(string topicPath, string name)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteSubscription(topicPath, name, c, s), new Action<IAsyncResult>(this.EndDeleteSubscription));
		}

		public void DeleteTopic(string path)
		{
			this.EndDeleteTopic(this.BeginDeleteTopic(path, null, null));
		}

		public Task DeleteTopicAsync(string path)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteTopic(path, c, s), new Action<IAsyncResult>(this.EndDeleteTopic));
		}

		internal void DeleteVolatileTopic(string path)
		{
			this.EndDeleteVolatileTopic(this.BeginDeleteVolatileTopic(path, null, null));
		}

		internal Task DeleteVolatileTopicAsync(string path)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeleteVolatileTopic(path, c, s), new Action<IAsyncResult>(this.EndDeleteVolatileTopic));
		}

		private ConsumerGroupDescription EndCreateConsumerGroup(IAsyncResult result)
		{
			return NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult.End(result).ConsumerGroup;
		}

		internal ConsumerGroupDescription EndCreateConsumerGroupIfNotExists(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult>.End(result).ConsumerGroup;
		}

		internal EventHubDescription EndCreateEventHub(IAsyncResult result)
		{
			return this.OnEndCreateEventHub(result);
		}

		internal EventHubDescription EndCreateEventHubIfNotExists(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateEventHubIfNotExistsAsyncResult>.End(result).EventHub;
		}

		private NotificationHubDescription EndCreateNotificationHub(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateNotificationHubAsyncResult>.End(result).NotificationHub;
		}

		public QueueDescription EndCreateQueue(IAsyncResult result)
		{
			return this.OnEndCreateQueue(result);
		}

		private TRegistrationDescription EndCreateRegistration<TRegistrationDescription>(IAsyncResult result)
		where TRegistrationDescription : RegistrationDescription
		{
			return NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.End(result).Registration;
		}

		internal RuleDescription EndCreateRule(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateRuleAsyncResult>.End(result).Rule;
		}

		public SubscriptionDescription EndCreateSubscription(IAsyncResult result)
		{
			return this.OnEndCreateSubscription(result);
		}

		public TopicDescription EndCreateTopic(IAsyncResult result)
		{
			return this.OnEndCreateTopic(result);
		}

		internal VolatileTopicDescription EndCreateVolatileTopic(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult>.End(result).Description;
		}

		internal void EndDeleteConsumerGroup(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		internal void EndDeleteEventHub(IAsyncResult result)
		{
			this.OnEndDeleteEventHub(result);
		}

		private void EndDeleteNotificationHub(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		public void EndDeleteQueue(IAsyncResult result)
		{
			this.OnEndDeleteQueue(result);
		}

		public void EndDeleteRegistration(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		internal void EndDeleteRule(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		public void EndDeleteSubscription(IAsyncResult result)
		{
			this.OnEndDeleteSubscription(result);
		}

		public void EndDeleteTopic(IAsyncResult result)
		{
			this.OnEndDeleteTopic(result);
		}

		internal void EndDeleteVolatileTopic(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		internal bool EndEventHubExists(IAsyncResult result)
		{
			return this.OnEndEventHubExists(result);
		}

		private CollectionQueryResult<RegistrationDescription> EndGetAllRegistrations(IAsyncResult result)
		{
			string str;
			CollectionQueryResult<RegistrationDescription> registrationDescriptions;
			try
			{
				SyndicationFeed syndicationFeed = ServiceBusResourceOperations.EndGetAll(result, out str);
				registrationDescriptions = new CollectionQueryResult<RegistrationDescription>((new NamespaceManager.RegistrationSyndicationFeed(syndicationFeed)).Registrations, str);
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				registrationDescriptions = new CollectionQueryResult<RegistrationDescription>(null, string.Empty);
			}
			return registrationDescriptions;
		}

		internal ConsumerGroupDescription EndGetConsumerGroup(IAsyncResult result)
		{
			string[] strArrays;
			ConsumerGroupDescription consumerGroupDescription = ServiceBusResourceOperations.EndGet<ConsumerGroupDescription>(result, out strArrays);
			consumerGroupDescription.EventHubPath = strArrays[0];
			consumerGroupDescription.Name = strArrays[1];
			consumerGroupDescription.IsReadOnly = false;
			return consumerGroupDescription;
		}

		private IEnumerable<ConsumerGroupDescription> EndGetConsumerGroups(IAsyncResult result)
		{
			NamespaceManager.GetAllAsyncResult getAllAsyncResult = AsyncResult<NamespaceManager.GetAllAsyncResult>.End(result);
			SyndicationFeed feed = getAllAsyncResult.Feed;
			return (new NamespaceManager.ConsumerGroupSyndicationFeed(feed, getAllAsyncResult.ResourceNames[0])).ConsumerGroups;
		}

		internal EventHubDescription EndGetEventHub(IAsyncResult result)
		{
			return this.OnEndGetEventHub(result);
		}

		internal PartitionDescription EndGetEventHubPartition(IAsyncResult result)
		{
			string[] strArrays;
			PartitionDescription partitionDescription = ServiceBusResourceOperations.EndGet<PartitionDescription>(result, out strArrays);
			partitionDescription.EventHubPath = strArrays[0];
			partitionDescription.ConsumerGroupName = strArrays[1];
			partitionDescription.PartitionId = strArrays[2];
			return partitionDescription;
		}

		internal IEnumerable<PartitionDescription> EndGetEventHubPartitions(IAsyncResult result)
		{
			return this.OnEndGetEventHubPartitions(result);
		}

		internal IEnumerable<EventHubDescription> EndGetEventHubs(IAsyncResult result)
		{
			return this.OnEndGetEventHubs(result);
		}

		private NotificationHubDescription EndGetNotificationHub(IAsyncResult result)
		{
			string[] strArrays;
			NotificationHubDescription notificationHubDescription = ServiceBusResourceOperations.EndGet<NotificationHubDescription>(result, out strArrays);
			notificationHubDescription.Path = strArrays[0];
			notificationHubDescription.IsReadOnly = false;
			return notificationHubDescription;
		}

		private IEnumerable<NotificationHubDescription> EndGetNotificationHubs(IAsyncResult result)
		{
			return (new NamespaceManager.NotificationHubSyndicationFeed(ServiceBusResourceOperations.EndGetAll(result))).NotificationHubs;
		}

		public QueueDescription EndGetQueue(IAsyncResult result)
		{
			return this.OnEndGetQueue(result);
		}

		public IEnumerable<QueueDescription> EndGetQueues(IAsyncResult result)
		{
			return this.OnEndGetQueues(result);
		}

		private TRegistrationDescription EndGetRegistration<TRegistrationDescription>(IAsyncResult result)
		where TRegistrationDescription : RegistrationDescription
		{
			string[] strArrays;
			TRegistrationDescription tRegistrationDescription = (TRegistrationDescription)ServiceBusResourceOperations.EndGet<RegistrationDescription>(result, out strArrays);
			tRegistrationDescription.NotificationHubPath = strArrays[0];
			tRegistrationDescription.IsReadOnly = false;
			return tRegistrationDescription;
		}

		private CollectionQueryResult<RegistrationDescription> EndGetRegistrationsByChannel(IAsyncResult result)
		{
			string str;
			CollectionQueryResult<RegistrationDescription> registrationDescriptions;
			try
			{
				SyndicationFeed syndicationFeed = ServiceBusResourceOperations.EndGetAll(result, out str);
				registrationDescriptions = new CollectionQueryResult<RegistrationDescription>((new NamespaceManager.RegistrationSyndicationFeed(syndicationFeed)).Registrations, str);
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				registrationDescriptions = new CollectionQueryResult<RegistrationDescription>(null, string.Empty);
			}
			return registrationDescriptions;
		}

		private CollectionQueryResult<RegistrationDescription> EndGetRegistrationsByTag(IAsyncResult result)
		{
			string str;
			CollectionQueryResult<RegistrationDescription> registrationDescriptions;
			try
			{
				SyndicationFeed syndicationFeed = ServiceBusResourceOperations.EndGetAll(result, out str);
				registrationDescriptions = new CollectionQueryResult<RegistrationDescription>((new NamespaceManager.RegistrationSyndicationFeed(syndicationFeed)).Registrations, str);
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				registrationDescriptions = new CollectionQueryResult<RegistrationDescription>(null, string.Empty);
			}
			return registrationDescriptions;
		}

		internal RuleDescription EndGetRule(IAsyncResult result)
		{
			return ServiceBusResourceOperations.EndGet<RuleDescription>(result);
		}

		public IEnumerable<RuleDescription> EndGetRules(IAsyncResult result)
		{
			return this.OnEndGetRules(result);
		}

		public SubscriptionDescription EndGetSubscription(IAsyncResult result)
		{
			return this.OnEndGetSubscription(result);
		}

		public IEnumerable<SubscriptionDescription> EndGetSubscriptions(IAsyncResult result)
		{
			return this.OnEndGetSubscriptions(result);
		}

		public TopicDescription EndGetTopic(IAsyncResult result)
		{
			return this.OnEndGetTopic(result);
		}

		public IEnumerable<TopicDescription> EndGetTopics(IAsyncResult result)
		{
			return this.OnEndGetTopics(result);
		}

		public string EndGetVersionInfo(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.GetVersionInfoAsyncResult>.End(result).Version;
		}

		internal VolatileTopicDescription EndGetVolatileTopic(IAsyncResult result)
		{
			return ServiceBusResourceOperations.EndGet<VolatileTopicDescription>(result);
		}

		internal IEnumerable<VolatileTopicDescription> EndGetVolatileTopics(IAsyncResult result)
		{
			return (new NamespaceManager.VolatileTopicSyndicationFeed(ServiceBusResourceOperations.EndGetAll(result))).VolatileTopics;
		}

		private bool EndNotificationHubExists(IAsyncResult result)
		{
			bool flag;
			try
			{
				flag = ServiceBusResourceOperations.EndGet<NotificationHubDescription>(result) != null;
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				flag = false;
			}
			return flag;
		}

		public bool EndQueueExists(IAsyncResult result)
		{
			return this.OnEndQueueExists(result);
		}

		public bool EndSubscriptionExists(IAsyncResult result)
		{
			return this.OnEndSubscriptionExists(result);
		}

		public bool EndTopicExists(IAsyncResult result)
		{
			return this.OnEndTopicExists(result);
		}

		private ConsumerGroupDescription EndUpdateConsumerGroup(IAsyncResult result)
		{
			return NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult.End(result).ConsumerGroup;
		}

		internal EventHubDescription EndUpdateEventHub(IAsyncResult result)
		{
			return this.OnEndUpdateEventHub(result);
		}

		private NotificationHubDescription EndUpdateNotificationHub(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateNotificationHubAsyncResult>.End(result).NotificationHub;
		}

		public QueueDescription EndUpdateQueue(IAsyncResult result)
		{
			return this.OnEndUpdateQueue(result);
		}

		private TRegistrationDescription EndUpdateRegistration<TRegistrationDescription>(IAsyncResult result)
		where TRegistrationDescription : RegistrationDescription
		{
			return NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.End(result).Registration;
		}

		public SubscriptionDescription EndUpdateSubscription(IAsyncResult result)
		{
			return this.OnEndUpdateSubscription(result);
		}

		public TopicDescription EndUpdateTopic(IAsyncResult result)
		{
			return this.OnEndUpdateTopic(result);
		}

		internal VolatileTopicDescription EndUpdateVolatileTopic(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult>.End(result).Description;
		}

		public bool EventHubExists(string path)
		{
			return this.EndEventHubExists(this.BeginEventHubExists(path, null, null));
		}

		public Task<bool> EventHubExistsAsync(string path)
		{
			return TaskHelpers.CreateTask<bool>((AsyncCallback c, object s) => this.BeginEventHubExists(path, c, s), new Func<IAsyncResult, bool>(this.EndEventHubExists));
		}

		private Task<RelayDescription> FixupRelayDescriptionPath(string originalPath, Task<RelayDescription> entityManagementTask)
		{
			TaskCompletionSource<RelayDescription> taskCompletionSource = new TaskCompletionSource<RelayDescription>();
			entityManagementTask.ContinueWith((Task<RelayDescription> createRelayTask) => {
				if (createRelayTask.IsFaulted)
				{
					taskCompletionSource.TrySetException(createRelayTask.Exception.InnerException);
					return;
				}
				if (createRelayTask.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
					return;
				}
				RelayDescription result = createRelayTask.Result;
				result.Path = originalPath;
				taskCompletionSource.TrySetResult(result);
			});
			return taskCompletionSource.Task;
		}

		internal CollectionQueryResult<RegistrationDescription> GetAllRegistrations(string notificationHubPath, string continuationToken, int top)
		{
			return this.EndGetAllRegistrations(this.BeginGetAllRegistrations(notificationHubPath, continuationToken, top, null, null));
		}

		internal Task<CollectionQueryResult<RegistrationDescription>> GetAllRegistrationsAsync(string notificationHubPath, string continuationToken, int top)
		{
			if (string.IsNullOrWhiteSpace(notificationHubPath))
			{
				throw new ArgumentNullException("notificationHubPath");
			}
			return TaskHelpers.CreateTask<CollectionQueryResult<RegistrationDescription>>((AsyncCallback c, object s) => this.BeginGetAllRegistrations(notificationHubPath, continuationToken, top, c, s), new Func<IAsyncResult, CollectionQueryResult<RegistrationDescription>>(this.EndGetAllRegistrations));
		}

		public ConsumerGroupDescription GetConsumerGroup(string eventHubPath, string name)
		{
			return this.EndGetConsumerGroup(this.BeginGetConsumerGroup(eventHubPath, name, null, null));
		}

		public Task<ConsumerGroupDescription> GetConsumerGroupAsync(string eventHubPath, string name)
		{
			return TaskHelpers.CreateTask<ConsumerGroupDescription>((AsyncCallback c, object s) => this.BeginGetConsumerGroup(eventHubPath, name, c, s), new Func<IAsyncResult, ConsumerGroupDescription>(this.EndGetConsumerGroup));
		}

		public IEnumerable<ConsumerGroupDescription> GetConsumerGroups(string eventHubPath)
		{
			return this.EndGetConsumerGroups(this.BeginGetConsumerGroups(eventHubPath, null, null));
		}

		public Task<IEnumerable<ConsumerGroupDescription>> GetConsumerGroupsAsync(string eventHubPath)
		{
			return TaskHelpers.CreateTask<IEnumerable<ConsumerGroupDescription>>((AsyncCallback c, object s) => this.BeginGetConsumerGroups(eventHubPath, c, s), new Func<IAsyncResult, IEnumerable<ConsumerGroupDescription>>(this.EndGetConsumerGroups));
		}

		public EventHubDescription GetEventHub(string path)
		{
			return this.EndGetEventHub(this.BeginGetEventHub(path, null, null));
		}

		public Task<EventHubDescription> GetEventHubAsync(string path)
		{
			return TaskHelpers.CreateTask<EventHubDescription>((AsyncCallback c, object s) => this.BeginGetEventHub(path, c, s), new Func<IAsyncResult, EventHubDescription>(this.EndGetEventHub));
		}

		public PartitionDescription GetEventHubPartition(string eventHubPath, string name)
		{
			return this.EndGetEventHubPartition(this.BeginGetEventHubPartition(eventHubPath, "$Default", name, null, null));
		}

		public Task<PartitionDescription> GetEventHubPartitionAsync(string eventHubPath, string name)
		{
			return TaskHelpers.CreateTask<PartitionDescription>((AsyncCallback c, object s) => this.BeginGetEventHubPartition(eventHubPath, "$Default", name, c, s), new Func<IAsyncResult, PartitionDescription>(this.EndGetEventHubPartition));
		}

		public Task<PartitionDescription> GetEventHubPartitionAsync(string eventHubPath, string consumerGroupName, string name)
		{
			return TaskHelpers.CreateTask<PartitionDescription>((AsyncCallback c, object s) => this.BeginGetEventHubPartition(eventHubPath, consumerGroupName, name, c, s), new Func<IAsyncResult, PartitionDescription>(this.EndGetEventHubPartition));
		}

		internal IEnumerable<PartitionDescription> GetEventHubPartitions(string eventHubPath)
		{
			return this.EndGetEventHubPartitions(this.BeginGetEventHubPartitions(eventHubPath, "$Default", null, null));
		}

		internal IEnumerable<PartitionDescription> GetEventHubPartitions(string eventHubPath, string consumerGroupName)
		{
			return this.EndGetEventHubPartitions(this.BeginGetEventHubPartitions(eventHubPath, consumerGroupName, null, null));
		}

		internal Task<IEnumerable<PartitionDescription>> GetEventHubPartitionsAsync(string eventHubPath)
		{
			return TaskHelpers.CreateTask<IEnumerable<PartitionDescription>>((AsyncCallback c, object s) => this.BeginGetEventHubPartitions(eventHubPath, "$Default", c, s), new Func<IAsyncResult, IEnumerable<PartitionDescription>>(this.EndGetEventHubPartitions));
		}

		internal Task<IEnumerable<PartitionDescription>> GetEventHubPartitionsAsync(string eventHubPath, string consumerGroupName)
		{
			return TaskHelpers.CreateTask<IEnumerable<PartitionDescription>>((AsyncCallback c, object s) => this.BeginGetEventHubPartitions(eventHubPath, consumerGroupName, c, s), new Func<IAsyncResult, IEnumerable<PartitionDescription>>(this.EndGetEventHubPartitions));
		}

		public IEnumerable<EventHubDescription> GetEventHubs()
		{
			return this.EndGetEventHubs(this.BeginGetEventHubs(null, null));
		}

		public Task<IEnumerable<EventHubDescription>> GetEventHubsAsync()
		{
			return TaskHelpers.CreateTask<IEnumerable<EventHubDescription>>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginGetEventHubs), new Func<IAsyncResult, IEnumerable<EventHubDescription>>(this.EndGetEventHubs));
		}

		public NotificationHubDescription GetNotificationHub(string path)
		{
			return this.EndGetNotificationHub(this.BeginGetNotificationHub(path, null, null));
		}

		public Task<NotificationHubDescription> GetNotificationHubAsync(string path)
		{
			return TaskHelpers.CreateTask<NotificationHubDescription>((AsyncCallback c, object s) => this.BeginGetNotificationHub(path, c, s), new Func<IAsyncResult, NotificationHubDescription>(this.EndGetNotificationHub));
		}

		public Task<NotificationHubJob> GetNotificationHubJobAsync(string jobId, string notificationHubPath)
		{
			NamespaceManager.CheckValidEntityName(jobId, 128, false, "jobId");
			NamespaceManager.CheckValidEntityName(notificationHubPath, 290, false, "notificationHubPath");
			IResourceDescription[] notificationHubJob = new IResourceDescription[] { new NotificationHubJob() };
			string[] strArrays = new string[] { notificationHubPath, jobId };
			return ServiceBusResourceOperations.GetAsync<NotificationHubJob>(notificationHubJob, strArrays, this);
		}

		public Task<IEnumerable<NotificationHubJob>> GetNotificationHubJobsAsync(string notificationHubPath)
		{
			Action<NotificationHubJob, string> action = null;
			NamespaceManager.CheckValidEntityName(notificationHubPath, 290, false, "notificationHubPath");
			TaskCompletionSource<IEnumerable<NotificationHubJob>> taskCompletionSource = new TaskCompletionSource<IEnumerable<NotificationHubJob>>();
			IResourceDescription[] notificationHubJob = new IResourceDescription[] { new NotificationHubJob() };
			string[] strArrays = new string[] { notificationHubPath };
			ServiceBusResourceOperations.GetAllAsync(notificationHubJob, strArrays, this).ContinueWith((Task<SyndicationFeed> getAllTask) => {
				if (getAllTask.IsFaulted)
				{
					taskCompletionSource.TrySetException(getAllTask.Exception.InnerException);
					return;
				}
				if (getAllTask.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
					return;
				}
				SyndicationFeed result = getAllTask.Result;
				if (action == null)
				{
					action = (NotificationHubJob description, string path) => {
					};
				}
				NamespaceManager.EntityDescriptionSyndicationFeed<NotificationHubJob> entityDescriptionSyndicationFeed = new NamespaceManager.EntityDescriptionSyndicationFeed<NotificationHubJob>(result, action);
				taskCompletionSource.TrySetResult(entityDescriptionSyndicationFeed.Entities);
			});
			return taskCompletionSource.Task;
		}

		public IEnumerable<NotificationHubDescription> GetNotificationHubs()
		{
			return this.EndGetNotificationHubs(this.BeginGetNotificationHubs(null, null));
		}

		public Task<IEnumerable<NotificationHubDescription>> GetNotificationHubsAsync()
		{
			return TaskHelpers.CreateTask<IEnumerable<NotificationHubDescription>>((AsyncCallback c, object s) => this.BeginGetNotificationHubs(c, s), new Func<IAsyncResult, IEnumerable<NotificationHubDescription>>(this.EndGetNotificationHubs));
		}

		public QueueDescription GetQueue(string path)
		{
			return this.EndGetQueue(this.BeginGetQueue(path, null, null));
		}

		public Task<QueueDescription> GetQueueAsync(string path)
		{
			return TaskHelpers.CreateTask<QueueDescription>((AsyncCallback c, object s) => this.BeginGetQueue(path, c, s), new Func<IAsyncResult, QueueDescription>(this.EndGetQueue));
		}

		public IEnumerable<QueueDescription> GetQueues()
		{
			return this.EndGetQueues(this.BeginGetQueues(null, null));
		}

		public IEnumerable<QueueDescription> GetQueues(string filter)
		{
			return this.EndGetQueues(this.BeginGetQueues(filter, null, null));
		}

		public Task<IEnumerable<QueueDescription>> GetQueuesAsync()
		{
			return TaskHelpers.CreateTask<IEnumerable<QueueDescription>>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginGetQueues), new Func<IAsyncResult, IEnumerable<QueueDescription>>(this.EndGetQueues));
		}

		public Task<IEnumerable<QueueDescription>> GetQueuesAsync(string filter)
		{
			return TaskHelpers.CreateTask<IEnumerable<QueueDescription>>((AsyncCallback c, object s) => this.BeginGetQueues(filter, c, s), new Func<IAsyncResult, IEnumerable<QueueDescription>>(this.EndGetQueues));
		}

		internal TRegistrationDescription GetRegistration<TRegistrationDescription>(string registrationId, string notificationHubPath)
		where TRegistrationDescription : RegistrationDescription
		{
			return this.EndGetRegistration<TRegistrationDescription>(this.BeginGetRegistration<TRegistrationDescription>(registrationId, notificationHubPath, null, null));
		}

		internal Task<TRegistrationDescription> GetRegistrationAsync<TRegistrationDescription>(string registrationId, string notificationHubPath)
		where TRegistrationDescription : RegistrationDescription
		{
			return TaskHelpers.CreateTask<TRegistrationDescription>((AsyncCallback c, object s) => this.BeginGetRegistration<TRegistrationDescription>(registrationId, notificationHubPath, c, s), new Func<IAsyncResult, TRegistrationDescription>(this.EndGetRegistration<TRegistrationDescription>));
		}

		internal CollectionQueryResult<RegistrationDescription> GetRegistrationsByChannel(string pnsHandle, string notificationHubPath, string continuationToken, int top)
		{
			return this.EndGetRegistrationsByChannel(this.BeginGetRegistrationsByChannel(pnsHandle, notificationHubPath, continuationToken, top, null, null));
		}

		internal Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByChannelAsync(string pnsHandle, string notificationHubPath, string continuationToken, int top)
		{
			if (string.IsNullOrWhiteSpace(pnsHandle))
			{
				throw new ArgumentNullException("pnsHandle");
			}
			if (string.IsNullOrWhiteSpace(notificationHubPath))
			{
				throw new ArgumentNullException("notificationHubPath");
			}
			return TaskHelpers.CreateTask<CollectionQueryResult<RegistrationDescription>>((AsyncCallback c, object s) => this.BeginGetRegistrationsByChannel(pnsHandle, notificationHubPath, continuationToken, top, c, s), new Func<IAsyncResult, CollectionQueryResult<RegistrationDescription>>(this.EndGetRegistrationsByChannel));
		}

		internal CollectionQueryResult<RegistrationDescription> GetRegistrationsByTag(string notificationHubPath, string tag, string continuationToken, int top)
		{
			return this.EndGetRegistrationsByTag(this.BeginGetRegistrationsByTag(notificationHubPath, tag, continuationToken, top, null, null));
		}

		internal Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByTagAsync(string notificationHubPath, string tag, string continuationToken, int top)
		{
			return TaskHelpers.CreateTask<CollectionQueryResult<RegistrationDescription>>((AsyncCallback c, object s) => this.BeginGetRegistrationsByTag(notificationHubPath, tag, continuationToken, top, c, s), new Func<IAsyncResult, CollectionQueryResult<RegistrationDescription>>(this.EndGetRegistrationsByTag));
		}

		public Task<RelayDescription> GetRelayAsync(string path)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			string[] strArrays = new string[] { path };
			return this.FixupRelayDescriptionPath(path, ServiceBusResourceOperations.GetAsync<RelayDescription>(strArrays, this));
		}

		public Task<IEnumerable<RelayDescription>> GetRelaysAsync()
		{
			Action<RelayDescription, string> action = null;
			TaskCompletionSource<IEnumerable<RelayDescription>> taskCompletionSource = new TaskCompletionSource<IEnumerable<RelayDescription>>();
			IResourceDescription[] relayDescription = new IResourceDescription[] { new RelayDescription() };
			ServiceBusResourceOperations.GetAllAsync(relayDescription, this).ContinueWith((Task<SyndicationFeed> getAllTask) => {
				if (getAllTask.IsFaulted)
				{
					taskCompletionSource.TrySetException(getAllTask.Exception.InnerException);
					return;
				}
				if (getAllTask.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
					return;
				}
				SyndicationFeed result = getAllTask.Result;
				if (action == null)
				{
					action = (RelayDescription description, string path) => description.Path = path;
				}
				NamespaceManager.EntityDescriptionSyndicationFeed<RelayDescription> entityDescriptionSyndicationFeed = new NamespaceManager.EntityDescriptionSyndicationFeed<RelayDescription>(result, action);
				taskCompletionSource.TrySetResult(entityDescriptionSyndicationFeed.Entities);
			});
			return taskCompletionSource.Task;
		}

		public IEnumerable<RuleDescription> GetRules(string topicPath, string subscriptionName)
		{
			return this.EndGetRules(this.BeginGetRules(topicPath, subscriptionName, null, null));
		}

		public IEnumerable<RuleDescription> GetRules(string topicPath, string subscriptionName, string filter)
		{
			return this.EndGetRules(this.BeginGetRules(topicPath, subscriptionName, filter, null, null));
		}

		public Task<IEnumerable<RuleDescription>> GetRulesAsync(string topicPath, string subscriptionName)
		{
			return TaskHelpers.CreateTask<IEnumerable<RuleDescription>>((AsyncCallback c, object s) => this.BeginGetRules(topicPath, subscriptionName, c, s), new Func<IAsyncResult, IEnumerable<RuleDescription>>(this.EndGetRules));
		}

		public Task<IEnumerable<RuleDescription>> GetRulesAsync(string topicPath, string subscriptionName, string filter)
		{
			return TaskHelpers.CreateTask<IEnumerable<RuleDescription>>((AsyncCallback c, object s) => this.BeginGetRules(topicPath, subscriptionName, filter, c, s), new Func<IAsyncResult, IEnumerable<RuleDescription>>(this.EndGetRules));
		}

		public SubscriptionDescription GetSubscription(string topicPath, string name)
		{
			return this.EndGetSubscription(this.BeginGetSubscription(topicPath, name, null, null));
		}

		public Task<SubscriptionDescription> GetSubscriptionAsync(string topicPath, string name)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginGetSubscription(topicPath, name, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndGetSubscription));
		}

		public IEnumerable<SubscriptionDescription> GetSubscriptions(string topicPath)
		{
			return this.EndGetSubscriptions(this.BeginGetSubscriptions(topicPath, null, null));
		}

		public IEnumerable<SubscriptionDescription> GetSubscriptions(string topicPath, string filter)
		{
			return this.EndGetSubscriptions(this.BeginGetSubscriptions(topicPath, filter, null, null));
		}

		public Task<IEnumerable<SubscriptionDescription>> GetSubscriptionsAsync(string topicPath)
		{
			return TaskHelpers.CreateTask<IEnumerable<SubscriptionDescription>>((AsyncCallback c, object s) => this.BeginGetSubscriptions(topicPath, c, s), new Func<IAsyncResult, IEnumerable<SubscriptionDescription>>(this.EndGetSubscriptions));
		}

		public Task<IEnumerable<SubscriptionDescription>> GetSubscriptionsAsync(string topicPath, string filter)
		{
			return TaskHelpers.CreateTask<IEnumerable<SubscriptionDescription>>((AsyncCallback c, object s) => this.BeginGetSubscriptions(topicPath, filter, c, s), new Func<IAsyncResult, IEnumerable<SubscriptionDescription>>(this.EndGetSubscriptions));
		}

		public TopicDescription GetTopic(string path)
		{
			return this.EndGetTopic(this.BeginGetTopic(path, null, null));
		}

		public Task<TopicDescription> GetTopicAsync(string path)
		{
			return TaskHelpers.CreateTask<TopicDescription>((AsyncCallback c, object s) => this.BeginGetTopic(path, c, s), new Func<IAsyncResult, TopicDescription>(this.EndGetTopic));
		}

		public IEnumerable<TopicDescription> GetTopics()
		{
			return this.EndGetTopics(this.BeginGetTopics(null, null));
		}

		public IEnumerable<TopicDescription> GetTopics(string filter)
		{
			return this.EndGetTopics(this.BeginGetTopics(filter, null, null));
		}

		public Task<IEnumerable<TopicDescription>> GetTopicsAsync()
		{
			return TaskHelpers.CreateTask<IEnumerable<TopicDescription>>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginGetTopics), new Func<IAsyncResult, IEnumerable<TopicDescription>>(this.EndGetTopics));
		}

		public Task<IEnumerable<TopicDescription>> GetTopicsAsync(string filter)
		{
			return TaskHelpers.CreateTask<IEnumerable<TopicDescription>>((AsyncCallback c, object s) => this.BeginGetTopics(filter, c, s), new Func<IAsyncResult, IEnumerable<TopicDescription>>(this.EndGetTopics));
		}

		public string GetVersionInfo()
		{
			return this.EndGetVersionInfo(this.BeginGetVersionInfo(null, null));
		}

		public Task<string> GetVersionInfoAsync()
		{
			return TaskHelpers.CreateTask<string>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginGetVersionInfo), new Func<IAsyncResult, string>(this.EndGetVersionInfo));
		}

		internal VolatileTopicDescription GetVolatileTopic(string path)
		{
			return this.EndGetVolatileTopic(this.BeginGetVolatileTopic(path, null, null));
		}

		internal Task<VolatileTopicDescription> GetVolatileTopicAsync(string path)
		{
			return TaskHelpers.CreateTask<VolatileTopicDescription>((AsyncCallback c, object s) => this.BeginGetVolatileTopic(path, c, s), new Func<IAsyncResult, VolatileTopicDescription>(this.EndGetVolatileTopic));
		}

		internal IEnumerable<VolatileTopicDescription> GetVolatileTopics()
		{
			return this.EndGetVolatileTopics(this.BeginGetVolatileTopics(null, null));
		}

		internal IEnumerable<VolatileTopicDescription> GetVolatileTopics(string filter)
		{
			return this.EndGetVolatileTopics(this.BeginGetVolatileTopics(filter, null, null));
		}

		internal Task<IEnumerable<VolatileTopicDescription>> GetVolatileTopicsAsync()
		{
			return TaskHelpers.CreateTask<IEnumerable<VolatileTopicDescription>>(new Func<AsyncCallback, object, IAsyncResult>(this.BeginGetVolatileTopics), new Func<IAsyncResult, IEnumerable<VolatileTopicDescription>>(this.EndGetVolatileTopics));
		}

		internal Task<IEnumerable<VolatileTopicDescription>> GetVolatileTopicsAsync(string filter)
		{
			return TaskHelpers.CreateTask<IEnumerable<VolatileTopicDescription>>((AsyncCallback c, object s) => this.BeginGetVolatileTopics(filter, c, s), new Func<IAsyncResult, IEnumerable<VolatileTopicDescription>>(this.EndGetVolatileTopics));
		}

		public bool NotificationHubExists(string path)
		{
			return this.EndNotificationHubExists(this.BeginNotificationHubExists(path, null, null));
		}

		public Task<bool> NotificationHubExistsAsync(string path)
		{
			return TaskHelpers.CreateTask<bool>((AsyncCallback c, object s) => this.BeginNotificationHubExists(path, c, s), new Func<IAsyncResult, bool>(this.EndNotificationHubExists));
		}

		private IAsyncResult OnBeginCreateEventHub(EventHubDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateEventHubAsyncResult createOrUpdateEventHubAsyncResult = new NamespaceManager.CreateOrUpdateEventHubAsyncResult(this, description, false, callback, state);
			createOrUpdateEventHubAsyncResult.Start();
			return createOrUpdateEventHubAsyncResult;
		}

		private IAsyncResult OnBeginCreateQueue(QueueDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateQueueAsyncResult createOrUpdateQueueAsyncResult = new NamespaceManager.CreateOrUpdateQueueAsyncResult(this, description, false, callback, state);
			createOrUpdateQueueAsyncResult.Start();
			return createOrUpdateQueueAsyncResult;
		}

		private IAsyncResult OnBeginCreateSubscription(RuleDescription ruleDescription, SubscriptionDescription description, AsyncCallback callback, object state)
		{
			if (ruleDescription == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("ruleDescription");
			}
			return new NamespaceManager.CreateOrUpdateSubscriptionAsyncResult(this, description, ruleDescription, false, callback, state);
		}

		private IAsyncResult OnBeginCreateTopic(TopicDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateTopicAsyncResult createOrUpdateTopicAsyncResult = new NamespaceManager.CreateOrUpdateTopicAsyncResult(this, description, false, callback, state);
			createOrUpdateTopicAsyncResult.Start();
			return createOrUpdateTopicAsyncResult;
		}

		private IAsyncResult OnBeginDeleteEventHub(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginDelete(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginDeleteQueue(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginDelete(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginDeleteSubscription(string name, string topicPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(name, 50, false, "name");
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, name));
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription() };
			string[] strArrays = new string[] { topicPath, name };
			return ServiceBusResourceOperations.BeginDelete(instance, subscriptionDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginDeleteTopic(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginDelete(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginEventHubExists(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<EventHubDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginGetEventHub(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<EventHubDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginGetEventHubPartitions(string eventHubPath, string consumerGroupName, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(eventHubPath, 260, "eventHubPath");
			NamespaceManager.CheckValidEntityName(consumerGroupName, 50, "consumerGroupName");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), eventHubPath);
			IResourceDescription[] consumerGroupDescription = new IResourceDescription[] { new ConsumerGroupDescription(), new PartitionDescription() };
			string[] strArrays = new string[] { eventHubPath, consumerGroupName };
			return new NamespaceManager.GetAllAsyncResult(instance, consumerGroupDescription, strArrays, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetEventHubs(AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] eventHubDescription = new IResourceDescription[] { new EventHubDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, eventHubDescription, null, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetQueue(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<QueueDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginGetQueues(AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] queueDescription = new IResourceDescription[] { new QueueDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, queueDescription, null, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetQueues(string filter, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] queueDescription = new IResourceDescription[] { new QueueDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, filter, queueDescription, null, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetRules(string subscriptionName, string topicPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(subscriptionName, 50, false, "subscriptionName");
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, subscriptionName));
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription(), new RuleDescription() };
			string[] strArrays = new string[] { topicPath, subscriptionName };
			return new NamespaceManager.GetAllAsyncResult(instance, subscriptionDescription, strArrays, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetRules(string subscriptionName, string topicPath, string filter, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(subscriptionName, 50, false, "subscriptionName");
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, subscriptionName));
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription(), new RuleDescription() };
			string[] strArrays = new string[] { topicPath, subscriptionName };
			return new NamespaceManager.GetAllAsyncResult(instance, subscriptionDescription, strArrays, filter, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetSubscription(string name, string topicPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(name, 50, false, "name");
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, name));
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription() };
			string[] strArrays = new string[] { topicPath, name };
			return ServiceBusResourceOperations.BeginGet<SubscriptionDescription>(instance, subscriptionDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginGetSubscriptions(string topicPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), topicPath);
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription() };
			string[] strArrays = new string[] { topicPath };
			return new NamespaceManager.GetAllAsyncResult(instance, subscriptionDescription, strArrays, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetSubscriptions(string topicPath, string filter, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), topicPath);
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription() };
			string[] strArrays = new string[] { topicPath };
			return new NamespaceManager.GetAllAsyncResult(instance, subscriptionDescription, strArrays, filter, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetTopic(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<TopicDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginGetTopics(AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] topicDescription = new IResourceDescription[] { new TopicDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, topicDescription, null, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginGetTopics(string filter, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			IResourceDescription[] topicDescription = new IResourceDescription[] { new TopicDescription() };
			return ServiceBusResourceOperations.BeginGetAll(instance, filter, topicDescription, null, this.addresses, this.settings, callback, state);
		}

		private IAsyncResult OnBeginQueueExists(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<QueueDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginSubscriptionExists(string name, string topicPath, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(name, 50, false, "name");
			NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, name));
			IResourceDescription[] subscriptionDescription = new IResourceDescription[] { new SubscriptionDescription() };
			string[] strArrays = new string[] { topicPath, name };
			return ServiceBusResourceOperations.BeginGet<SubscriptionDescription>(instance, subscriptionDescription, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginTopicExists(string path, AsyncCallback callback, object state)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), path);
			string[] strArrays = new string[] { path };
			return ServiceBusResourceOperations.BeginGet<TopicDescription>(instance, null, strArrays, this.addresses, this.settings, this.settings.InternalOperationTimeout, callback, state);
		}

		private IAsyncResult OnBeginUpdateEventHub(EventHubDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateEventHubAsyncResult createOrUpdateEventHubAsyncResult = new NamespaceManager.CreateOrUpdateEventHubAsyncResult(this, description, true, callback, state);
			createOrUpdateEventHubAsyncResult.Start();
			return createOrUpdateEventHubAsyncResult;
		}

		private IAsyncResult OnBeginUpdateQueue(QueueDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateQueueAsyncResult createOrUpdateQueueAsyncResult = new NamespaceManager.CreateOrUpdateQueueAsyncResult(this, description, true, callback, state);
			createOrUpdateQueueAsyncResult.Start();
			return createOrUpdateQueueAsyncResult;
		}

		private IAsyncResult OnBeginUpdateSubscription(SubscriptionDescription description, AsyncCallback callback, object state)
		{
			return new NamespaceManager.CreateOrUpdateSubscriptionAsyncResult(this, description, null, true, callback, state);
		}

		private IAsyncResult OnBeginUpdateTopic(TopicDescription description, AsyncCallback callback, object state)
		{
			NamespaceManager.CreateOrUpdateTopicAsyncResult createOrUpdateTopicAsyncResult = new NamespaceManager.CreateOrUpdateTopicAsyncResult(this, description, true, callback, state);
			createOrUpdateTopicAsyncResult.Start();
			return createOrUpdateTopicAsyncResult;
		}

		private EventHubDescription OnEndCreateEventHub(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateEventHubAsyncResult>.End(result).EventHub;
		}

		private QueueDescription OnEndCreateQueue(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateQueueAsyncResult>.End(result).Queue;
		}

		private SubscriptionDescription OnEndCreateSubscription(IAsyncResult result)
		{
			return NamespaceManager.CreateOrUpdateSubscriptionAsyncResult.End(result).Subscription;
		}

		private TopicDescription OnEndCreateTopic(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateTopicAsyncResult>.End(result).Topic;
		}

		private void OnEndDeleteEventHub(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		private void OnEndDeleteQueue(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		private void OnEndDeleteSubscription(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		private void OnEndDeleteTopic(IAsyncResult result)
		{
			ServiceBusResourceOperations.EndDelete(result);
		}

		private bool OnEndEventHubExists(IAsyncResult result)
		{
			bool flag;
			try
			{
				flag = ServiceBusResourceOperations.EndGet<EventHubDescription>(result) != null;
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				flag = false;
			}
			return flag;
		}

		private EventHubDescription OnEndGetEventHub(IAsyncResult result)
		{
			string[] strArrays;
			EventHubDescription eventHubDescription = ServiceBusResourceOperations.EndGet<EventHubDescription>(result, out strArrays);
			eventHubDescription.Path = strArrays[0];
			eventHubDescription.IsReadOnly = false;
			return eventHubDescription;
		}

		private IEnumerable<PartitionDescription> OnEndGetEventHubPartitions(IAsyncResult result)
		{
			NamespaceManager.GetAllAsyncResult getAllAsyncResult = AsyncResult<NamespaceManager.GetAllAsyncResult>.End(result);
			SyndicationFeed feed = getAllAsyncResult.Feed;
			string resourceNames = getAllAsyncResult.ResourceNames[0];
			string str = getAllAsyncResult.ResourceNames[1];
			return (new NamespaceManager.PartitionSyndicationFeed(feed, resourceNames, str)).Partitions;
		}

		private IEnumerable<EventHubDescription> OnEndGetEventHubs(IAsyncResult result)
		{
			return (new NamespaceManager.EventHubSyndicationFeed(ServiceBusResourceOperations.EndGetAll(result))).EventHubs;
		}

		private QueueDescription OnEndGetQueue(IAsyncResult result)
		{
			string[] strArrays;
			QueueDescription queueDescription = ServiceBusResourceOperations.EndGet<QueueDescription>(result, out strArrays);
			queueDescription.Path = strArrays[0];
			queueDescription.IsReadOnly = false;
			return queueDescription;
		}

		private IEnumerable<QueueDescription> OnEndGetQueues(IAsyncResult result)
		{
			return (new NamespaceManager.QueueSyndicationFeed(ServiceBusResourceOperations.EndGetAll(result))).Queues;
		}

		private IEnumerable<RuleDescription> OnEndGetRules(IAsyncResult result)
		{
			NamespaceManager.GetAllAsyncResult getAllAsyncResult = AsyncResult<NamespaceManager.GetAllAsyncResult>.End(result);
			string resourceNames = getAllAsyncResult.ResourceNames[0];
			string str = getAllAsyncResult.ResourceNames[1];
			return (new NamespaceManager.RuleSyndicationFeed(getAllAsyncResult.Feed, str, resourceNames)).Rules;
		}

		private SubscriptionDescription OnEndGetSubscription(IAsyncResult result)
		{
			string[] strArrays;
			SubscriptionDescription subscriptionDescription = ServiceBusResourceOperations.EndGet<SubscriptionDescription>(result, out strArrays);
			subscriptionDescription.TopicPath = strArrays[0];
			subscriptionDescription.Name = strArrays[1];
			subscriptionDescription.IsReadOnly = false;
			return subscriptionDescription;
		}

		private IEnumerable<SubscriptionDescription> OnEndGetSubscriptions(IAsyncResult result)
		{
			NamespaceManager.GetAllAsyncResult getAllAsyncResult = AsyncResult<NamespaceManager.GetAllAsyncResult>.End(result);
			SyndicationFeed feed = getAllAsyncResult.Feed;
			return (new NamespaceManager.SubscriptionSyndicationFeed(feed, getAllAsyncResult.ResourceNames[0])).Subscriptions;
		}

		private TopicDescription OnEndGetTopic(IAsyncResult result)
		{
			string[] strArrays;
			TopicDescription topicDescription = ServiceBusResourceOperations.EndGet<TopicDescription>(result, out strArrays);
			topicDescription.Path = strArrays[0];
			topicDescription.IsReadOnly = false;
			return topicDescription;
		}

		private IEnumerable<TopicDescription> OnEndGetTopics(IAsyncResult result)
		{
			return (new NamespaceManager.TopicSyndicationFeed(ServiceBusResourceOperations.EndGetAll(result))).Topics;
		}

		private bool OnEndQueueExists(IAsyncResult result)
		{
			bool flag;
			try
			{
				flag = ServiceBusResourceOperations.EndGet<QueueDescription>(result) != null;
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				flag = false;
			}
			return flag;
		}

		private bool OnEndSubscriptionExists(IAsyncResult result)
		{
			bool flag;
			try
			{
				flag = ServiceBusResourceOperations.EndGet<SubscriptionDescription>(result) != null;
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				flag = false;
			}
			return flag;
		}

		private bool OnEndTopicExists(IAsyncResult result)
		{
			bool flag;
			try
			{
				flag = ServiceBusResourceOperations.EndGet<TopicDescription>(result) != null;
			}
			catch (MessagingEntityNotFoundException messagingEntityNotFoundException)
			{
				flag = false;
			}
			return flag;
		}

		private EventHubDescription OnEndUpdateEventHub(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateEventHubAsyncResult>.End(result).EventHub;
		}

		private QueueDescription OnEndUpdateQueue(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateQueueAsyncResult>.End(result).Queue;
		}

		private SubscriptionDescription OnEndUpdateSubscription(IAsyncResult result)
		{
			return NamespaceManager.CreateOrUpdateSubscriptionAsyncResult.End(result).Subscription;
		}

		private TopicDescription OnEndUpdateTopic(IAsyncResult result)
		{
			return AsyncResult<NamespaceManager.CreateOrUpdateTopicAsyncResult>.End(result).Topic;
		}

		public bool QueueExists(string path)
		{
			return this.EndQueueExists(this.BeginQueueExists(path, null, null));
		}

		public Task<bool> QueueExistsAsync(string path)
		{
			return TaskHelpers.CreateTask<bool>((AsyncCallback c, object s) => this.BeginQueueExists(path, c, s), new Func<IAsyncResult, bool>(this.EndQueueExists));
		}

		public Task<bool> RelayExistsAsync(string path)
		{
			NamespaceManager.CheckValidEntityName(path, 260, "path");
			TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
			this.GetRelayAsync(path).ContinueWith((Task<RelayDescription> getTask) => {
				if (!getTask.IsFaulted)
				{
					if (getTask.IsCanceled)
					{
						taskCompletionSource.TrySetCanceled();
						return;
					}
					RelayDescription result = getTask.Result;
					taskCompletionSource.TrySetResult(true);
					return;
				}
				if (getTask.Exception.InnerException is MessagingEntityNotFoundException)
				{
					taskCompletionSource.TrySetResult(false);
					return;
				}
				taskCompletionSource.TrySetException(getTask.Exception.InnerException);
			});
			return taskCompletionSource.Task;
		}

		public Task<NotificationHubJob> SubmitNotificationHubJobAsync(NotificationHubJob job, string notificationHubPath)
		{
			NamespaceManager.CheckValidEntityName(notificationHubPath, 290, false, "notificationHubPath");
			if (job == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("job");
			}
			if (job.OutputContainerUri == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("OutputContainerUri");
			}
			IResourceDescription[] notificationHubJob = new IResourceDescription[] { new NotificationHubJob() };
			string[] strArrays = new string[] { notificationHubPath, string.Empty };
			return ServiceBusResourceOperations.CreateAsync<NotificationHubJob>(job, notificationHubJob, strArrays, this);
		}

		public bool SubscriptionExists(string topicPath, string name)
		{
			return this.EndSubscriptionExists(this.BeginSubscriptionExists(topicPath, name, null, null));
		}

		public Task<bool> SubscriptionExistsAsync(string topicPath, string name)
		{
			return TaskHelpers.CreateTask<bool>((AsyncCallback c, object s) => this.BeginSubscriptionExists(topicPath, name, c, s), new Func<IAsyncResult, bool>(this.EndSubscriptionExists));
		}

		public bool TopicExists(string path)
		{
			return this.EndTopicExists(this.BeginTopicExists(path, null, null));
		}

		public Task<bool> TopicExistsAsync(string path)
		{
			return TaskHelpers.CreateTask<bool>((AsyncCallback c, object s) => this.BeginTopicExists(path, c, s), new Func<IAsyncResult, bool>(this.EndTopicExists));
		}

		public ConsumerGroupDescription UpdateConsumerGroup(ConsumerGroupDescription description)
		{
			return this.EndUpdateConsumerGroup(this.BeginUpdateConsumerGroup(description, null, null));
		}

		public Task<ConsumerGroupDescription> UpdateConsumerGroupAsync(ConsumerGroupDescription description)
		{
			return TaskHelpers.CreateTask<ConsumerGroupDescription>((AsyncCallback c, object s) => this.BeginUpdateConsumerGroup(description, c, s), new Func<IAsyncResult, ConsumerGroupDescription>(this.EndUpdateConsumerGroup));
		}

		public EventHubDescription UpdateEventHub(EventHubDescription description)
		{
			return this.EndUpdateEventHub(this.BeginUpdateEventHub(description, null, null));
		}

		public Task<EventHubDescription> UpdateEventHubAsync(EventHubDescription description)
		{
			return TaskHelpers.CreateTask<EventHubDescription>((AsyncCallback c, object s) => this.BeginUpdateEventHub(description, c, s), new Func<IAsyncResult, EventHubDescription>(this.EndUpdateEventHub));
		}

		public NotificationHubDescription UpdateNotificationHub(NotificationHubDescription description)
		{
			return this.EndUpdateNotificationHub(this.BeginUpdateNotificationHub(description, null, null));
		}

		public Task<NotificationHubDescription> UpdateNotificationHubAsync(NotificationHubDescription description)
		{
			return TaskHelpers.CreateTask<NotificationHubDescription>((AsyncCallback c, object s) => this.BeginUpdateNotificationHub(description, c, s), new Func<IAsyncResult, NotificationHubDescription>(this.EndUpdateNotificationHub));
		}

		public QueueDescription UpdateQueue(QueueDescription description)
		{
			return this.EndUpdateQueue(this.BeginUpdateQueue(description, null, null));
		}

		public Task<QueueDescription> UpdateQueueAsync(QueueDescription description)
		{
			return TaskHelpers.CreateTask<QueueDescription>((AsyncCallback c, object s) => this.BeginUpdateQueue(description, c, s), new Func<IAsyncResult, QueueDescription>(this.EndUpdateQueue));
		}

		internal TRegistrationDescription UpdateRegistration<TRegistrationDescription>(TRegistrationDescription registrationDescription)
		where TRegistrationDescription : RegistrationDescription
		{
			return this.EndUpdateRegistration<TRegistrationDescription>(this.BeginUpdateRegistration<TRegistrationDescription>(registrationDescription, null, null));
		}

		internal Task<TRegistrationDescription> UpdateRegistrationAsync<TRegistrationDescription>(TRegistrationDescription registrationDescription)
		where TRegistrationDescription : RegistrationDescription
		{
			return TaskHelpers.CreateTask<TRegistrationDescription>((AsyncCallback c, object s) => this.BeginUpdateRegistration<TRegistrationDescription>(registrationDescription, c, s), new Func<IAsyncResult, TRegistrationDescription>(this.EndUpdateRegistration<TRegistrationDescription>));
		}

		public Task<RelayDescription> UpdateRelayAsync(RelayDescription description)
		{
			if (description == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
			}
			NamespaceManager.CheckValidEntityName(description.Path, 260, "description.Path");
			string[] path = new string[] { description.Path };
			return ServiceBusResourceOperations.UpdateAsync<RelayDescription>(description, path, this);
		}

		public SubscriptionDescription UpdateSubscription(SubscriptionDescription description)
		{
			return this.EndUpdateSubscription(this.BeginUpdateSubscription(description, null, null));
		}

		public Task<SubscriptionDescription> UpdateSubscriptionAsync(SubscriptionDescription description)
		{
			return TaskHelpers.CreateTask<SubscriptionDescription>((AsyncCallback c, object s) => this.BeginUpdateSubscription(description, c, s), new Func<IAsyncResult, SubscriptionDescription>(this.EndUpdateSubscription));
		}

		public TopicDescription UpdateTopic(TopicDescription description)
		{
			return this.EndUpdateTopic(this.BeginUpdateTopic(description, null, null));
		}

		public Task<TopicDescription> UpdateTopicAsync(TopicDescription description)
		{
			return TaskHelpers.CreateTask<TopicDescription>((AsyncCallback c, object s) => this.BeginUpdateTopic(description, c, s), new Func<IAsyncResult, TopicDescription>(this.EndUpdateTopic));
		}

		internal VolatileTopicDescription UpdateVolatileTopic(VolatileTopicDescription description)
		{
			NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult createOrUpdateVolatileTopicAsyncResult = new NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult(this, description, true, null, null);
			createOrUpdateVolatileTopicAsyncResult.RunSynchronously();
			return createOrUpdateVolatileTopicAsyncResult.Description;
		}

		internal Task<VolatileTopicDescription> UpdateVolatileTopicAsync(VolatileTopicDescription description)
		{
			return TaskHelpers.CreateTask<VolatileTopicDescription>((AsyncCallback c, object s) => this.BeginCreateVolatileTopic(description, c, s), new Func<IAsyncResult, VolatileTopicDescription>(this.EndCreateVolatileTopic));
		}

		private string ValidateAndNormalizeForwardToAddress(string entityName, string forwardTo)
		{
			Uri uri;
			NamespaceManager.CheckValidEntityName(forwardTo, NamespaceManager.ForwardToEntityNameMaxLength, "description.ForwardTo");
			if (!Uri.TryCreate(forwardTo, UriKind.Absolute, out uri))
			{
				string absoluteUri = this.Address.AbsoluteUri;
				if (!absoluteUri.EndsWith("/", StringComparison.Ordinal))
				{
					absoluteUri = string.Concat(absoluteUri, "/");
				}
				uri = new Uri(new Uri(absoluteUri), forwardTo);
			}
			return uri.AbsoluteUri;
		}

		private class ConsumerGroupSyndicationFeed
		{
			public IEnumerable<ConsumerGroupDescription> ConsumerGroups
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Unable to read Atom XML content"), null);
								}
								ConsumerGroupDescription eventHubPath = content.ReadContent<ConsumerGroupDescription>();
								eventHubPath.EventHubPath = this.EventHubPath;
								eventHubPath.Name = text;
								eventHubPath.IsReadOnly = false;
								yield return eventHubPath;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Atom Xml Title property is empty."), null);
				}
			}

			public string EventHubPath
			{
				get;
				private set;
			}

			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public ConsumerGroupSyndicationFeed(SyndicationFeed feed, string eventHubPath)
			{
				this.EventHubPath = eventHubPath;
				this.Feed = feed;
			}
		}

		private sealed class CreateConsumerGroupIfNotExistsAsyncResult : IteratorAsyncResult<NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			public ConsumerGroupDescription ConsumerGroup
			{
				get;
				private set;
			}

			public CreateConsumerGroupIfNotExistsAsyncResult(NamespaceManager manager, ConsumerGroupDescription description, AsyncCallback callback, object state) : base(manager.settings.InternalOperationTimeout, callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.EventHubPath, 260, "description.EventHubPath");
				NamespaceManager.CheckValidEntityName(description.Name, 50, "description.Name");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatConsumerGroupPath(description.EventHubPath, description.Name));
				this.manager = manager;
				this.ConsumerGroup = description;
			}

			private static void CreateDescriptionAfterCreate(NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult thisPtr, IAsyncResult r)
			{
				string eventHubPath = thisPtr.ConsumerGroup.EventHubPath;
				string name = thisPtr.ConsumerGroup.Name;
				thisPtr.ConsumerGroup = NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult.End(r).ConsumerGroup;
				thisPtr.ConsumerGroup.EventHubPath = eventHubPath;
				thisPtr.ConsumerGroup.Name = name;
				thisPtr.ConsumerGroup.IsReadOnly = false;
			}

			private static void CreateDescriptionAfterGet(NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult thisPtr, IAsyncResult r)
			{
				string[] strArrays;
				thisPtr.ConsumerGroup = ServiceBusResourceOperations.EndGet<ConsumerGroupDescription>(r, out strArrays);
				thisPtr.ConsumerGroup.EventHubPath = strArrays[0];
				thisPtr.ConsumerGroup.Name = strArrays[1];
				thisPtr.ConsumerGroup.IsReadOnly = false;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
					if (base.RemainingTime() > TimeSpan.Zero)
					{
						NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult createConsumerGroupIfNotExistsAsyncResult = this;
						yield return createConsumerGroupIfNotExistsAsyncResult.CallAsync((NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => new NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult(thisPtr.trackingContext, thisPtr.manager, thisPtr.ConsumerGroup, false, c, s), new IteratorAsyncResult<NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult>.EndCall(NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult.CreateDescriptionAfterCreate), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							break;
						}
						bool flag = false;
						if (base.LastAsyncStepException is MessagingEntityAlreadyExistsException)
						{
							NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult createConsumerGroupIfNotExistsAsyncResult1 = this;
							yield return createConsumerGroupIfNotExistsAsyncResult1.CallAsync((NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginGet<ConsumerGroupDescription>(thisPtr.trackingContext, new IResourceDescription[] { new ConsumerGroupDescription() }, new string[] { thisPtr.ConsumerGroup.EventHubPath, thisPtr.ConsumerGroup.Name }, thisPtr.manager.addresses, thisPtr.manager.settings, thisPtr.manager.settings.InternalOperationTimeout, c, s), new IteratorAsyncResult<NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult>.EndCall(NamespaceManager.CreateConsumerGroupIfNotExistsAsyncResult.CreateDescriptionAfterGet), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							if (base.LastAsyncStepException is MessagingEntityNotFoundException)
							{
								base.LastAsyncStepException = null;
								flag = true;
							}
						}
						if (!flag)
						{
							base.Complete(base.LastAsyncStepException);
							break;
						}
					}
					else
					{
						base.Complete(new TimeoutException(SRCore.TimeoutOnOperation(base.OriginalTimeout)));
						break;
					}
				}
			}
		}

		private sealed class CreateEventHubIfNotExistsAsyncResult : IteratorAsyncResult<NamespaceManager.CreateEventHubIfNotExistsAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			public EventHubDescription EventHub
			{
				get;
				private set;
			}

			public CreateEventHubIfNotExistsAsyncResult(NamespaceManager manager, EventHubDescription description, AsyncCallback callback, object state) : base(manager.settings.InternalOperationTimeout, callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.Path, 260, "description.Path");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), description.Path);
				this.manager = manager;
				this.EventHub = description;
			}

			private static void CreateDescriptionAfterCreate(NamespaceManager.CreateEventHubIfNotExistsAsyncResult thisPtr, IAsyncResult r)
			{
				string path = thisPtr.EventHub.Path;
				thisPtr.EventHub = AsyncResult<NamespaceManager.CreateOrUpdateEventHubAsyncResult>.End(r).EventHub;
				thisPtr.EventHub.Path = path;
				thisPtr.EventHub.IsReadOnly = false;
			}

			private static void CreateDescriptionAfterGet(NamespaceManager.CreateEventHubIfNotExistsAsyncResult thisPtr, IAsyncResult r)
			{
				string[] strArrays;
				thisPtr.EventHub = ServiceBusResourceOperations.EndGet<EventHubDescription>(r, out strArrays);
				thisPtr.EventHub.Path = strArrays[0];
				thisPtr.EventHub.IsReadOnly = false;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateEventHubIfNotExistsAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
					if (base.RemainingTime() > TimeSpan.Zero)
					{
						NamespaceManager.CreateEventHubIfNotExistsAsyncResult createEventHubIfNotExistsAsyncResult = this;
						yield return createEventHubIfNotExistsAsyncResult.CallAsync((NamespaceManager.CreateEventHubIfNotExistsAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => (new NamespaceManager.CreateOrUpdateEventHubAsyncResult(thisPtr.trackingContext, thisPtr.manager, thisPtr.EventHub, false, c, s)).Start(), new IteratorAsyncResult<NamespaceManager.CreateEventHubIfNotExistsAsyncResult>.EndCall(NamespaceManager.CreateEventHubIfNotExistsAsyncResult.CreateDescriptionAfterCreate), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							break;
						}
						bool flag = false;
						if (base.LastAsyncStepException is MessagingEntityAlreadyExistsException)
						{
							NamespaceManager.CreateEventHubIfNotExistsAsyncResult createEventHubIfNotExistsAsyncResult1 = this;
							yield return createEventHubIfNotExistsAsyncResult1.CallAsync((NamespaceManager.CreateEventHubIfNotExistsAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginGet<EventHubDescription>(thisPtr.trackingContext, null, new string[] { thisPtr.EventHub.Path }, thisPtr.manager.addresses, thisPtr.manager.settings, thisPtr.manager.settings.InternalOperationTimeout, c, s), new IteratorAsyncResult<NamespaceManager.CreateEventHubIfNotExistsAsyncResult>.EndCall(NamespaceManager.CreateEventHubIfNotExistsAsyncResult.CreateDescriptionAfterGet), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							if (base.LastAsyncStepException is MessagingEntityNotFoundException)
							{
								base.LastAsyncStepException = null;
								flag = true;
							}
						}
						if (!flag)
						{
							base.Complete(base.LastAsyncStepException);
							break;
						}
					}
					else
					{
						base.Complete(new TimeoutException(SRCore.TimeoutOnOperation(base.OriginalTimeout)));
						break;
					}
				}
			}
		}

		private sealed class CreateOrUpdateEventHubAsyncResult : IteratorAsyncResult<NamespaceManager.CreateOrUpdateEventHubAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private readonly bool isUpdate;

			private readonly static TimeSpan MinCreateTime;

			public EventHubDescription EventHub
			{
				get;
				private set;
			}

			static CreateOrUpdateEventHubAsyncResult()
			{
				NamespaceManager.CreateOrUpdateEventHubAsyncResult.MinCreateTime = TimeSpan.FromMinutes(2);
			}

			public CreateOrUpdateEventHubAsyncResult(NamespaceManager manager, EventHubDescription description, bool isUpdate, AsyncCallback callback, object state) : this(null, manager, description, isUpdate, callback, state)
			{
			}

			public CreateOrUpdateEventHubAsyncResult(TrackingContext trackingContext, NamespaceManager manager, EventHubDescription description, bool isUpdate, AsyncCallback callback, object state) : base((manager.settings.InternalOperationTimeout > NamespaceManager.CreateOrUpdateEventHubAsyncResult.MinCreateTime ? manager.settings.InternalOperationTimeout : NamespaceManager.CreateOrUpdateEventHubAsyncResult.MinCreateTime), callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.Path, 260, false, "description.Path");
				this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid(), description.Path);
				this.manager = manager;
				this.EventHub = description;
				this.isUpdate = isUpdate;
			}

			private void CreateDescription(NamespaceManager.CreateOrUpdateEventHubAsyncResult thisPtr, IAsyncResult r)
			{
				string path = thisPtr.EventHub.Path;
				thisPtr.EventHub = ServiceBusResourceOperations.EndCreate<EventHubDescription>(r);
				thisPtr.EventHub.Path = path;
				thisPtr.EventHub.IsReadOnly = false;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateOrUpdateEventHubAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				NamespaceManager.CreateOrUpdateEventHubAsyncResult createOrUpdateEventHubAsyncResult = this;
				yield return createOrUpdateEventHubAsyncResult.CallAsync((NamespaceManager.CreateOrUpdateEventHubAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginCreateOrUpdate<EventHubDescription>(thisPtr.trackingContext, thisPtr.EventHub, null, new string[] { thisPtr.EventHub.Path }, thisPtr.manager.addresses, t, false, thisPtr.isUpdate, null, thisPtr.manager.settings, c, s), (NamespaceManager.CreateOrUpdateEventHubAsyncResult thisPtr, IAsyncResult r) => this.CreateDescription(thisPtr, r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class CreateOrUpdateEventHubConsumerGroupAsyncResult : AsyncResult
		{
			private readonly static AsyncResult.AsyncCompletion onCreateConsumerGroup;

			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			public ConsumerGroupDescription ConsumerGroup
			{
				get;
				private set;
			}

			static CreateOrUpdateEventHubConsumerGroupAsyncResult()
			{
				NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult.onCreateConsumerGroup = new AsyncResult.AsyncCompletion(NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult.OnCreateConsumerGroup);
			}

			public CreateOrUpdateEventHubConsumerGroupAsyncResult(TrackingContext trackingContext, NamespaceManager manager, ConsumerGroupDescription description, bool isUpdate, AsyncCallback callback, object state) : base(callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.EventHubPath, 260, false, "description.EventHubPath");
				NamespaceManager.CheckValidEntityName(description.Name, 50, false, "description.Name");
				this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatConsumerGroupPath(description.EventHubPath, description.Name));
				this.manager = manager;
				this.ConsumerGroup = description;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(this.manager.settings.InternalOperationTimeout);
				TrackingContext trackingContext1 = this.trackingContext;
				ConsumerGroupDescription consumerGroup = this.ConsumerGroup;
				IResourceDescription[] resourceDescriptionArray = new IResourceDescription[] { this.ConsumerGroup };
				string[] eventHubPath = new string[] { this.ConsumerGroup.EventHubPath, this.ConsumerGroup.Name };
				if (base.SyncContinue(ServiceBusResourceOperations.BeginCreateOrUpdate<ConsumerGroupDescription>(trackingContext1, consumerGroup, resourceDescriptionArray, eventHubPath, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult.onCreateConsumerGroup), this)))
				{
					base.Complete(true);
				}
			}

			public static new NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult End(IAsyncResult result)
			{
				return AsyncResult.End<NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult>(result);
			}

			private static bool OnCreateConsumerGroup(IAsyncResult result)
			{
				NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult asyncState = (NamespaceManager.CreateOrUpdateEventHubConsumerGroupAsyncResult)result.AsyncState;
				string eventHubPath = asyncState.ConsumerGroup.EventHubPath;
				string name = asyncState.ConsumerGroup.Name;
				asyncState.ConsumerGroup = ServiceBusResourceOperations.EndCreate<ConsumerGroupDescription>(result);
				asyncState.ConsumerGroup.EventHubPath = eventHubPath;
				asyncState.ConsumerGroup.Name = name;
				asyncState.ConsumerGroup.IsReadOnly = false;
				return true;
			}
		}

		private sealed class CreateOrUpdateNotificationHubAsyncResult : IteratorAsyncResult<NamespaceManager.CreateOrUpdateNotificationHubAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private readonly bool isUpdate;

			public NotificationHubDescription NotificationHub
			{
				get;
				private set;
			}

			public CreateOrUpdateNotificationHubAsyncResult(NamespaceManager manager, NotificationHubDescription description, bool isUpdate, AsyncCallback callback, object state) : base(manager.settings.InternalOperationTimeout, callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.Path, 260, "description.Path");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), description.Path);
				this.manager = manager;
				this.NotificationHub = description;
				this.isUpdate = isUpdate;
			}

			private void CreateDescription(NamespaceManager.CreateOrUpdateNotificationHubAsyncResult thisPtr, IAsyncResult r)
			{
				string path = thisPtr.NotificationHub.Path;
				thisPtr.NotificationHub = ServiceBusResourceOperations.EndCreate<NotificationHubDescription>(r);
				thisPtr.NotificationHub.Path = path;
				thisPtr.NotificationHub.IsReadOnly = false;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateOrUpdateNotificationHubAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				NamespaceManager.CreateOrUpdateNotificationHubAsyncResult createOrUpdateNotificationHubAsyncResult = this;
				yield return createOrUpdateNotificationHubAsyncResult.CallAsync((NamespaceManager.CreateOrUpdateNotificationHubAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginCreateOrUpdate<NotificationHubDescription>(thisPtr.trackingContext, thisPtr.NotificationHub, null, new string[] { thisPtr.NotificationHub.Path }, thisPtr.manager.addresses, t, thisPtr.NotificationHub.IsAnonymousAccessible, thisPtr.isUpdate, null, thisPtr.manager.settings, c, s), (NamespaceManager.CreateOrUpdateNotificationHubAsyncResult thisPtr, IAsyncResult r) => this.CreateDescription(thisPtr, r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class CreateOrUpdateQueueAsyncResult : IteratorAsyncResult<NamespaceManager.CreateOrUpdateQueueAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private readonly bool isUpdate;

			public QueueDescription Queue
			{
				get;
				private set;
			}

			public CreateOrUpdateQueueAsyncResult(NamespaceManager manager, QueueDescription description, bool isUpdate, AsyncCallback callback, object state) : base(manager.settings.InternalOperationTimeout, callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.Path, 260, "description.Path");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), description.Path);
				if (!string.IsNullOrEmpty(description.ForwardTo))
				{
					description.ForwardTo = manager.ValidateAndNormalizeForwardToAddress(description.Path, description.ForwardTo);
				}
				if (!string.IsNullOrEmpty(description.ForwardDeadLetteredMessagesTo))
				{
					description.InternalForwardDeadLetteredMessagesTo = manager.ValidateAndNormalizeForwardToAddress(description.Path, description.ForwardDeadLetteredMessagesTo);
				}
				this.manager = manager;
				this.Queue = description;
				this.isUpdate = isUpdate;
			}

			private void CreateDescription(NamespaceManager.CreateOrUpdateQueueAsyncResult thisPtr, IAsyncResult r)
			{
				string path = thisPtr.Queue.Path;
				thisPtr.Queue = ServiceBusResourceOperations.EndCreate<QueueDescription>(r);
				thisPtr.Queue.Path = path;
				thisPtr.Queue.IsReadOnly = false;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateOrUpdateQueueAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				NamespaceManager.CreateOrUpdateQueueAsyncResult createOrUpdateQueueAsyncResult = this;
				yield return createOrUpdateQueueAsyncResult.CallAsync((NamespaceManager.CreateOrUpdateQueueAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginCreateOrUpdate<QueueDescription>(thisPtr.trackingContext, thisPtr.Queue, null, new string[] { thisPtr.Queue.Path }, thisPtr.manager.addresses, t, thisPtr.Queue.IsAnonymousAccessible, thisPtr.isUpdate, null, thisPtr.manager.settings, c, s), (NamespaceManager.CreateOrUpdateQueueAsyncResult thisPtr, IAsyncResult r) => this.CreateDescription(thisPtr, r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription> : AsyncResult
		where TRegistrationDescription : RegistrationDescription
		{
			private readonly static AsyncResult.AsyncCompletion onCreateRegistration;

			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			public TRegistrationDescription Registration
			{
				get;
				private set;
			}

			static CreateOrUpdateRegistrationAsyncResult()
			{
				NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration = new AsyncResult.AsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.OnCreateRegistration);
			}

			public CreateOrUpdateRegistrationAsyncResult(NamespaceManager manager, TRegistrationDescription description, bool isUpdate, AsyncCallback callback, object state) : base(callback, state)
			{
				string[] strArrays;
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), description.NotificationHubPath);
				this.manager = manager;
				this.Registration = description;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(this.manager.settings.InternalOperationTimeout);
				if (isUpdate)
				{
					string[] notificationHubPath = new string[] { this.Registration.NotificationHubPath, this.Registration.RegistrationId };
					strArrays = notificationHubPath;
				}
				else
				{
					string[] notificationHubPath1 = new string[] { this.Registration.NotificationHubPath, string.Empty };
					strArrays = notificationHubPath1;
				}
				string[] strArrays1 = strArrays;
				this.Registration.RegistrationId = null;
				this.Registration.ExpirationTime = null;
				if (base.SyncContinue(this.BeginCreateOrUpdateOperation(strArrays1, isUpdate)))
				{
					base.Complete(true);
				}
			}

			private IAsyncResult BeginCreateOrUpdateOperation(string[] resourceNames, bool isUpdate)
			{
				if (this.Registration.GetType().Name == typeof(AppleTemplateRegistrationDescription).Name)
				{
					TrackingContext trackingContext = this.trackingContext;
					AppleTemplateRegistrationDescription registration = (object)this.Registration as AppleTemplateRegistrationDescription;
					IResourceDescription[] resourceDescriptionArray = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<AppleTemplateRegistrationDescription>(trackingContext, registration, resourceDescriptionArray, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(AppleRegistrationDescription).Name)
				{
					TrackingContext trackingContext1 = this.trackingContext;
					AppleRegistrationDescription appleRegistrationDescription = (object)this.Registration as AppleRegistrationDescription;
					IResourceDescription[] registration1 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<AppleRegistrationDescription>(trackingContext1, appleRegistrationDescription, registration1, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(WindowsTemplateRegistrationDescription).Name)
				{
					TrackingContext trackingContext2 = this.trackingContext;
					WindowsTemplateRegistrationDescription windowsTemplateRegistrationDescription = (object)this.Registration as WindowsTemplateRegistrationDescription;
					IResourceDescription[] resourceDescriptionArray1 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<WindowsTemplateRegistrationDescription>(trackingContext2, windowsTemplateRegistrationDescription, resourceDescriptionArray1, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(WindowsRegistrationDescription).Name)
				{
					TrackingContext trackingContext3 = this.trackingContext;
					WindowsRegistrationDescription windowsRegistrationDescription = (object)this.Registration as WindowsRegistrationDescription;
					IResourceDescription[] registration2 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<WindowsRegistrationDescription>(trackingContext3, windowsRegistrationDescription, registration2, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(GcmTemplateRegistrationDescription).Name)
				{
					TrackingContext trackingContext4 = this.trackingContext;
					GcmTemplateRegistrationDescription gcmTemplateRegistrationDescription = (object)this.Registration as GcmTemplateRegistrationDescription;
					IResourceDescription[] resourceDescriptionArray2 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<GcmTemplateRegistrationDescription>(trackingContext4, gcmTemplateRegistrationDescription, resourceDescriptionArray2, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(GcmRegistrationDescription).Name)
				{
					TrackingContext trackingContext5 = this.trackingContext;
					GcmRegistrationDescription gcmRegistrationDescription = (object)this.Registration as GcmRegistrationDescription;
					IResourceDescription[] registration3 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<GcmRegistrationDescription>(trackingContext5, gcmRegistrationDescription, registration3, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(MpnsTemplateRegistrationDescription).Name)
				{
					TrackingContext trackingContext6 = this.trackingContext;
					MpnsTemplateRegistrationDescription mpnsTemplateRegistrationDescription = (object)this.Registration as MpnsTemplateRegistrationDescription;
					IResourceDescription[] resourceDescriptionArray3 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<MpnsTemplateRegistrationDescription>(trackingContext6, mpnsTemplateRegistrationDescription, resourceDescriptionArray3, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(MpnsRegistrationDescription).Name)
				{
					TrackingContext trackingContext7 = this.trackingContext;
					MpnsRegistrationDescription mpnsRegistrationDescription = (object)this.Registration as MpnsRegistrationDescription;
					IResourceDescription[] registration4 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<MpnsRegistrationDescription>(trackingContext7, mpnsRegistrationDescription, registration4, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(EmailRegistrationDescription).Name)
				{
					TrackingContext trackingContext8 = this.trackingContext;
					EmailRegistrationDescription emailRegistrationDescription = (object)this.Registration as EmailRegistrationDescription;
					IResourceDescription[] resourceDescriptionArray4 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<EmailRegistrationDescription>(trackingContext8, emailRegistrationDescription, resourceDescriptionArray4, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(AdmRegistrationDescription).Name)
				{
					TrackingContext trackingContext9 = this.trackingContext;
					AdmRegistrationDescription admRegistrationDescription = (object)this.Registration as AdmRegistrationDescription;
					IResourceDescription[] registration5 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<AdmRegistrationDescription>(trackingContext9, admRegistrationDescription, registration5, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(AdmTemplateRegistrationDescription).Name)
				{
					TrackingContext trackingContext10 = this.trackingContext;
					AdmTemplateRegistrationDescription admTemplateRegistrationDescription = (object)this.Registration as AdmTemplateRegistrationDescription;
					IResourceDescription[] resourceDescriptionArray5 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<AdmTemplateRegistrationDescription>(trackingContext10, admTemplateRegistrationDescription, resourceDescriptionArray5, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(NokiaXTemplateRegistrationDescription).Name)
				{
					TrackingContext trackingContext11 = this.trackingContext;
					NokiaXTemplateRegistrationDescription nokiaXTemplateRegistrationDescription = (object)this.Registration as NokiaXTemplateRegistrationDescription;
					IResourceDescription[] registration6 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<NokiaXTemplateRegistrationDescription>(trackingContext11, nokiaXTemplateRegistrationDescription, registration6, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(NokiaXRegistrationDescription).Name)
				{
					TrackingContext trackingContext12 = this.trackingContext;
					NokiaXRegistrationDescription nokiaXRegistrationDescription = (object)this.Registration as NokiaXRegistrationDescription;
					IResourceDescription[] resourceDescriptionArray6 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<NokiaXRegistrationDescription>(trackingContext12, nokiaXRegistrationDescription, resourceDescriptionArray6, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name == typeof(BaiduTemplateRegistrationDescription).Name)
				{
					TrackingContext trackingContext13 = this.trackingContext;
					BaiduTemplateRegistrationDescription baiduTemplateRegistrationDescription = (object)this.Registration as BaiduTemplateRegistrationDescription;
					IResourceDescription[] registration7 = new IResourceDescription[] { this.Registration };
					return ServiceBusResourceOperations.BeginCreateOrUpdate<BaiduTemplateRegistrationDescription>(trackingContext13, baiduTemplateRegistrationDescription, registration7, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
				}
				if (this.Registration.GetType().Name != typeof(BaiduRegistrationDescription).Name)
				{
					throw new InvalidOperationException("Unknow RegistrationDescription type");
				}
				TrackingContext trackingContext14 = this.trackingContext;
				BaiduRegistrationDescription baiduRegistrationDescription = (object)this.Registration as BaiduRegistrationDescription;
				IResourceDescription[] resourceDescriptionArray7 = new IResourceDescription[] { this.Registration };
				return ServiceBusResourceOperations.BeginCreateOrUpdate<BaiduRegistrationDescription>(trackingContext14, baiduRegistrationDescription, resourceDescriptionArray7, resourceNames, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>.onCreateRegistration), this);
			}

			public static new NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription> End(IAsyncResult result)
			{
				return AsyncResult.End<NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>>(result);
			}

			private static bool OnCreateRegistration(IAsyncResult result)
			{
				NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription> asyncState = (NamespaceManager.CreateOrUpdateRegistrationAsyncResult<TRegistrationDescription>)result.AsyncState;
				string notificationHubPath = asyncState.Registration.NotificationHubPath;
				if (asyncState.Registration.GetType().Name == typeof(AppleTemplateRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<AppleTemplateRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(AppleRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<AppleRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(WindowsTemplateRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<WindowsTemplateRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(WindowsRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<WindowsRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(GcmTemplateRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<GcmTemplateRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(GcmRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<GcmRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(NokiaXTemplateRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<NokiaXTemplateRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(NokiaXRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<NokiaXRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(BaiduRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<BaiduRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(BaiduTemplateRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<BaiduTemplateRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(MpnsTemplateRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<MpnsTemplateRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(MpnsRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<MpnsRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name == typeof(EmailRegistrationDescription).Name)
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<EmailRegistrationDescription>(result) as TRegistrationDescription);
				}
				else if (asyncState.Registration.GetType().Name != typeof(AdmRegistrationDescription).Name)
				{
					if (asyncState.Registration.GetType().Name != typeof(AdmTemplateRegistrationDescription).Name)
					{
						throw new InvalidOperationException("Unknow RegistrationDescription type");
					}
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<AdmTemplateRegistrationDescription>(result) as TRegistrationDescription);
				}
				else
				{
					asyncState.Registration = (TRegistrationDescription)(ServiceBusResourceOperations.EndCreate<AdmRegistrationDescription>(result) as TRegistrationDescription);
				}
				asyncState.Registration.NotificationHubPath = notificationHubPath;
				asyncState.Registration.IsReadOnly = false;
				return true;
			}
		}

		private sealed class CreateOrUpdateSubscriptionAsyncResult : AsyncResult
		{
			private readonly static AsyncResult.AsyncCompletion onCreateSubscription;

			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			public SubscriptionDescription Subscription
			{
				get;
				private set;
			}

			static CreateOrUpdateSubscriptionAsyncResult()
			{
				NamespaceManager.CreateOrUpdateSubscriptionAsyncResult.onCreateSubscription = new AsyncResult.AsyncCompletion(NamespaceManager.CreateOrUpdateSubscriptionAsyncResult.OnCreateSubscription);
			}

			public CreateOrUpdateSubscriptionAsyncResult(NamespaceManager manager, SubscriptionDescription description, RuleDescription ruleDescription, bool isUpdate, AsyncCallback callback, object state) : base(callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				if (!isUpdate && ruleDescription != null && string.IsNullOrWhiteSpace(ruleDescription.Name))
				{
					ruleDescription.Name = "$Default";
				}
				NamespaceManager.CheckValidEntityName(description.TopicPath, 260, "description.TopicPath");
				NamespaceManager.CheckValidEntityName(description.Name, 50, false, "description.Name");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(description.TopicPath, description.Name));
				if (!string.IsNullOrEmpty(description.ForwardTo))
				{
					description.ForwardTo = manager.ValidateAndNormalizeForwardToAddress(description.Name, description.ForwardTo);
				}
				if (!string.IsNullOrEmpty(description.ForwardDeadLetteredMessagesTo))
				{
					description.ForwardDeadLetteredMessagesTo = manager.ValidateAndNormalizeForwardToAddress(description.Name, description.ForwardDeadLetteredMessagesTo);
				}
				this.manager = manager;
				this.Subscription = description;
				this.Subscription.DefaultRuleDescription = ruleDescription;
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(this.manager.settings.InternalOperationTimeout);
				TrackingContext trackingContext = this.trackingContext;
				SubscriptionDescription subscription = this.Subscription;
				IResourceDescription[] resourceDescriptionArray = new IResourceDescription[] { this.Subscription };
				string[] topicPath = new string[] { this.Subscription.TopicPath, this.Subscription.Name };
				if (base.SyncContinue(ServiceBusResourceOperations.BeginCreateOrUpdate<SubscriptionDescription>(trackingContext, subscription, resourceDescriptionArray, topicPath, this.manager.addresses, this.timeoutHelper.RemainingTime(), false, isUpdate, null, this.manager.settings, base.PrepareAsyncCompletion(NamespaceManager.CreateOrUpdateSubscriptionAsyncResult.onCreateSubscription), this)))
				{
					base.Complete(true);
				}
			}

			public static new NamespaceManager.CreateOrUpdateSubscriptionAsyncResult End(IAsyncResult result)
			{
				return AsyncResult.End<NamespaceManager.CreateOrUpdateSubscriptionAsyncResult>(result);
			}

			private static bool OnCreateSubscription(IAsyncResult result)
			{
				NamespaceManager.CreateOrUpdateSubscriptionAsyncResult asyncState = (NamespaceManager.CreateOrUpdateSubscriptionAsyncResult)result.AsyncState;
				string topicPath = asyncState.Subscription.TopicPath;
				string name = asyncState.Subscription.Name;
				asyncState.Subscription = ServiceBusResourceOperations.EndCreate<SubscriptionDescription>(result);
				asyncState.Subscription.TopicPath = topicPath;
				asyncState.Subscription.Name = name;
				asyncState.Subscription.IsReadOnly = false;
				return true;
			}
		}

		private sealed class CreateOrUpdateTopicAsyncResult : IteratorAsyncResult<NamespaceManager.CreateOrUpdateTopicAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private readonly bool isUpdate;

			public TopicDescription Topic
			{
				get;
				private set;
			}

			public CreateOrUpdateTopicAsyncResult(NamespaceManager manager, TopicDescription description, bool isUpdate, AsyncCallback callback, object state) : base(manager.settings.InternalOperationTimeout, callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.Path, 260, "description.Path");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), description.Path);
				this.manager = manager;
				this.Topic = description;
				this.isUpdate = isUpdate;
			}

			private void CreateDescription(NamespaceManager.CreateOrUpdateTopicAsyncResult thisPtr, IAsyncResult r)
			{
				string path = thisPtr.Topic.Path;
				thisPtr.Topic = ServiceBusResourceOperations.EndCreate<TopicDescription>(r);
				thisPtr.Topic.Path = path;
				thisPtr.Topic.IsReadOnly = false;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateOrUpdateTopicAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				NamespaceManager.CreateOrUpdateTopicAsyncResult createOrUpdateTopicAsyncResult = this;
				yield return createOrUpdateTopicAsyncResult.CallAsync((NamespaceManager.CreateOrUpdateTopicAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginCreateOrUpdate<TopicDescription>(thisPtr.trackingContext, thisPtr.Topic, null, new string[] { thisPtr.Topic.Path }, thisPtr.manager.addresses, t, thisPtr.Topic.IsAnonymousAccessible, thisPtr.isUpdate, null, thisPtr.manager.settings, c, s), (NamespaceManager.CreateOrUpdateTopicAsyncResult thisPtr, IAsyncResult r) => this.CreateDescription(thisPtr, r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class CreateOrUpdateVolatileTopicAsyncResult : IteratorAsyncResult<NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private readonly bool isUpdate;

			public VolatileTopicDescription Description
			{
				get;
				private set;
			}

			public CreateOrUpdateVolatileTopicAsyncResult(NamespaceManager manager, VolatileTopicDescription description, bool isUpdate, AsyncCallback callback, object state) : base(manager.settings.InternalOperationTimeout, callback, state)
			{
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				NamespaceManager.CheckValidEntityName(description.Path, 260, "description.Path");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), description.Path);
				this.manager = manager;
				this.Description = description;
				this.isUpdate = isUpdate;
			}

			private void CreateDescription(IAsyncResult r)
			{
				string path = this.Description.Path;
				this.Description = ServiceBusResourceOperations.EndCreate<VolatileTopicDescription>(r);
				this.Description.Path = path;
				this.Description.IsReadOnly = false;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult createOrUpdateVolatileTopicAsyncResult = this;
				IteratorAsyncResult<NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult>.BeginCall beginCall = (NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginCreateOrUpdate<VolatileTopicDescription>(thisPtr.trackingContext, thisPtr.Description, null, new string[] { thisPtr.Description.Path }, thisPtr.manager.addresses, t, thisPtr.Description.IsAnonymousAccessible, thisPtr.isUpdate, null, thisPtr.manager.settings, c, s);
				yield return createOrUpdateVolatileTopicAsyncResult.CallAsync(beginCall, (NamespaceManager.CreateOrUpdateVolatileTopicAsyncResult thisPtr, IAsyncResult r) => thisPtr.CreateDescription(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class CreateRuleAsyncResult : IteratorAsyncResult<NamespaceManager.CreateRuleAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			private readonly string ruleName;

			private readonly string subscriptionName;

			private readonly string topicPath;

			public RuleDescription Rule
			{
				get;
				private set;
			}

			public CreateRuleAsyncResult(NamespaceManager manager, string ruleName, string subscriptionName, string topicPath, RuleDescription description, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				NamespaceManager.CheckValidEntityName(ruleName, 50, false, "ruleName");
				NamespaceManager.CheckValidEntityName(subscriptionName, 50, false, "subscriptionName");
				NamespaceManager.CheckValidEntityName(topicPath, 260, "topicPath");
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid(), EntityNameHelper.FormatSubscriptionPath(topicPath, subscriptionName));
				Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNonPositiveArgument(timeout);
				if (description == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("description");
				}
				this.manager = manager;
				this.ruleName = ruleName;
				this.subscriptionName = subscriptionName;
				this.topicPath = topicPath;
				this.Rule = description;
			}

			private void CreateDescription(NamespaceManager.CreateRuleAsyncResult thisPtr, IAsyncResult r)
			{
				thisPtr.Rule = ServiceBusResourceOperations.EndCreate<RuleDescription>(r);
				thisPtr.Rule.IsReadOnly = true;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.CreateRuleAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				yield return base.CallAsync((NamespaceManager.CreateRuleAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginCreateOrUpdate<RuleDescription>(thisPtr.trackingContext, thisPtr.Rule, new IResourceDescription[] { new SubscriptionDescription(), this.Rule }, new string[] { thisPtr.topicPath, thisPtr.subscriptionName, thisPtr.ruleName }, thisPtr.manager.addresses, t, false, false, null, thisPtr.manager.settings, c, s), (NamespaceManager.CreateRuleAsyncResult thisPtr, IAsyncResult r) => this.CreateDescription(thisPtr, r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private class EntityDescriptionSyndicationFeed<TEntityDescription>
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			private readonly Action<TEntityDescription, string> onFeedEntry;

			public IEnumerable<TEntityDescription> Entities
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EntityDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EntityDescription: Unable to read Atom XML content"), null);
								}
								TEntityDescription tEntityDescription = content.ReadContent<TEntityDescription>();
								if (this.onFeedEntry != null)
								{
									this.onFeedEntry(tEntityDescription, text);
								}
								tEntityDescription.IsReadOnly = false;
								yield return tEntityDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EntityDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EntityDescription: Atom Xml Title property is empty."), null);
				}
			}

			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public EntityDescriptionSyndicationFeed(SyndicationFeed feed, Action<TEntityDescription, string> onFeedEntry)
			{
				this.Feed = feed;
				this.onFeedEntry = onFeedEntry;
			}
		}

		private class EventHubConsumerGroupSyndicationFeed
		{
			public IEnumerable<ConsumerGroupDescription> ConsumerGroups
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Unable to read Atom XML content"), null);
								}
								ConsumerGroupDescription consumerGroupDescription = content.ReadContent<ConsumerGroupDescription>();
								consumerGroupDescription.Name = text;
								consumerGroupDescription.IsReadOnly = false;
								yield return consumerGroupDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("ConsumerGroupDescription: Atom Xml Title property is empty."), null);
				}
			}

			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public EventHubConsumerGroupSyndicationFeed(SyndicationFeed feed)
			{
				this.Feed = feed;
			}
		}

		private class EventHubSyndicationFeed
		{
			public IEnumerable<EventHubDescription> EventHubs
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EVentHubDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EVentHubDescription: Unable to read Atom XML content"), null);
								}
								EventHubDescription eventHubDescription = content.ReadContent<EventHubDescription>();
								eventHubDescription.Path = text;
								eventHubDescription.IsReadOnly = false;
								yield return eventHubDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EVentHubDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("EVentHubDescription: Atom Xml Title property is empty."), null);
				}
			}

			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public EventHubSyndicationFeed(SyndicationFeed feed)
			{
				this.Feed = feed;
			}
		}

		private sealed class GetAllAsyncResult : IteratorAsyncResult<NamespaceManager.GetAllAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManagerSettings settings;

			private readonly string filter;

			private IEnumerable<Uri> Addresses
			{
				get;
				set;
			}

			public IResourceDescription[] Descriptions
			{
				get;
				private set;
			}

			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public string[] ResourceNames
			{
				get;
				private set;
			}

			public GetAllAsyncResult(TrackingContext trackingContext, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.trackingContext = trackingContext;
				this.settings = settings;
				this.Descriptions = descriptions;
				this.ResourceNames = resourceNames;
				this.Addresses = addresses;
				base.Start();
			}

			public GetAllAsyncResult(TrackingContext trackingContext, IResourceDescription[] descriptions, string[] resourceNames, string filter, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.trackingContext = trackingContext;
				this.filter = filter;
				this.settings = settings;
				this.Descriptions = descriptions;
				this.ResourceNames = resourceNames;
				this.Addresses = addresses;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.GetAllAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				NamespaceManager.GetAllAsyncResult getAllAsyncResult = this;
				IteratorAsyncResult<NamespaceManager.GetAllAsyncResult>.BeginCall beginCall = (NamespaceManager.GetAllAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginGetAll(thisPtr.trackingContext, thisPtr.filter, thisPtr.Descriptions, thisPtr.ResourceNames, thisPtr.Addresses, thisPtr.settings, c, s);
				yield return getAllAsyncResult.CallAsync(beginCall, (NamespaceManager.GetAllAsyncResult thisPtr, IAsyncResult r) => thisPtr.Feed = ServiceBusResourceOperations.EndGetAll(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class GetVersionInfoAsyncResult : IteratorAsyncResult<NamespaceManager.GetVersionInfoAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly NamespaceManager manager;

			public string Version
			{
				get;
				private set;
			}

			public GetVersionInfoAsyncResult(NamespaceManager manager, AsyncCallback callback, object state) : base(manager.settings.InternalOperationTimeout, callback, state)
			{
				this.trackingContext = TrackingContext.GetInstance(Guid.NewGuid());
				this.manager = manager;
				this.Version = string.Empty;
			}

			protected override IEnumerator<IteratorAsyncResult<NamespaceManager.GetVersionInfoAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				NamespaceManager.GetVersionInfoAsyncResult getVersionInfoAsyncResult = this;
				IteratorAsyncResult<NamespaceManager.GetVersionInfoAsyncResult>.BeginCall beginCall = (NamespaceManager.GetVersionInfoAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => ServiceBusResourceOperations.BeginGetInformation(thisPtr.trackingContext, thisPtr.manager.addresses, thisPtr.manager.settings, t, c, s);
				yield return getVersionInfoAsyncResult.CallAsync(beginCall, (NamespaceManager.GetVersionInfoAsyncResult thisPtr, IAsyncResult r) => {
					IDictionary<string, string> strs = ServiceBusResourceOperations.EndGetInformation(r);
					thisPtr.Version = (strs.ContainsKey("MaxProtocolVersion") ? strs["MaxProtocolVersion"] : string.Empty);
				}, IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private class NotificationHubSyndicationFeed
		{
			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<NotificationHubDescription> NotificationHubs
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("NotificationHubDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("NotificationHubDescription: Unable to read Atom XML content"), null);
								}
								NotificationHubDescription notificationHubDescription = content.ReadContent<NotificationHubDescription>();
								notificationHubDescription.Path = text;
								notificationHubDescription.IsReadOnly = false;
								yield return notificationHubDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("NotificationHubDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("NotificationHubDescription: Atom Xml Title property is empty."), null);
				}
			}

			public NotificationHubSyndicationFeed(SyndicationFeed feed)
			{
				this.Feed = feed;
			}
		}

		private class PartitionSyndicationFeed
		{
			public string ConsumerGroupName
			{
				get;
				private set;
			}

			public string EventHubPath
			{
				get;
				private set;
			}

			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<PartitionDescription> Partitions
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("PartitionDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("PartitionDescription: Unable to read Atom XML content"), null);
								}
								PartitionDescription eventHubPath = content.ReadContent<PartitionDescription>();
								eventHubPath.EventHubPath = this.EventHubPath;
								eventHubPath.PartitionId = text;
								eventHubPath.ConsumerGroupName = this.ConsumerGroupName;
								eventHubPath.IsReadOnly = false;
								yield return eventHubPath;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("PartitionDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("PartitionDescription: Atom Xml Title property is empty."), null);
				}
			}

			public PartitionSyndicationFeed(SyndicationFeed feed, string eventHubName, string consumerGroupName)
			{
				this.EventHubPath = eventHubName;
				this.Feed = feed;
				this.ConsumerGroupName = consumerGroupName;
			}
		}

		private class QueueSyndicationFeed
		{
			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<QueueDescription> Queues
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Unable to read Atom XML content"), null);
								}
								QueueDescription queueDescription = content.ReadContent<QueueDescription>();
								queueDescription.Path = text;
								queueDescription.IsReadOnly = false;
								yield return queueDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Atom Xml Title property is empty."), null);
				}
			}

			public QueueSyndicationFeed(SyndicationFeed feed)
			{
				this.Feed = feed;
			}
		}

		internal class RegistrationSyndicationFeed
		{
			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<RegistrationDescription> Registrations
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								if (string.IsNullOrEmpty(current.Title.Text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RegistrationDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RegistrationDescription: Unable to read Atom XML content"), null);
								}
								XmlDictionaryReader readerAtContent = content.GetReaderAtContent();
								readerAtContent.Read();
								if (readerAtContent.Name == typeof(WindowsRegistrationDescription).Name)
								{
									yield return this.TryReadContent<WindowsRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(WindowsTemplateRegistrationDescription).Name)
								{
									yield return this.TryReadContent<WindowsTemplateRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(AppleRegistrationDescription).Name)
								{
									yield return this.TryReadContent<AppleRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(AppleTemplateRegistrationDescription).Name)
								{
									yield return this.TryReadContent<AppleTemplateRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(GcmRegistrationDescription).Name)
								{
									yield return this.TryReadContent<GcmRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(GcmTemplateRegistrationDescription).Name)
								{
									yield return this.TryReadContent<GcmTemplateRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(MpnsRegistrationDescription).Name)
								{
									yield return this.TryReadContent<MpnsRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(MpnsTemplateRegistrationDescription).Name)
								{
									yield return this.TryReadContent<MpnsTemplateRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(AdmRegistrationDescription).Name)
								{
									yield return this.TryReadContent<AdmRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(AdmTemplateRegistrationDescription).Name)
								{
									yield return this.TryReadContent<AdmTemplateRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(NokiaXRegistrationDescription).Name)
								{
									yield return this.TryReadContent<NokiaXRegistrationDescription>(content);
								}
								else if (readerAtContent.Name == typeof(NokiaXTemplateRegistrationDescription).Name)
								{
									yield return this.TryReadContent<NokiaXTemplateRegistrationDescription>(content);
								}
								else if (readerAtContent.Name != typeof(BaiduRegistrationDescription).Name)
								{
									if (readerAtContent.Name != typeof(BaiduTemplateRegistrationDescription).Name)
									{
										continue;
									}
									yield return this.TryReadContent<BaiduTemplateRegistrationDescription>(content);
								}
								else
								{
									yield return this.TryReadContent<BaiduRegistrationDescription>(content);
								}
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RegistrationDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RegistrationDescription: Atom Xml Title property is empty."), null);
				}
			}

			public RegistrationSyndicationFeed(SyndicationFeed feed)
			{
				this.Feed = feed;
			}

			private TContent TryReadContent<TContent>(XmlSyndicationContent syndicationContent)
			where TContent : RegistrationDescription
			{
				TContent tContent;
				try
				{
					tContent = syndicationContent.ReadContent<TContent>();
				}
				catch (SerializationException serializationException)
				{
					return default(TContent);
				}
				return tContent;
			}
		}

		private class RuleSyndicationFeed
		{
			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<RuleDescription> Rules
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RuleDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RuleDescription: Unable to read Atom XML content"), null);
								}
								RuleDescription ruleDescription = content.ReadContent<RuleDescription>();
								ruleDescription.Name = text;
								ruleDescription.IsReadOnly = true;
								yield return ruleDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RuleDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("RuleDescription: Atom Xml Title property is empty."), null);
				}
			}

			public RuleSyndicationFeed(SyndicationFeed feed, string subscriptionName, string topicPath)
			{
				this.Feed = feed;
			}
		}

		private class SubscriptionSyndicationFeed
		{
			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<SubscriptionDescription> Subscriptions
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("SubscriptionDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("SubscriptionDescription: Unable to read Atom XML content"), null);
								}
								SubscriptionDescription topicPath = content.ReadContent<SubscriptionDescription>();
								topicPath.TopicPath = this.TopicPath;
								topicPath.Name = text;
								topicPath.IsReadOnly = false;
								yield return topicPath;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("SubscriptionDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("SubscriptionDescription: Atom Xml Title property is empty."), null);
				}
			}

			public string TopicPath
			{
				get;
				private set;
			}

			public SubscriptionSyndicationFeed(SyndicationFeed feed, string topicName)
			{
				this.TopicPath = topicName;
				this.Feed = feed;
			}
		}

		private class TopicSyndicationFeed
		{
			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<TopicDescription> Topics
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("TopicDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("TopicDescription: Unable to read Atom XML content"), null);
								}
								TopicDescription topicDescription = content.ReadContent<TopicDescription>();
								topicDescription.Path = text;
								topicDescription.IsReadOnly = false;
								yield return topicDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("TopicDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("TopicDescription: Atom Xml Title property is empty."), null);
				}
			}

			public TopicSyndicationFeed(SyndicationFeed feed)
			{
				this.Feed = feed;
			}
		}

		private sealed class VolatileTopicSyndicationFeed
		{
			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public IEnumerable<VolatileTopicDescription> VolatileTopics
			{
				get
				{
					if (this.Feed != null)
					{
						using (IEnumerator<SyndicationItem> enumerator = this.Feed.Items.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								SyndicationItem current = enumerator.Current;
								string text = current.Title.Text;
								if (string.IsNullOrEmpty(text))
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Atom Xml Title property is empty."), null);
								}
								XmlSyndicationContent content = current.Content as XmlSyndicationContent;
								if (content == null)
								{
									throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Unable to read Atom XML content"), null);
								}
								VolatileTopicDescription volatileTopicDescription = content.ReadContent<VolatileTopicDescription>();
								volatileTopicDescription.Path = text;
								volatileTopicDescription.IsReadOnly = false;
								yield return volatileTopicDescription;
							}
							goto Label2;
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Unable to read Atom XML content"), null);
						}
					}
				Label2:
					yield break;
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new MessagingException("QueueDescription: Atom Xml Title property is empty."), null);
				}
			}

			public VolatileTopicSyndicationFeed(SyndicationFeed feed)
			{
				this.Feed = feed;
			}
		}
	}
}