using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Common
{
	[Serializable]
	internal class CallbackException : FatalException
	{
		public CallbackException()
		{
		}

		public CallbackException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected CallbackException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}