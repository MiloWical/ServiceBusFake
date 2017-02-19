using Microsoft.ServiceBus;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus.Tracing
{
	internal class SystemTrackerHeader : MessageHeader
	{
		public override string Name
		{
			get
			{
				return "SystemTracker";
			}
		}

		public override string Namespace
		{
			get
			{
				return "http://schemas.microsoft.com/servicebus/2010/08/protocol/";
			}
		}

		public string Tracker
		{
			get;
			private set;
		}

		public SystemTrackerHeader() : this(string.Empty)
		{
		}

		public SystemTrackerHeader(string tracker)
		{
			this.Tracker = tracker;
		}

		protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
		{
			writer.WriteString(this.Tracker.ToString());
		}

		public static SystemTrackerHeader Read(MessageHeaders messageHeaders)
		{
			SystemTrackerHeader systemTrackerHeader;
			if (!SystemTrackerHeader.TryRead(messageHeaders, out systemTrackerHeader))
			{
				throw new ArgumentException(SRClient.SystemTrackerHeaderMissing, "messageHeaders");
			}
			return systemTrackerHeader;
		}

		public static bool Remove(MessageHeaders messageHeaders)
		{
			if (messageHeaders != null)
			{
				int num = messageHeaders.FindHeader("SystemTracker", "http://schemas.microsoft.com/servicebus/2010/08/protocol/");
				if (num >= 0)
				{
					messageHeaders.RemoveAt(num);
					return true;
				}
			}
			return false;
		}

		public static bool TryRead(MessageHeaders messageHeaders, out SystemTrackerHeader systemTrackerHeader)
		{
			systemTrackerHeader = null;
			if (messageHeaders == null)
			{
				return false;
			}
			int num = messageHeaders.FindHeader("SystemTracker", "http://schemas.microsoft.com/servicebus/2010/08/protocol/");
			if (num >= 0)
			{
				using (XmlDictionaryReader readerAtHeader = messageHeaders.GetReaderAtHeader(num))
				{
					readerAtHeader.ReadStartElement();
					systemTrackerHeader = new SystemTrackerHeader(readerAtHeader.ReadString());
				}
			}
			return systemTrackerHeader != null;
		}
	}
}