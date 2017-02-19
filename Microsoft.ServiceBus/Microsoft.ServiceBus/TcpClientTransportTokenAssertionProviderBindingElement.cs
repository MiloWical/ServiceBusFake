using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class TcpClientTransportTokenAssertionProviderBindingElement : BindingElement, ITransportTokenAssertionProvider
	{
		public TcpClientTransportTokenAssertionProviderBindingElement()
		{
		}

		public override BindingElement Clone()
		{
			return new TcpClientTransportTokenAssertionProviderBindingElement();
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			if (context == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("context");
			}
			return context.GetInnerProperty<T>();
		}

		public XmlElement GetTransportTokenAssertion()
		{
			return (new SslStreamSecurityBindingElement()).GetTransportTokenAssertion();
		}
	}
}