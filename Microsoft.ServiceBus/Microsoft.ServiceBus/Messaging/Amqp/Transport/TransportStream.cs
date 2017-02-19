using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
	internal sealed class TransportStream : Stream
	{
		private readonly static Action<TransportAsyncCallbackArgs> onIOComplete;

		private readonly TransportBase transport;

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override long Length
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		public override long Position
		{
			get
			{
				throw new InvalidOperationException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		static TransportStream()
		{
			TransportStream.onIOComplete = new Action<TransportAsyncCallbackArgs>(TransportStream.OnIOComplete);
		}

		public TransportStream(TransportBase transport)
		{
			this.transport = transport;
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs();
			transportAsyncCallbackArg.SetBuffer(buffer, offset, count);
			transportAsyncCallbackArg.CompletedCallback = TransportStream.onIOComplete;
			transportAsyncCallbackArg.UserToken = this;
			transportAsyncCallbackArg.UserToken2 = Tuple.Create<AsyncCallback, object>(callback, state);
			if (!this.transport.ReadAsync(transportAsyncCallbackArg))
			{
				this.CompleteOperation(transportAsyncCallbackArg);
			}
			return transportAsyncCallbackArg;
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			TransportAsyncCallbackArgs transportAsyncCallbackArg = new TransportAsyncCallbackArgs();
			transportAsyncCallbackArg.SetBuffer(buffer, offset, count);
			transportAsyncCallbackArg.CompletedCallback = TransportStream.onIOComplete;
			transportAsyncCallbackArg.UserToken = this;
			transportAsyncCallbackArg.UserToken2 = Tuple.Create<AsyncCallback, object>(callback, state);
			if (!this.transport.WriteAsync(transportAsyncCallbackArg))
			{
				this.CompleteOperation(transportAsyncCallbackArg);
			}
			return transportAsyncCallbackArg;
		}

		private void CompleteOperation(TransportAsyncCallbackArgs args)
		{
			Tuple<AsyncCallback, object> userToken2 = (Tuple<AsyncCallback, object>)args.UserToken2;
			AsyncCallback item1 = userToken2.Item1;
			args.UserToken = userToken2.Item2;
			if (item1 != null)
			{
				item1(args);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.transport.SafeClose();
			}
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			TransportAsyncCallbackArgs transportAsyncCallbackArg = (TransportAsyncCallbackArgs)asyncResult;
			if (transportAsyncCallbackArg.Exception != null)
			{
				throw transportAsyncCallbackArg.Exception;
			}
			return transportAsyncCallbackArg.BytesTransfered;
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			TransportAsyncCallbackArgs transportAsyncCallbackArg = (TransportAsyncCallbackArgs)asyncResult;
			if (transportAsyncCallbackArg.Exception != null)
			{
				throw transportAsyncCallbackArg.Exception;
			}
		}

		public override void Flush()
		{
			throw new InvalidOperationException();
		}

		private static void OnIOComplete(TransportAsyncCallbackArgs args)
		{
			((TransportStream)args.UserToken).CompleteOperation(args);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new InvalidOperationException();
		}

		public override void SetLength(long value)
		{
			throw new InvalidOperationException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new InvalidOperationException();
		}
	}
}