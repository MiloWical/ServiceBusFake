using Microsoft.ServiceBus.Common;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal abstract class PumpStream : Stream
	{
		protected Stream inputStream;

		protected Stream outputStream;

		protected Pump pump;

		private bool disposed;

		public Action PumpCompletedEvent;

		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		public override bool CanTimeout
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
				throw Fx.Exception.AsError(new NotSupportedException(), null);
			}
		}

		public override long Position
		{
			get
			{
				throw Fx.Exception.AsError(new NotSupportedException(), null);
			}
			set
			{
				throw Fx.Exception.AsError(new NotSupportedException(), null);
			}
		}

		protected PumpStream(Stream input, Stream output, Pump pump)
		{
			this.inputStream = input;
			this.outputStream = output;
			this.pump = pump;
		}

		public IAsyncResult BeginRunPump()
		{
			return this.BeginRunPump(new AsyncCallback(this.OnPumpComplete), null);
		}

		public IAsyncResult BeginRunPump(AsyncCallback callback, object state)
		{
			return this.pump.BeginRunPump(callback, state);
		}

		protected override void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.disposed = true;
				try
				{
					if (disposing)
					{
						if (this.pump != null)
						{
							this.pump.Dispose();
							this.pump = null;
						}
						if (this.outputStream != null)
						{
							this.outputStream.Close();
							this.outputStream = null;
						}
						if (this.inputStream != null)
						{
							Stream stream = this.inputStream;
							IOThreadScheduler.ScheduleCallbackNoFlow((object o) => {
								try
								{
									((Stream)o).Close();
								}
								catch (Exception exception1)
								{
									Exception exception = exception1;
									if (Fx.IsFatal(exception))
									{
										throw;
									}
									Fx.Exception.TraceHandled(exception, "PumpStream.Dispose", null);
								}
							}, stream);
							this.inputStream = null;
						}
					}
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		public void EndRunPump(IAsyncResult result)
		{
			this.OnPumpComplete(result);
		}

		private void OnPumpComplete(IAsyncResult result)
		{
			try
			{
				try
				{
					Pump.EndRunPump(result);
					if (this.outputStream != null)
					{
						this.outputStream.Flush();
					}
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					Fx.Exception.TraceHandled(exception, "PumpStream.OnPumpComplete", null);
				}
			}
			finally
			{
				Action pumpCompletedEvent = this.PumpCompletedEvent;
				if (pumpCompletedEvent != null)
				{
					pumpCompletedEvent();
				}
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw Fx.Exception.AsError(new NotSupportedException(), null);
		}

		public override void SetLength(long value)
		{
			throw Fx.Exception.AsError(new NotSupportedException(), null);
		}

		public abstract void Shutdown();

		protected void WaitForPumpToEnd()
		{
			if (this.pump != null)
			{
				this.pump.WaitForCompletion();
			}
		}
	}
}