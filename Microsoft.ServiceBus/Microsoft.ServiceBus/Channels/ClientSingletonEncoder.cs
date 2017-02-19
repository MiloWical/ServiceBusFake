using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class ClientSingletonEncoder : SingletonEncoder
	{
		public static byte[] PreambleEndBytes;

		public static byte[] ModeBytes;

		static ClientSingletonEncoder()
		{
			ClientSingletonEncoder.PreambleEndBytes = new byte[] { 12 };
			ClientSingletonEncoder.ModeBytes = new byte[] { 0, 1, 0, 1, 1 };
		}

		private ClientSingletonEncoder()
		{
		}

		public static int CalcStartSize(EncodedVia via, EncodedContentType contentType)
		{
			return (int)via.EncodedBytes.Length + (int)contentType.EncodedBytes.Length;
		}

		public static void EncodeStart(byte[] buffer, int offset, EncodedVia via, EncodedContentType contentType)
		{
			Buffer.BlockCopy(via.EncodedBytes, 0, buffer, offset, (int)via.EncodedBytes.Length);
			Buffer.BlockCopy(contentType.EncodedBytes, 0, buffer, offset + (int)via.EncodedBytes.Length, (int)contentType.EncodedBytes.Length);
		}
	}
}