using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel.Security;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus
{
	public class SimpleWebSecurityTokenSerializer : SecurityTokenSerializer
	{
		private const string LocalName = "BinarySecurityToken";

		private const string NamespaceUri = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

		private const string UtilityNamespaceUri = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

		private const string ValueTypeUri = "http://schemas.xmlsoap.org/ws/2009/11/swt-token-profile-1.0";

		private const string EncodingTypeBase64 = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary";

		public readonly static SimpleWebSecurityTokenSerializer DefaultInstance;

		private SecurityTokenSerializer innerSerializer;

		static SimpleWebSecurityTokenSerializer()
		{
			SimpleWebSecurityTokenSerializer.DefaultInstance = new SimpleWebSecurityTokenSerializer();
		}

		public SimpleWebSecurityTokenSerializer() : this(WSSecurityTokenSerializer.DefaultInstance)
		{
		}

		public SimpleWebSecurityTokenSerializer(SecurityTokenSerializer innerSerializer)
		{
			if (innerSerializer == null)
			{
				throw new ArgumentNullException("innerSerializer");
			}
			this.innerSerializer = innerSerializer;
		}

		protected override bool CanReadKeyIdentifierClauseCore(XmlReader reader)
		{
			return this.innerSerializer.CanReadKeyIdentifierClause(reader);
		}

		protected override bool CanReadKeyIdentifierCore(XmlReader reader)
		{
			return this.innerSerializer.CanReadKeyIdentifier(reader);
		}

		protected override bool CanReadTokenCore(XmlReader reader)
		{
			if (SimpleWebSecurityTokenSerializer.IsSimpleWebSecurityToken(reader))
			{
				return true;
			}
			return this.innerSerializer.CanReadToken(reader);
		}

		protected override bool CanWriteKeyIdentifierClauseCore(SecurityKeyIdentifierClause keyIdentifierClause)
		{
			return this.innerSerializer.CanWriteKeyIdentifierClause(keyIdentifierClause);
		}

		protected override bool CanWriteKeyIdentifierCore(SecurityKeyIdentifier keyIdentifier)
		{
			return this.innerSerializer.CanWriteKeyIdentifier(keyIdentifier);
		}

		protected override bool CanWriteTokenCore(SecurityToken token)
		{
			if (token is SimpleWebSecurityToken)
			{
				return true;
			}
			return this.innerSerializer.CanWriteToken(token);
		}

		private static bool IsSimpleWebSecurityToken(XmlReader reader)
		{
			if (reader.IsStartElement("BinarySecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd") && reader.GetAttribute("ValueType", null) == "http://schemas.xmlsoap.org/ws/2009/11/swt-token-profile-1.0")
			{
				return true;
			}
			return false;
		}

		private static SecurityToken ReadBinaryCore(string id, byte[] rawData)
		{
			string str = Encoding.UTF8.GetString(rawData);
			if (str.StartsWith("SharedAccessSignature", StringComparison.Ordinal))
			{
				return new SharedAccessSignatureToken(id, str);
			}
			return new SimpleWebSecurityToken(id, str);
		}

		protected override SecurityKeyIdentifierClause ReadKeyIdentifierClauseCore(XmlReader reader)
		{
			return this.innerSerializer.ReadKeyIdentifierClause(reader);
		}

		protected override SecurityKeyIdentifier ReadKeyIdentifierCore(XmlReader reader)
		{
			return this.innerSerializer.ReadKeyIdentifier(reader);
		}

		protected override SecurityToken ReadTokenCore(XmlReader reader, SecurityTokenResolver tokenResolver)
		{
			if (!SimpleWebSecurityTokenSerializer.IsSimpleWebSecurityToken(reader))
			{
				return this.innerSerializer.ReadToken(reader, tokenResolver);
			}
			XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateDictionaryReader(reader);
			string attribute = xmlDictionaryReader.GetAttribute("Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd");
			string str = xmlDictionaryReader.GetAttribute("EncodingType", null);
			if (str != null && !(str == "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"))
			{
				throw new NotSupportedException(SRClient.UnsupportedEncodingType);
			}
			return SimpleWebSecurityTokenSerializer.ReadBinaryCore(attribute, xmlDictionaryReader.ReadElementContentAsBase64());
		}

		private static void WriteBinaryCore(SecurityToken token, out string id, out byte[] rawData)
		{
			SimpleWebSecurityToken simpleWebSecurityToken = token as SimpleWebSecurityToken;
			if (simpleWebSecurityToken == null)
			{
				throw new ArgumentNullException("token");
			}
			id = simpleWebSecurityToken.Id;
			rawData = Encoding.UTF8.GetBytes(simpleWebSecurityToken.Token);
		}

		protected override void WriteKeyIdentifierClauseCore(XmlWriter writer, SecurityKeyIdentifierClause keyIdentifierClause)
		{
			this.innerSerializer.WriteKeyIdentifierClause(writer, keyIdentifierClause);
		}

		protected override void WriteKeyIdentifierCore(XmlWriter writer, SecurityKeyIdentifier keyIdentifier)
		{
			this.innerSerializer.WriteKeyIdentifier(writer, keyIdentifier);
		}

		protected override void WriteTokenCore(XmlWriter writer, SecurityToken token)
		{
			string str;
			byte[] numArray;
			if (!(token is SimpleWebSecurityToken))
			{
				this.innerSerializer.WriteToken(writer, token);
				return;
			}
			SimpleWebSecurityTokenSerializer.WriteBinaryCore(token, out str, out numArray);
			if (numArray == null)
			{
				throw new ArgumentNullException(SRClient.NullRawDataInToken);
			}
			writer.WriteStartElement("wsse", "BinarySecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
			if (str != null)
			{
				writer.WriteAttributeString("wsu", "Id", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", str);
			}
			writer.WriteAttributeString("ValueType", null, "http://schemas.xmlsoap.org/ws/2009/11/swt-token-profile-1.0");
			writer.WriteAttributeString("EncodingType", null, "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary");
			writer.WriteBase64(numArray, 0, (int)numArray.Length);
			writer.WriteEndElement();
		}
	}
}