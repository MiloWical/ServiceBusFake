using System;
using System.Globalization;
using System.Net;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class RelayedHttpStatusCodeHeader : MessageHeader
	{
		public const string HeaderName = "StatusCode";

		public const string HeaderNamespace = "http://schemas.microsoft.com/netservices/2009/05/servicebus/statuscode";

		private HttpStatusCode statusCode;

		public override string Name
		{
			get
			{
				return "StatusCode";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/netservices/2009/05/servicebus/statuscode";
			}
		}

		public RelayedHttpStatusCodeHeader(HttpStatusCode statusCode)
		{
			this.statusCode = statusCode;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteString(this.statusCode.ToString(CultureInfo.InvariantCulture));
		}

		public static HttpStatusCode ReadHeader(Message message)
		{
			return RelayedHttpStatusCodeHeader.ReadHeader(message, HttpStatusCode.Accepted);
		}

		public static HttpStatusCode ReadHeader(Message message, HttpStatusCode defaultCode)
		{
			object obj;
			MessageHeaders headers = message.Headers;
			int num = headers.FindHeader("StatusCode", "http://schemas.microsoft.com/netservices/2009/05/servicebus/statuscode");
			if (num != -1)
			{
				XmlDictionaryReader readerAtHeader = headers.GetReaderAtHeader(num);
				return (HttpStatusCode)int.Parse(readerAtHeader.ReadString(), CultureInfo.InvariantCulture);
			}
			if (!message.Properties.TryGetValue(HttpResponseMessageProperty.Name, out obj))
			{
				return defaultCode;
			}
			return ((HttpResponseMessageProperty)obj).StatusCode;
		}
	}
}