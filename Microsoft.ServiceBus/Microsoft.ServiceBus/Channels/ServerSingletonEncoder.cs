using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerSingletonEncoder : SingletonEncoder
	{
		public static byte[] AckResponseBytes;

		public static byte[] UpgradeResponseBytes;

		static ServerSingletonEncoder()
		{
			ServerSingletonEncoder.AckResponseBytes = new byte[] { 11 };
			ServerSingletonEncoder.UpgradeResponseBytes = new byte[] { 10 };
		}

		private ServerSingletonEncoder()
		{
		}
	}
}