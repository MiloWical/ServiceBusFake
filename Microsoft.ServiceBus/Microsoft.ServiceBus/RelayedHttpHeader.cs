using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class RelayedHttpHeader : MessageHeader
	{
		public const string HeaderNamespace = "http://schemas.microsoft.com/netservices/2009/05/servicebus/web";

		private string name;

		private string @value;

		public override string Name
		{
			get
			{
				return this.name;
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/netservices/2009/05/servicebus/web";
			}
		}

		public RelayedHttpHeader(string name, string value)
		{
			this.name = name;
			this.@value = value;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteString(this.@value);
		}

		public static string ReadHeader(Message message, string headerName, string defaultValue)
		{
			string str;
			MessageHeaders headers = message.Headers;
			int num = headers.FindHeader(headerName, "http://schemas.microsoft.com/netservices/2009/05/servicebus/web");
			if (num == -1)
			{
				return defaultValue;
			}
			using (XmlDictionaryReader readerAtHeader = headers.GetReaderAtHeader(num))
			{
				str = readerAtHeader.ReadString();
			}
			return str;
		}
	}
}