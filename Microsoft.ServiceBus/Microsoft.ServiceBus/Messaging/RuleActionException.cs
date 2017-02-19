using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class RuleActionException : MessagingException
	{
		public RuleActionException(string message) : base(message)
		{
			base.IsTransient = false;
		}

		public RuleActionException(string message, Exception innerException) : base(message, innerException)
		{
			base.IsTransient = false;
		}

		private RuleActionException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			base.IsTransient = false;
		}
	}
}