using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	public static class ServiceBusEnvironment
	{
		private readonly static ConnectivitySettings HttpListenerSettings;

		public static string DefaultIdentityHostName
		{
			get
			{
				return RelayEnvironment.StsHostName;
			}
		}

		public static ConnectivitySettings SystemConnectivity
		{
			get
			{
				return ServiceBusEnvironment.HttpListenerSettings;
			}
		}

		internal static bool? UseNoRendezvous
		{
			get;
			set;
		}

		static ServiceBusEnvironment()
		{
			ServiceBusEnvironment.HttpListenerSettings = new ConnectivitySettings();
		}

		internal static string CreateAccessControlIssuer(string serviceNamespace)
		{
			if (string.IsNullOrEmpty(serviceNamespace))
			{
				throw new ArgumentException(SRClient.NullServiceNameSpace, "serviceNamespace");
			}
			if (RelayEnvironment.StsHttpsPort == 443)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { serviceNamespace, RelayEnvironment.StsHostName };
				return string.Format(invariantCulture, "https://{0}-sb.{1}/", objArray);
			}
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray1 = new object[] { serviceNamespace, RelayEnvironment.StsHostName, RelayEnvironment.StsHttpsPort };
			return string.Format(cultureInfo, "https://{0}-sb.{1}:{2}/", objArray1);
		}

		public static Uri CreateAccessControlUri(string serviceNamespace)
		{
			if (string.IsNullOrEmpty(serviceNamespace))
			{
				throw new ArgumentException(SRClient.NullServiceNameSpace, "serviceNamespace");
			}
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { serviceNamespace, RelayEnvironment.StsHostName, RelayEnvironment.StsHttpsPort };
			return new Uri(string.Format(invariantCulture, "https://{0}-sb.{1}:{2}/WRAPv0.9/", objArray));
		}

		public static Uri CreateServiceUri(string scheme, string serviceNamespace, string servicePath)
		{
			return ServiceBusEnvironment.CreateServiceUri(scheme, serviceNamespace, servicePath, false, RelayEnvironment.RelayHostRootName);
		}

		public static Uri CreateServiceUri(string scheme, string serviceNamespace, string servicePath, bool suppressRelayPathPrefix)
		{
			return ServiceBusEnvironment.CreateServiceUri(scheme, serviceNamespace, servicePath, suppressRelayPathPrefix, RelayEnvironment.RelayHostRootName);
		}

		private static Uri CreateServiceUri(string scheme, string serviceNamespace, string servicePath, bool suppressRelayPathPrefix, string hostName)
		{
			string str = serviceNamespace.Trim();
			ServiceBusEnvironment.ValidateSchemeAndNamespace(scheme, str);
			if (!servicePath.EndsWith("/", StringComparison.Ordinal))
			{
				servicePath = string.Concat(servicePath, "/");
			}
			Uri uri = ServiceBusUriHelper.CreateServiceUri(scheme, str, hostName, servicePath, suppressRelayPathPrefix);
			if (!ServiceBusUriHelper.IsSafeBasicLatinUriPath(uri))
			{
				throw new ArgumentException(SRClient.PathSegmentASCIICharacters, servicePath);
			}
			return uri;
		}

		private static bool ValidateScheme(string scheme)
		{
			if (string.IsNullOrEmpty(scheme))
			{
				return false;
			}
			if (scheme.Equals("http") || scheme.Equals("https"))
			{
				return true;
			}
			return scheme.Equals("sb");
		}

		private static void ValidateSchemeAndNamespace(string scheme, string serviceNamespace)
		{
			if (!ServiceBusEnvironment.ValidateScheme(scheme))
			{
				throw new ArgumentException(SRClient.InvalidSchemeValue("sb"), "scheme");
			}
			if (!ServiceBusEnvironment.ValidateServiceNamespace(serviceNamespace))
			{
				throw new ArgumentException(SRClient.InvalidServiceNameSpace(serviceNamespace), "serviceNamespace");
			}
		}

		private static bool ValidateServiceNamespace(string serviceNamespace)
		{
			if (string.IsNullOrEmpty(serviceNamespace))
			{
				return false;
			}
			return ServiceBusUriHelper.IsBasicLatinNonControlString(serviceNamespace);
		}
	}
}