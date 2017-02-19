using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class RelayedHttpBodyFormatHeader : MessageHeader
	{
		public const string HeaderName = "BodyFormat";

		public const string HeaderNamespace = "http://schemas.microsoft.com/netservices/2009/05/servicebus/body";

		private readonly string format;

		public string Format
		{
			get
			{
				return this.format;
			}
		}

		public override string Name
		{
			get
			{
				return "BodyFormat";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/netservices/2009/05/servicebus/body";
			}
		}

		public RelayedHttpBodyFormatHeader(string format)
		{
			this.format = format;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteString(this.format);
		}

		public static string ReadHeader(Message message)
		{
			string str;
			MessageHeaders headers = message.Headers;
			int num = headers.FindHeader("BodyFormat", "http://schemas.microsoft.com/netservices/2009/05/servicebus/body");
			if (num == -1)
			{
				return RelayedHttpUtility.FormatStringDefault;
			}
			using (XmlDictionaryReader readerAtHeader = headers.GetReaderAtHeader(num))
			{
				str = readerAtHeader.ReadString();
			}
			return str;
		}
	}
}