using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal abstract class DescribedAnnotations : DescribedMap
	{
		private Annotations annotations;

		public Annotations Map
		{
			get
			{
				if (this.annotations == null)
				{
					this.annotations = new Annotations();
					this.annotations.SetMap(base.InnerMap);
				}
				return this.annotations;
			}
		}

		protected DescribedAnnotations(AmqpSymbol name, ulong code) : base(name, code)
		{
		}
	}
}