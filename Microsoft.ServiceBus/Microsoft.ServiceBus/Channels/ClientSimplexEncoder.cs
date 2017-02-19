using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class ClientSimplexEncoder : SessionEncoder
	{
		public static byte[] ModeBytes;

		static ClientSimplexEncoder()
		{
			ClientSimplexEncoder.ModeBytes = new byte[] { 0, 1, 0, 1, 3 };
		}

		private ClientSimplexEncoder()
		{
		}
	}
}