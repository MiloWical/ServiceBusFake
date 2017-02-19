using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="AdmCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class AdmCredential : PnsCredential
	{
		internal const string AppPlatformName = "adm";

		internal const string ProdAuthTokenUrl = "https://api.amazon.com/auth/O2/token";

		internal const string MockAuthTokenUrl = "http://localhost:8450/adm/token";

		internal const string MockRunnerAuthTokenUrl = "http://pushtestservice.cloudapp.net/adm/token";

		internal const string MockIntAuthTokenUrl = "http://pushtestservice2.cloudapp.net/adm/token";

		internal const string MockPerformanceAuthTokenUrl = "http://pushperfnotificationserver.cloudapp.net/adm/token";

		internal const string MockEndurancAuthTokenUrl = "http://pushstressnotificationserver.cloudapp.net/adm/token";

		internal const string MockEndurancAuthTokenUrl1 = "http://pushnotificationserver.cloudapp.net/adm/token";

		internal const string ProdSendUrlTemplate = "https://api.amazon.com/messaging/registrations/{0}/messages";

		internal const string MockSendUrlTemplate = "http://localhost:8450/adm/send/{0}/messages";

		internal const string MockRunnerSendUrlTemplate = "http://pushtestservice.cloudapp.net/adm/send/{0}/messages";

		internal const string MockIntSendUrlTemplate = "http://pushtestservice2.cloudapp.net/adm/send/{0}/messages";

		internal const string MockPerformanceSendUrlTemplate = "http://pushperfnotificationserver.cloudapp.net/adm/send/{0}/messages";

		internal const string MockEndurancSendUrlTemplate = "http://pushstressnotificationserver.cloudapp.net/adm/send/{0}/messages";

		internal const string MockEndurancSendUrlTemplate1 = "http://pushnotificationserver.cloudapp.net/adm/send/{0}/messages";

		private const string ClientIdName = "ClientId";

		private const string ClientSecretName = "ClientSecret";

		private const string AuthTokenUrlName = "AuthTokenUrl";

		private const string SendUrlTemplateName = "SendUrlTemplate";

		private const string RequiredPropertiesList = "ClientId, ClientSecret";

		internal override string AppPlatform
		{
			get
			{
				return "adm";
			}
		}

		public string AuthTokenUrl
		{
			get
			{
				return base["AuthTokenUrl"] ?? "https://api.amazon.com/auth/O2/token";
			}
			set
			{
				base["AuthTokenUrl"] = value;
			}
		}

		public string ClientId
		{
			get
			{
				return base["ClientId"];
			}
			set
			{
				base["ClientId"] = value;
			}
		}

		public string ClientSecret
		{
			get
			{
				return base["ClientSecret"];
			}
			set
			{
				base["ClientSecret"] = value;
			}
		}

		public string SendUrlTemplate
		{
			get
			{
				return base["SendUrlTemplate"] ?? "https://api.amazon.com/messaging/registrations/{0}/messages";
			}
			set
			{
				base["SendUrlTemplate"] = value;
			}
		}

		public AdmCredential()
		{
		}

		public AdmCredential(string clientId, string clientSecret)
		{
			this.ClientId = clientId;
			this.ClientSecret = clientSecret;
		}

		public override bool Equals(object other)
		{
			AdmCredential admCredential = other as AdmCredential;
			if (admCredential == null)
			{
				return false;
			}
			if (admCredential.ClientId != this.ClientId)
			{
				return false;
			}
			return admCredential.ClientSecret == this.ClientSecret;
		}

		public override int GetHashCode()
		{
			if (string.IsNullOrWhiteSpace(this.ClientId) || string.IsNullOrWhiteSpace(this.ClientSecret))
			{
				return base.GetHashCode();
			}
			return this.ClientId.GetHashCode() ^ this.ClientSecret.GetHashCode();
		}

		internal static bool IsMockAdm(string endpoint)
		{
			return !endpoint.ToUpperInvariant().Contains("//API.AMAZON.COM");
		}

		protected override void OnValidate(bool allowLocalMockPns)
		{
			Uri uri;
			if (base.Properties == null || string.IsNullOrEmpty(base.Properties["ClientId"]) && string.IsNullOrEmpty(base.Properties["ClientSecret"]))
			{
				throw new InvalidDataContractException(SRClient.RequiredPropertiesNotSpecified("ClientId, ClientSecret"));
			}
			if (string.IsNullOrEmpty(base.Properties["ClientId"]))
			{
				throw new InvalidDataContractException(SRClient.RequiredPropertyNotSpecified("ClientId"));
			}
			if (string.IsNullOrEmpty(base.Properties["ClientSecret"]))
			{
				throw new InvalidDataContractException(SRClient.RequiredPropertyNotSpecified("ClientSecret"));
			}
			if (base.Properties.Count > 2)
			{
				int count = base.Properties.Count;
				Dictionary<!0, !1>.KeyCollection keys = base.Properties.Keys;
				string[] strArrays = new string[] { "ClientId", "ClientSecret", "SendUrlTemplate", "AuthTokenUrl" };
				if (count > keys.Intersect<string>(strArrays).Count<string>())
				{
					throw new InvalidDataContractException(SRClient.OnlyNPropertiesRequired(2, "ClientId, ClientSecret"));
				}
			}
			if (!Uri.TryCreate(this.AuthTokenUrl, UriKind.Absolute, out uri) || !string.Equals(this.AuthTokenUrl, "https://api.amazon.com/auth/O2/token", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.AuthTokenUrl, "http://pushtestservice.cloudapp.net/adm/token", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.AuthTokenUrl, "http://pushtestservice2.cloudapp.net/adm/token", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.AuthTokenUrl, "http://pushperfnotificationserver.cloudapp.net/adm/token", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.AuthTokenUrl, "http://pushstressnotificationserver.cloudapp.net/adm/token", StringComparison.OrdinalIgnoreCase) && !string.Equals(this.AuthTokenUrl, "http://pushnotificationserver.cloudapp.net/adm/token", StringComparison.OrdinalIgnoreCase) && (!allowLocalMockPns || !string.Equals(this.AuthTokenUrl, "http://localhost:8450/adm/token", StringComparison.OrdinalIgnoreCase)))
			{
				throw new InvalidDataContractException(SRClient.InvalidAdmAuthTokenUrl);
			}
			try
			{
				if (!string.IsNullOrWhiteSpace(this.SendUrlTemplate))
				{
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					string sendUrlTemplate = this.SendUrlTemplate;
					object[] objArray = new object[] { "AdmRegistrationId" };
					if (Uri.TryCreate(string.Format(invariantCulture, sendUrlTemplate, objArray), UriKind.Absolute, out uri) && (string.Equals(this.SendUrlTemplate, "https://api.amazon.com/messaging/registrations/{0}/messages", StringComparison.OrdinalIgnoreCase) || string.Equals(this.SendUrlTemplate, "http://pushtestservice.cloudapp.net/adm/send/{0}/messages", StringComparison.OrdinalIgnoreCase) || string.Equals(this.SendUrlTemplate, "http://pushtestservice2.cloudapp.net/adm/send/{0}/messages", StringComparison.OrdinalIgnoreCase) || string.Equals(this.SendUrlTemplate, "http://pushperfnotificationserver.cloudapp.net/adm/send/{0}/messages", StringComparison.OrdinalIgnoreCase) || string.Equals(this.SendUrlTemplate, "http://pushstressnotificationserver.cloudapp.net/adm/send/{0}/messages", StringComparison.OrdinalIgnoreCase) || string.Equals(this.SendUrlTemplate, "http://pushnotificationserver.cloudapp.net/adm/send/{0}/messages", StringComparison.OrdinalIgnoreCase) || allowLocalMockPns && string.Equals(this.SendUrlTemplate, "http://localhost:8450/adm/send/{0}/messages", StringComparison.OrdinalIgnoreCase)))
					{
						return;
					}
				}
				throw new InvalidDataContractException(SRClient.InvalidAdmSendUrlTemplate);
			}
			catch (FormatException formatException)
			{
				throw new InvalidDataContractException(SRClient.InvalidAdmSendUrlTemplate);
			}
		}
	}
}