using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal sealed class AmqpSequence : DescribedList
	{
		public readonly static string Name;

		public readonly static ulong Code;

		private IList innerList;

		protected override int FieldCount
		{
			get
			{
				return this.innerList.Count;
			}
		}

		public IList List
		{
			get
			{
				return this.innerList;
			}
		}

		static AmqpSequence()
		{
			AmqpSequence.Name = "amqp:amqp-sequence:list";
			AmqpSequence.Code = (ulong)118;
		}

		public AmqpSequence() : this(new List<object>())
		{
		}

		public AmqpSequence(IList innerList) : base(AmqpSequence.Name, AmqpSequence.Code)
		{
			this.innerList = innerList;
		}

		protected override void OnDecode(ByteBuffer buffer, int count)
		{
			for (int i = 0; i < count; i++)
			{
				this.innerList.Add(AmqpEncoding.DecodeObject(buffer));
			}
		}

		protected override void OnEncode(ByteBuffer buffer)
		{
			foreach (object obj in this.innerList)
			{
				AmqpEncoding.EncodeObject(obj, buffer);
			}
		}

		protected override int OnValueSize()
		{
			return ListEncoding.GetValueSize(this.innerList);
		}

		public override string ToString()
		{
			return "sequence()";
		}
	}
}