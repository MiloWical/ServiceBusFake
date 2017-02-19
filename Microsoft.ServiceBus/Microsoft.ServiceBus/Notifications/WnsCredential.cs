using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="WnsCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class WnsCredential : PnsCredential
	{
		internal const string AppPlatformName = "windows";

		internal const string ProdAccessTokenServiceUrl = "https://login.live.com/accesstoken.srf";

		internal const string MockAccessTokenServiceUrl = "http://localhost:8450/LiveID/accesstoken.srf";

		internal const string MockIntAccessTokenServiceUrl = "http://pushtestservice.cloudapp.net/LiveID/accesstoken.srf";

		internal const string MockRunnerAccessTokenServiceUrl = "http://pushtestservice2.cloudapp.net/LiveID/accesstoken.srf";

		internal const string MockIntInvalidAccessTokenServiceUrl = "http://pushtestserviceInvalid.cloudapp.net/LiveID/accesstoken.srf";

		internal const string MockPerformanceAccessTokenServiceUrl = "http://pushperfnotificationserver.cloudapp.net/LiveID/accesstoken.srf";

		internal const string MockEnduranceAccessTokenServiceUrl = "http://pushstressnotificationserver.cloudapp.net/LiveID/accesstoken.srf";

		internal const string MockEnduranceAccessTokenServiceUrl1 = "http://pushnotificationserver.cloudapp.net/LiveID/accesstoken.srf";

		internal override string AppPlatform
		{
			get
			{
				return "windows";
			}
		}

		public string PackageSid
		{
			get
			{
				return base["PackageSid"];
			}
			set
			{
				base["PackageSid"] = value;
			}
		}

		public string SecretKey
		{
			get
			{
				return base["SecretKey"];
			}
			set
			{
				base["SecretKey"] = value;
			}
		}

		public string WindowsLiveEndpoint
		{
			get
			{
				return base["WindowsLiveEndpoint"] ?? "https://login.live.com/accesstoken.srf";
			}
			set
			{
				base["WindowsLiveEndpoint"] = value;
			}
		}

		public WnsCredential()
		{
		}

		public WnsCredential(string packageSid, string secretKey)
		{
			this.PackageSid = packageSid;
			this.SecretKey = secretKey;
		}

		public override bool Equals(object other)
		{
			WnsCredential wnsCredential = other as WnsCredential;
			if (wnsCredential == null)
			{
				return false;
			}
			if (wnsCredential.PackageSid != this.PackageSid)
			{
				return false;
			}
			return wnsCredential.SecretKey == this.SecretKey;
		}

		public override int GetHashCode()
		{
			if (string.IsNullOrWhiteSpace(this.PackageSid) || string.IsNullOrWhiteSpace(this.SecretKey))
			{
				return base.GetHashCode();
			}
			return this.PackageSid.GetHashCode() ^ this.SecretKey.GetHashCode();
		}

		protected override void OnValidate(bool allowLocalMockPns)
		{
			Uri uri;
			if (base.Properties == null || base.Properties.Count > 3)
			{
				throw new InvalidDataContractException(SRClient.PackageSidAndSecretKeyAreRequired);
			}
			if (base.Properties.Count < 2 || string.IsNullOrWhiteSpace(this.PackageSid) || string.IsNullOrWhiteSpace(this.SecretKey))
			{
				throw new InvalidDataContractException(SRClient.PackageSidOrSecretKeyInvalid);
			}
			if (base.Properties.Count == 3 && string.IsNullOrEmpty(base["WindowsLiveEndpoint"]))
			{
				throw new InvalidDataContractException(SRClient.PackageSidAndSecretKeyAreRequired);
			}
			if (!Uri.TryCreate(this.WindowsLiveEndpoint, UriKind.Absolute, out uri) || !string.Equals(this.WindowsLiveEndpoint, "https://login.live.com/accesstoken.srf", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.WindowsLiveEndpoint, "http://pushtestservice.cloudapp.net/LiveID/accesstoken.srf", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.WindowsLiveEndpoint, "http://pushtestservice2.cloudapp.net/LiveID/accesstoken.srf", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.WindowsLiveEndpoint, "http://pushtestserviceInvalid.cloudapp.net/LiveID/accesstoken.srf", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.WindowsLiveEndpoint, "http://pushperfnotificationserver.cloudapp.net/LiveID/accesstoken.srf", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.WindowsLiveEndpoint, "http://pushstressnotificationserver.cloudapp.net/LiveID/accesstoken.srf", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.WindowsLiveEndpoint, "http://pushnotificationserver.cloudapp.net/LiveID/accesstoken.srf", StringComparison.OrdinalIgnoreCase) && (!allowLocalMockPns || !string.Equals(this.WindowsLiveEndpoint, "http://localhost:8450/LiveID/accesstoken.srf", StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidDataContractException(SRClient.InvalidWindowsLiveEndpoint);
			}
		}
	}
}