using Microsoft.ServiceBus;
using System;

namespace Microsoft.ServiceBus.Configuration
{
	public class WS2007HttpRelayBindingElement : WSHttpRelayBindingElement
	{
		protected override Type BindingElementType
		{
			get
			{
				return typeof(WS2007HttpRelayBinding);
			}
		}

		public WS2007HttpRelayBindingElement(string name) : base(name)
		{
		}

		public WS2007HttpRelayBindingElement() : this(null)
		{
		}
	}
}