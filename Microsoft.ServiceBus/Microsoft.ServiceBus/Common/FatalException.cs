using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Common
{
	[Serializable]
	internal class FatalException : Exception
	{
		public FatalException()
		{
		}

		public FatalException(string message) : base(message)
		{
		}

		public FatalException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected FatalException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}