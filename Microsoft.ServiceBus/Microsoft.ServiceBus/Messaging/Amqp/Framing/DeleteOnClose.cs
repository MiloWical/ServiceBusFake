using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class DeleteOnClose : LifeTimePolicy
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static DeleteOnClose()
		{
			DeleteOnClose.Name = "amqp:delete-on-close:list";
			DeleteOnClose.Code = (ulong)43;
		}

		public DeleteOnClose() : base(DeleteOnClose.Name, DeleteOnClose.Code)
		{
		}

		public override string ToString()
		{
			return "deleted-on-close()";
		}
	}
}