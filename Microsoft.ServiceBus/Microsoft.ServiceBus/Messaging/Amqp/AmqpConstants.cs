using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Globalization;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class AmqpConstants
	{
		public const string Apache = "apache.org";

		public const string Vendor = "com.microsoft";

		public const string SchemeAmqp = "amqp";

		public const string SchemeAmqps = "amqps";

		public const string TimeSpanName = "com.microsoft:timespan";

		public const string UriName = "com.microsoft:uri";

		public const string DateTimeOffsetName = "com.microsoft:datetime-offset";

		public const string OpenErrorName = "com.microsoft:open-error";

		public const string BadCommand = "BadCommand";

		public const string AddRule = "AddRule";

		public const string DeleteRule = "DeleteRule";

		public const string Publish = "Publish";

		public const string Consume = "Consume";

		public const string Dispose = "Dispose";

		public const uint AmqpBatchedMessageFormat = 2147563264;

		public const uint AmqpMessageFormat = 0;

		public const int DefaultPort = 5672;

		public const int DefaultSecurePort = 5671;

		public const int ProtocolHeaderSize = 8;

		public const int TransportBufferSize = 65536;

		public const int MinMaxFrameSize = 512;

		public const uint DefaultMaxFrameSize = 65536;

		public const ushort DefaultMaxConcurrentChannels = 10000;

		public const ushort DefaultMaxLinkHandles = 255;

		public const uint DefaultHeartBeatInterval = 90000;

		public const uint MinimumHeartBeatIntervalMs = 60000;

		public const int DefaultTimeout = 60;

		public const int DefaultTryCloseTimeout = 15;

		public const uint DefaultWindowSize = 5000;

		public const uint DefaultLinkCredit = 1000;

		public const uint DefaultNextTransferId = 1;

		public const int DefaultDispositionTimeout = 20;

		public const int SegmentSize = 512;

		public const byte AmqpFormat = 1;

		public readonly static AmqpSymbol BatchedMessageFormat;

		public readonly static AmqpSymbol SimpleWebTokenPropertyName;

		public readonly static AmqpSymbol ContainerId;

		public readonly static AmqpSymbol ConnectionId;

		public readonly static AmqpSymbol LinkName;

		public readonly static AmqpSymbol ClientMaxFrameSize;

		public readonly static AmqpSymbol HostName;

		public readonly static AmqpSymbol NetworkHost;

		public readonly static AmqpSymbol Port;

		public readonly static AmqpSymbol Address;

		public readonly static AmqpSymbol PublisherId;

		public readonly static ArraySegment<byte> NullBinary;

		public readonly static ArraySegment<byte> EmptyBinary;

		public readonly static AmqpVersion DefaultProtocolVersion;

		public readonly static DateTime StartOfEpoch;

		public readonly static DateTime MaxAbsoluteExpiryTime;

		public readonly static Accepted AcceptedOutcome;

		public readonly static Released ReleasedOutcome;

		public readonly static Rejected RejectedOutcome;

		public readonly static Rejected RejectedNotFoundOutcome;

		static AmqpConstants()
		{
			AmqpConstants.BatchedMessageFormat = "com.microsoft:batched-message-format";
			AmqpConstants.SimpleWebTokenPropertyName = "com.microsoft:swt";
			AmqpConstants.ContainerId = "container-id";
			AmqpConstants.ConnectionId = "connection-id";
			AmqpConstants.LinkName = "link-name";
			AmqpConstants.ClientMaxFrameSize = "client-max-frame-size";
			AmqpConstants.HostName = "hostname";
			AmqpConstants.NetworkHost = "network-host";
			AmqpConstants.Port = "port";
			AmqpConstants.Address = "address";
			AmqpConstants.PublisherId = "publisher-id";
			AmqpConstants.NullBinary = new ArraySegment<byte>();
			AmqpConstants.EmptyBinary = new ArraySegment<byte>(new byte[0]);
			AmqpConstants.DefaultProtocolVersion = new AmqpVersion(1, 0, 0);
			DateTime dateTime = DateTime.Parse("1970-01-01T00:00:00.0000000Z", CultureInfo.InvariantCulture);
			AmqpConstants.StartOfEpoch = dateTime.ToUniversalTime();
			DateTime maxValue = DateTime.MaxValue;
			AmqpConstants.MaxAbsoluteExpiryTime = maxValue.ToUniversalTime() - TimeSpan.FromDays(1);
			AmqpConstants.AcceptedOutcome = new Accepted();
			AmqpConstants.ReleasedOutcome = new Released();
			AmqpConstants.RejectedOutcome = new Rejected();
			AmqpConstants.RejectedNotFoundOutcome = new Rejected()
			{
				Error = AmqpError.NotFound
			};
		}
	}
}