using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Web.Services.Description;
using System.Xml;

namespace Microsoft.ServiceBus.Description
{
	internal static class WSAddressingHelper
	{
		internal static EndpointAddress ImportAddress(Port wsdlPort)
		{
			if (wsdlPort != null)
			{
				XmlElement xmlElement = wsdlPort.Extensions.Find("EndpointReference", "http://www.w3.org/2005/08/addressing");
				XmlElement xmlElement1 = wsdlPort.Extensions.Find("EndpointReference", "http://schemas.xmlsoap.org/ws/2004/08/addressing");
				SoapAddressBinding soapAddressBinding = (SoapAddressBinding)wsdlPort.Extensions.Find(typeof(SoapAddressBinding));
				if (xmlElement != null)
				{
					return EndpointAddress.ReadFrom(AddressingVersion.WSAddressing10, new XmlNodeReader(xmlElement));
				}
				if (xmlElement1 != null)
				{
					return EndpointAddress.ReadFrom(AddressingVersion.WSAddressingAugust2004, new XmlNodeReader(xmlElement1));
				}
				if (soapAddressBinding != null)
				{
					return new EndpointAddress(soapAddressBinding.Location);
				}
			}
			return null;
		}
	}
}