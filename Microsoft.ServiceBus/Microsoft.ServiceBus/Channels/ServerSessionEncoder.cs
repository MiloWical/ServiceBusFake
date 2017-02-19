using System;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ServerSessionEncoder : SessionEncoder
	{
		public static byte[] AckResponseBytes;

		public static byte[] UpgradeResponseBytes;

		static ServerSessionEncoder()
		{
			ServerSessionEncoder.AckResponseBytes = new byte[] { 11 };
			ServerSessionEncoder.UpgradeResponseBytes = new byte[] { 10 };
		}

		protected ServerSessionEncoder()
		{
		}
	}
}