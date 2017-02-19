using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace Microsoft.ServiceBus
{
	internal static class ApiVersionHelper
	{
		public static int OldRuntimeApiVersion;

		public static int CurrentRuntimeApiVersion;

		public static int PartitionedEntityMinimumRuntimeApiVersion;

		public static int VersionSix;

		public static int VersionSeven;

		public static int VersionEight;

		public static int VersionNine;

		public static int VersionTen;

		public static int VersionEleven;

		public static int SubscriptionPartitioningMinimumRuntimeApiVersion;

		public readonly static ApiVersion CurrentApiVersion;

		static ApiVersionHelper()
		{
			ApiVersionHelper.OldRuntimeApiVersion = (int)ApiVersionHelper.GetVersion("2013-07");
			ApiVersionHelper.CurrentRuntimeApiVersion = (int)ApiVersionHelper.GetVersion("2014-09");
			ApiVersionHelper.PartitionedEntityMinimumRuntimeApiVersion = (int)ApiVersionHelper.GetVersion("2013-10");
			ApiVersionHelper.VersionSix = (int)ApiVersionHelper.GetVersion("2013-08");
			ApiVersionHelper.VersionSeven = (int)ApiVersionHelper.GetVersion("2013-10");
			ApiVersionHelper.VersionEight = (int)ApiVersionHelper.GetVersion("2014-01");
			ApiVersionHelper.VersionNine = (int)ApiVersionHelper.GetVersion("2014-05");
			ApiVersionHelper.VersionTen = (int)ApiVersionHelper.GetVersion("2014-08");
			ApiVersionHelper.VersionEleven = (int)ApiVersionHelper.GetVersion("2014-09");
			ApiVersionHelper.SubscriptionPartitioningMinimumRuntimeApiVersion = (int)ApiVersionHelper.GetVersion("2014-01");
			ApiVersionHelper.CurrentApiVersion = ApiVersionHelper.GetVersion("2014-09");
		}

		public static string GetApiVersionQueryString(ApiVersion version)
		{
			string apiVersionString = ApiVersionHelper.GetApiVersionString(version);
			if (string.IsNullOrEmpty(apiVersionString))
			{
				return string.Empty;
			}
			string[] strArrays = new string[] { "api-version", apiVersionString };
			return string.Join("=", strArrays);
		}

		public static string GetApiVersionString(Uri requestUri)
		{
			if (string.IsNullOrEmpty(requestUri.Query))
			{
				return null;
			}
			return HttpUtility.ParseQueryString(requestUri.Query)["api-version"];
		}

		public static string GetApiVersionString(ApiVersion version)
		{
			switch (version)
			{
				case ApiVersion.One:
				{
					return string.Empty;
				}
				case ApiVersion.Two:
				{
					return "2012-03";
				}
				case ApiVersion.Three:
				{
					return "2012-08";
				}
				case ApiVersion.Four:
				{
					return "2013-04";
				}
				case ApiVersion.Five:
				{
					return "2013-07";
				}
				case ApiVersion.Six:
				{
					return "2013-08";
				}
				case ApiVersion.Seven:
				{
					return "2013-10";
				}
				case ApiVersion.Eight:
				{
					return "2014-01";
				}
				case ApiVersion.Nine:
				{
					return "2014-05";
				}
				case ApiVersion.Ten:
				{
					return "2014-08";
				}
				case ApiVersion.Eleven:
				{
					return "2014-09";
				}
			}
			throw new ArgumentException("api-version");
		}

		public static ApiVersion GetClientApiVersion(Uri requestUri)
		{
			return ApiVersionHelper.GetClientApiVersion(requestUri.Query);
		}

		public static ApiVersion GetClientApiVersion(string queryString)
		{
			string item = null;
			if (!string.IsNullOrEmpty(queryString))
			{
				item = HttpUtility.ParseQueryString(queryString)["api-version"];
			}
			return ApiVersionHelper.GetVersion(item);
		}

		public static ApiVersion GetVersion(string version)
		{
			if (string.IsNullOrEmpty(version))
			{
				return ApiVersion.One;
			}
			if (version.Equals("2012-03", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Two;
			}
			if (version.Equals("2012-08", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Three;
			}
			if (version.Equals("2013-04", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Four;
			}
			if (version.Equals("2013-07", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Five;
			}
			if (version.Equals("2013-08", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Six;
			}
			if (version.Equals("2013-10", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Seven;
			}
			if (version.Equals("2014-01", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Eight;
			}
			if (version.Equals("2014-05", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Nine;
			}
			if (version.Equals("2014-08", StringComparison.OrdinalIgnoreCase))
			{
				return ApiVersion.Ten;
			}
			if (!version.Equals("2014-09", StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SRClient.UnknownApiVersion(ApiVersionConstants.SupportedVersions), new object[0]));
			}
			return ApiVersion.Eleven;
		}
	}
}