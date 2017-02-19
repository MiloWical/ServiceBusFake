using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class SessionEncoder
	{
		public const int MaxMessageFrameSize = 6;

		public static byte[] PreambleEndBytes;

		public static byte[] EndBytes;

		static SessionEncoder()
		{
			SessionEncoder.PreambleEndBytes = new byte[] { 12 };
			SessionEncoder.EndBytes = new byte[] { 7 };
		}

		protected SessionEncoder()
		{
		}

		public static int CalcStartSize(EncodedVia via, EncodedContentType contentType)
		{
			return (int)via.EncodedBytes.Length + (int)contentType.EncodedBytes.Length;
		}

		public static ArraySegment<byte> EncodeMessageFrame(ArraySegment<byte> messageFrame)
		{
			int encodedSize = 1 + IntEncoder.GetEncodedSize(messageFrame.Count);
			int offset = messageFrame.Offset - encodedSize;
			if (offset < 0)
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				object obj = messageFrame.Offset;
				string spaceNeededExceedsMessageFrameOffset = Resources.SpaceNeededExceedsMessageFrameOffset;
				object[] objArray = new object[] { encodedSize };
				throw exceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("messageFrame.Offset", obj, Microsoft.ServiceBus.SR.GetString(spaceNeededExceedsMessageFrameOffset, objArray)));
			}
			byte[] array = messageFrame.Array;
			int num = offset;
			offset = num + 1;
			array[num] = 6;
			IntEncoder.Encode(messageFrame.Count, array, offset);
			return new ArraySegment<byte>(array, messageFrame.Offset - encodedSize, messageFrame.Count + encodedSize);
		}

		public static void EncodeStart(byte[] buffer, int offset, EncodedVia via, EncodedContentType contentType)
		{
			Buffer.BlockCopy(via.EncodedBytes, 0, buffer, offset, (int)via.EncodedBytes.Length);
			Buffer.BlockCopy(contentType.EncodedBytes, 0, buffer, offset + (int)via.EncodedBytes.Length, (int)contentType.EncodedBytes.Length);
		}
	}
}