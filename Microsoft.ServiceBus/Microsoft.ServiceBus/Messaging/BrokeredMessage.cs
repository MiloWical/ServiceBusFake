using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[XmlRoot("BrokeredMessage", Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	public sealed class BrokeredMessage : IXmlSerializable, IDisposable
	{
		internal const string MessageIdHeaderName = "MessageId";

		internal const string CorrelationIdHeaderName = "CorrelationId";

		internal const string ToHeaderName = "To";

		internal const string ReplyToHeaderName = "ReplyTo";

		internal const string SessionIdHeaderName = "SessionId";

		internal const string LabelHeaderName = "Label";

		internal const string ContentTypeHeaderName = "ContentType";

		internal const string ReplyToSessionIdHeaderName = "ReplyToSessionId";

		internal const string TimeToLiveHeaderName = "TimeToLive";

		internal const string ScheduledEnqueueTimeUtcHeaderName = "ScheduledEnqueueTimeUtc";

		internal const string PartitionKeyHeaderName = "PartitionKey";

		internal const string EnqueuedTimeUtcHeaderName = "EnqueuedTimeUtc";

		internal const string SequenceNumberHeaderName = "SequenceNumber";

		internal const string LockTokenHeaderName = "LockToken";

		internal const string LockedUntilUtcHeaderName = "LockedUntilUtc";

		internal const string DeliveryCountHeaderName = "DeliveryCount";

		internal const string EnqueuedSequenceNumberHeaderName = "EnqueuedSequenceNumber";

		internal const string ViaPartitionKeyHeaderName = "ViaPartitionKey";

		internal const string DestinationHeaderName = "Destination";

		internal const string ForcePersistenceHeaderName = "ForcePersistence";

		internal const string PublisherHeaderName = "Publisher";

		private const BrokeredMessage.MessageMembers V1MessageMembers = BrokeredMessage.MessageMembers.MessageId | BrokeredMessage.MessageMembers.CorrelationId | BrokeredMessage.MessageMembers.To | BrokeredMessage.MessageMembers.ReplyTo | BrokeredMessage.MessageMembers.TimeToLive | BrokeredMessage.MessageMembers.SessionId | BrokeredMessage.MessageMembers.Label | BrokeredMessage.MessageMembers.ContentType | BrokeredMessage.MessageMembers.ScheduledEnqueueTimeUtc | BrokeredMessage.MessageMembers.PartitionKey | BrokeredMessage.MessageMembers.ReplyToSessionId | BrokeredMessage.MessageMembers.EnqueuedTimeUtc | BrokeredMessage.MessageMembers.SequenceNumber | BrokeredMessage.MessageMembers.LockToken | BrokeredMessage.MessageMembers.LockedUntilUtc | BrokeredMessage.MessageMembers.DeliveryCount;

		private const BrokeredMessage.MessageMembers RegularTopicHeaders = BrokeredMessage.MessageMembers.MessageId | BrokeredMessage.MessageMembers.EnqueuedTimeUtc | BrokeredMessage.MessageMembers.SequenceNumber | BrokeredMessage.MessageMembers.DeliveryCount | BrokeredMessage.MessageMembers.EnqueuedSequenceNumber;

		private static BrokeredMessage.BrokeredMessageMode mode;

		private static BrokeredMessage.BinarySerializationItem[] binarySerializationItems;

		private readonly static Dictionary<string, Func<BrokeredMessage, object>> SystemPropertyAccessorDictionary;

		private readonly int headerStreamInitialSize = 512;

		private readonly static int headerStreamMaxSize;

		private readonly static int deadLetterheaderStreamMaxSize;

		private static byte[] messageFlags;

		internal readonly static int MessageVersion1;

		internal readonly static int MessageVersion2;

		internal readonly static int MessageVersion3;

		internal readonly static int MessageVersion4;

		internal readonly static int MessageVersion5;

		internal readonly static int MessageVersion6;

		internal readonly static int MessageVersion7;

		internal readonly static int MessageVersion8;

		internal readonly static int MessageVersion9;

		internal readonly static int MessageVersion10;

		internal static int MessageVersion;

		private bool supportsEmptySerializedHeader;

		private int? originalDeliveryCount;

		private int version;

		private Stream bodyStream;

		private string correlationId;

		private string sessionId;

		private string publisher;

		private MessageState state;

		private string replyToSessionId;

		private bool disposed;

		private bool headersDeserialized;

		private object headerSerializationSyncObject = new object();

		private BufferedInputStream headerStream;

		private Stream rawHeaderStream;

		private Stream prefilteredHeaders;

		private Stream prefilteredProperties;

		private BrokeredMessage.MessageMembers initializedMembers;

		private string messageId;

		private Microsoft.ServiceBus.Messaging.ReceiveContext receiveContext;

		private long bodyId;

		private short partitionId;

		private string transferSource;

		private string transferSessionId;

		private string transferDestination;

		private long transferDestinationResourceId;

		private long transferSequenceNumber;

		private int transferHopCount;

		private IDictionary<string, object> properties;

		private BrokeredMessage.ReceiverHeaders receiverHeaders;

		private string replyTo;

		private string to;

		private DateTime enqueuedTimeUtc;

		private DateTime scheduledEnqueueTimeUtc = DateTime.MinValue;

		private TimeSpan timeToLive;

		private string partitionKey;

		private string viaPartitionKey;

		private string destination;

		private string label;

		private string contentType;

		private bool isBodyLoaded = true;

		private bool ownsBodyStream;

		private bool ownsRawHeaderStream;

		private object bodyObject;

		private bool bodyObjectDecoded;

		private long headerSize;

		private long bodySize;

		private int getBodyCalled;

		private int messageConsumed;

		private bool forcePersistence;

		private bool isFromCache;

		private long persistedMessageSize;

		private BrokeredMessageFormat messageFormat;

		private IBrokeredMessageEncoder messageEncoder;

		private volatile List<IDisposable> attachedDisposables;

		private object disposablesSyncObject = new object();

		private bool arePropertiesModifiedByBroker;

		internal bool AllowOverflowOnPersist
		{
			get;
			set;
		}

		internal bool ArePropertiesModifiedByBroker
		{
			get
			{
				this.ThrowIfDisposed();
				return this.arePropertiesModifiedByBroker;
			}
		}

		internal long BodyId
		{
			get
			{
				this.EnsureHeaderDeserialized();
				return this.bodyId;
			}
			set
			{
				this.bodyId = value;
			}
		}

		internal long BodySize
		{
			get
			{
				this.ThrowIfDisposed();
				if (this.bodyStream != null && this.bodyStream.CanSeek)
				{
					this.bodySize = this.bodyStream.Length;
				}
				return this.bodySize;
			}
		}

		internal Stream BodyStream
		{
			get
			{
				this.ThrowIfDisposed();
				return this.bodyStream;
			}
			set
			{
				this.ThrowIfDisposed();
				if (this.bodyStream != null && this.ownsBodyStream)
				{
					this.bodyStream.Dispose();
				}
				this.bodyStream = value;
			}
		}

		public string ContentType
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.contentType;
			}
			set
			{
				this.ThrowIfDisposed();
				this.contentType = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.ContentType);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.ContentType;
			}
		}

		public string CorrelationId
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.correlationId;
			}
			set
			{
				this.ThrowIfDisposed();
				this.correlationId = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.CorrelationId);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.CorrelationId;
			}
		}

		private static string DefaultNewMessageId
		{
			get
			{
				return Guid.NewGuid().ToString("N");
			}
		}

		public int DeliveryCount
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				this.ThrowIfNotReceived();
				return this.receiverHeaders.DeliveryCount;
			}
			internal set
			{
				this.ThrowIfDisposed();
				this.EnsureReceiverHeaders();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.DeliveryCount;
				this.receiverHeaders.DeliveryCount = value;
			}
		}

		internal string Destination
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.destination;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage.ValidateDestination(value);
				this.destination = value;
				if (value != null)
				{
					BrokeredMessage brokeredMessage = this;
					brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.Destination;
				}
				else
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.Destination);
				}
				if (this.version < BrokeredMessage.MessageVersion9)
				{
					this.version = BrokeredMessage.MessageVersion9;
				}
			}
		}

		public long EnqueuedSequenceNumber
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				this.ThrowIfNotReceived();
				return this.receiverHeaders.EnqueuedSequenceNumber;
			}
			internal set
			{
				this.ThrowIfDisposed();
				this.EnsureReceiverHeaders();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.EnqueuedSequenceNumber;
				this.receiverHeaders.EnqueuedSequenceNumber = value;
				if (this.version < BrokeredMessage.MessageVersion5)
				{
					this.version = BrokeredMessage.MessageVersion5;
				}
			}
		}

		public DateTime EnqueuedTimeUtc
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				this.ThrowIfNotReceived();
				return this.enqueuedTimeUtc;
			}
			internal set
			{
				this.ThrowIfDisposed();
				this.EnsureReceiverHeaders();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.EnqueuedTimeUtc;
				this.enqueuedTimeUtc = value;
			}
		}

		public DateTime ExpiresAtUtc
		{
			get
			{
				this.ThrowIfDisposed();
				this.ThrowIfNotReceived();
				if (this.RecordedExpiredAtUtc.HasValue)
				{
					return this.RecordedExpiredAtUtc.Value;
				}
				if (this.TimeToLive >= DateTime.MaxValue.Subtract(this.enqueuedTimeUtc))
				{
					return DateTime.MaxValue;
				}
				return this.EnqueuedTimeUtc.Add(this.TimeToLive);
			}
		}

		public bool ForcePersistence
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.forcePersistence;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.ForcePersistence;
				this.forcePersistence = value;
				if (this.version < BrokeredMessage.MessageVersion9)
				{
					this.version = BrokeredMessage.MessageVersion9;
				}
			}
		}

		internal long HeaderSize
		{
			get
			{
				this.ThrowIfDisposed();
				return this.headerSize;
			}
		}

		internal BufferedInputStream HeaderStream
		{
			get
			{
				return this.headerStream;
			}
			set
			{
				if (this.headerStream != null)
				{
					this.headerStream.Dispose();
				}
				this.headerStream = value;
			}
		}

		internal int HeaderStreamMaxSize
		{
			get
			{
				if (this.SubqueueType == Microsoft.ServiceBus.Messaging.SubqueueType.DeadLettered)
				{
					return BrokeredMessage.deadLetterheaderStreamMaxSize;
				}
				return BrokeredMessage.headerStreamMaxSize;
			}
		}

		internal BrokeredMessage.MessageMembers InitializedMembers
		{
			get
			{
				return this.initializedMembers;
			}
			set
			{
				this.initializedMembers = value;
			}
		}

		internal BrokeredMessageState InternalBrokeredMessageState
		{
			get;
			set;
		}

		internal Guid InternalId
		{
			get;
			set;
		}

		internal IDictionary<string, object> InternalProperties
		{
			get
			{
				if (this.properties == null)
				{
					Interlocked.CompareExchange<IDictionary<string, object>>(ref this.properties, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);
				}
				return this.properties;
			}
		}

		internal bool IsActivatingScheduledMessage
		{
			get;
			set;
		}

		public bool IsBodyConsumed
		{
			get
			{
				this.ThrowIfDisposed();
				if (this.getBodyCalled == 1)
				{
					return true;
				}
				return this.messageConsumed == 1;
			}
		}

		internal bool IsBodyLoaded
		{
			get
			{
				this.ThrowIfDisposed();
				return this.isBodyLoaded;
			}
			set
			{
				this.ThrowIfDisposed();
				this.isBodyLoaded = value;
			}
		}

		internal bool IsConsumed
		{
			get
			{
				return 1 == Interlocked.Exchange(ref this.messageConsumed, 1);
			}
			set
			{
				Interlocked.Exchange(ref this.messageConsumed, (value ? 1 : 0));
			}
		}

		internal bool IsFromCache
		{
			get
			{
				return this.isFromCache;
			}
			set
			{
				this.isFromCache = value;
			}
		}

		internal bool IsLockTokenSet
		{
			get
			{
				this.EnsureHeaderDeserialized();
				return (int)(this.initializedMembers & BrokeredMessage.MessageMembers.LockToken) != 0;
			}
		}

		internal bool IsPingMessage
		{
			get
			{
				return string.Equals("application/vnd.ms-servicebus-ping", this.ContentType, StringComparison.OrdinalIgnoreCase);
			}
		}

		internal bool IsTransferMessage
		{
			get
			{
				return !string.IsNullOrWhiteSpace(this.TransferDestination);
			}
		}

		public string Label
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.label;
			}
			set
			{
				this.ThrowIfDisposed();
				this.label = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.Label);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.Label;
			}
		}

		public DateTime LockedUntilUtc
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				this.ThrowIfNotLocked();
				return this.receiverHeaders.LockedUntilUtc;
			}
			internal set
			{
				this.ThrowIfDisposed();
				this.EnsureReceiverHeaders();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.LockedUntilUtc;
				this.receiverHeaders.LockedUntilUtc = value;
			}
		}

		public Guid LockToken
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				this.ThrowIfNotLocked();
				return this.receiverHeaders.LockToken;
			}
			internal set
			{
				this.ThrowIfDisposed();
				this.EnsureReceiverHeaders();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.LockToken;
				this.receiverHeaders.LockToken = value;
			}
		}

		internal IBrokeredMessageEncoder MessageEncoder
		{
			get
			{
				this.ThrowIfDisposed();
				return this.messageEncoder;
			}
			set
			{
				this.ThrowIfDisposed();
				this.messageEncoder = value;
			}
		}

		internal BrokeredMessageFormat MessageFormat
		{
			get
			{
				this.ThrowIfDisposed();
				return this.messageFormat;
			}
			set
			{
				this.ThrowIfDisposed();
				this.messageFormat = value;
			}
		}

		public string MessageId
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.messageId;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage.ValidateMessageId(value);
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.MessageId;
				this.messageId = value;
			}
		}

		internal long Offset
		{
			get
			{
				return this.EnqueuedSequenceNumber;
			}
		}

		internal short PartitionId
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.partitionId;
			}
			set
			{
				if (value < 0)
				{
					throw Fx.Exception.AsError(new ArgumentOutOfRangeException("PartitionId"), null);
				}
				this.ThrowIfDisposed();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.PartitionId;
				this.partitionId = value;
				if (this.version < BrokeredMessage.MessageVersion6)
				{
					this.version = BrokeredMessage.MessageVersion6;
				}
			}
		}

		public string PartitionKey
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.partitionKey;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage.ValidatePartitionKey("PartitionKey", value);
				this.ThrowIfDominatingPropertyIsNotEqualToNonNullDormantProperty(BrokeredMessage.MessageMembers.PartitionKey, BrokeredMessage.MessageMembers.SessionId, value, this.sessionId);
				this.partitionKey = value;
				if (value != null)
				{
					BrokeredMessage brokeredMessage = this;
					brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.PartitionKey;
				}
				else
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.PartitionKey);
				}
				if (this.version < BrokeredMessage.MessageVersion8)
				{
					this.version = BrokeredMessage.MessageVersion8;
				}
			}
		}

		internal long PersistedMessageSize
		{
			get
			{
				return this.persistedMessageSize;
			}
			set
			{
				this.persistedMessageSize = value;
			}
		}

		internal Stream PrefilteredHeaders
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.prefilteredHeaders;
			}
			set
			{
				this.ThrowIfDisposed();
				if (this.prefilteredHeaders != null)
				{
					this.prefilteredHeaders.Dispose();
				}
				this.prefilteredHeaders = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.PrefilteredHeaders);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.PrefilteredHeaders;
			}
		}

		internal Stream PrefilteredProperties
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.prefilteredProperties;
			}
			set
			{
				this.ThrowIfDisposed();
				if (this.prefilteredProperties != null)
				{
					this.prefilteredProperties.Dispose();
				}
				this.prefilteredProperties = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.PrefilteredProperties);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.PrefilteredProperties;
			}
		}

		public IDictionary<string, object> Properties
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.InternalProperties;
			}
		}

		internal string Publisher
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.publisher;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage.ValidatePartitionKey("Publisher", value);
				if (value != null)
				{
					this.ThrowIfDominatingPropertyIsNotEqualToNonNullDormantProperty(BrokeredMessage.MessageMembers.Publisher, BrokeredMessage.MessageMembers.PartitionKey, value, this.partitionKey);
				}
				if (!string.IsNullOrEmpty(value))
				{
					BrokeredMessage brokeredMessage = this;
					brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.Publisher;
				}
				else
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.Publisher);
				}
				if (this.version < BrokeredMessage.MessageVersion10)
				{
					this.version = BrokeredMessage.MessageVersion10;
				}
				this.publisher = value;
			}
		}

		internal Stream RawHeaderStream
		{
			get
			{
				this.ThrowIfDisposed();
				return this.rawHeaderStream;
			}
			set
			{
				this.ThrowIfDisposed();
				if (this.rawHeaderStream != null && this.ownsRawHeaderStream)
				{
					this.rawHeaderStream.Dispose();
				}
				this.rawHeaderStream = value;
			}
		}

		internal Microsoft.ServiceBus.Messaging.ReceiveContext ReceiveContext
		{
			get
			{
				this.ThrowIfDisposed();
				return this.receiveContext;
			}
			set
			{
				this.ThrowIfDisposed();
				this.receiveContext = value;
			}
		}

		internal DateTime? RecordedExpiredAtUtc
		{
			get;
			set;
		}

		internal Guid RecordInfoAsGuid
		{
			get;
			set;
		}

		public string ReplyTo
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.replyTo;
			}
			set
			{
				this.ThrowIfDisposed();
				this.replyTo = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.ReplyTo);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.ReplyTo;
			}
		}

		public string ReplyToSessionId
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.replyToSessionId;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage.ValidateSessionId(value);
				this.replyToSessionId = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.ReplyToSessionId);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.ReplyToSessionId;
			}
		}

		public DateTime ScheduledEnqueueTimeUtc
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.scheduledEnqueueTimeUtc;
			}
			set
			{
				this.ThrowIfDisposed();
				if (value == DateTime.MaxValue)
				{
					throw Fx.Exception.AsError(new ArgumentOutOfRangeException("ScheduledEnqueueTimeUtc"), null);
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.ScheduledEnqueueTimeUtc;
				this.scheduledEnqueueTimeUtc = value;
			}
		}

		public long SequenceNumber
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				this.ThrowIfNotReceived();
				return this.receiverHeaders.SequenceNumber;
			}
			internal set
			{
				this.ThrowIfDisposed();
				this.EnsureReceiverHeaders();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.SequenceNumber;
				this.receiverHeaders.SequenceNumber = value;
			}
		}

		public string SessionId
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.sessionId;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage.ValidateSessionId(value);
				this.sessionId = value;
				if (value != null)
				{
					BrokeredMessage brokeredMessage = this;
					brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.SessionId;
				}
				else
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.SessionId);
				}
				if (this.version >= BrokeredMessage.MessageVersion8)
				{
					this.PartitionKey = value;
				}
			}
		}

		public long Size
		{
			get
			{
				this.ThrowIfDisposed();
				return this.HeaderSize + this.BodySize;
			}
		}

		public MessageState State
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.state;
			}
			internal set
			{
				this.ThrowIfDisposed();
				this.state = value;
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.MessageState;
				if (this.version < BrokeredMessage.MessageVersion4)
				{
					this.version = BrokeredMessage.MessageVersion4;
				}
			}
		}

		internal Microsoft.ServiceBus.Messaging.SubqueueType SubqueueType
		{
			get;
			set;
		}

		internal bool SupportsEmptySerializedHeader
		{
			get
			{
				if (!this.supportsEmptySerializedHeader)
				{
					return false;
				}
				if (this.originalDeliveryCount.HasValue && this.receiverHeaders != null)
				{
					int deliveryCount = this.DeliveryCount;
					int? nullable = this.originalDeliveryCount;
					if ((deliveryCount != nullable.GetValueOrDefault() ? true : !nullable.HasValue))
					{
						return false;
					}
				}
				return (this.InitializedMembers | BrokeredMessage.MessageMembers.MessageId | BrokeredMessage.MessageMembers.EnqueuedTimeUtc | BrokeredMessage.MessageMembers.SequenceNumber | BrokeredMessage.MessageMembers.DeliveryCount | BrokeredMessage.MessageMembers.EnqueuedSequenceNumber) == (BrokeredMessage.MessageMembers.MessageId | BrokeredMessage.MessageMembers.EnqueuedTimeUtc | BrokeredMessage.MessageMembers.SequenceNumber | BrokeredMessage.MessageMembers.DeliveryCount | BrokeredMessage.MessageMembers.EnqueuedSequenceNumber);
			}
			set
			{
				this.supportsEmptySerializedHeader = value;
			}
		}

		public TimeSpan TimeToLive
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				if (this.timeToLive == TimeSpan.Zero)
				{
					return TimeSpan.MaxValue;
				}
				return this.timeToLive;
			}
			set
			{
				Microsoft.ServiceBus.Common.TimeoutHelper.ThrowIfNonPositiveArgument(value);
				this.ThrowIfDisposed();
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.TimeToLive;
				this.timeToLive = value;
			}
		}

		public string To
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.to;
			}
			set
			{
				this.ThrowIfDisposed();
				this.to = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.To);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.To;
			}
		}

		internal string TransferDestination
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.transferDestination;
			}
			set
			{
				this.ThrowIfDisposed();
				this.transferDestination = value;
				if (string.IsNullOrEmpty(value))
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.TransferDestination);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.TransferDestination;
			}
		}

		internal long TransferDestinationResourceId
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.transferDestinationResourceId;
			}
			set
			{
				this.ThrowIfDisposed();
				this.transferDestinationResourceId = value;
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.TransferDestinationEntityId;
			}
		}

		internal int TransferHopCount
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.transferHopCount;
			}
			set
			{
				if (value < 0)
				{
					throw Fx.Exception.AsError(new ArgumentOutOfRangeException("TransferHopCount"), null);
				}
				this.ThrowIfDisposed();
				this.transferHopCount = value;
				if (value == 0)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.TransferHopCount);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.TransferHopCount;
			}
		}

		internal long TransferSequenceNumber
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.transferSequenceNumber;
			}
			set
			{
				this.ThrowIfDisposed();
				this.transferSequenceNumber = value;
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.TransferSequenceNumber;
			}
		}

		internal string TransferSessionId
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.transferSessionId;
			}
			set
			{
				this.ThrowIfDisposed();
				this.transferSessionId = value;
				if (value == null)
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.TransferSessionId);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.TransferSessionId;
			}
		}

		internal string TransferSource
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.transferSource;
			}
			set
			{
				this.ThrowIfDisposed();
				this.transferSource = value;
				if (string.IsNullOrEmpty(value))
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.TransferSource);
					return;
				}
				BrokeredMessage brokeredMessage = this;
				brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.TransferSource;
			}
		}

		internal int Version
		{
			get
			{
				return this.version;
			}
		}

		public string ViaPartitionKey
		{
			get
			{
				this.ThrowIfDisposed();
				this.EnsureHeaderDeserialized();
				return this.viaPartitionKey;
			}
			set
			{
				this.ThrowIfDisposed();
				BrokeredMessage.ValidatePartitionKey("ViaPartitionKey", value);
				this.viaPartitionKey = value;
				if (value != null)
				{
					BrokeredMessage brokeredMessage = this;
					brokeredMessage.initializedMembers = brokeredMessage.initializedMembers | BrokeredMessage.MessageMembers.ViaPartitionKey;
				}
				else
				{
					this.ClearInitializedMember(BrokeredMessage.MessageMembers.ViaPartitionKey);
				}
				if (this.version < BrokeredMessage.MessageVersion8)
				{
					this.version = BrokeredMessage.MessageVersion8;
				}
			}
		}

		static BrokeredMessage()
		{
			BrokeredMessage.mode = BrokeredMessage.BrokeredMessageMode.Client;
			BrokeredMessage.binarySerializationItems = BrokeredMessage.BuildBinarySerializationItems(BrokeredMessage.mode);
			Dictionary<string, Func<BrokeredMessage, object>> strs = new Dictionary<string, Func<BrokeredMessage, object>>(StringComparer.OrdinalIgnoreCase)
			{
				{ "MessageId", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.MessageId) },
				{ "CorrelationId", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.CorrelationId) },
				{ "To", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.To) },
				{ "ReplyTo", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.ReplyTo) },
				{ "SessionId", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.SessionId) },
				{ "Label", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.Label) },
				{ "ContentType", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.ContentType) },
				{ "ReplyToSessionId", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.ReplyToSessionId) },
				{ "TimeToLive", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.TimeToLive) },
				{ "ScheduledEnqueueTimeUtc", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.ScheduledEnqueueTimeUtc) },
				{ "PartitionKey", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.PartitionKey) },
				{ "EnqueuedTimeUtc", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.EnqueuedTimeUtc) },
				{ "SequenceNumber", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.SequenceNumber) },
				{ "LockToken", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.LockToken) },
				{ "LockedUntilUtc", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.LockedUntilUtc) },
				{ "DeliveryCount", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.DeliveryCount) },
				{ "EnqueuedSequenceNumber", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.EnqueuedSequenceNumber) },
				{ "ViaPartitionKey", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.ViaPartitionKey) },
				{ "Destination", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.Destination) },
				{ "ForcePersistence", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.ForcePersistence) },
				{ "Publisher", new Func<BrokeredMessage, object>((BrokeredMessage message) => message.Publisher) }
			};
			BrokeredMessage.SystemPropertyAccessorDictionary = strs;
			BrokeredMessage.headerStreamMaxSize = 65536;
			BrokeredMessage.deadLetterheaderStreamMaxSize = BrokeredMessage.headerStreamMaxSize + 2048;
			BrokeredMessage.messageFlags = new byte[2];
			BrokeredMessage.MessageVersion1 = 1;
			BrokeredMessage.MessageVersion2 = 2;
			BrokeredMessage.MessageVersion3 = 3;
			BrokeredMessage.MessageVersion4 = 4;
			BrokeredMessage.MessageVersion5 = 5;
			BrokeredMessage.MessageVersion6 = 6;
			BrokeredMessage.MessageVersion7 = 7;
			BrokeredMessage.MessageVersion8 = 8;
			BrokeredMessage.MessageVersion9 = 9;
			BrokeredMessage.MessageVersion10 = 10;
			BrokeredMessage.MessageVersion = BrokeredMessage.MessageVersion10;
		}

		public BrokeredMessage() : this(BrokeredMessage.DefaultNewMessageId)
		{
		}

		public BrokeredMessage(object serializableObject) : this(serializableObject, (serializableObject == null ? null : new DataContractBinarySerializer(BrokeredMessage.GetObjectType(serializableObject))))
		{
			this.bodyObject = serializableObject;
		}

		public BrokeredMessage(object serializableObject, XmlObjectSerializer serializer) : this(BrokeredMessage.DefaultNewMessageId)
		{
			if (serializableObject != null)
			{
				if (serializer == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new ArgumentNullException("serializer"), null);
				}
				MemoryStream memoryStream = new MemoryStream(256);
				serializer.WriteObject(memoryStream, serializableObject);
				memoryStream.Flush();
				memoryStream.Position = (long)0;
				this.BodyStream = memoryStream;
				this.ownsBodyStream = true;
			}
		}

		public BrokeredMessage(Stream messageBodyStream) : this(messageBodyStream, false)
		{
		}

		public BrokeredMessage(Stream messageBodyStream, bool ownsStream) : this(BrokeredMessage.DefaultNewMessageId)
		{
			this.ownsBodyStream = ownsStream;
			this.BodyStream = messageBodyStream;
		}

		internal BrokeredMessage(Stream rawHeaderStream, bool ownsRawHeaderStream, Stream messageBodyStream, bool ownsStream, BrokeredMessageFormat format) : this(BrokeredMessage.DefaultNewMessageId)
		{
			this.rawHeaderStream = rawHeaderStream;
			this.ownsRawHeaderStream = ownsRawHeaderStream;
			this.ownsBodyStream = ownsStream;
			this.BodyStream = messageBodyStream;
			this.messageFormat = format;
			this.messageEncoder = BrokeredMessageEncoder.GetEncoder(this.messageFormat);
		}

		internal BrokeredMessage(BufferedInputStream headerStream, Stream bodyStream)
		{
			this.headerStream = headerStream;
			this.BodyStream = bodyStream;
			this.isBodyLoaded = true;
			this.ownsBodyStream = true;
			this.InternalBrokeredMessageState = BrokeredMessageState.Active;
			this.isFromCache = false;
			if (this.headerStream.CanSeek)
			{
				this.headerSize = this.headerStream.Length;
			}
		}

		internal BrokeredMessage(object bodyObject, Stream bodyStream) : this()
		{
			this.bodyObject = bodyObject;
			this.bodyObjectDecoded = true;
			this.bodyStream = bodyStream;
			this.ownsBodyStream = true;
		}

		private BrokeredMessage(string messageId)
		{
			BrokeredMessage.ValidateMessageId(messageId);
			this.MessageId = messageId;
			this.headersDeserialized = true;
			this.InternalBrokeredMessageState = BrokeredMessageState.Active;
			this.isFromCache = false;
			this.version = BrokeredMessage.MessageVersion;
		}

		private BrokeredMessage(BrokeredMessage originalMessage, bool clientSideCloning)
		{
			this.version = originalMessage.version;
			this.messageFormat = originalMessage.messageFormat;
			this.messageEncoder = originalMessage.messageEncoder;
			this.arePropertiesModifiedByBroker = originalMessage.arePropertiesModifiedByBroker;
			this.CopyMessageHeaders(originalMessage, clientSideCloning);
			if (originalMessage.rawHeaderStream != null)
			{
				this.rawHeaderStream = BrokeredMessage.CloneStream(originalMessage.rawHeaderStream, clientSideCloning);
				this.ownsRawHeaderStream = true;
			}
			if (originalMessage.BodyStream != null)
			{
				this.BodyStream = BrokeredMessage.CloneStream(originalMessage.BodyStream, clientSideCloning);
				this.ownsBodyStream = true;
			}
			this.AttachDisposables(BrokeredMessage.CloneDisposables(originalMessage.attachedDisposables));
			this.InternalId = originalMessage.InternalId;
			this.InternalBrokeredMessageState = originalMessage.InternalBrokeredMessageState;
			this.SubqueueType = originalMessage.SubqueueType;
			this.IsActivatingScheduledMessage = originalMessage.IsActivatingScheduledMessage;
			this.isFromCache = originalMessage.isFromCache;
			this.IsBodyLoaded = originalMessage.isBodyLoaded;
			this.persistedMessageSize = originalMessage.persistedMessageSize;
			this.AllowOverflowOnPersist = originalMessage.AllowOverflowOnPersist;
			this.RecordInfoAsGuid = originalMessage.RecordInfoAsGuid;
			if (originalMessage.receiverHeaders != null)
			{
				this.originalDeliveryCount = new int?(originalMessage.receiverHeaders.DeliveryCount);
			}
		}

		public void Abandon()
		{
			this.Abandon(null);
		}

		public void Abandon(IDictionary<string, object> propertiesToModify)
		{
			this.ThrowIfReceiveContextIsNull();
			this.EndAbandon(this.BeginAbandon(propertiesToModify, this.ReceiveContext.OperationTimeout, null, null));
		}

		public Task AbandonAsync()
		{
			return TaskHelpers.CreateTask(new Func<AsyncCallback, object, IAsyncResult>(this.BeginAbandon), new Action<IAsyncResult>(this.EndAbandon));
		}

		public Task AbandonAsync(IDictionary<string, object> propertiesToModify)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginAbandon(propertiesToModify, c, s), new Action<IAsyncResult>(this.EndAbandon));
		}

		internal void AttachDisposables(IEnumerable<IDisposable> disposables)
		{
			if (disposables == null)
			{
				return;
			}
			if (this.attachedDisposables != null)
			{
				lock (this.disposablesSyncObject)
				{
					this.attachedDisposables.AddRange(disposables);
				}
			}
			else
			{
				lock (this.disposablesSyncObject)
				{
					if (this.attachedDisposables == null)
					{
						this.attachedDisposables = new List<IDisposable>(4);
					}
					this.attachedDisposables.AddRange(disposables);
				}
			}
		}

		public IAsyncResult BeginAbandon(AsyncCallback callback, object state)
		{
			return this.BeginAbandon(null, callback, state);
		}

		public IAsyncResult BeginAbandon(IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginAbandon(propertiesToModify, this.ReceiveContext.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginAbandon(IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfDisposed();
			this.ThrowIfNotLocked();
			this.ThrowIfReceiveContextIsNull();
			return this.ReceiveContext.BeginAbandon(propertiesToModify, timeout, callback, state);
		}

		public IAsyncResult BeginComplete(AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginComplete(this.ReceiveContext.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginComplete(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfDisposed();
			this.ThrowIfNotLocked();
			this.ThrowIfReceiveContextIsNull();
			return this.ReceiveContext.BeginComplete(timeout, callback, state);
		}

		public IAsyncResult BeginDeadLetter(AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginDeadLetter(null, null, null, this.ReceiveContext.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginDeadLetter(string deadLetterReason, string deadLetterErrorDescription, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginDeadLetter(null, deadLetterReason, deadLetterErrorDescription, this.ReceiveContext.OperationTimeout, callback, state);
		}

		public IAsyncResult BeginDeadLetter(IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginDeadLetter(propertiesToModify, null, null, this.ReceiveContext.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginDeadLetter(IDictionary<string, object> propertiesToModify, string deadLetterReason, string deadLetterErrorDescription, TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfDisposed();
			this.ThrowIfNotLocked();
			this.ThrowIfReceiveContextIsNull();
			return this.ReceiveContext.BeginDeadLetter(propertiesToModify, deadLetterReason, deadLetterErrorDescription, timeout, callback, state);
		}

		public IAsyncResult BeginDefer(AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginDefer(null, callback, state);
		}

		public IAsyncResult BeginDefer(IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginDefer(propertiesToModify, this.ReceiveContext.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginDefer(IDictionary<string, object> propertiesToModify, TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfDisposed();
			this.ThrowIfNotLocked();
			this.ThrowIfReceiveContextIsNull();
			return this.ReceiveContext.BeginDefer(propertiesToModify, timeout, callback, state);
		}

		public IAsyncResult BeginRenewLock(AsyncCallback callback, object state)
		{
			this.ThrowIfReceiveContextIsNull();
			return this.BeginRenewLock(this.ReceiveContext.OperationTimeout, callback, state);
		}

		internal IAsyncResult BeginRenewLock(TimeSpan timeout, AsyncCallback callback, object state)
		{
			this.ThrowIfDisposed();
			this.ThrowIfNotLocked();
			this.ThrowIfReceiveContextIsNull(SRClient.InvalidMethodWhilePeeking("RenewLock"));
			return this.ReceiveContext.BeginRenewLock(timeout, callback, state);
		}

		internal void BrokerRemoveProperty(string name)
		{
			this.Properties.Remove(name);
			this.SetPropertiesAsModifiedByBroker();
		}

		internal void BrokerUpdateProperty(string name, object value)
		{
			this.Properties[name] = value;
			this.SetPropertiesAsModifiedByBroker();
		}

		private static BrokeredMessage.BinarySerializationItem[] BuildBinarySerializationItems(BrokeredMessage.BrokeredMessageMode type)
		{
			BrokeredMessage.BinarySerializationItem binarySerializationItem;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray = new BrokeredMessage.BinarySerializationItem[31];
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray1 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem1 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.MessageId,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.MessageId) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.MessageId),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.MessageId)
			};
			binarySerializationItemArray1[0] = binarySerializationItem1;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray2 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem2 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.CorrelationId,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.CorrelationId) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.CorrelationId),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.CorrelationId)
			};
			binarySerializationItemArray2[1] = binarySerializationItem2;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray3 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem3 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.SessionId,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.SessionId) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.SessionId),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.SessionId)
			};
			binarySerializationItemArray3[2] = binarySerializationItem3;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray4 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem4 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.TimeToLive,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.TimeToLive) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.TimeSpan, messageArg.TimeToLive),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetTimeSpanSize(messageArg.TimeToLive)
			};
			binarySerializationItemArray4[3] = binarySerializationItem4;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray5 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem5 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.ReplyTo,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.ReplyTo) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.ReplyTo),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.ReplyTo)
			};
			binarySerializationItemArray5[4] = binarySerializationItem5;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray6 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem6 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.To,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.To) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.To),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.To)
			};
			binarySerializationItemArray6[5] = binarySerializationItem6;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray7 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem7 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.ReplyToSessionId,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.ReplyToSessionId) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.ReplyToSessionId),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.ReplyToSessionId)
			};
			binarySerializationItemArray7[6] = binarySerializationItem7;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray8 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem8 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.EnqueuedTimeUtc,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.EnqueuedTimeUtc) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.DateTime, messageArg.EnqueuedTimeUtc),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetDateTimeSize(messageArg.EnqueuedTimeUtc)
			};
			binarySerializationItemArray8[7] = binarySerializationItem8;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray9 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem9 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.ScheduledEnqueueTimeUtc,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.ScheduledEnqueueTimeUtc) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.DateTime, messageArg.ScheduledEnqueueTimeUtc),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetDateTimeSize(messageArg.ScheduledEnqueueTimeUtc)
			};
			binarySerializationItemArray9[8] = binarySerializationItem9;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray10 = binarySerializationItemArray;
			if (type == BrokeredMessage.BrokeredMessageMode.Broker)
			{
				BrokeredMessage.BinarySerializationItem binarySerializationItem10 = new BrokeredMessage.BinarySerializationItem()
				{
					FieldId = BrokeredMessage.FieldId.SequenceNumber,
					ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.SequenceNumber) != 0,
					Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => {
						long sequenceNumber = messageArg.SequenceNumber;
						if (serializationTarget == SerializationTarget.Communication && messageArg.HasHeader(BrokeredMessage.MessageMembers.PartitionId))
						{
							sequenceNumber = ((long)messageArg.PartitionId << 48) + messageArg.SequenceNumber;
						}
						return SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int64, sequenceNumber);
					},
					CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetLongSize(messageArg.SequenceNumber)
				};
				binarySerializationItem = binarySerializationItem10;
			}
			else
			{
				BrokeredMessage.BinarySerializationItem binarySerializationItem11 = new BrokeredMessage.BinarySerializationItem()
				{
					FieldId = BrokeredMessage.FieldId.SequenceNumber,
					ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.SequenceNumber) != 0,
					Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int64, messageArg.SequenceNumber),
					CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetLongSize(messageArg.SequenceNumber)
				};
				binarySerializationItem = binarySerializationItem11;
			}
			binarySerializationItemArray10[9] = binarySerializationItem;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray11 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem12 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.LockToken,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.LockToken) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Guid, messageArg.LockToken),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetGuidSize(messageArg.LockToken)
			};
			binarySerializationItemArray11[10] = binarySerializationItem12;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray12 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem13 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.LockedUntilUtc,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.LockedUntilUtc) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.DateTime, messageArg.LockedUntilUtc),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetDateTimeSize(messageArg.LockedUntilUtc)
			};
			binarySerializationItemArray12[11] = binarySerializationItem13;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray13 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem14 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.DeliveryCount,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.DeliveryCount) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int32, messageArg.DeliveryCount),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetIntSize(messageArg.DeliveryCount)
			};
			binarySerializationItemArray13[12] = binarySerializationItem14;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray14 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem15 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.PartitionKey,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.PartitionKey) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.PartitionKey),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.PartitionKey)
			};
			binarySerializationItemArray14[13] = binarySerializationItem15;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray15 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem16 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.Label,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.Label) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.Label),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.Label)
			};
			binarySerializationItemArray15[14] = binarySerializationItem16;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray16 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem17 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.ContentType,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.ContentType) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.ContentType),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.ContentType)
			};
			binarySerializationItemArray16[15] = binarySerializationItem17;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray17 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem18 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.PrefilteredMessageHeaders,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.PrefilteredHeaders) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Stream, messageArg.PrefilteredHeaders),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStreamSize(messageArg.PrefilteredHeaders)
			};
			binarySerializationItemArray17[16] = binarySerializationItem18;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray18 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem19 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.PrefilteredMessageProperties,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.PrefilteredProperties) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Stream, messageArg.PrefilteredProperties),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStreamSize(messageArg.PrefilteredProperties)
			};
			binarySerializationItemArray18[17] = binarySerializationItem19;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray19 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem20 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.TransferDestination,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.TransferDestination) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.TransferDestination),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.TransferDestination)
			};
			binarySerializationItemArray19[18] = binarySerializationItem20;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray20 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem21 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.TransferDestinationResourceId,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.TransferDestinationEntityId) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int64, messageArg.TransferDestinationResourceId)
			};
			binarySerializationItemArray20[19] = binarySerializationItem21;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray21 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem22 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.PartitionId,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.PartitionId) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int16, messageArg.PartitionId),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetShortSize(messageArg.PartitionId)
			};
			binarySerializationItemArray21[20] = binarySerializationItem22;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray22 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem23 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.TransferSessionId,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.TransferSessionId) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.TransferSessionId),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.TransferSessionId)
			};
			binarySerializationItemArray22[21] = binarySerializationItem23;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray23 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem24 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.TransferSource,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.TransferSource) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.TransferSource),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.TransferSource)
			};
			binarySerializationItemArray23[22] = binarySerializationItem24;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray24 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem25 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.TransferSequenceNumber,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.TransferSequenceNumber) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int64, messageArg.TransferSequenceNumber),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetLongSize(messageArg.TransferSequenceNumber)
			};
			binarySerializationItemArray24[23] = binarySerializationItem25;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray25 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem26 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.TransferHopCount,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.TransferHopCount) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int32, messageArg.TransferHopCount),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetIntSize(messageArg.TransferHopCount)
			};
			binarySerializationItemArray25[24] = binarySerializationItem26;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray26 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem27 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.MessageState,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.MessageState) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int32, messageArg.State),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetIntSize((int)messageArg.State)
			};
			binarySerializationItemArray26[25] = binarySerializationItem27;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray27 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem28 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.EnqueuedSequenceNumber,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.EnqueuedSequenceNumber) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Int64, messageArg.EnqueuedSequenceNumber),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetLongSize(messageArg.EnqueuedSequenceNumber)
			};
			binarySerializationItemArray27[26] = binarySerializationItem28;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray28 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem29 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.ViaPartitionKey,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.ViaPartitionKey) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.ViaPartitionKey),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.ViaPartitionKey)
			};
			binarySerializationItemArray28[27] = binarySerializationItem29;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray29 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem30 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.Destination,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.Destination) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.Destination),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.Destination)
			};
			binarySerializationItemArray29[28] = binarySerializationItem30;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray30 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem31 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.ForcePersistence,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.ForcePersistence) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.Boolean, messageArg.ForcePersistence),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetBooleanSize(messageArg.ForcePersistence)
			};
			binarySerializationItemArray30[29] = binarySerializationItem31;
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray31 = binarySerializationItemArray;
			BrokeredMessage.BinarySerializationItem binarySerializationItem32 = new BrokeredMessage.BinarySerializationItem()
			{
				FieldId = BrokeredMessage.FieldId.Publisher,
				ShouldSerialize = (BrokeredMessage msg, SerializationTarget serializationTarget) => (int)(msg.initializedMembers & BrokeredMessage.MessageMembers.Publisher) != 0,
				Extractor = (BrokeredMessage messageArg, SerializationTarget serializationTarget) => SerializationUtilities.ConvertNativeValueToByteArray(messageArg.version, PropertyValueType.String, messageArg.Publisher),
				CalculateSize = (BrokeredMessage messageArg) => SerializationUtilities.GetStringSize(messageArg.Publisher)
			};
			binarySerializationItemArray31[30] = binarySerializationItem32;
			return binarySerializationItemArray;
		}

		private static long CalculateSerializedHeadersSize(BrokeredMessage message, SerializationTarget serializationTarget)
		{
			long length = (long)0;
			length = length + (long)(4 + (int)BrokeredMessage.messageFlags.Length + 2);
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray = BrokeredMessage.binarySerializationItems;
			for (int i = 0; i < (int)binarySerializationItemArray.Length; i++)
			{
				BrokeredMessage.BinarySerializationItem binarySerializationItem = binarySerializationItemArray[i];
				if (binarySerializationItem.ShouldSerialize(message, serializationTarget))
				{
					length = length + (long)(3 + binarySerializationItem.CalculateSize(message));
				}
			}
			return length;
		}

		private static long CalculateSerializedPropertiesSize(BrokeredMessage message)
		{
			long stringSize = (long)3;
			if (message.Properties.Count > 0)
			{
				foreach (KeyValuePair<string, object> property in message.Properties)
				{
					stringSize = stringSize + (long)(2 + SerializationUtilities.GetStringSize(property.Key));
					stringSize = stringSize + (long)1;
					PropertyValueType typeId = SerializationUtilities.GetTypeId(property.Value);
					if (typeId == PropertyValueType.Null)
					{
						continue;
					}
					byte[] byteArray = SerializationUtilities.ConvertNativeValueToByteArray(message.version, typeId, property.Value);
					stringSize = stringSize + (long)(2 + (int)byteArray.Length);
				}
			}
			return stringSize;
		}

		internal object ClearBodyObject()
		{
			object obj = this.bodyObject;
			this.bodyObject = null;
			return obj;
		}

		private void ClearInitializedMember(BrokeredMessage.MessageMembers memberToClear)
		{
			BrokeredMessage brokeredMessage = this;
			brokeredMessage.initializedMembers = brokeredMessage.initializedMembers & ~memberToClear;
		}

		internal void ClearPartitionId()
		{
			this.partitionId = 0;
			this.ClearInitializedMember(BrokeredMessage.MessageMembers.PartitionId);
		}

		public BrokeredMessage Clone()
		{
			return this.Clone(true);
		}

		internal BrokeredMessage Clone(bool includeSystemProperties)
		{
			this.ThrowIfDisposed();
			if (includeSystemProperties)
			{
				return new BrokeredMessage(this, true);
			}
			Stream stream = null;
			if (this.BodyStream != null)
			{
				stream = BrokeredMessage.CloneStream(this.BodyStream, true);
			}
			BrokeredMessage brokeredMessage = new BrokeredMessage(stream, true);
			foreach (KeyValuePair<string, object> property in this.Properties)
			{
				brokeredMessage.Properties.Add(property);
			}
			brokeredMessage.AttachDisposables(BrokeredMessage.CloneDisposables(this.attachedDisposables));
			return brokeredMessage;
		}

		internal static IEnumerable<IDisposable> CloneDisposables(IEnumerable<IDisposable> disposables)
		{
			if (disposables == null)
			{
				return null;
			}
			List<IDisposable> disposables1 = new List<IDisposable>();
			foreach (IDisposable disposable in disposables)
			{
				ICloneable cloneable = disposable as ICloneable;
				if (cloneable == null)
				{
					continue;
				}
				disposables1.Add((IDisposable)cloneable.Clone());
			}
			if (disposables1.Count <= 0)
			{
				return null;
			}
			return disposables1;
		}

		internal static Stream CloneStream(Stream originalStream, bool canThrowException = false)
		{
			Stream memoryStream = null;
			if (originalStream != null)
			{
				MemoryStream memoryStream1 = originalStream as MemoryStream;
				MemoryStream memoryStream2 = memoryStream1;
				if (memoryStream1 == null)
				{
					ICloneable cloneable = originalStream as ICloneable;
					ICloneable cloneable1 = cloneable;
					if (cloneable == null)
					{
						if (!canThrowException)
						{
							throw Fx.AssertAndThrow(string.Concat("Does not support cloning of Stream Type: ", originalStream.GetType()));
						}
						throw Fx.Exception.AsError(new InvalidOperationException(SRClient.BrokeredMessageStreamNotCloneable(originalStream.GetType().FullName)), null);
					}
					memoryStream = (Stream)cloneable1.Clone();
				}
				else
				{
					memoryStream = new MemoryStream(memoryStream2.ToArray(), 0, (int)memoryStream2.Length, false, true);
				}
			}
			return memoryStream;
		}

		public void Complete()
		{
			this.ThrowIfDisposed();
			this.ThrowIfNotLocked();
			this.ThrowIfReceiveContextIsNull();
			this.ReceiveContext.Complete();
		}

		public Task CompleteAsync()
		{
			return TaskHelpers.CreateTask(new Func<AsyncCallback, object, IAsyncResult>(this.BeginComplete), new Action<IAsyncResult>(this.EndComplete));
		}

		private void CopyMessageHeaders(BrokeredMessage originalMessage, bool clientSideCloning = false)
		{
			this.messageFormat = originalMessage.messageFormat;
			this.headersDeserialized = true;
			this.MessageId = originalMessage.MessageId;
			this.headerSize = originalMessage.HeaderSize;
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.SessionId) != 0)
			{
				this.SessionId = originalMessage.SessionId;
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.PartitionKey) != 0)
			{
				this.PartitionKey = originalMessage.PartitionKey;
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.ViaPartitionKey) != 0)
			{
				this.ViaPartitionKey = originalMessage.ViaPartitionKey;
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.Destination) != 0)
			{
				this.Destination = originalMessage.Destination;
			}
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.ScheduledEnqueueTimeUtc) != 0)
			{
				this.ScheduledEnqueueTimeUtc = originalMessage.ScheduledEnqueueTimeUtc;
			}
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.TimeToLive) != 0)
			{
				this.TimeToLive = originalMessage.TimeToLive;
			}
			if (!clientSideCloning && (int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.PartitionId) != 0)
			{
				this.PartitionId = originalMessage.PartitionId;
			}
			BrokeredMessage.ReceiverHeaders receiverHeader = originalMessage.receiverHeaders;
			if (receiverHeader != null && !clientSideCloning)
			{
				this.BodyId = originalMessage.BodyId;
				this.DeliveryCount = receiverHeader.DeliveryCount;
				this.SequenceNumber = receiverHeader.SequenceNumber;
				this.EnqueuedTimeUtc = originalMessage.EnqueuedTimeUtc;
				this.EnqueuedSequenceNumber = receiverHeader.EnqueuedSequenceNumber;
			}
			string label = originalMessage.Label;
			if (label != null)
			{
				this.Label = label;
			}
			if (originalMessage.CorrelationId != null)
			{
				this.CorrelationId = originalMessage.CorrelationId;
			}
			if (originalMessage.ReplyTo != null)
			{
				this.ReplyTo = originalMessage.ReplyTo;
			}
			if (originalMessage.To != null)
			{
				this.To = originalMessage.To;
			}
			if (originalMessage.Publisher != null)
			{
				this.Publisher = originalMessage.Publisher;
			}
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.ReplyToSessionId) != 0)
			{
				this.ReplyToSessionId = originalMessage.ReplyToSessionId;
			}
			if (originalMessage.ContentType != null)
			{
				this.ContentType = originalMessage.ContentType;
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.PrefilteredHeaders) != 0)
			{
				this.PrefilteredHeaders = BrokeredMessage.CloneStream(originalMessage.PrefilteredHeaders, false);
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.PrefilteredProperties) != 0)
			{
				this.PrefilteredProperties = BrokeredMessage.CloneStream(originalMessage.PrefilteredProperties, false);
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.TransferDestination) != 0)
			{
				this.TransferDestination = originalMessage.TransferDestination;
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.TransferDestinationEntityId) != 0)
			{
				this.TransferDestinationResourceId = originalMessage.TransferDestinationResourceId;
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.TransferSessionId) != 0)
			{
				this.TransferSessionId = originalMessage.TransferSessionId;
			}
			if ((int)(originalMessage.initializedMembers & BrokeredMessage.MessageMembers.TransferSource) != 0)
			{
				this.TransferSource = originalMessage.TransferSource;
			}
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.TransferSequenceNumber) != 0)
			{
				this.TransferSequenceNumber = originalMessage.TransferSequenceNumber;
			}
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.TransferHopCount) != 0)
			{
				this.TransferHopCount = originalMessage.TransferHopCount;
			}
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.MessageState) != 0)
			{
				this.State = originalMessage.State;
			}
			if ((int)(originalMessage.InitializedMembers & BrokeredMessage.MessageMembers.ForcePersistence) != 0)
			{
				this.ForcePersistence = originalMessage.ForcePersistence;
			}
			foreach (KeyValuePair<string, object> property in originalMessage.Properties)
			{
				this.InternalProperties.Add(property);
			}
		}

		internal BrokeredMessage CreateCopy()
		{
			this.ThrowIfDisposed();
			return new BrokeredMessage(this, false);
		}

		internal static BrokeredMessage CreateEmptyMessage()
		{
			return new BrokeredMessage(null);
		}

		internal static BrokeredMessage CreateMessage(BrokeredMessage originalMessage)
		{
			BrokeredMessage minValue = originalMessage.CreateCopy();
			minValue.DeliveryCount = 0;
			minValue.LockedUntilUtc = DateTime.MinValue;
			minValue.LockToken = Guid.Empty;
			minValue.EnqueuedTimeUtc = DateTime.MinValue;
			minValue.InternalBrokeredMessageState = BrokeredMessageState.Active;
			minValue.SubqueueType = Microsoft.ServiceBus.Messaging.SubqueueType.Active;
			minValue.IsActivatingScheduledMessage = false;
			BrokeredMessage initializedMembers = minValue;
			initializedMembers.InitializedMembers = initializedMembers.InitializedMembers & (BrokeredMessage.MessageMembers.MessageId | BrokeredMessage.MessageMembers.CorrelationId | BrokeredMessage.MessageMembers.To | BrokeredMessage.MessageMembers.ReplyTo | BrokeredMessage.MessageMembers.TimeToLive | BrokeredMessage.MessageMembers.SessionId | BrokeredMessage.MessageMembers.Label | BrokeredMessage.MessageMembers.ContentType | BrokeredMessage.MessageMembers.ScheduledEnqueueTimeUtc | BrokeredMessage.MessageMembers.PartitionKey | BrokeredMessage.MessageMembers.ReplyToSessionId | BrokeredMessage.MessageMembers.ViaPartitionKey | BrokeredMessage.MessageMembers.Destination | BrokeredMessage.MessageMembers.ForcePersistence | BrokeredMessage.MessageMembers.Publisher | BrokeredMessage.MessageMembers.SequenceNumber | BrokeredMessage.MessageMembers.MessageState | BrokeredMessage.MessageMembers.EnqueuedSequenceNumber | BrokeredMessage.MessageMembers.PartitionId | BrokeredMessage.MessageMembers.TransferSessionId | BrokeredMessage.MessageMembers.PrefilteredHeaders | BrokeredMessage.MessageMembers.PrefilteredProperties | BrokeredMessage.MessageMembers.TransferDestination | BrokeredMessage.MessageMembers.TransferSource | BrokeredMessage.MessageMembers.TransferSequenceNumber | BrokeredMessage.MessageMembers.TransferHopCount | BrokeredMessage.MessageMembers.TransferDestinationEntityId);
			return minValue;
		}

		public void DeadLetter(string deadLetterReason, string deadLetterErrorDescription)
		{
			this.ThrowIfReceiveContextIsNull();
			this.EndDeadLetter(this.BeginDeadLetter(null, deadLetterReason, deadLetterErrorDescription, this.ReceiveContext.OperationTimeout, null, null));
		}

		public void DeadLetter(IDictionary<string, object> propertiesToModify)
		{
			this.ThrowIfReceiveContextIsNull();
			this.EndDeadLetter(this.BeginDeadLetter(propertiesToModify, null, null, this.ReceiveContext.OperationTimeout, null, null));
		}

		public void DeadLetter()
		{
			this.ThrowIfReceiveContextIsNull();
			this.EndDeadLetter(this.BeginDeadLetter(null, null, null, this.ReceiveContext.OperationTimeout, null, null));
		}

		public Task DeadLetterAsync()
		{
			return TaskHelpers.CreateTask(new Func<AsyncCallback, object, IAsyncResult>(this.BeginDeadLetter), new Action<IAsyncResult>(this.EndDeadLetter));
		}

		public Task DeadLetterAsync(string deadLetterReason, string deadLetterErrorDescription)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeadLetter(deadLetterReason, deadLetterErrorDescription, c, s), new Action<IAsyncResult>(this.EndDeadLetter));
		}

		public Task DeadLetterAsync(IDictionary<string, object> propertiesToModify)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDeadLetter(propertiesToModify, c, s), new Action<IAsyncResult>(this.EndDeadLetter));
		}

		public void Defer()
		{
			this.ThrowIfReceiveContextIsNull();
			this.EndDefer(this.BeginDefer(null, this.ReceiveContext.OperationTimeout, null, null));
		}

		public void Defer(IDictionary<string, object> propertiesToModify)
		{
			this.ThrowIfReceiveContextIsNull();
			this.EndDefer(this.BeginDefer(propertiesToModify, this.ReceiveContext.OperationTimeout, null, null));
		}

		public Task DeferAsync()
		{
			return TaskHelpers.CreateTask(new Func<AsyncCallback, object, IAsyncResult>(this.BeginDefer), new Action<IAsyncResult>(this.EndDefer));
		}

		public Task DeferAsync(IDictionary<string, object> propertiesToModify)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginDefer(propertiesToModify, c, s), new Action<IAsyncResult>(this.EndDefer));
		}

		private static long DeserializeBodyStream(BrokeredMessage message, XmlReader reader)
		{
			int num;
			long num1;
			byte[] numArray = BrokeredMessage.ReadBytes(reader, 9);
			long num2 = BitConverter.ToInt64(numArray, 1);
			if (num2 == (long)0)
			{
				return num2;
			}
			InternalBufferManager bufferManager = ThrottledBufferManager.GetBufferManager();
			using (BufferedOutputStream bufferedOutputStream = new BufferedOutputStream(1024, 2147483647, bufferManager))
			{
				byte[] numArray1 = bufferManager.TakeBuffer(1024);
				long num3 = (long)0;
				try
				{
					while (true)
					{
						int num4 = reader.ReadContentAsBase64(numArray1, 0, (int)numArray1.Length);
						if (num4 == 0)
						{
							break;
						}
						num3 = num3 + (long)num4;
						bufferedOutputStream.Write(numArray1, 0, num4);
					}
				}
				finally
				{
					bufferManager.ReturnBuffer(numArray1);
				}
				byte[] array = bufferedOutputStream.ToArray(out num);
				message.BodyStream = new BufferedInputStream(array, num, bufferManager);
				message.ownsBodyStream = true;
				if (num2 > (long)0 && num3 != num2)
				{
					throw Fx.Exception.AsError(new InvalidOperationException(SRClient.FailedToDeSerializeEntireBodyStream), null);
				}
				num1 = num3;
			}
			return num1;
		}

		internal static long DeserializeHeadersFromBinary(BrokeredMessage message, XmlReader reader)
		{
			long length = (long)2;
			int num = BitConverter.ToInt16(BrokeredMessage.ReadBytes(reader, 2), 0);
			for (int i = 0; i < num; i++)
			{
				byte[] numArray = BrokeredMessage.ReadBytes(reader, 3);
				int num1 = BitConverter.ToInt16(numArray, 1);
				byte[] numArray1 = BrokeredMessage.ReadBytes(reader, num1);
				if (message != null)
				{
					message.SetMessageHeader(numArray[0], numArray1, message.version > BrokeredMessage.MessageVersion);
				}
				length = length + (long)((int)numArray.Length + (int)numArray1.Length);
			}
			return length;
		}

		internal static long DeserializePropertiesFromBinary(BrokeredMessage message, XmlReader reader)
		{
			long num = (long)4;
			byte[] numArray = BrokeredMessage.ReadBytes(reader, 3);
			short num1 = BitConverter.ToInt16(numArray, 1);
			if (num1 > 0)
			{
				bool flag = message.version > BrokeredMessage.MessageVersion;
				for (short i = 0; i < num1; i = (short)(i + 1))
				{
					byte[] numArray1 = BrokeredMessage.ReadBytes(reader, 2);
					int num2 = BitConverter.ToInt16(numArray1, 0);
					byte[] numArray2 = BrokeredMessage.ReadBytes(reader, num2);
					string str = Encoding.UTF8.GetString(numArray2);
					int num3 = BrokeredMessage.ReadBytes(reader, 1)[0];
					num = num + (long)(2 + num2 + 1);
					object nativeValue = null;
					int num4 = 0;
					if (num3 != 0)
					{
						numArray1 = BrokeredMessage.ReadBytes(reader, 2);
						num4 = BitConverter.ToInt16(numArray1, 0);
						byte[] numArray3 = BrokeredMessage.ReadBytes(reader, num4);
						num = num + (long)(num4 + 2);
						if (num3 < 21 || !flag)
						{
							nativeValue = SerializationUtilities.ConvertByteArrayToNativeValue(message.version, (PropertyValueType)num3, numArray3);
						}
					}
					if (num3 == 0 || nativeValue != null)
					{
						message.InternalProperties.Add(str, nativeValue);
					}
				}
			}
			return num;
		}

		private static long DeserializeVersionAndFlagsFromBinary(BrokeredMessage message, XmlReader reader)
		{
			int num = BitConverter.ToInt32(BrokeredMessage.ReadBytes(reader, 4), 0);
			byte[] numArray = BrokeredMessage.ReadBytes(reader, 2);
			if (message != null)
			{
				message.version = num;
				message.messageFormat = (BrokeredMessageFormat)numArray[0];
			}
			return (long)6;
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.PrefilteredHeaders != null)
					{
						this.PrefilteredHeaders.Dispose();
						this.PrefilteredHeaders = null;
					}
					if (this.PrefilteredProperties != null)
					{
						this.PrefilteredProperties.Dispose();
						this.PrefilteredProperties = null;
					}
					if (this.headerStream != null)
					{
						this.headerStream.Dispose();
						this.headerStream = null;
					}
					if (this.rawHeaderStream != null && this.ownsRawHeaderStream)
					{
						this.rawHeaderStream.Dispose();
						this.rawHeaderStream = null;
					}
					if (this.BodyStream != null && this.ownsBodyStream)
					{
						this.BodyStream.Dispose();
						this.BodyStream = null;
					}
					this.bodyObject = null;
					if (this.attachedDisposables != null)
					{
						foreach (IDisposable attachedDisposable in this.attachedDisposables)
						{
							attachedDisposable.Dispose();
						}
					}
				}
				this.disposed = true;
			}
		}

		internal void DownGradeToVersion1()
		{
			this.version = BrokeredMessage.MessageVersion1;
			this.initializedMembers = this.initializedMembers & (BrokeredMessage.MessageMembers.MessageId | BrokeredMessage.MessageMembers.CorrelationId | BrokeredMessage.MessageMembers.To | BrokeredMessage.MessageMembers.ReplyTo | BrokeredMessage.MessageMembers.TimeToLive | BrokeredMessage.MessageMembers.SessionId | BrokeredMessage.MessageMembers.Label | BrokeredMessage.MessageMembers.ContentType | BrokeredMessage.MessageMembers.ScheduledEnqueueTimeUtc | BrokeredMessage.MessageMembers.PartitionKey | BrokeredMessage.MessageMembers.ReplyToSessionId | BrokeredMessage.MessageMembers.EnqueuedTimeUtc | BrokeredMessage.MessageMembers.SequenceNumber | BrokeredMessage.MessageMembers.LockToken | BrokeredMessage.MessageMembers.LockedUntilUtc | BrokeredMessage.MessageMembers.DeliveryCount);
			List<string> strs = new List<string>(8);
			foreach (KeyValuePair<string, object> property in this.Properties)
			{
				if (!(property.Value is Stream))
				{
					continue;
				}
				strs.Add(property.Key);
			}
			foreach (string str in strs)
			{
				this.Properties.Remove(str);
			}
		}

		public void EndAbandon(IAsyncResult result)
		{
			this.ReceiveContext.EndAbandon(result);
		}

		public void EndComplete(IAsyncResult result)
		{
			this.ReceiveContext.EndComplete(result);
		}

		public void EndDeadLetter(IAsyncResult result)
		{
			this.ReceiveContext.EndDeadLetter(result);
		}

		public void EndDefer(IAsyncResult result)
		{
			this.ReceiveContext.EndDefer(result);
		}

		public void EndRenewLock(IAsyncResult result)
		{
			IEnumerable<DateTime> dateTimes = this.ReceiveContext.EndRenewLock(result);
			if (dateTimes != null && dateTimes.Any<DateTime>())
			{
				this.LockedUntilUtc = dateTimes.First<DateTime>();
			}
		}

		internal void EnsureHeaderDeserialized()
		{
			if (!this.headersDeserialized)
			{
				lock (this.headerSerializationSyncObject)
				{
					if (!this.headersDeserialized)
					{
						this.ThrowIfDisposed();
						using (BufferedInputStream bufferedInputStream = (BufferedInputStream)this.headerStream.Clone())
						{
							using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(bufferedInputStream, XmlDictionaryReaderQuotas.Max))
							{
								xmlDictionaryReader.Read();
								xmlDictionaryReader.ReadStartElement();
								this.ReadHeader(xmlDictionaryReader, SerializationTarget.Storing);
								xmlDictionaryReader.ReadEndElement();
							}
						}
					}
					this.headersDeserialized = true;
				}
			}
		}

		private void EnsureReceiverHeaders()
		{
			if (this.receiverHeaders == null)
			{
				this.receiverHeaders = new BrokeredMessage.ReceiverHeaders();
			}
		}

		public T GetBody<T>()
		{
			if (typeof(T) == typeof(Stream))
			{
				this.SetGetBodyCalled();
				return (T)this.BodyStream;
			}
			if (this.bodyObjectDecoded && this.bodyObject != null)
			{
				this.SetGetBodyCalled();
				return (T)this.bodyObject;
			}
			return this.GetBody<T>(new DataContractBinarySerializer(typeof(T)));
		}

		public T GetBody<T>(XmlObjectSerializer serializer)
		{
			if (serializer == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new ArgumentNullException("serializer"), null);
			}
			this.ThrowIfDisposed();
			this.SetGetBodyCalled();
			if (this.BodyStream == null)
			{
				if (typeof(T).IsValueType)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(SRClient.MessageBodyNull), null);
				}
				return default(T);
			}
			if (this.BodyStream.CanSeek)
			{
				if (this.BodyStream.Length == (long)0)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(SRClient.MessageBodyNull), null);
				}
				this.BodyStream.Position = (long)0;
			}
			return (T)serializer.ReadObject(this.BodyStream);
		}

		private short GetHeaderCount()
		{
			int num = (int)this.initializedMembers;
			short num1 = 0;
			while (num != 0)
			{
				num1 = (short)(num1 + 1);
				num = num & num - 1;
			}
			return num1;
		}

		internal BufferedInputStream GetHeaderStream(InternalBufferManager bufferManager, SerializationTarget serializationTarget)
		{
			int num;
			if (this.headerStream == null)
			{
				lock (this.headerSerializationSyncObject)
				{
					if (this.headerStream == null)
					{
						try
						{
							using (BufferedOutputStream bufferedOutputStream = new BufferedOutputStream(this.headerStreamInitialSize, this.HeaderStreamMaxSize, bufferManager))
							{
								using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(bufferedOutputStream))
								{
									xmlDictionaryWriter.WriteStartElement("MessageHeaders");
									this.WriteHeader(xmlDictionaryWriter, serializationTarget);
									xmlDictionaryWriter.WriteEndElement();
								}
								byte[] array = bufferedOutputStream.ToArray(out num);
								this.headerStream = new BufferedInputStream(array, num, bufferManager);
								this.headerSize = this.headerStream.Length;
							}
						}
						catch (InvalidOperationException invalidOperationException1)
						{
							InvalidOperationException invalidOperationException = invalidOperationException1;
							throw Fx.Exception.AsError(new SerializationException(invalidOperationException.Message, invalidOperationException), null);
						}
					}
				}
			}
			return this.headerStream;
		}

		private static Type GetObjectType(object value)
		{
			if (value != null)
			{
				return value.GetType();
			}
			return typeof(object);
		}

		internal long GetSerializedSize(SerializationTarget serializationTarget)
		{
			if (this.headerSize == (long)0)
			{
				lock (this.headerSerializationSyncObject)
				{
					if (this.headerSize == (long)0)
					{
						this.headerSize = BrokeredMessage.CalculateSerializedHeadersSize(this, serializationTarget) + BrokeredMessage.CalculateSerializedPropertiesSize(this);
					}
				}
			}
			return this.headerSize + this.BodySize;
		}

		internal object GetSystemProperty(string propertyName)
		{
			return BrokeredMessage.SystemPropertyAccessorDictionary[propertyName](this);
		}

		internal bool HasHeader(BrokeredMessage.MessageMembers headerMember)
		{
			return (int)(this.initializedMembers & headerMember) != 0;
		}

		internal bool IsMembersSet(BrokeredMessage.MessageMembers members)
		{
			return (int)(this.InitializedMembers & members) != 0;
		}

		private static byte[] ReadBytes(XmlReader reader, int bytesToRead)
		{
			return SerializationUtilities.ReadBytes(reader, bytesToRead);
		}

		internal long ReadHeader(XmlReader reader, SerializationTarget serializationTarget)
		{
			long num = (long)0;
			num = num + BrokeredMessage.DeserializeVersionAndFlagsFromBinary(this, reader);
			this.messageEncoder = this.messageEncoder ?? BrokeredMessageEncoder.GetEncoder(this.messageFormat);
			num = num + this.messageEncoder.ReadHeader(reader, this, serializationTarget);
			return num;
		}

		public void RenewLock()
		{
			this.ThrowIfReceiveContextIsNull(SRClient.InvalidMethodWhilePeeking("RenewLock"));
			this.EndRenewLock(this.BeginRenewLock(this.ReceiveContext.OperationTimeout, null, null));
		}

		public Task RenewLockAsync()
		{
			return TaskHelpers.CreateTask(new Func<AsyncCallback, object, IAsyncResult>(this.BeginRenewLock), new Action<IAsyncResult>(this.EndRenewLock));
		}

		internal void ResetPropertiesAsModifiedByBroker()
		{
			this.arePropertiesModifiedByBroker = false;
		}

		internal static long SerializeBodyStream(BrokeredMessage message, XmlWriter writer)
		{
			writer.WriteBase64(new byte[] { 31 }, 0, 1);
			long num = (long)0;
			if (message.BodyStream != null)
			{
				num = (message.BodyStream.CanSeek ? message.BodyStream.Length : (long)-1);
			}
			writer.WriteBase64(BitConverter.GetBytes(num), 0, 8);
			if (message.BodyStream == null)
			{
				return (long)0;
			}
			if (message.BodyStream.CanSeek && message.BodyStream.Position != (long)0)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.CannotSerializeMessageWithPartiallyConsumedBodyStream), null);
			}
			byte[] numArray = new byte[1024];
			long num1 = (long)0;
			while (true)
			{
				int num2 = message.BodyStream.Read(numArray, 0, (int)numArray.Length);
				if (num2 == 0)
				{
					break;
				}
				num1 = num1 + (long)num2;
				writer.WriteBase64(numArray, 0, num2);
			}
			if (num > (long)0 && num1 != num)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.FailedToSerializeEntireBodyStream), null);
			}
			return num1;
		}

		internal static long SerializeHeadersAsBinary(BrokeredMessage message, XmlWriter writer, SerializationTarget serializationTarget)
		{
			long length = (long)2;
			writer.WriteBase64(BitConverter.GetBytes(message.GetHeaderCount()), 0, 2);
			byte[] fieldId = new byte[3];
			BrokeredMessage.BinarySerializationItem[] binarySerializationItemArray = BrokeredMessage.binarySerializationItems;
			for (int i = 0; i < (int)binarySerializationItemArray.Length; i++)
			{
				BrokeredMessage.BinarySerializationItem binarySerializationItem = binarySerializationItemArray[i];
				if (binarySerializationItem.ShouldSerialize(message, serializationTarget))
				{
					fieldId[0] = (byte)binarySerializationItem.FieldId;
					byte[] extractor = binarySerializationItem.Extractor(message, serializationTarget);
					if ((int)extractor.Length > Constants.MaximumMessageHeaderPropertySize)
					{
						throw Fx.Exception.AsError(new SerializationException(SRClient.ExceededMessagePropertySizeLimit(binarySerializationItem.FieldId.ToString(), Constants.MaximumMessageHeaderPropertySize)), null);
					}
					fieldId[1] = (byte)((int)extractor.Length & 255);
					fieldId[2] = (byte)(((int)extractor.Length & 65280) >> 8);
					length = length + (long)((int)extractor.Length + 3);
					writer.WriteBase64(fieldId, 0, 3);
					writer.WriteBase64(extractor, 0, (int)extractor.Length);
				}
			}
			writer.Flush();
			return length;
		}

		internal static long SerializePropertiesAsBinary(BrokeredMessage message, XmlWriter writer)
		{
			long length = (long)1;
			writer.WriteBase64(new byte[] { 30 }, 0, 1);
			if (message.Properties.Count > 32767)
			{
				throw Fx.Exception.AsError(new SerializationException(SRClient.TooManyMessageProperties((short)32767, message.Properties.Count)), null);
			}
			length = length + (long)2;
			writer.WriteBase64(BitConverter.GetBytes((short)message.Properties.Count), 0, 2);
			byte[] numArray = new byte[3];
			if (message.Properties.Count > 0)
			{
				foreach (KeyValuePair<string, object> property in message.Properties)
				{
					byte[] bytes = Encoding.UTF8.GetBytes(property.Key);
					if ((int)bytes.Length > 32767)
					{
						throw Fx.Exception.AsError(new SerializationException(SRClient.ExceededMessagePropertySizeLimit(property.Key, (short)32767)), null);
					}
					length = length + (long)((int)bytes.Length + 2);
					writer.WriteBase64(BitConverter.GetBytes((short)((int)bytes.Length)), 0, 2);
					writer.WriteBase64(bytes, 0, (int)bytes.Length);
					PropertyValueType typeId = SerializationUtilities.GetTypeId(property.Value);
					numArray[0] = (byte)typeId;
					if (typeId == PropertyValueType.Null)
					{
						length = length + (long)1;
						writer.WriteBase64(numArray, 0, 1);
					}
					else
					{
						byte[] byteArray = SerializationUtilities.ConvertNativeValueToByteArray(message.version, typeId, property.Value);
						if ((int)byteArray.Length > 32767)
						{
							throw Fx.Exception.AsError(new SerializationException(SRClient.ExceededMessagePropertySizeLimit(property.Key, (short)32767)), null);
						}
						numArray[1] = (byte)((int)byteArray.Length & 255);
						numArray[2] = (byte)(((int)byteArray.Length & 65280) >> 8);
						length = length + (long)((int)byteArray.Length + 3);
						writer.WriteBase64(numArray, 0, 3);
						writer.WriteBase64(byteArray, 0, (int)byteArray.Length);
					}
				}
				writer.Flush();
			}
			return length;
		}

		private static long SerializeVersionAndFlagsAsBinary(BrokeredMessage message, XmlWriter writer)
		{
			long num = (long)6;
			writer.WriteBase64(BitConverter.GetBytes(message.version), 0, 4);
			byte[] numArray = new byte[] { message.messageFormat, 0 };
			writer.WriteBase64(numArray, 0, 2);
			return num;
		}

		internal static void SetBrokerMode()
		{
			BrokeredMessage.mode = BrokeredMessage.BrokeredMessageMode.Broker;
			BrokeredMessage.binarySerializationItems = BrokeredMessage.BuildBinarySerializationItems(BrokeredMessage.mode);
		}

		private void SetGetBodyCalled()
		{
			if (1 == Interlocked.Exchange(ref this.getBodyCalled, 1))
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.MessageBodyConsumed), null);
			}
		}

		internal void SetHeaderStreamSize(SerializationTarget serializationTarget)
		{
			if (this.headerStream == null)
			{
				lock (this.headerSerializationSyncObject)
				{
					if (this.headerStream == null)
					{
						using (MemoryStream memoryStream = new MemoryStream(this.headerStreamInitialSize))
						{
							using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(memoryStream, null, null, false))
							{
								xmlDictionaryWriter.WriteStartElement("MessageHeaders");
								this.WriteHeader(xmlDictionaryWriter, serializationTarget);
								xmlDictionaryWriter.WriteEndElement();
							}
							this.headerSize = memoryStream.Length;
						}
					}
				}
			}
		}

		private void SetMessageHeader(byte memberId, byte[] value, bool ignoreUnknown)
		{
			switch (memberId)
			{
				case 1:
				{
					this.MessageId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 2:
				{
					this.CorrelationId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 3:
				{
					this.To = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 4:
				{
					this.ReplyTo = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 5:
				{
					this.TimeToLive = (TimeSpan)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.TimeSpan, value);
					return;
				}
				case 6:
				{
					this.SessionId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 7:
				{
					this.Label = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 8:
				{
					this.ContentType = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 9:
				{
					this.ScheduledEnqueueTimeUtc = (DateTime)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.DateTime, value);
					return;
				}
				case 10:
				{
					this.PartitionKey = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 11:
				{
					this.ReplyToSessionId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 12:
				{
					this.ViaPartitionKey = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 13:
				{
					this.Destination = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 14:
				{
					this.ForcePersistence = (bool)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Boolean, value);
					return;
				}
				case 15:
				case 16:
				case 17:
				case 18:
				case 27:
				case 28:
				case 29:
				case 30:
				case 31:
				case 32:
				case 33:
				case 34:
				case 35:
				case 36:
				case 37:
				case 38:
				case 39:
				{
					if (!ignoreUnknown)
					{
						throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("memberId", memberId, string.Empty);
					}
					return;
				}
				case 19:
				{
					this.Publisher = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 20:
				{
					this.EnqueuedTimeUtc = (DateTime)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.DateTime, value);
					return;
				}
				case 21:
				{
					this.SequenceNumber = (long)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int64, value);
					return;
				}
				case 22:
				{
					this.LockToken = (Guid)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Guid, value);
					return;
				}
				case 23:
				{
					this.LockedUntilUtc = (DateTime)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.DateTime, value);
					return;
				}
				case 24:
				{
					this.DeliveryCount = (int)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int32, value);
					return;
				}
				case 25:
				{
					this.State = (MessageState)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int32, value);
					return;
				}
				case 26:
				{
					this.EnqueuedSequenceNumber = (long)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int64, value);
					return;
				}
				case 40:
				{
					this.PrefilteredHeaders = (Stream)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Stream, value);
					return;
				}
				case 41:
				{
					this.PrefilteredProperties = (Stream)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Stream, value);
					return;
				}
				case 42:
				{
					this.TransferDestination = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 43:
				{
					this.TransferSource = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 44:
				{
					this.TransferSequenceNumber = (long)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int64, value);
					return;
				}
				case 45:
				{
					this.TransferHopCount = (int)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int32, value);
					return;
				}
				case 46:
				{
					this.TransferDestinationResourceId = (long)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int64, value);
					return;
				}
				case 47:
				{
					this.TransferSessionId = (string)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.String, value);
					return;
				}
				case 48:
				{
					this.PartitionId = (short)SerializationUtilities.ConvertByteArrayToNativeValue(this.version, PropertyValueType.Int16, value);
					return;
				}
				default:
				{
					if (!ignoreUnknown)
					{
						throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentOutOfRange("memberId", memberId, string.Empty);
					}
					return;
				}
			}
		}

		internal void SetPropertiesAsModifiedByBroker()
		{
			this.arePropertiesModifiedByBroker = true;
		}

		XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
		{
			return null;
		}

		void System.Xml.Serialization.IXmlSerializable.ReadXml(XmlReader reader)
		{
			long num = (long)0;
			reader.Read();
			reader.ReadStartElement();
			num = num + this.ReadHeader(reader, SerializationTarget.Communication);
			this.bodySize = BrokeredMessage.DeserializeBodyStream(this, reader);
			reader.ReadEndElement();
			this.headerSize = num;
		}

		void System.Xml.Serialization.IXmlSerializable.WriteXml(XmlWriter writer)
		{
			long num = (long)8;
			writer.WriteStartElement("Message");
			num = num + this.WriteHeader(writer, SerializationTarget.Communication);
			this.bodySize = BrokeredMessage.SerializeBodyStream(this, writer);
			writer.WriteEndElement();
			this.headerSize = num;
		}

		private void ThrowIfDisposed()
		{
			if (this.disposed)
			{
				throw Fx.Exception.ObjectDisposed("BrokeredMessage has been disposed.");
			}
		}

		private void ThrowIfDominatingPropertyIsNotEqualToNonNullDormantProperty(BrokeredMessage.MessageMembers dominatingProperty, BrokeredMessage.MessageMembers dormantProperty, string dominatingPropsValue, string dormantPropsValue)
		{
			if ((int)(this.initializedMembers & dormantProperty) != 0 && !string.Equals(dominatingPropsValue, dormantPropsValue))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(SRClient.DominatingPropertyMustBeEqualsToNonNullDormantProperty(dominatingProperty, dormantProperty)), null);
			}
		}

		private void ThrowIfNotLocked()
		{
			if (this.receiverHeaders == null || this.receiverHeaders.LockToken == Guid.Empty)
			{
				if (this.ReceiveContext == null || this.ReceiveContext.MessageReceiver == null || this.ReceiveContext.MessageReceiver.Mode != ReceiveMode.ReceiveAndDelete)
				{
					throw Fx.Exception.AsError(new InvalidOperationException(), null);
				}
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(SRClient.PeekLockModeRequired), null);
			}
		}

		private void ThrowIfNotReceived()
		{
			if (this.receiverHeaders == null)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(), null);
			}
		}

		private void ThrowIfReceiveContextIsNull()
		{
			this.ThrowIfReceiveContextIsNull(SRClient.ReceiveContextNull);
		}

		private void ThrowIfReceiveContextIsNull(string message)
		{
			if (this.receiveContext == null)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(message), null);
			}
		}

		public override string ToString()
		{
			CultureInfo currentCulture = CultureInfo.CurrentCulture;
			object[] str = new object[] { this.ToString(), this.MessageId.ToString(CultureInfo.CurrentCulture) };
			return string.Format(currentCulture, "{0}{{MessageId:{1}}}", str);
		}

		private static void ValidateDestination(string destination)
		{
			if (destination != null && destination.Length > 128)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("Destination", SRClient.PropertyOverMaxValue("Destination", 128));
			}
		}

		private static void ValidateMessageId(string messageId)
		{
			if (string.IsNullOrEmpty(messageId) || messageId.Length > 128)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("messageId", SRClient.MessageIdIsNullOrEmptyOrOverMaxValue(128));
			}
		}

		private static void ValidatePartitionKey(string partitionKeyPropertyName, string partitionKey)
		{
			if (partitionKey != null && partitionKey.Length > 128)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument(partitionKeyPropertyName, SRClient.PropertyOverMaxValue(partitionKeyPropertyName, 128));
			}
		}

		private static void ValidateSessionId(string sessionId)
		{
			if (sessionId != null && sessionId.Length > 128)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("sessionId", SRClient.SessionIdIsOverMaxValue(128));
			}
		}

		internal long WriteHeader(XmlWriter writer, SerializationTarget serializationTarget)
		{
			long num = (long)0;
			num = num + BrokeredMessage.SerializeVersionAndFlagsAsBinary(this, writer);
			this.messageEncoder = this.messageEncoder ?? BrokeredMessageEncoder.GetEncoder(this.messageFormat);
			num = num + this.messageEncoder.WriteHeader(writer, this, serializationTarget);
			return num;
		}

		private sealed class BinarySerializationItem
		{
			public Func<BrokeredMessage, int> CalculateSize
			{
				get;
				set;
			}

			public Func<BrokeredMessage, SerializationTarget, byte[]> Extractor
			{
				get;
				set;
			}

			public BrokeredMessage.FieldId FieldId
			{
				get;
				set;
			}

			public Func<BrokeredMessage, SerializationTarget, bool> ShouldSerialize
			{
				get;
				set;
			}

			public BinarySerializationItem()
			{
			}
		}

		private enum BrokeredMessageMode
		{
			Client,
			Broker
		}

		private enum FieldId : byte
		{
			MessageId = 1,
			CorrelationId = 2,
			To = 3,
			ReplyTo = 4,
			TimeToLive = 5,
			SessionId = 6,
			Label = 7,
			ContentType = 8,
			ScheduledEnqueueTimeUtc = 9,
			PartitionKey = 10,
			ReplyToSessionId = 11,
			ViaPartitionKey = 12,
			Destination = 13,
			ForcePersistence = 14,
			Publisher = 19,
			EnqueuedTimeUtc = 20,
			SequenceNumber = 21,
			LockToken = 22,
			LockedUntilUtc = 23,
			DeliveryCount = 24,
			MessageState = 25,
			EnqueuedSequenceNumber = 26,
			Properties = 30,
			BodyStream = 31,
			PrefilteredMessageHeaders = 40,
			PrefilteredMessageProperties = 41,
			TransferDestination = 42,
			TransferSource = 43,
			TransferSequenceNumber = 44,
			TransferHopCount = 45,
			TransferDestinationResourceId = 46,
			TransferSessionId = 47,
			PartitionId = 48
		}

		[Flags]
		internal enum MessageMembers
		{
			TransferDestinationEntityId = -2147483648,
			MessageId = 1,
			CorrelationId = 2,
			To = 4,
			ReplyTo = 8,
			TimeToLive = 16,
			SessionId = 32,
			Label = 64,
			ContentType = 128,
			ScheduledEnqueueTimeUtc = 256,
			PartitionKey = 512,
			ReplyToSessionId = 1024,
			ViaPartitionKey = 2048,
			Destination = 4096,
			ForcePersistence = 8192,
			Publisher = 32768,
			EnqueuedTimeUtc = 65536,
			SequenceNumber = 131072,
			LockToken = 262144,
			LockedUntilUtc = 524288,
			DeliveryCount = 1048576,
			MessageState = 2097152,
			EnqueuedSequenceNumber = 4194304,
			PartitionId = 8388608,
			TransferSessionId = 16777216,
			PrefilteredHeaders = 33554432,
			PrefilteredProperties = 67108864,
			TransferDestination = 134217728,
			TransferSource = 268435456,
			TransferSequenceNumber = 536870912,
			TransferHopCount = 1073741824
		}

		private sealed class ReceiverHeaders
		{
			public int DeliveryCount
			{
				get;
				set;
			}

			public long EnqueuedSequenceNumber
			{
				get;
				set;
			}

			public DateTime LockedUntilUtc
			{
				get;
				set;
			}

			public Guid LockToken
			{
				get;
				set;
			}

			public long SequenceNumber
			{
				get;
				set;
			}

			public ReceiverHeaders()
			{
			}
		}
	}
}