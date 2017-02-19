using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=5L)]
	[DataContract(Name="WindowsRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class WindowsRegistrationDescription : RegistrationDescription
	{
		internal const string WnsHeaderPrefix = "X-WNS-";

		internal const string Type = "X-WNS-Type";

		internal const string Raw = "wns/raw";

		internal const string Badge = "wns/badge";

		internal const string Tile = "wns/tile";

		internal const string Toast = "wns/toast";

		internal const string ProdChannelUriPart = "notify.windows.com";

		internal const string MockChannelUriPart = "localhost:8450/WNS/Mock";

		internal const string MockRunnerChannelUriPart = "pushtestservice.cloudapp.net";

		internal const string MockIntChannelUriPart = "pushtestservice2.cloudapp.net";

		internal const string MockPerformanceChannelUriPart = "pushperfnotificationserver.cloudapp.net";

		internal const string MockEnduranceChannelUriPart = "pushstressnotificationserver.cloudapp.net";

		internal const string MockEnduranceChannelUriPart1 = "pushnotificationserver.cloudapp.net";

		internal static HashSet<string> SupportedWnsTypes;

		internal override string AppPlatForm
		{
			get
			{
				return "windows";
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
				return "windows";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "windows";
			}
		}

		static WindowsRegistrationDescription()
		{
			HashSet<string> strs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			strs.Add("wns/raw");
			strs.Add("wns/badge");
			strs.Add("wns/toast");
			strs.Add("wns/tile");
			WindowsRegistrationDescription.SupportedWnsTypes = strs;
		}

		public WindowsRegistrationDescription(string channelUri) : this(string.Empty, new Uri(channelUri), null)
		{
		}

		public WindowsRegistrationDescription(string channelUri, IEnumerable<string> tags) : this(string.Empty, new Uri(channelUri), tags)
		{
		}

		public WindowsRegistrationDescription(Uri channelUri) : this(string.Empty, channelUri, null)
		{
		}

		public WindowsRegistrationDescription(Uri channelUri, IEnumerable<string> tags) : this(string.Empty, channelUri, tags)
		{
		}

		public WindowsRegistrationDescription(WindowsRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.ChannelUri = sourceRegistration.ChannelUri;
		}

		internal WindowsRegistrationDescription(string notificationHubPath, string channelUri, IEnumerable<string> tags) : base(notificationHubPath)
		{
			if (string.IsNullOrWhiteSpace(channelUri))
			{
				throw new ArgumentNullException("channelUri");
			}
			this.ChannelUri = new Uri(channelUri);
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal WindowsRegistrationDescription(string notificationHubPath, Uri channelUri, IEnumerable<string> tags) : base(notificationHubPath)
		{
			if (channelUri == null)
			{
				throw new ArgumentNullException("channelUri");
			}
			this.ChannelUri = channelUri;
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal override RegistrationDescription Clone()
		{
			return new WindowsRegistrationDescription(this);
		}

		internal override string GetPnsHandle()
		{
			return this.ChannelUri.AbsoluteUri;
		}

		internal bool IsMockWns()
		{
			return this.ChannelUri.Host.ToUpperInvariant().Contains("CLOUDAPP.NET");
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.ChannelUri.ToString()) || !this.ChannelUri.IsAbsoluteUri)
			{
				throw new InvalidDataContractException(SRClient.ChannelUriNullOrEmpty);
			}
			if (!this.ChannelUri.Host.EndsWith("notify.windows.com", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushtestservice2.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushtestservice.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushperfnotificationserver.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushstressnotificationserver.cloudapp.net", StringComparison.OrdinalIgnoreCase) && !this.ChannelUri.Host.EndsWith("pushnotificationserver.cloudapp.net", StringComparison.OrdinalIgnoreCase) && (!allowLocalMockPns || !this.ChannelUri.ToString().Contains("localhost:8450/WNS/Mock")))
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