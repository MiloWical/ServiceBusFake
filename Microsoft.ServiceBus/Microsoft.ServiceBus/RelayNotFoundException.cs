using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[Serializable]
	public class RelayNotFoundException : Exception
	{
		public RelayNotFoundException()
		{
		}

		public RelayNotFoundException(string message) : base(message)
		{
		}

		public RelayNotFoundException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected RelayNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}