using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="BaiduCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class BaiduCredential : PnsCredential
	{
		internal const string AppPlatformName = "baidu";

		internal const string ProdAccessTokenServiceUrl = "https://channel.api.duapp.com/rest/2.0/channel/channel";

		internal const string NokiaProdAccessTokenServiceUrl = "https://nnapi.ovi.com/nnapi/2.0/send";

		internal const string MockAccessTokenServiceUrl = "http://localhost:8450/gcm/send";

		internal const string MockRunnerAccessTokenServiceUrl = "http://pushtestservice.cloudapp.net/gcm/send";

		internal const string MockIntAccessTokenServiceUrl = "http://pushtestservice2.cloudapp.net/gcm/send";

		internal const string MockPerformanceAccessTokenServiceUrl = "http://pushperfnotificationserver.cloudapp.net/gcm/send";

		internal const string MockEnduranceAccessTokenServiceUrl = "http://pushstressnotificationserver.cloudapp.net/gcm/send";

		internal const string MockEnduranceAccessTokenServiceUrl1 = "http://pushnotificationserver.cloudapp.net/gcm/send";

		internal override string AppPlatform
		{
			get
			{
				return "baidu";
			}
		}

		public string BaiduApiKey
		{
			get
			{
				return base["BaiduApiKey"];
			}
			set
			{
				base["BaiduApiKey"] = value;
			}
		}

		public string BaiduEndPoint
		{
			get
			{
				return base["BaiduEndPoint"] ?? "https://channel.api.duapp.com/rest/2.0/channel/channel";
			}
			set
			{
				base["BaiduEndPoint"] = value;
			}
		}

		public string BaiduSecretKey
		{
			get
			{
				return base["BaiduSecretKey"];
			}
			set
			{
				base["BaiduSecretKey"] = value;
			}
		}

		public BaiduCredential()
		{
		}

		public BaiduCredential(string baiduApiKey)
		{
			this.BaiduApiKey = baiduApiKey;
		}

		public override bool Equals(object obj)
		{
			BaiduCredential baiduCredential = obj as BaiduCredential;
			if (baiduCredential == null)
			{
				return false;
			}
			return baiduCredential.BaiduApiKey == this.BaiduApiKey;
		}

		public override int GetHashCode()
		{
			if (string.IsNullOrWhiteSpace(this.BaiduApiKey))
			{
				return base.GetHashCode();
			}
			return this.BaiduApiKey.GetHashCode();
		}

		internal static bool IsMockBaidu(string endPoint)
		{
			return endPoint.ToUpperInvariant().Contains("CLOUDAPP.NET");
		}

		protected override void OnValidate(bool allowLocalMockPns)
		{
			Uri uri;
			bool flag;
			if (base.Properties == null || base.Properties.Count > 2)
			{
				throw new InvalidDataContractException(SRClient.BaiduRequiredProperties);
			}
			if (base.Properties.Count < 1 || string.IsNullOrWhiteSpace(this.BaiduApiKey))
			{
				throw new InvalidDataContractException(SRClient.BaiduApiKeyNotSpecified);
			}
			if (string.Equals(this.BaiduEndPoint, "https://channel.api.duapp.com/rest/2.0/channel/channel", StringComparison.OrdinalIgnoreCase) || string.Equals(this.BaiduEndPoint, "https://nnapi.ovi.com/nnapi/2.0/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.BaiduEndPoint, "http://pushtestservice.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.BaiduEndPoint, "http://pushtestservice2.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.BaiduEndPoint, "http://pushperfnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.BaiduEndPoint, "http://pushstressnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.BaiduEndPoint, "http://pushnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase))
			{
				flag = false;
			}
			else
			{
				flag = (!allowLocalMockPns ? true : !string.Equals(this.BaiduEndPoint, "http://localhost:8450/gcm/send", StringComparison.OrdinalIgnoreCase));
			}
			bool flag1 = flag;
			if (!Uri.TryCreate(this.BaiduEndPoint, UriKind.Absolute, out uri) || flag1)
			{
				throw new InvalidDataContractException(SRClient.InvalidBaiduEndpoint);
			}
		}
	}
}