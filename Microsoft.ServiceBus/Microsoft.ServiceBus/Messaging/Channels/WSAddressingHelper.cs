using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Description;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal static class WSAddressingHelper
	{
		private static XmlDocument xmlDocument;

		private static XmlDocument XmlDoc
		{
			get
			{
				if (WSAddressingHelper.xmlDocument == null)
				{
					NameTable nameTable = new NameTable();
					nameTable.Add("Policy");
					nameTable.Add("All");
					nameTable.Add("ExactlyOne");
					nameTable.Add("PolicyURIs");
					nameTable.Add("UsingAddressing");
					nameTable.Add("UsingAddressing");
					nameTable.Add("Addressing");
					nameTable.Add("AnonymousResponses");
					nameTable.Add("NonAnonymousResponses");
					WSAddressingHelper.xmlDocument = new XmlDocument(nameTable);
				}
				return WSAddressingHelper.xmlDocument;
			}
		}

		internal static void AddAddressToWsdlPort(Port wsdlPort, EndpointAddress addr, AddressingVersion addressing)
		{
			if (addressing == AddressingVersion.None)
			{
				return;
			}
			MemoryStream memoryStream = new MemoryStream();
			XmlWriter xmlWriter = XmlWriter.Create(memoryStream);
			xmlWriter.WriteStartElement("temp");
			if (addressing != AddressingVersion.WSAddressing10)
			{
				if (addressing != AddressingVersion.WSAddressingAugust2004)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(string.Concat("This addressing version is not supported: ", addressing.ToString())), null);
				}
				xmlWriter.WriteAttributeString("xmlns", "wsa", null, "http://schemas.xmlsoap.org/ws/2004/08/addressing");
			}
			else
			{
				xmlWriter.WriteAttributeString("xmlns", "wsa10", null, "http://www.w3.org/2005/08/addressing");
			}
			addr.WriteTo(addressing, xmlWriter);
			xmlWriter.WriteEndElement();
			xmlWriter.Flush();
			memoryStream.Seek((long)0, SeekOrigin.Begin);
			XmlReader xmlReader = XmlReader.Create(memoryStream);
			xmlReader.MoveToContent();
			XmlElement itemOf = (XmlElement)WSAddressingHelper.XmlDoc.ReadNode(xmlReader).ChildNodes[0];
			wsdlPort.Extensions.Add(itemOf);
		}

		internal static void AddWSAddressingAssertion(MetadataExporter exporter, PolicyConversionContext context, AddressingVersion addressVersion)
		{
			XmlElement xmlElement;
			string str;
			if (addressVersion == AddressingVersion.WSAddressingAugust2004)
			{
				xmlElement = WSAddressingHelper.XmlDoc.CreateElement("wsap", "UsingAddressing", "http://schemas.xmlsoap.org/ws/2004/08/addressing/policy");
			}
			else if (addressVersion != AddressingVersion.WSAddressing10)
			{
				if (addressVersion != AddressingVersion.None)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(string.Concat("This addressing version is not supported: ", addressVersion.ToString())), null);
				}
				xmlElement = null;
			}
			else if (exporter.PolicyVersion != PolicyVersion.Policy12)
			{
				xmlElement = WSAddressingHelper.XmlDoc.CreateElement("wsam", "Addressing", "http://www.w3.org/2007/05/addressing/metadata");
				SupportedAddressingMode item = SupportedAddressingMode.Anonymous;
				string name = typeof(SupportedAddressingMode).Name;
				if (exporter.State.ContainsKey(name) && exporter.State[name] as SupportedAddressingMode != SupportedAddressingMode.Anonymous)
				{
					item = (SupportedAddressingMode)exporter.State[name];
				}
				if (item != SupportedAddressingMode.Mixed)
				{
					str = (item != SupportedAddressingMode.Anonymous ? "NonAnonymousResponses" : "AnonymousResponses");
					XmlElement xmlElement1 = WSAddressingHelper.XmlDoc.CreateElement("wsp", "Policy", "http://www.w3.org/ns/ws-policy");
					XmlElement xmlElement2 = WSAddressingHelper.XmlDoc.CreateElement("wsam", str, "http://www.w3.org/2007/05/addressing/metadata");
					xmlElement1.AppendChild(xmlElement2);
					xmlElement.AppendChild(xmlElement1);
				}
			}
			else
			{
				xmlElement = WSAddressingHelper.XmlDoc.CreateElement("wsaw", "UsingAddressing", "http://www.w3.org/2006/05/addressing/wsdl");
			}
			if (xmlElement != null)
			{
				context.GetBindingAssertions().Add(xmlElement);
			}
		}
	}
}