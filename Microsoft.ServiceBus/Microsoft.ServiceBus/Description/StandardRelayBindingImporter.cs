using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Schema;

namespace Microsoft.ServiceBus.Description
{
	public class StandardRelayBindingImporter : IWsdlImportExtension
	{
		public StandardRelayBindingImporter()
		{
		}

		public void BeforeImport(ServiceDescriptionCollection wsdlDocuments, XmlSchemaSet xmlSchemas, ICollection<XmlElement> policy)
		{
		}

		public void ImportContract(WsdlImporter importer, WsdlContractConversionContext context)
		{
		}

		public void ImportEndpoint(WsdlImporter importer, WsdlEndpointConversionContext endpointContext)
		{
			System.ServiceModel.Channels.Binding binding;
			if (endpointContext == null)
			{
				throw new ArgumentNullException("endpointContext");
			}
			if (endpointContext.Endpoint.Binding == null)
			{
				throw new ArgumentNullException("endpointContext.Binding");
			}
			if (endpointContext.Endpoint.Binding is CustomBinding)
			{
				BindingElementCollection elements = ((CustomBinding)endpointContext.Endpoint.Binding).Elements;
				if (elements.Find<HttpRelayTransportBindingElement>() != null)
				{
					elements.Remove<HttpsTransportBindingElement>();
					if (WSHttpRelayBindingBase.TryCreate(elements, out binding))
					{
						StandardRelayBindingImporter.SetBinding(endpointContext.Endpoint, binding);
						return;
					}
					if (BasicHttpRelayBinding.TryCreate(elements, out binding))
					{
						StandardRelayBindingImporter.SetBinding(endpointContext.Endpoint, binding);
						return;
					}
				}
				else if (elements.Find<TcpRelayTransportBindingElement>() != null && NetTcpRelayBinding.TryCreate(elements, out binding))
				{
					StandardRelayBindingImporter.SetBinding(endpointContext.Endpoint, binding);
				}
			}
		}

		private static void SetBinding(ServiceEndpoint endpoint, System.ServiceModel.Channels.Binding binding)
		{
			binding.Name = endpoint.Binding.Name;
			binding.Namespace = endpoint.Binding.Namespace;
			endpoint.Binding = binding;
		}
	}
}