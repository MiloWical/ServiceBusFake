using Microsoft.ServiceBus.Common;
using System;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal sealed class ThrottledPipeStream : PipeStream
	{
		private Semaphore sempahore;

		public ThrottledPipeStream(int throttleCapacity)
		{
			this.Initialize(throttleCapacity);
		}

		public ThrottledPipeStream(int throttleCapacity, TimeSpan naglingDelay) : base(naglingDelay)
		{
			this.Initialize(throttleCapacity);
		}

		private void ChunkDequeued()
		{
			this.sempahore.Release();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				this.sempahore.Close();
			}
		}

		protected override void EnqueueChunk(byte[] chunk)
		{
			WaitHandle[] shutdownEvent = new WaitHandle[] { this.sempahore, base.ShutdownEvent };
			if (WaitHandle.WaitAny(shutdownEvent) == 0)
			{
				base.DataChunksQueue.EnqueueAndDispatch(new NullableArray<byte>(chunk), new Action(this.ChunkDequeued));
			}
		}

		private void Initialize(int throttleCapacity)
		{
			this.sempahore = new Semaphore(throttleCapacity, throttleCapacity);
		}
	}
}