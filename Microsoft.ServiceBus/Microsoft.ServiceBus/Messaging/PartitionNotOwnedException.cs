using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class PartitionNotOwnedException : MessagingException
	{
		public PartitionNotOwnedException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public PartitionNotOwnedException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private PartitionNotOwnedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}