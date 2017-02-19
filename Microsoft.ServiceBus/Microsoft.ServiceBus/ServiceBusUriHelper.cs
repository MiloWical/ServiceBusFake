using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;

namespace Microsoft.ServiceBus
{
	internal static class ServiceBusUriHelper
	{
		public readonly static Regex SafeBasicLatinUriSegmentExpression;

		public readonly static Regex SafeMessagingEntityNameExpression;

		private readonly static Regex BasicLatinNonControlStringExpression;

		private static bool IsOneboxEnvironment
		{
			get
			{
				return !string.IsNullOrEmpty(RelayEnvironment.RelayPathPrefix);
			}
		}

		static ServiceBusUriHelper()
		{
			ServiceBusUriHelper.SafeBasicLatinUriSegmentExpression = new Regex("^[\\u0020\\u0021\\u0024-\\u002E\\u0030-\\u003B\\u003D\\u0040-\\u005B\\u005D-\\u007D\\u007E]*/?$", RegexOptions.Compiled);
			ServiceBusUriHelper.SafeMessagingEntityNameExpression = new Regex("^[\\w-\\.\\$]*/?$", RegexOptions.Compiled | RegexOptions.ECMAScript);
			ServiceBusUriHelper.BasicLatinNonControlStringExpression = new Regex("^[\\u0020-\\u007E]*$", RegexOptions.Compiled);
		}

		internal static Uri CreateServiceUri(string scheme, string authority, string servicePath)
		{
			return ServiceBusUriHelper.CreateServiceUri(scheme, authority, servicePath, false);
		}

		internal static Uri CreateServiceUri(string scheme, string authority, string servicePath, bool suppressRelayPathPrefix)
		{
			UriBuilder uriBuilder = new UriBuilder(string.Concat("tempscheme://", authority));
			if (uriBuilder.Port == -1)
			{
				uriBuilder.Port = ServiceBusUriHelper.GetSchemePort(scheme);
			}
			uriBuilder.Scheme = scheme;
			uriBuilder.Path = ServiceBusUriHelper.RefinePath(servicePath, suppressRelayPathPrefix);
			return uriBuilder.Uri;
		}

		internal static Uri CreateServiceUri(string scheme, string serviceNamespace, string hostName, string servicePath)
		{
			return ServiceBusUriHelper.CreateServiceUri(scheme, serviceNamespace, hostName, servicePath, false);
		}

		internal static Uri CreateServiceUri(string scheme, string serviceNamespace, string hostName, string servicePath, bool suppressRelayPathPrefix)
		{
			string str;
			if (string.IsNullOrEmpty(serviceNamespace))
			{
				str = hostName;
			}
			else
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { serviceNamespace, hostName };
				str = string.Format(invariantCulture, "{0}.{1}", objArray);
			}
			return ServiceBusUriHelper.CreateServiceUri(scheme, str, servicePath, suppressRelayPathPrefix);
		}

		private static int GetSchemePort(string scheme)
		{
			string str = scheme;
			string str1 = str;
			if (str != null)
			{
				if (str1 == "http")
				{
					return RelayEnvironment.RelayHttpPort;
				}
				if (str1 == "https")
				{
					return RelayEnvironment.RelayHttpsPort;
				}
			}
			return -1;
		}

		internal static bool IsBasicLatinNonControlString(string str)
		{
			return ServiceBusUriHelper.BasicLatinNonControlStringExpression.IsMatch(str);
		}

		internal static bool IsRequestAuthorityIpAddress(Uri requestUri)
		{
			if (null == requestUri)
			{
				return false;
			}
			if (requestUri.HostNameType == UriHostNameType.IPv4)
			{
				return true;
			}
			return requestUri.HostNameType == UriHostNameType.IPv6;
		}

