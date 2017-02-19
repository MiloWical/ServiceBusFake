using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class ClientDuplexEncoder : SessionEncoder
	{
		public static byte[] ModeBytes;

		static ClientDuplexEncoder()
		{
			ClientDuplexEncoder.ModeBytes = new byte[] { 0, 1, 0, 1, 2 };
		}

		private ClientDuplexEncoder()
		{
		}
	}
}