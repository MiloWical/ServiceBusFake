using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class DeleteOnNoLinksOrMessages : LifeTimePolicy
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static DeleteOnNoLinksOrMessages()
		{
			DeleteOnNoLinksOrMessages.Name = "amqp:delete-on-no-links-or-messages:list";
			DeleteOnNoLinksOrMessages.Code = (ulong)46;
		}

		public DeleteOnNoLinksOrMessages() : base(DeleteOnNoLinksOrMessages.Name, DeleteOnNoLinksOrMessages.Code)
		{
		}

		public override string ToString()
		{
			return "delete-on-no-links-or-messages()";
		}
	}
}