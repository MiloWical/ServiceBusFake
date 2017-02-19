using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=9L)]
	[XmlSchemaProvider("GenerateSchema")]
	public sealed class CDataMember : IXmlSerializable
	{
		[AmqpMember(Mandatory=false, Order=0)]
		public string Value
		{
			get;
			set;
		}

		public CDataMember()
		{
		}

		public CDataMember(string value)
		{
			this.Value = value;
		}

		public static XmlQualifiedName GenerateSchema(XmlSchemaSet xs)
		{
			return XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String).QualifiedName;
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public static implicit operator String(CDataMember value)
		{
			if (value == null)
			{
				return null;
			}
			return value.Value;
		}

		public static implicit operator CDataMember(string value)
		{
			if (value == null)
			{
				return null;
			}
			return new CDataMember(value);
		}

		public void ReadXml(XmlReader reader)
		{
			if (reader.IsEmptyElement)
			{
				this.Value = string.Empty;
				return;
			}
			reader.Read();
			XmlNodeType nodeType = reader.NodeType;
			switch (nodeType)
			{
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				{
					this.Value = reader.ReadContentAsString();
					return;
				}
				default:
				{
					if (nodeType != XmlNodeType.EndElement)
					{
						break;
					}
					else
					{
						this.Value = string.Empty;
						return;
					}
				}
			}
			throw new SerializationException("Expected text/cdata");
		}

		public override string ToString()
		{
			return this.Value;
		}

		public void WriteXml(XmlWriter writer)
		{
			if (!string.IsNullOrEmpty(this.Value))
			{
				writer.WriteCData(this.Value);
			}
		}
	}
}