using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class DeleteOnNoLinks : LifeTimePolicy
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static DeleteOnNoLinks()
		{
			DeleteOnNoLinks.Name = "amqp:delete-on-no-links:list";
			DeleteOnNoLinks.Code = (ulong)44;
		}

		public DeleteOnNoLinks() : base(DeleteOnNoLinks.Name, DeleteOnNoLinks.Code)
		{
		}

		public override string ToString()
		{
			return "delete-on-no-links()";
		}
	}
}