		internal static bool IsSafeBasicLatinUriPath(Uri uri)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			string[] segments = uri.Segments;
			for (int i = 1; i < (int)segments.Length; i++)
			{
				string str = HttpUtility.UrlDecode(segments[i]);
				if (!ServiceBusUriHelper.SafeBasicLatinUriSegmentExpression.IsMatch(str))
				{
					return false;
				}
			}
			return true;
		}

		internal static bool IsSafeMessagingEntityUriPath(Uri uri)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			if (!string.IsNullOrWhiteSpace(uri.Fragment))
			{
				return false;
			}
			if (!string.IsNullOrWhiteSpace(uri.UserInfo))
			{
				return false;
			}
			string[] segments = uri.Segments;
			for (int i = 1; i < (int)segments.Length; i++)
			{
				string str = HttpUtility.UrlDecode(segments[i]);
				if (!ServiceBusUriHelper.SafeMessagingEntityNameExpression.IsMatch(str))
				{
					return false;
				}
			}
			return true;
		}

		internal static bool IsSafeUri(Uri uri)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			string str = HttpUtility.UrlDecode(uri.AbsoluteUri);
			int length = str.Length;
			bool flag = false;
			for (int i = 0; i < length; i++)
			{
				char chr = str[i];
				if (flag || (32 > chr || chr > '~') && (160 > chr || chr > '\uD7FF') && (57344 > chr || chr > '\uFFFD'))
				{
					if (flag || 55296 > chr || chr > '\uDBFF')
					{
						if (!flag || 56320 > chr || chr > '\uDFFF')
						{
							return false;
						}
						flag = false;
					}
					else
					{
						flag = true;
					}
				}
			}
			return !flag;
		}

		internal static string NormalizeUri(Uri uri, bool ensureTrailingSlash = false)
		{
			return ServiceBusUriHelper.NormalizeUri(uri.AbsoluteUri, uri.Scheme, true, false, ensureTrailingSlash);
		}

		internal static string NormalizeUri(string uri, string scheme, bool stripQueryParameters = true, bool stripPath = false, bool ensureTrailingSlash = false)
		{
			UriBuilder uriBuilder = new UriBuilder(uri)
			{
				Scheme = scheme,
				Port = -1,
				Fragment = string.Empty,
				Password = string.Empty,
				UserName = string.Empty
			};
			UriBuilder empty = uriBuilder;
			if (stripPath)
			{
				empty.Path = string.Empty;
			}
			if (stripQueryParameters)
			{
				empty.Query = string.Empty;
			}
			if (ensureTrailingSlash && !empty.Path.EndsWith("/", StringComparison.Ordinal))
			{
				UriBuilder uriBuilder1 = empty;
				uriBuilder1.Path = string.Concat(uriBuilder1.Path, "/");
			}
			return empty.Uri.AbsoluteUri;
		}

		internal static string ParseServiceNamespace(this Uri uri, string expectedHostnameSuffix, bool isReservedSuffixAllowed)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}
			if (expectedHostnameSuffix == null)
			{
				throw new ArgumentNullException("expectedHostnameSuffix");
			}
			string host = uri.Host;
			string str = null;
			if (host.EndsWith(expectedHostnameSuffix, StringComparison.OrdinalIgnoreCase))
			{
				str = host.Replace(expectedHostnameSuffix, string.Empty);
			}
			if (str == null)
			{
				throw new FormatException(SRClient.UnexpedtedURIHostName(uri));
			}
			if (!ServiceBusUriHelper.ServiceBusStringExtension.IsValidServiceNamespace(str, isReservedSuffixAllowed))
			{
				throw new FormatException(SRClient.URIServiceNameSpace(uri));
			}
			return str;
		}

		private static string RefinePath(string path, bool suppressRelayPathPrefix)
		{
			string relayPathPrefix = path;
			if (!ServiceBusUriHelper.IsOneboxEnvironment || suppressRelayPathPrefix)
			{
				return relayPathPrefix;
			}
			if (!string.IsNullOrEmpty(path))
			{
				relayPathPrefix = (path.StartsWith("/", StringComparison.OrdinalIgnoreCase) ? string.Concat(RelayEnvironment.RelayPathPrefix, path) : string.Concat(RelayEnvironment.RelayPathPrefix, "/", path));
			}
			else
			{
				relayPathPrefix = RelayEnvironment.RelayPathPrefix;
			}
			return relayPathPrefix;
		}

		internal static string RemoveServicePathPrefix(string path)
		{
			string str;
			ServiceBusUriHelper.TryRemoveServicePathPrefix(path, out str, true);
			return str;
		}

		internal static bool TryRemoveServicePathPrefix(string path, out string refinedPath)
		{
			return ServiceBusUriHelper.TryRemoveServicePathPrefix(path, out refinedPath, false);
		}

		private static bool TryRemoveServicePathPrefix(string path, out string refinedPath, bool throwOnFailure)
		{
			refinedPath = path;
			if (!ServiceBusUriHelper.IsOneboxEnvironment)
			{
				return true;
			}
			if (!path.StartsWith(RelayEnvironment.RelayPathPrefix, StringComparison.OrdinalIgnoreCase))
			{
				if (throwOnFailure)
				{
					throw new ArgumentException(SRClient.InputURIPath(path));
				}
				return false;
			}
			refinedPath = path.Substring(RelayEnvironment.RelayPathPrefix.Length);
			return true;
		}

		private static class ServiceBusStringExtension
		{
			private const int MinServiceNamespaceLength = 6;

			private const int MaxServiceNamespaceLength = 50;

			private readonly static string ServiceNamespacePattern;

			private readonly static Regex ServiceNamespaceRegex;

			private static IEnumerable<string> reservedHostnameSuffixes;

			static ServiceBusStringExtension()
			{
				object[] objArray = new object[] { "^[a-zA-Z][a-zA-Z0-9-]{", 4, ",", 48, "}[a-zA-Z0-9]$" };
				ServiceBusUriHelper.ServiceBusStringExtension.ServiceNamespacePattern = string.Concat(objArray);
				ServiceBusUriHelper.ServiceBusStringExtension.ServiceNamespaceRegex = new Regex(ServiceBusUriHelper.ServiceBusStringExtension.ServiceNamespacePattern, RegexOptions.Compiled);
				ServiceBusUriHelper.ServiceBusStringExtension.reservedHostnameSuffixes = new string[] { "-sb", "-mgmt", "-sb-mgmt" };
			}

			private static bool HasReservedSuffix(string serviceNamespaceCandidate, out string suffix)
			{
				suffix = ServiceBusUriHelper.ServiceBusStringExtension.reservedHostnameSuffixes.FirstOrDefault<string>((string s) => serviceNamespaceCandidate.EndsWith(s, StringComparison.Ordinal));
				return suffix != null;
			}

			public static bool IsValidServiceNamespace(string serviceNamespaceCandidate, bool isReservedSuffixAllowed)
			{
				string str;
				if (serviceNamespaceCandidate == null)
				{
					return false;
				}
				bool flag = ServiceBusUriHelper.ServiceBusStringExtension.HasReservedSuffix(serviceNamespaceCandidate, out str);
				if (!isReservedSuffixAllowed && flag)
				{
					return false;
				}
				serviceNamespaceCandidate = (flag ? ServiceBusUriHelper.ServiceBusStringExtension.StripReservedSuffix(serviceNamespaceCandidate, str) : serviceNamespaceCandidate);
				if (!ServiceBusUriHelper.ServiceBusStringExtension.ServiceNamespaceRegex.IsMatch(serviceNamespaceCandidate))
				{
					return false;
				}
				if (serviceNamespaceCandidate.StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				return true;
			}

			private static string StripReservedSuffix(string serviceNamespaceCandidate, string suffix)
			{
				return serviceNamespaceCandidate.Substring(0, serviceNamespaceCandidate.Length - suffix.Length);
			}
		}
	}
}