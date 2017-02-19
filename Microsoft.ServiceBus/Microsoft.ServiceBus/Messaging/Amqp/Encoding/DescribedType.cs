using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
	internal class DescribedType
	{
		public object Descriptor
		{
			get;
			set;
		}

		public object Value
		{
			get;
			set;
		}

		public DescribedType(object descriptor, object value)
		{
			this.Descriptor = descriptor;
			this.Value = value;
		}

		public override string ToString()
		{
			return this.Descriptor.ToString();
		}
	}
}