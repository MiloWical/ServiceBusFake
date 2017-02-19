using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class FramingInputPump : Pump
	{
		private readonly static TimeSpan AllowedTimeBetweenReadingBytes;

		private readonly BufferRead bufferRead;

		private readonly BufferWrite bufferWrite;

		private readonly Action bufferDone;

		private readonly byte[] inputBuffer;

		private readonly byte[] preambleBuffer;

		private readonly Uri uri;

		private Thread pumpThread;

		private EventTraceActivity Activity
		{
			get;
			set;
		}

		static FramingInputPump()
		{
			FramingInputPump.AllowedTimeBetweenReadingBytes = TimeSpan.FromSeconds(30);
		}

		public FramingInputPump(BufferRead bufferRead, BufferWrite bufferWrite, Action bufferDone, EventTraceActivity activity, Uri uri)
		{
			this.bufferRead = bufferRead;
			this.bufferWrite = bufferWrite;
			this.bufferDone = bufferDone;
			this.inputBuffer = new byte[65536];
			this.preambleBuffer = new byte[2];
			this.Activity = activity;
			this.uri = uri;
		}

		public override IAsyncResult BeginRunPump(AsyncCallback callback, object state)
		{
			if (base.IsRunning)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.AlreadyRunning), this.Activity);
			}
			base.IsRunning = true;
			base.Caller = new Pump.PumpAsyncResult(callback, state);
			this.pumpThread = new Thread(new ThreadStart(this.Run))
			{
				IsBackground = true
			};
			this.pumpThread.Start();
			return base.Caller;
		}

		private void CheckDataTime(Stopwatch watch, int preambleBytesRead)
		{
			TimeSpan elapsed = watch.Elapsed;
			if (elapsed > FramingInputPump.AllowedTimeBetweenReadingBytes)
			{
				MessagingClientEtwProvider.Provider.WebStreamFramingInputPumpSlowRead(this.Activity, this.uri.AbsoluteUri, preambleBytesRead, elapsed.ToString());
			}
			if (preambleBytesRead > 0)
			{
				watch.Reset();
			}
		}

		private void CheckExceptionTime(TimeSpan elapsed, Exception exception)
		{
			if (elapsed > FramingInputPump.AllowedTimeBetweenReadingBytes)
			{
				MessagingClientEtwProvider.Provider.WebStreamFramingInputPumpSlowReadWithException(this.Activity, this.uri.AbsoluteUri, elapsed.ToString(), exception.ToString());
			}
		}

		private void Run()
		{
			bool flag = false;
			bool flag1 = false;
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				do
				{
					int length = this.bufferRead(this.preambleBuffer, 0, (int)this.preambleBuffer.Length);
					this.CheckDataTime(stopwatch, length);
					if (length == 1)
					{
						length = length + this.bufferRead(this.preambleBuffer, 1, 1);
						this.CheckDataTime(stopwatch, length);
					}
					if (length != (int)this.preambleBuffer.Length)
					{
						break;
					}
					ushort num = BitConverter.ToUInt16(this.preambleBuffer, 0);
					if (num <= 0)
					{
						continue;
					}
					int num1 = 0;
					do
					{
						int num2 = this.bufferRead(this.inputBuffer, num1, num - num1);
						this.CheckDataTime(stopwatch, num2);
						if (num2 <= 0)
						{
							break;
						}
						num1 = num1 + num2;
					}
					while (num1 < num);
					this.bufferWrite(this.inputBuffer, 0, num1);
				}
				while (!base.IsClosed);
				flag = true;
				this.bufferDone();
				flag1 = true;
				base.SetComplete();
			}
			catch (Exception exception3)
			{
				Exception exception = exception3;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				Fx.Exception.TraceHandled(exception, string.Concat("FramingInputPump.Run uri: ", this.uri.AbsoluteUri), this.Activity);
				this.CheckExceptionTime(stopwatch.Elapsed, exception);
				try
				{
					try
					{
						if (!flag)
						{
							this.bufferDone();
						}
					}
					catch (Exception exception2)
					{
						Exception exception1 = exception2;
						if (Fx.IsFatal(exception1))
						{
							throw;
						}
						Fx.Exception.TraceHandled(exception1, string.Concat("FramingInputPump.Run(bufferDone) uri: ", this.uri.AbsoluteUri), this.Activity);
					}
				}
				finally
				{
					if (!flag1)
					{
						base.SetComplete(exception);
					}
				}
			}
		}
	}
}