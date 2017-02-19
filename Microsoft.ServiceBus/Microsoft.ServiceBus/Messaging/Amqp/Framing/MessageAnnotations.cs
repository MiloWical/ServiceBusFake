using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class MessageAnnotations : DescribedAnnotations
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static MessageAnnotations()
		{
			MessageAnnotations.Name = "amqp:message-annotations:map";
			MessageAnnotations.Code = (ulong)114;
		}

		public MessageAnnotations() : base(MessageAnnotations.Name, MessageAnnotations.Code)
		{
		}
	}
}