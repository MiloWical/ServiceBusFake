using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class FilterException : MessagingException
	{
		public FilterException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public FilterException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private FilterException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}