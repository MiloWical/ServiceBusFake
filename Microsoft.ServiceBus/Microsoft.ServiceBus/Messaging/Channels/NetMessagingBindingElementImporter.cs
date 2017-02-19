using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Schema;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal class NetMessagingBindingElementImporter : IPolicyImportExtension, IWsdlImportExtension
	{
		public NetMessagingBindingElementImporter()
		{
		}

		public void BeforeImport(ServiceDescriptionCollection wsdlDocuments, XmlSchemaSet xmlSchemas, ICollection<XmlElement> policy)
		{
		}

		private static void ImportAddress(WsdlEndpointConversionContext context)
		{
			EndpointAddress endpointAddress = null;
			if (context.WsdlPort != null)
			{
				XmlElement xmlElement = context.WsdlPort.Extensions.Find("EndpointReference", "http://www.w3.org/2005/08/addressing");
				XmlElement xmlElement1 = context.WsdlPort.Extensions.Find("EndpointReference", "http://schemas.xmlsoap.org/ws/2004/08/addressing");
				SoapAddressBinding soapAddressBinding = (SoapAddressBinding)context.WsdlPort.Extensions.Find(typeof(SoapAddressBinding));
				if (xmlElement != null)
				{
					endpointAddress = EndpointAddress.ReadFrom(AddressingVersion.WSAddressing10, new XmlNodeReader(xmlElement));
				}
				if (xmlElement1 != null)
				{
					endpointAddress = EndpointAddress.ReadFrom(AddressingVersion.WSAddressingAugust2004, new XmlNodeReader(xmlElement1));
				}
				else if (soapAddressBinding != null)
				{
					endpointAddress = new EndpointAddress(soapAddressBinding.Location);
				}
			}
			if (endpointAddress != null)
			{
				context.Endpoint.Address = endpointAddress;
			}
		}

		public void ImportContract(WsdlImporter importer, WsdlContractConversionContext context)
		{
		}

		public void ImportEndpoint(WsdlImporter importer, WsdlEndpointConversionContext context)
		{
			NetMessagingBinding name;
			if (context == null || context.Endpoint.Binding == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull((context == null ? "context" : "context.Endpoint.Binding"));
			}
			BindingElementCollection bindingElementCollection = context.Endpoint.Binding.CreateBindingElements();
			NetMessagingTransportBindingElement netMessagingTransportBindingElement = bindingElementCollection.Find<TransportBindingElement>() as NetMessagingTransportBindingElement;
			if (netMessagingTransportBindingElement != null)
			{
				NetMessagingBindingElementImporter.ImportAddress(context);
			}
			if (context.Endpoint.Binding is CustomBinding && netMessagingTransportBindingElement != null && NetMessagingBinding.TryCreate(bindingElementCollection, out name))
			{
				name.Name = context.Endpoint.Binding.Name;
				name.Namespace = context.Endpoint.Binding.Namespace;
				context.Endpoint.Binding = name;
			}
		}

		void System.ServiceModel.Description.IPolicyImportExtension.ImportPolicy(MetadataImporter importer, PolicyConversionContext context)
		{
			if (importer == null || context == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull((importer == null ? "importer" : "context"));
			}
			if (context.GetBindingAssertions().Remove("netMessaging", "http://sample.schemas.microsoft.com/policy/netMessaging") != null)
			{
				NetMessagingTransportBindingElement netMessagingTransportBindingElement = new NetMessagingTransportBindingElement();
				context.BindingElements.Add(netMessagingTransportBindingElement);
			}
		}
	}
}