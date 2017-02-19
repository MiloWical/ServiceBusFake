using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="NokiaXCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class NokiaXCredential : PnsCredential
	{
		internal const string AppPlatformName = "nokiax";

		internal const string ProdAccessTokenServiceUrl = "https://nnapi.ovi.com/nnapi/2.0/send";

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
				return "nokiax";
			}
		}

		public string AuthorizationKey
		{
			get
			{
				return base["AuthorizationKey"];
			}
			set
			{
				base["AuthorizationKey"] = value;
			}
		}

		public string NokiaXEndPoint
		{
			get
			{
				return base["NokiaXEndPoint"] ?? "https://nnapi.ovi.com/nnapi/2.0/send";
			}
			set
			{
				base["NokiaXEndPoint"] = value;
			}
		}

		public NokiaXCredential()
		{
		}

		public NokiaXCredential(string nokiaXAuthorizationKey)
		{
			this.AuthorizationKey = nokiaXAuthorizationKey;
		}

		public override bool Equals(object obj)
		{
			NokiaXCredential nokiaXCredential = obj as NokiaXCredential;
			if (nokiaXCredential == null)
			{
				return false;
			}
			return nokiaXCredential.AuthorizationKey == this.AuthorizationKey;
		}

		public override int GetHashCode()
		{
			if (string.IsNullOrWhiteSpace(this.AuthorizationKey))
			{
				return base.GetHashCode();
			}
			return this.AuthorizationKey.GetHashCode();
		}

		internal static bool IsMockNokiaX(string endpoint)
		{
			return endpoint.ToUpperInvariant().Contains("CLOUDAPP.NET");
		}

		protected override void OnValidate(bool allowLocalMockPns)
		{
			Uri uri;
			bool flag;
			if (base.Properties == null || base.Properties.Count > 2)
			{
				throw new InvalidDataContractException(SRClient.NokiaXRequiredProperties);
			}
			if (base.Properties.Count < 1 || string.IsNullOrWhiteSpace(this.AuthorizationKey))
			{
				throw new InvalidDataContractException(SRClient.NokiaXAuthorizationKeyNotSpecified);
			}
			if (string.Equals(this.NokiaXEndPoint, "https://nnapi.ovi.com/nnapi/2.0/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.NokiaXEndPoint, "http://pushtestservice.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.NokiaXEndPoint, "http://pushtestservice2.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.NokiaXEndPoint, "http://pushperfnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.NokiaXEndPoint, "http://pushstressnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) || string.Equals(this.NokiaXEndPoint, "http://pushnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase))
			{
				flag = false;
			}
			else
			{
				flag = (!allowLocalMockPns ? true : !string.Equals(this.NokiaXEndPoint, "http://localhost:8450/gcm/send", StringComparison.OrdinalIgnoreCase));
			}
			bool flag1 = flag;
			if (!Uri.TryCreate(this.NokiaXEndPoint, UriKind.Absolute, out uri) || flag1)
			{
				throw new InvalidDataContractException(SRClient.InvalidNokiaXEndpoint);
			}
		}
	}
}