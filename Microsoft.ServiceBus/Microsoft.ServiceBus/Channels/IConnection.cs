using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal interface IConnection
	{
		EventTraceActivity Activity
		{
			get;
		}

		byte[] AsyncReadBuffer
		{
			get;
		}

		int AsyncReadBufferSize
		{
			get;
		}

		TraceEventType ExceptionEventType
		{
			get;
			set;
		}

		IPEndPoint RemoteIPEndPoint
		{
			get;
		}

		void Abort();

		AsyncReadResult BeginRead(int offset, int size, TimeSpan timeout, WaitCallback callback, object state);

		IAsyncResult BeginWrite(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, AsyncCallback callback, object state);

		void Close(TimeSpan timeout);

		int EndRead();

		void EndWrite(IAsyncResult result);

		T GetProperty<T>();

		int Read(byte[] buffer, int offset, int size, TimeSpan timeout);

		void Shutdown(TimeSpan timeout);

		bool Validate(Uri uri);

		void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout);

		void Write(byte[] buffer, int offset, int size, bool immediate, TimeSpan timeout, BufferManager bufferManager);
	}
}