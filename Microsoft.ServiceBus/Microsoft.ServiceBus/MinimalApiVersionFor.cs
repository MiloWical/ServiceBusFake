using System;

namespace Microsoft.ServiceBus
{
	internal static class MinimalApiVersionFor
	{
		public static ApiVersion AdmSupport
		{
			get
			{
				return ApiVersion.Eight;
			}
		}

		public static ApiVersion BaiduSupport
		{
			get
			{
				return ApiVersion.Eleven;
			}
		}

		public static ApiVersion DisableNotificationHub
		{
			get
			{
				return ApiVersion.Ten;
			}
		}

		public static ApiVersion InstallationApi
		{
			get
			{
				return ApiVersion.Nine;
			}
		}

		public static ApiVersion NamespacesWithoutACS
		{
			get
			{
				return ApiVersion.Nine;
			}
		}
	}
}