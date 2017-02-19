using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.ServiceBus
{
	internal class FramingOutputPump : Pump
	{
		private readonly static TimeSpan MaxSendTimeAllowed;

		private readonly BufferRead bufferRead;

		private readonly BufferWrite bufferWrite;

		private readonly byte[] inputBuffer;

		private readonly int pingFrequency;

		private readonly object threadLock = new object();

		private readonly object timerLock = new object();

		private readonly Uri uri;

		private readonly EventTraceActivity activity;

		private IOThreadTimer pingTimer;

		private bool abandonWrites;

		private Thread pumpThread;

		static FramingOutputPump()
		{
			FramingOutputPump.MaxSendTimeAllowed = TimeSpan.FromSeconds(30);
		}

		public FramingOutputPump(BufferRead bufferRead, BufferWrite bufferWrite, int pingFrequency, EventTraceActivity activity, Uri uri)
		{
			this.bufferRead = bufferRead;
			this.bufferWrite = bufferWrite;
			this.inputBuffer = new byte[65536];
			this.pingFrequency = pingFrequency;
			this.activity = activity;
			this.uri = uri;
		}

		public override IAsyncResult BeginRunPump(AsyncCallback callback, object state)
		{
			if (base.IsRunning)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.AlreadyRunning), null);
			}
			base.IsRunning = true;
			base.Caller = new Pump.PumpAsyncResult(callback, state);
			if (this.pingFrequency > 0)
			{
				this.pingTimer = new IOThreadTimer(new Action<object>(this.WriteIdleFrame), null, false);
				this.pingTimer.Set(this.pingFrequency);
			}
			this.pumpThread = new Thread(new ThreadStart(this.Run))
			{
				IsBackground = true
			};
			this.pumpThread.Start();
			return base.Caller;
		}

		private void CancelPingTimer()
		{
			lock (this.timerLock)
			{
				if (this.pingTimer != null)
				{
					this.pingTimer.Cancel();
					this.pingTimer = null;
				}
			}
		}

		private bool ChangePingTimer(int period)
		{
			bool flag;
			lock (this.timerLock)
			{
				if (this.pingTimer == null)
				{
					return false;
				}
				else
				{
					if (period != -1)
					{
						this.pingTimer.Set(period);
					}
					else
					{
						this.pingTimer.Cancel();
					}
					flag = true;
				}
			}
			return flag;
		}

		private void CheckFramingPumpTime(TimeSpan elapsed)
		{
			if (elapsed > FramingOutputPump.MaxSendTimeAllowed)
			{
				MessagingClientEtwProvider.Provider.WebStreamFramingOuputPumpSlow(this.activity, this.uri.AbsoluteUri, elapsed.ToString());
			}
		}

		private void CheckFramingPumpTimeWithException(Exception exception, TimeSpan elapsed)
		{
			if (elapsed > FramingOutputPump.MaxSendTimeAllowed)
			{
				MessagingClientEtwProvider.Provider.WebStreamFramingOuputPumpSlowException(this.activity, this.uri.AbsoluteUri, elapsed.ToString(), exception.ToStringSlim());
			}
		}

		private void CheckPingTime(TimeSpan elapsed)
		{
			if (elapsed > FramingOutputPump.MaxSendTimeAllowed)
			{
				MessagingClientEtwProvider.Provider.WebStreamFramingOuputPumpPingSlow(this.activity, this.uri.AbsoluteUri, elapsed.ToString());
			}
		}

		private void CheckPingTimeWithException(Exception exception, TimeSpan elapsed)
		{
			if (elapsed > FramingOutputPump.MaxSendTimeAllowed)
			{
				MessagingClientEtwProvider.Provider.WebStreamFramingOuputPumpPingSlowException(this.activity, this.uri.AbsoluteUri, elapsed.ToString(), exception.ToStringSlim());
			}
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				this.CancelPingTimer();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		private void Run()
		{
			bool flag = false;
			try
			{
				Stopwatch stopwatch = new Stopwatch();
				for (int i = this.bufferRead(this.inputBuffer, 2, (int)this.inputBuffer.Length - 2); i > 0; i = this.bufferRead(this.inputBuffer, 2, (int)this.inputBuffer.Length - 2))
				{
					lock (this.threadLock)
					{
						if (!this.abandonWrites)
						{
							try
							{
								byte[] bytes = BitConverter.GetBytes((ushort)i);
								stopwatch.Restart();
								Buffer.BlockCopy(bytes, 0, this.inputBuffer, 0, 2);
								this.CheckFramingPumpTime(stopwatch.Elapsed);
								this.bufferWrite(this.inputBuffer, 0, i + 2);
							}
							catch (Exception exception1)
							{
								Exception exception = exception1;
								if (Fx.IsFatal(exception))
								{
									throw;
								}
								this.abandonWrites = true;
								this.CheckFramingPumpTimeWithException(exception, stopwatch.Elapsed);
								throw;
							}
						}
					}
					if (base.IsClosed)
					{
						break;
					}
					this.pingTimer.Set(this.pingFrequency);
				}
				this.CancelPingTimer();
				flag = true;
				base.SetComplete();
			}
			catch (Exception exception3)
			{
				Exception exception2 = exception3;
				if (Fx.IsFatal(exception2))
				{
					throw;
				}
				MessagingClientEtwProvider.Provider.FramingOuputPumpRunException(this.activity, this.uri.AbsoluteUri, exception2.ToStringSlim());
				this.CancelPingTimer();
				if (!flag)
				{
					base.SetComplete(exception2);
				}
			}
		}

		private void WriteIdleFrame(object timer)
		{
			if (this.ChangePingTimer(-1))
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				try
				{
					lock (this.threadLock)
					{
						if (!this.abandonWrites)
						{
							try
							{
								byte[] bytes = BitConverter.GetBytes((ushort)0);
								this.bufferWrite(bytes, 0, (int)bytes.Length);
								this.CheckPingTime(stopwatch.Elapsed);
							}
							catch (Exception exception1)
							{
								Exception exception = exception1;
								if (Fx.IsFatal(exception))
								{
									throw;
								}
								this.abandonWrites = true;
								this.CheckPingTimeWithException(exception, stopwatch.Elapsed);
								this.CancelPingTimer();
								base.SetComplete(exception);
								throw;
							}
						}
					}
					this.ChangePingTimer(this.pingFrequency);
				}
				catch (Exception exception3)
				{
					Exception exception2 = exception3;
					if (Fx.IsFatal(exception2))
					{
						throw;
					}
					MessagingClientEtwProvider.Provider.FramingOuputPumpPingException(this.activity, this.uri.AbsoluteUri, exception2.ToStringSlim());
					this.CancelPingTimer();
				}
			}
		}
	}
}