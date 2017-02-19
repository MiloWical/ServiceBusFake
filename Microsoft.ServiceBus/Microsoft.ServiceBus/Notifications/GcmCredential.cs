using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="GcmCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class GcmCredential : PnsCredential
	{
		internal const string AppPlatformName = "gcm";

		internal const string ProdAccessTokenServiceUrl = "https://android.googleapis.com/gcm/send";

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
				return "gcm";
			}
		}

		public string GcmEndpoint
		{
			get
			{
				return base["GcmEndpoint"] ?? "https://android.googleapis.com/gcm/send";
			}
			set
			{
				base["GcmEndpoint"] = value;
			}
		}

		public string GoogleApiKey
		{
			get
			{
				return base["GoogleApiKey"];
			}
			set
			{
				base["GoogleApiKey"] = value;
			}
		}

		public GcmCredential()
		{
		}

		public GcmCredential(string googleApiKey)
		{
			this.GoogleApiKey = googleApiKey;
		}

		public override bool Equals(object other)
		{
			GcmCredential gcmCredential = other as GcmCredential;
			if (gcmCredential == null)
			{
				return false;
			}
			return gcmCredential.GoogleApiKey == this.GoogleApiKey;
		}

		public override int GetHashCode()
		{
			if (string.IsNullOrWhiteSpace(this.GoogleApiKey))
			{
				return base.GetHashCode();
			}
			return this.GoogleApiKey.GetHashCode();
		}

		internal static bool IsMockGcm(string endpoint)
		{
			return endpoint.ToUpperInvariant().Contains("CLOUDAPP.NET");
		}

		protected override void OnValidate(bool allowLocalMockPns)
		{
			Uri uri;
			if (base.Properties == null || base.Properties.Count > 2)
			{
				throw new InvalidDataContractException(SRClient.GcmRequiredProperties);
			}
			if (base.Properties.Count < 1 || string.IsNullOrWhiteSpace(this.GoogleApiKey))
			{
				throw new InvalidDataContractException(SRClient.GoogleApiKeyNotSpecified);
			}
			if (base.Properties.Count == 2 && string.IsNullOrEmpty(base["GcmEndpoint"]))
			{
				throw new InvalidDataContractException(SRClient.GcmEndpointNotSpecified);
			}
			if (!Uri.TryCreate(this.GcmEndpoint, UriKind.Absolute, out uri) || !string.Equals(this.GcmEndpoint, "https://android.googleapis.com/gcm/send", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.GcmEndpoint, "http://pushtestservice.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.GcmEndpoint, "http://pushtestservice2.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.GcmEndpoint, "http://pushperfnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.GcmEndpoint, "http://pushstressnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.GcmEndpoint, "http://pushnotificationserver.cloudapp.net/gcm/send", StringComparison.OrdinalIgnoreCase) && (!allowLocalMockPns || !string.Equals(this.GcmEndpoint, "http://localhost:8450/gcm/send", StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidDataContractException(SRClient.InvalidGcmEndpoint);
			}
		}
	}
}