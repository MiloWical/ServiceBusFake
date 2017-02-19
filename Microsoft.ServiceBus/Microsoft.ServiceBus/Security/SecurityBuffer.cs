using System;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace Microsoft.ServiceBus.Security
{
	internal class SecurityBuffer
	{
		public int size;

		public BufferType type;

		public byte[] token;

		public int offset;

		public SafeHandle unmanagedToken;

		public SecurityBuffer(byte[] data, int offset, int size, BufferType tokentype)
		{
			this.offset = offset;
			this.size = (data == null ? 0 : size);
			this.type = tokentype;
			this.token = data;
		}

		public SecurityBuffer(byte[] data, BufferType tokentype)
		{
			this.size = (data == null ? 0 : (int)data.Length);
			this.type = tokentype;
			this.token = data;
		}

		public SecurityBuffer(int size, BufferType tokentype)
		{
			byte[] numArray;
			this.size = size;
			this.type = tokentype;
			if (size == 0)
			{
				numArray = null;
			}
			else
			{
				numArray = new byte[size];
			}
			this.token = numArray;
		}

		public SecurityBuffer(ChannelBinding channelBinding)
		{
			this.size = channelBinding.Size;
			this.type = BufferType.ChannelBindings;
			this.unmanagedToken = channelBinding;
		}
	}
}