using Microsoft.ServiceBus;
using System;
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
	public class TcpRelayTransportBindingElementImporter : IWsdlImportExtension, IPolicyImportExtension
	{
		public TcpRelayTransportBindingElementImporter()
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
			TcpRelayTransportBindingElement tcpRelayTransportBindingElement = null;
			RelayClientAuthenticationType relayClientAuthenticationType = RelayClientAuthenticationType.None;
			bool flag = false;
			foreach (XmlElement bindingAssertion in bindingAssertions)
			{
				if (bindingAssertion.NamespaceURI == "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect")
				{
					if (bindingAssertion.LocalName == "RelaySocketConnection")
					{
						tcpRelayTransportBindingElement = new TcpRelayTransportBindingElement()
						{
							ConnectionMode = TcpRelayConnectionMode.Relayed
						};
						xmlElements.Add(bindingAssertion);
					}
					else if (bindingAssertion.LocalName == "HybridSocketConnection")
					{
						tcpRelayTransportBindingElement = new TcpRelayTransportBindingElement()
						{
							ConnectionMode = TcpRelayConnectionMode.Hybrid
						};
						xmlElements.Add(bindingAssertion);
					}
					else if (bindingAssertion.LocalName == "SenderRelayCredential")
					{
						relayClientAuthenticationType = RelayClientAuthenticationType.RelayAccessToken;
						xmlElements.Add(bindingAssertion);
					}
					else if (bindingAssertion.LocalName == "ListenerRelayCredential")
					{
						xmlElements.Add(bindingAssertion);
					}
				}
				if (!(bindingAssertion.NamespaceURI == "http://schemas.microsoft.com/ws/2006/05/framing/policy") || !(bindingAssertion.LocalName == "SslTransportSecurity"))
				{
					continue;
				}
				flag = true;
				xmlElements.Add(bindingAssertion);
			}
			if (tcpRelayTransportBindingElement != null)
			{
				tcpRelayTransportBindingElement.RelayClientAuthenticationType = relayClientAuthenticationType;
				tcpRelayTransportBindingElement.TransportProtectionEnabled = flag;
				context.BindingElements.Add(tcpRelayTransportBindingElement);
				for (int i = 0; i < xmlElements.Count; i++)
				{
					bindingAssertions.Remove(xmlElements[i]);
				}
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