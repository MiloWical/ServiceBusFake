using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class DeliveryAnnotations : DescribedAnnotations
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static DeliveryAnnotations()
		{
			DeliveryAnnotations.Name = "amqp:delivery-annotations:map";
			DeliveryAnnotations.Code = (ulong)113;
		}

		public DeliveryAnnotations() : base(DeliveryAnnotations.Name, DeliveryAnnotations.Code)
		{
		}
	}
}