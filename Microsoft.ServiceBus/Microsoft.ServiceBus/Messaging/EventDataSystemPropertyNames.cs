using System;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class EventDataSystemPropertyNames
	{
		public const string Publisher = "Publisher";

		public const string PartitionKey = "PartitionKey";

		public const string Offset = "Offset";

		public const string SequenceNumber = "SequenceNumber";

		public const string EnqueuedTimeUtc = "EnqueuedTimeUtc";

		public EventDataSystemPropertyNames()
		{
		}
	}
}