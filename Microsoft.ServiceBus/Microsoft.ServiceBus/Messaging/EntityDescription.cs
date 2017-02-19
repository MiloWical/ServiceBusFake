using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Notifications;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(QueueDescription))]
	[KnownType(typeof(RelayDescription))]
	[KnownType(typeof(RuleDescription))]
	[KnownType(typeof(SubscriptionDescription))]
	[KnownType(typeof(TopicDescription))]
	[KnownType(typeof(NotificationHubDescription))]
	public abstract class EntityDescription : IExtensibleDataObject
	{
		public ExtensionDataObject ExtensionData
		{
			get;
			set;
		}

		public bool IsReadOnly
		{
			get;
			internal set;
		}

		internal virtual bool RequiresEncryption
		{
			get
			{
				return false;
			}
		}

		internal EntityDescription()
		{
		}

		internal virtual bool IsValidForVersion(ApiVersion version)
		{
			return true;
		}

		internal virtual void OverrideEntityAvailabilityStatus(EntityAvailabilityStatus status)
		{
		}

		internal virtual void OverrideEntityStatus(EntityStatus status)
		{
		}

		protected void ThrowIfReadOnly()
		{
			if (this.IsReadOnly)
			{
				throw new InvalidOperationException(SRClient.ObjectIsReadOnly);
			}
		}

		internal virtual void UpdateForVersion(ApiVersion version, EntityDescription existingDescription = null)
		{
		}
	}
}