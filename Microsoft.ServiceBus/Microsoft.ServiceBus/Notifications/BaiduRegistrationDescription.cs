using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=15L)]
	[DataContract(Name="BaiduRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class BaiduRegistrationDescription : RegistrationDescription
	{
		internal override string AppPlatForm
		{
			get
			{
				return "baidu";
			}
		}

		[AmqpMember(Order=4, Mandatory=false)]
		[DataMember(Name="BaiduChannelId", Order=2002, IsRequired=true)]
		public string BaiduChannelId
		{
			get;
			set;
		}

		[AmqpMember(Order=3, Mandatory=true)]
		[DataMember(Name="BaiduUserId", Order=2001, IsRequired=true)]
		public string BaiduUserId
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "baidu";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "baidu";
			}
		}

		public BaiduRegistrationDescription(string pnsHandle) : base(string.Empty)
		{
			int num = pnsHandle.IndexOf('-');
			this.BaiduUserId = pnsHandle.Substring(0, num);
			this.BaiduChannelId = pnsHandle.Substring(num + 1, pnsHandle.Length - num - 1);
			if (string.IsNullOrWhiteSpace(this.BaiduUserId))
			{
				throw new ArgumentNullException("baiduRegistrationId");
			}
		}

		public BaiduRegistrationDescription(BaiduRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.BaiduUserId = sourceRegistration.BaiduUserId;
			this.BaiduChannelId = sourceRegistration.BaiduChannelId;
		}

		public BaiduRegistrationDescription(string baiduUserId, string baiduChannelId, IEnumerable<string> tags) : this(string.Empty, baiduUserId, baiduChannelId, tags)
		{
		}

		internal BaiduRegistrationDescription(string notificationHubPath, string baiduUserId, string baiduChannelId, IEnumerable<string> tags) : base(notificationHubPath)
		{
			if (string.IsNullOrWhiteSpace(baiduUserId))
			{
				throw new ArgumentNullException("baiduRegistrationId");
			}
			this.BaiduUserId = baiduUserId;
			this.BaiduChannelId = baiduChannelId;
			if (tags != null)
			{
				base.Tags = new HashSet<string>(tags);
			}
		}

		internal override RegistrationDescription Clone()
		{
			return new BaiduRegistrationDescription(this);
		}

		internal override string GetPnsHandle()
		{
			return string.Concat(this.BaiduUserId, "-", this.BaiduChannelId);
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			if (string.IsNullOrWhiteSpace(this.BaiduChannelId))
			{
				throw new InvalidDataContractException(SRClient.BaiduRegistrationInvalidId);
			}
		}

		internal override void SetPnsHandle(string pnsHandle)
		{
			if (!string.IsNullOrEmpty(pnsHandle))
			{
				int num = pnsHandle.IndexOf('-');
				this.BaiduUserId = pnsHandle.Substring(0, num);
				this.BaiduChannelId = pnsHandle.Substring(num + 1, pnsHandle.Length - num - 1);
			}
		}
	}
}