using System;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.ServiceBus
{
	[XmlSchemaProvider("GetProcessAtSchemaType")]
	internal class ProcessAtHeader : MessageHeader, IXmlSerializable
	{
		internal const string HeaderName = "ProcessAt";

		internal const string HeaderNamespace = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect";

		private string role = "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/roles/relay";

		public override string Name
		{
			get
			{
				return "ProcessAt";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect";
			}
		}

		public string Role
		{
			get
			{
				return this.role;
			}
			set
			{
				this.role = value;
			}
		}

		public ProcessAtHeader()
		{
		}

		public static XmlSchemaType GetProcessAtSchemaType(XmlSchemaSet xmlSchemaSet)
		{
			XmlSchemaComplexType xmlSchemaComplexType = new XmlSchemaComplexType();
			XmlSchemaAttribute xmlSchemaAttribute = new XmlSchemaAttribute()
			{
				Name = "role"
			};
			xmlSchemaComplexType.Attributes.Add(xmlSchemaAttribute);
			return xmlSchemaComplexType;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteStartAttribute("role");
			writer.WriteString(this.role);
			writer.WriteEndAttribute();
		}

		XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
		{
			return null;
		}

		void System.Xml.Serialization.IXmlSerializable.ReadXml(XmlReader reader)
		{
			this.role = reader.GetAttribute("role");
		}

		void System.Xml.Serialization.IXmlSerializable.WriteXml(XmlWriter writer)
		{
			this.OnWriteHeaderContents(XmlDictionaryWriter.CreateDictionaryWriter(writer), MessageVersion.Soap12WSAddressing10);
		}
	}
}