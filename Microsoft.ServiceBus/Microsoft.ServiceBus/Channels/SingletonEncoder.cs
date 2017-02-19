using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class SingletonEncoder
	{
		public static byte[] EnvelopeStartBytes;

		public static byte[] EnvelopeEndBytes;

		public static byte[] EnvelopeEndFramingEndBytes;

		public static byte[] EndBytes;

		static SingletonEncoder()
		{
			SingletonEncoder.EnvelopeStartBytes = new byte[] { 5 };
			SingletonEncoder.EnvelopeEndBytes = new byte[1];
			SingletonEncoder.EnvelopeEndFramingEndBytes = new byte[] { 0, 7 };
			SingletonEncoder.EndBytes = new byte[] { 7 };
		}

		protected SingletonEncoder()
		{
		}

		public static ArraySegment<byte> EncodeMessageFrame(ArraySegment<byte> messageFrame)
		{
			int encodedSize = IntEncoder.GetEncodedSize(messageFrame.Count);
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
			IntEncoder.Encode(messageFrame.Count, array, offset);
			return new ArraySegment<byte>(array, offset, messageFrame.Count + encodedSize);
		}
	}
}