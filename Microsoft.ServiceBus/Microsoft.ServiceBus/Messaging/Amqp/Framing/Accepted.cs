using Microsoft.ServiceBus.Messaging.Amqp;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class Accepted : Outcome
	{
		private const int Fields = 0;

		public readonly static string Name;

		public readonly static ulong Code;

		protected override int FieldCount
		{
			get
			{
				return 0;
			}
		}

		static Accepted()
		{
			Accepted.Name = "amqp:accepted:list";
			Accepted.Code = (ulong)36;
		}

		public Accepted() : base(Accepted.Name, Accepted.Code)
		{
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
		}

		protected override int OnValueSize()
		{
			return 0;
		}

		public override string ToString()
		{
			return "accepted()";
		}
	}
}