using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[Serializable]
	internal class AddressReplacedException : Exception
	{
		public AddressReplacedException()
		{
		}

		public AddressReplacedException(string message) : base(message)
		{
		}

		public AddressReplacedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected AddressReplacedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}