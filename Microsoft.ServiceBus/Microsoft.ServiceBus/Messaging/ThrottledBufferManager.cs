using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class ThrottledBufferManager
	{
		private const int DefaultMaxSize = 104857600;

		private static ThrottledBufferManager instance;

		private static object syncLock;

		private InternalBufferManager bufferManager;

		private long currentAllocationSize;

		private InternalBufferManager ProxyBufferManager
		{
			get;
			set;
		}

		static ThrottledBufferManager()
		{
			ThrottledBufferManager.syncLock = new object();
		}

		private ThrottledBufferManager(int maxSize)
		{
			this.bufferManager = InternalBufferManager.Create((long)maxSize, 2147483647, false);
			this.ProxyBufferManager = new ThrottledBufferManager.WrappedBufferManager(this);
		}

		public static InternalBufferManager GetBufferManager()
		{
			return ThrottledBufferManager.GetThrottledBufferManager().ProxyBufferManager;
		}

		public static ThrottledBufferManager GetThrottledBufferManager()
		{
			return ThrottledBufferManager.GetThrottledBufferManager(104857600);
		}

		public static ThrottledBufferManager GetThrottledBufferManager(int maxSize)
		{
			if (ThrottledBufferManager.instance == null)
			{
				lock (ThrottledBufferManager.syncLock)
				{
					if (ThrottledBufferManager.instance == null)
					{
						ThrottledBufferManager.instance = new ThrottledBufferManager(maxSize);
					}
				}
			}
			return ThrottledBufferManager.instance;
		}

		public void ReturnBuffer(byte[] buffer)
		{
			Interlocked.Add(ref this.currentAllocationSize, (long)(-(int)buffer.Length));
			this.bufferManager.ReturnBuffer(buffer);
		}

		public bool TryTakeBuffer(int bufferSize, out byte[] buffer)
		{
			buffer = this.bufferManager.TakeBuffer(bufferSize);
			Interlocked.Add(ref this.currentAllocationSize, (long)((int)buffer.Length));
			return true;
		}

		private sealed class WrappedBufferManager : InternalBufferManager
		{
			private ThrottledBufferManager ThrottledBufferManager
			{
				get;
				set;
			}

			public WrappedBufferManager(ThrottledBufferManager bufferManager)
			{
				this.ThrottledBufferManager = bufferManager;
			}

			public override void Clear()
			{
				throw FxTrace.Exception.AsError(new NotImplementedException(), null);
			}

			public override void ReturnBuffer(byte[] buffer)
			{
				this.ThrottledBufferManager.ReturnBuffer(buffer);
			}

			public override byte[] TakeBuffer(int bufferSize)
			{
				byte[] numArray;
				this.ThrottledBufferManager.TryTakeBuffer(bufferSize, out numArray);
				return numArray;
			}
		}
	}
}