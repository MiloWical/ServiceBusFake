using Microsoft.ServiceBus;
using System;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Tracing
{
	internal class TrackingIdHeader : MessageHeader
	{
		private string id;

		public string Id
		{
			get
			{
				return this.id;
			}
		}

		public override string Name
		{
			get
			{
				return "TrackingId";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/servicebus/2010/08/protocol/";
			}
		}

		public TrackingIdHeader() : this(Guid.NewGuid().ToString())
		{
		}

		public TrackingIdHeader(string id)
		{
			this.id = id;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteString(this.id);
		}

		public static TrackingIdHeader Read(MessageHeaders messageHeaders)
		{
			TrackingIdHeader trackingIdHeader;
			if (!TrackingIdHeader.TryRead(messageHeaders, out trackingIdHeader))
			{
				throw new ArgumentException(SRClient.TrackingIDHeaderMissing, "messageHeaders");
			}
			return trackingIdHeader;
		}

		public static bool Remove(MessageHeaders messageHeaders)
		{
			if (messageHeaders != null)
			{
				int num = messageHeaders.FindHeader("TrackingId", "http://schemas.microsoft.com/servicebus/2010/08/protocol/");
				if (num >= 0)
				{
					messageHeaders.RemoveAt(num);
					return true;
				}
			}
			return false;
		}

		public static bool TryAddOrUpdate(MessageHeaders messageHeaders, string trackingId)
		{
			if (messageHeaders == null)
			{
				return false;
			}
			TrackingIdHeader trackingIdHeader = new TrackingIdHeader(trackingId);
			int num = messageHeaders.FindHeader("TrackingId", "http://schemas.microsoft.com/servicebus/2010/08/protocol/");
			if (num < 0)
			{
				messageHeaders.Add(trackingIdHeader);
			}
			else
			{
				messageHeaders.RemoveAt(num);
				messageHeaders.Add(trackingIdHeader);
			}
			return true;
		}

		public static bool TryRead(MessageHeaders messageHeaders, out TrackingIdHeader trackingIdHeader)
		{
			trackingIdHeader = null;
			if (messageHeaders == null)
			{
				return false;
			}
			int num = messageHeaders.FindHeader("TrackingId", "http://schemas.microsoft.com/servicebus/2010/08/protocol/");
			if (num >= 0)
			{
				using (XmlDictionaryReader readerAtHeader = messageHeaders.GetReaderAtHeader(num))
				{
					readerAtHeader.ReadStartElement();
					trackingIdHeader = new TrackingIdHeader(readerAtHeader.ReadString());
				}
			}
			return trackingIdHeader != null;
		}
	}
}