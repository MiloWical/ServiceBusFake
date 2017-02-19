using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class DeleteOnNoMessages : LifeTimePolicy
	{
		public readonly static string Name;

		public readonly static ulong Code;

		static DeleteOnNoMessages()
		{
			DeleteOnNoMessages.Name = "amqp:delete-on-no-messages:list";
			DeleteOnNoMessages.Code = (ulong)45;
		}

		public DeleteOnNoMessages() : base(DeleteOnNoMessages.Name, DeleteOnNoMessages.Code)
		{
		}

		public override string ToString()
		{
			return "delete-on-no-messages()";
		}
	}
}