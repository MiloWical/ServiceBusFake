using Microsoft.ServiceBus.Common;
using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.ServiceBus
{
	internal class RelayTokenHeader : MessageHeader, IXmlSerializable
	{
		private SecurityToken token;

		public override string Name
		{
			get
			{
				return "RelayAccessToken";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect";
			}
		}

		public SecurityToken Token
		{
			get
			{
				return this.token;
			}
		}

		public RelayTokenHeader(SecurityToken token)
		{
			this.token = token;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			SecurityTokenSerializer defaultInstance = SimpleWebSecurityTokenSerializer.DefaultInstance;
			if (this.token != null && defaultInstance.CanWriteToken(this.token))
			{
				defaultInstance.WriteToken(writer, this.token);
			}
		}

		XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema()
		{
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}

		void System.Xml.Serialization.IXmlSerializable.ReadXml(XmlReader reader)
		{
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}

		void System.Xml.Serialization.IXmlSerializable.WriteXml(XmlWriter writer)
		{
			this.OnWriteHeaderContents(XmlDictionaryWriter.CreateDictionaryWriter(writer), MessageVersion.Soap12WSAddressing10);
		}

		public AddressHeader ToAddressHeader()
		{
			return AddressHeader.CreateAddressHeader(this.Name, this.Namespace, this);
		}
	}
}