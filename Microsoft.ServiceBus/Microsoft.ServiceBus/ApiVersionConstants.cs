using System;

namespace Microsoft.ServiceBus
{
	internal class ApiVersionConstants
	{
		public const string Name = "api-version";

		public const string VersionTwo = "2012-03";

		public const string VersionThree = "2012-08";

		public const string VersionFour = "2013-04";

		public const string VersionFive = "2013-07";

		public const string VersionSix = "2013-08";

		public const string VersionSeven = "2013-10";

		public const string VersionEight = "2014-01";

		public const string VersionNine = "2014-05";

		public const string VersionTen = "2014-08";

		public const string VersionEleven = "2014-09";

		public const string MaxSupportedApiVersion = "2014-09";

		public const string OldRuntimeVersion = "2013-07";

		public const string PartitionedEntityMinimumRuntimeApiVersionText = "2013-10";

		public const string SubscriptionPartitioningMinimumRuntimeApiVersionText = "2014-01";

		public readonly static string SupportedVersions;

		static ApiVersionConstants()
		{
			string[] strArrays = new string[] { "2012-03", "2012-08", "2013-04", "2013-07", "2013-08", "2013-10", "2014-01", "2014-05", "2014-08", "2014-09" };
			ApiVersionConstants.SupportedVersions = string.Join(",", strArrays);
		}

		public ApiVersionConstants()
		{
		}
	}
}