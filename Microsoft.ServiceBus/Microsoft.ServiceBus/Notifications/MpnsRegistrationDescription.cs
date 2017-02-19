using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=7L)]
	[DataContract(Name="MpnsRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class MpnsRegistrationDescription : RegistrationDescription
	{
		internal const string MpnsHeaderPrefix = "X-";

		internal const string NotificationClass = "X-NotificationClass";

		internal const string Type = "X-WindowsPhone-Target";

		internal const string Tile = "token";

		internal const string Toast = "toast";

		internal const string TileClass = "1";

		internal const string ToastClass = "2";

		internal const string RawClass = "3";

		internal const string NamespaceName = "WPNotification";

		internal const string NotificationElementName = "Notification";

		internal const string ProdChannelUriPart = ".notify.live.net";

		internal const string MockChannelUriPart = "localhost:8450/MPNS/Mock";

		internal const string MockSSLChannelUriPart = "localhost:8451/MPNS/Mock";

		internal const string MockRunnerChannelUriPart = "pushtestservice.cloudapp.net";

		internal const string MockIntChannelUriPart = "pushtestservice2.cloudapp.net";

		internal const string MockPerformanceChannelUriPart = "pushperfnotificationserver.cloudapp.net";

		internal const string MockEnduranceChannelUriPart = "pushstressnotificationserver.cloudapp.net";

		internal const string MockEnduranceChannelUriPart1 = "pushnotificationserver.cloudapp.net";

		internal override string AppPlatForm
		{
			get
			{
				return "windowsphone";
			}
		}

		[AmqpMember(Mandatory=false, Order=3)]
		[DataMember(Name="ChannelUri", Order=2001, IsRequired=true)]
		public Uri ChannelUri
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "windowsphone";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "windowsphone";
			}
		}

		public MpnsRegistrationDescription(MpnsRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.ChannelUri = sourceRegistration.ChannelUri;
		}

		public MpnsRegistrationDescription(string channelUri) : this(string.Empty, new Uri(channelUri), null)
		{
		}

		public MpnsRegistrationDescription(Uri channelUri) : this(string.Empty, channelUri, null)
		{
		}

		public MpnsRegistrationDescription(string channelUri, IEnumerable<string> tags) : this(string.Empty, new Uri(channelUri), tags)
		{
		}

		public MpnsRegistrationDescription(Uri channelUri, IEnumerable<string> tags) : this(string.Empty, channelUri, tags)
		{
		}

		internal MpnsRegistrationDescription(string notificationHubPath, string channelUri, IEnumerable<string> tags) : this(notificationHubPath, new Uri(channelUri), tags)
		{
		}

		internal MpnsRegistrationDescription(string notificationHubPath, Uri channelUri, IEnumerable<string> tags) : base(notificationHubPath)
		{
			this.ChannelUri = channelUri;
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal override RegistrationDescription Clone()
		{
			return new MpnsRegistrationDescription(this);
		}

		internal override string GetPnsHandle()
		{
			return this.ChannelUri.AbsoluteUri;
		}

		internal bool IsMockMpns()
		{
			return this.ChannelUri.Host.ToUpperInvariant().Contains("CLOUDAPP.NET");
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.ChannelUri.ToString()) || !this.ChannelUri.IsAbsoluteUri)
			{
				throw new InvalidDataContractException(SRClient.ChannelUriNullOrEmpty);
			}
			if (!this.ChannelUri.Host.EndsWith(".notify.live.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushtestservice2.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushtestservice.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushperfnotificationserver.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushstressnotificationserver.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushnotificationserver.cloudapp.net", StringComparison.OrdinalIgnoreCase) && (!allowLocalMockPns || !this.ChannelUri.ToString().Contains("localhost:8450/MPNS/Mock") && !this.ChannelUri.ToString().Contains("localhost:8451/MPNS/Mock")))
			{
				throw new InvalidDataContractException(SRClient.UnsupportedChannelUri(this.ChannelUri));
			}
		}

		internal override void SetPnsHandle(string pnsHandle)
		{
			if (string.IsNullOrEmpty(pnsHandle))
			{
				this.ChannelUri = null;
				return;
			}
			this.ChannelUri = new Uri(pnsHandle);
		}
	}
}