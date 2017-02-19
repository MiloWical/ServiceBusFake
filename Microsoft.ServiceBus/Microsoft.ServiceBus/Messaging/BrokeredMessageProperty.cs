using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class BrokeredMessageProperty : IMessageProperty
	{
		public readonly static string Name;

		private BrokeredMessage brokeredMessage;

		public string ContentType
		{
			get
			{
				return this.brokeredMessage.ContentType;
			}
			set
			{
				this.brokeredMessage.ContentType = value;
			}
		}

		public string CorrelationId
		{
			get
			{
				return this.brokeredMessage.CorrelationId;
			}
			set
			{
				this.brokeredMessage.CorrelationId = value;
			}
		}

		public int DeliveryCount
		{
			get
			{
				return this.brokeredMessage.DeliveryCount;
			}
		}

		internal string Destination
		{
			get
			{
				return this.brokeredMessage.Destination;
			}
			set
			{
				this.brokeredMessage.Destination = value;
			}
		}

		public DateTime EnqueuedTimeUtc
		{
			get
			{
				return this.brokeredMessage.EnqueuedTimeUtc;
			}
		}

		public DateTime ExpiresAtUtc
		{
			get
			{
				return this.brokeredMessage.ExpiresAtUtc;
			}
		}

		public bool ForcePersistence
		{
			get
			{
				return this.brokeredMessage.ForcePersistence;
			}
			set
			{
				this.brokeredMessage.ForcePersistence = value;
			}
		}

		public string Label
		{
			get
			{
				return this.brokeredMessage.Label;
			}
			set
			{
				this.brokeredMessage.Label = value;
			}
		}

		public DateTime LockedUntilUtc
		{
			get
			{
				return this.brokeredMessage.LockedUntilUtc;
			}
		}

		public Guid LockToken
		{
			get
			{
				return this.brokeredMessage.LockToken;
			}
		}

		public BrokeredMessage Message
		{
			get
			{
				return this.brokeredMessage;
			}
		}

		public string MessageId
		{
			get
			{
				return this.brokeredMessage.MessageId;
			}
			set
			{
				this.brokeredMessage.MessageId = value;
			}
		}

		public string PartitionKey
		{
			get
			{
				return this.brokeredMessage.PartitionKey;
			}
			set
			{
				this.brokeredMessage.PartitionKey = value;
			}
		}

		public IDictionary<string, object> Properties
		{
			get
			{
				return this.brokeredMessage.Properties;
			}
		}

		internal string Publisher
		{
			get
			{
				return this.brokeredMessage.Publisher;
			}
			set
			{
				this.brokeredMessage.Publisher = value;
			}
		}

		public string ReplyTo
		{
			get
			{
				return this.brokeredMessage.ReplyTo;
			}
			set
			{
				this.brokeredMessage.ReplyTo = value;
			}
		}

		public string ReplyToSessionId
		{
			get
			{
				return this.brokeredMessage.ReplyToSessionId;
			}
			set
			{
				this.brokeredMessage.ReplyToSessionId = value;
			}
		}

		public DateTime ScheduledEnqueueTimeUtc
		{
			get
			{
				return this.brokeredMessage.ScheduledEnqueueTimeUtc;
			}
			set
			{
				this.brokeredMessage.ScheduledEnqueueTimeUtc = value;
			}
		}

		public long SequenceNumber
		{
			get
			{
				return this.brokeredMessage.SequenceNumber;
			}
		}

		public string SessionId
		{
			get
			{
				return this.brokeredMessage.SessionId;
			}
			set
			{
				this.brokeredMessage.SessionId = value;
			}
		}

		public TimeSpan TimeToLive
		{
			get
			{
				return this.brokeredMessage.TimeToLive;
			}
			set
			{
				this.brokeredMessage.TimeToLive = value;
			}
		}

		public string To
		{
			get
			{
				return this.brokeredMessage.To;
			}
			set
			{
				this.brokeredMessage.To = value;
			}
		}

		public string ViaPartitionKey
		{
			get
			{
				return this.brokeredMessage.ViaPartitionKey;
			}
			set
			{
				this.brokeredMessage.ViaPartitionKey = value;
			}
		}

		static BrokeredMessageProperty()
		{
			BrokeredMessageProperty.Name = "BrokeredMessageProperty";
		}

		public BrokeredMessageProperty()
		{
			this.brokeredMessage = new BrokeredMessage();
		}

		internal BrokeredMessageProperty(BrokeredMessageProperty other)
		{
			this.brokeredMessage = other.brokeredMessage;
		}

		private BrokeredMessageProperty(BrokeredMessage afmsMessage)
		{
			this.brokeredMessage = afmsMessage;
		}

		internal static void AddPropertyToWcfMessage(BrokeredMessage sourceAfmsMessage, System.ServiceModel.Channels.Message destinationWcfMessage)
		{
			BrokeredMessageProperty brokeredMessageProperty = new BrokeredMessageProperty(sourceAfmsMessage);
			destinationWcfMessage.Properties[BrokeredMessageProperty.Name] = brokeredMessageProperty;
		}

		IMessageProperty System.ServiceModel.Channels.IMessageProperty.CreateCopy()
		{
			return new BrokeredMessageProperty(this);
		}

		internal static bool TryGet(MessageProperties messageProperties, out BrokeredMessageProperty property)
		{
			object obj;
			bool flag = messageProperties.TryGetValue(BrokeredMessageProperty.Name, out obj);
			if (!flag)
			{
				property = null;
			}
			else
			{
				property = (BrokeredMessageProperty)obj;
			}
			return flag;
		}
	}
}