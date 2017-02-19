using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Footer : DescribedAnnotations
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static Footer()
		{
			Footer.Name = "amqp:footer:map";
			Footer.Code = (ulong)120;
		}

		public Footer() : base(Footer.Name, Footer.Code)
		{
		}

		public override string ToString()
		{
			return "footer";
		}
	}
}