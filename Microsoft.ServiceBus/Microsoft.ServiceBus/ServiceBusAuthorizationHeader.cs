using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class ServiceBusAuthorizationHeader : MessageHeader
	{
		public const string HeaderName = "Authorization";

		public const string HeaderNamespace = "http://schemas.microsoft.com/servicebus/2010/08/protocol/";

		private SimpleWebSecurityToken simpleWebSecurityToken;

		public override string Name
		{
			get
			{
				return "Authorization";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/servicebus/2010/08/protocol/";
			}
		}

		internal string TokenString
		{
			get
			{
				return this.simpleWebSecurityToken.Token;
			}
		}

		public ServiceBusAuthorizationHeader(SimpleWebSecurityToken token)
		{
			this.simpleWebSecurityToken = token;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			if (this.simpleWebSecurityToken != null)
			{
				writer.WriteString(this.simpleWebSecurityToken.Token);
			}
		}
	}
}