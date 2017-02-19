using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal class RelayViaHeader : MessageHeader
	{
		private Uri via;

		public override string Name
		{
			get
			{
				return "RelayVia";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect";
			}
		}

		public Uri Via
		{
			get
			{
				return this.via;
			}
		}

		public RelayViaHeader(Uri via)
		{
			if (via == null)
			{
				throw Fx.Exception.ArgumentNull("via");
			}
			this.via = via;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteString(this.via.ToString());
		}

		private static RelayViaHeader ReadHeader(MessageHeaders headers)
		{
			int num = headers.FindHeader("RelayVia", "http://schemas.microsoft.com/netservices/2009/05/servicebus/connect");
			if (num == -1)
			{
				return null;
			}
			XmlDictionaryReader readerAtHeader = headers.GetReaderAtHeader(num);
			return new RelayViaHeader(new Uri(readerAtHeader.ReadString()));
		}

		public static RelayViaHeader ReadHeader(Message message)
		{
			return RelayViaHeader.ReadHeader(message.Headers);
		}
	}
}