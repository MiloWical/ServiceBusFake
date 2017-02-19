using Microsoft.ServiceBus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Description;
using System.Xml;
using System.Xml.Schema;

namespace Microsoft.ServiceBus.Description
{
	public class OnewayRelayTransportBindingElementImporter : IWsdlImportExtension, IPolicyImportExtension
	{
		public OnewayRelayTransportBindingElementImporter()
		{
		}

		void System.ServiceModel.Description.IPolicyImportExtension.ImportPolicy(MetadataImporter importer, PolicyConversionContext context)
		{
			if (importer == null)
			{
				throw new ArgumentNullException("importer");
			}
			if (context == null)
			{
				throw new ArgumentNullException("context");
			}
			ICollection<XmlElement> bindingAssertions = context.GetBindingAssertions();
			List<XmlElement> xmlElements = new List<XmlElement>();
			RelayedOnewayTransportBindingElement relayedOnewayTransportBindingElement = null;
			foreach (XmlElement bindingAssertion in bindingAssertions)
			{
				if (!(bindingAssertion.NamespaceURI == "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect") || !(bindingAssertion.LocalName == "RelayedOneway"))
				{
					continue;
				}
				bool flag = false;
				bool flag1 = false;
				foreach (XmlElement childNode in bindingAssertion.ChildNodes)
				{
					if (!(childNode.NamespaceURI == "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect") || !(childNode.LocalName == "Multicast"))
					{
						continue;
					}
					flag = true;
					if (flag1)
					{
						throw new NotSupportedException(SRClient.MultipleConnectionModeAssertions);
					}
					flag1 = true;
				}
				relayedOnewayTransportBindingElement = new RelayedOnewayTransportBindingElement(RelayClientAuthenticationType.RelayAccessToken, (flag ? RelayedOnewayConnectionMode.Multicast : RelayedOnewayConnectionMode.Unicast));
				xmlElements.Add(bindingAssertion);
			}
			if (relayedOnewayTransportBindingElement != null)
			{
				context.BindingElements.Add(relayedOnewayTransportBindingElement);
			}
			for (int i = 0; i < xmlElements.Count; i++)
			{
				bindingAssertions.Remove(xmlElements[i]);
			}
		}

		void System.ServiceModel.Description.IWsdlImportExtension.BeforeImport(ServiceDescriptionCollection wsdlDocuments, XmlSchemaSet xmlSchemas, ICollection<XmlElement> policy)
		{
		}

		void System.ServiceModel.Description.IWsdlImportExtension.ImportContract(WsdlImporter importer, WsdlContractConversionContext context)
		{
		}

		void System.ServiceModel.Description.IWsdlImportExtension.ImportEndpoint(WsdlImporter importer, WsdlEndpointConversionContext context)
		{
			if (context.WsdlPort != null)
			{
				ServiceEndpoint endpoint = context.Endpoint;
				EndpointAddress endpointAddress = WSAddressingHelper.ImportAddress(context.WsdlPort);
				EndpointAddress endpointAddress1 = endpointAddress;
				endpoint.Address = endpointAddress;
				EndpointAddress endpointAddress2 = endpointAddress1;
				if (endpointAddress2 != null)
				{
					context.Endpoint.Address = endpointAddress2;
				}
			}
			SoapBinding soapBinding = (SoapBinding)context.WsdlBinding.Extensions.Find(typeof(SoapBinding));
			soapBinding.Handled = true;
		}
	}
}