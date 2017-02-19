using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal struct AmqpSymbol : IEquatable<AmqpSymbol>
	{
		private int valueSize;

		public string Value
		{
			get;
			private set;
		}

		public int ValueSize
		{
			get
			{
				if (this.valueSize == 0)
				{
					this.valueSize = SymbolEncoding.GetValueSize(this);
				}
				return this.valueSize;
			}
		}

		public AmqpSymbol(string value)
		{
			this = new AmqpSymbol()
			{
				Value = value
			};
		}

		public bool Equals(AmqpSymbol other)
		{
			if (this.Value == null && other.Value == null)
			{
				return true;
			}
			if (this.Value == null || other.Value == null)
			{
				return false;
			}
			return string.Compare(this.Value, other.Value, StringComparison.Ordinal) == 0;
		}

		public override int GetHashCode()
		{
			if (this.Value == null)
			{
				return 0;
			}
			return this.Value.GetHashCode();
		}

		public static implicit operator AmqpSymbol(string value)
		{
			return new AmqpSymbol()
			{
				Value = value
			};
		}

		public override string ToString()
		{
			return this.Value;
		}
	}
}