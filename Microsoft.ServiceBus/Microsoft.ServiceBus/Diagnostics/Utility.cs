using Microsoft.ServiceBus.Common;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class Utility
	{
		private Microsoft.ServiceBus.Diagnostics.ExceptionUtility exceptionUtility;

		private Microsoft.ServiceBus.Diagnostics.ExceptionUtility ExceptionUtility
		{
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get
			{
				return this.exceptionUtility;
			}
		}

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.Utility instead")]
		internal Utility(Microsoft.ServiceBus.Diagnostics.ExceptionUtility exceptionUtility)
		{
			this.exceptionUtility = exceptionUtility;
		}

		internal byte[] AllocateByteArray(int size)
		{
			return Fx.AllocateByteArray(size);
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal virtual bool CallHandler(Exception exception)
		{
			return false;
		}

		internal static void CloseInvalidOutCriticalHandle(CriticalHandle handle)
		{
			if (handle != null)
			{
				handle.SetHandleAsInvalid();
			}
		}

		internal static void CloseInvalidOutSafeHandle(SafeHandle handle)
		{
			if (handle != null)
			{
				handle.SetHandleAsInvalid();
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private bool HandleAtThreadBase(Exception exception)
		{
			bool flag;
			if (Microsoft.ServiceBus.Diagnostics.ExceptionUtility.IsInfrastructureException(exception))
			{
				this.TraceExceptionNoThrow(exception, TraceEventType.Warning);
				return false;
			}
			this.TraceExceptionNoThrow(exception, TraceEventType.Critical);
			try
			{
				flag = this.CallHandler(exception);
			}
			catch (Exception exception1)
			{
				this.TraceExceptionNoThrow(exception1, TraceEventType.Error);
				return false;
			}
			return flag;
		}

		internal AsyncCallback ThunkCallback(AsyncCallback callback)
		{
			return (new Utility.AsyncThunk(callback, this)).ThunkFrame;
		}

		internal TimerCallback ThunkCallback(TimerCallback callback)
		{
			return (new Utility.TimerThunk(callback, this)).ThunkFrame;
		}

		internal WaitOrTimerCallback ThunkCallback(WaitOrTimerCallback callback)
		{
			return (new Utility.WaitOrTimerThunk(callback, this)).ThunkFrame;
		}

		internal IOCompletionCallback ThunkCallback(IOCompletionCallback callback)
		{
			return (new Utility.IOCompletionThunk(callback, this)).ThunkFrame;
		}

		internal WaitCallback ThunkCallback(WaitCallback callback)
		{
			return (new Utility.WaitThunk(callback, this)).ThunkFrame;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private void TraceExceptionNoThrow(Exception exception, TraceEventType eventType)
		{
			try
			{
				this.ExceptionUtility.TraceHandledException(exception, eventType);
			}
			catch
			{
			}
		}

		private sealed class AsyncThunk : Utility.Thunk<AsyncCallback>
		{
			public AsyncCallback ThunkFrame
			{
				get
				{
					return new AsyncCallback(this.UnhandledExceptionFrame);
				}
			}

			public AsyncThunk(AsyncCallback callback, Utility utility) : base(callback, utility)
			{
			}

			private void UnhandledExceptionFrame(IAsyncResult result)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.callback(result);
				}
				catch (Exception exception)
				{
					if (!this.utility.HandleAtThreadBase(exception))
					{
						throw;
					}
				}
			}
		}

		private sealed class IOCompletionThunk : Utility.Thunk<IOCompletionCallback>
		{
			public IOCompletionCallback ThunkFrame
			{
				get
				{
					return new IOCompletionCallback(this.UnhandledExceptionFrame);
				}
			}

			public IOCompletionThunk(IOCompletionCallback callback, Utility utility) : base(callback, utility)
			{
			}

			private unsafe void UnhandledExceptionFrame(uint error, uint bytesRead, NativeOverlapped* nativeOverlapped)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.callback(error, bytesRead, nativeOverlapped);
				}
				catch (Exception exception)
				{
					if (!this.utility.HandleAtThreadBase(exception))
					{
						throw;
					}
				}
			}
		}

		private class Thunk<T>
		where T : class
		{
			protected T callback;

			protected Utility utility;

			public Thunk(T callback, Utility utility)
			{
				this.callback = callback;
				this.utility = utility;
			}
		}

		private sealed class TimerThunk : Utility.Thunk<TimerCallback>
		{
			public TimerCallback ThunkFrame
			{
				get
				{
					return new TimerCallback(this.UnhandledExceptionFrame);
				}
			}

			public TimerThunk(TimerCallback callback, Utility utility) : base(callback, utility)
			{
			}

			private void UnhandledExceptionFrame(object state)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.callback(state);
				}
				catch (Exception exception)
				{
					if (!this.utility.HandleAtThreadBase(exception))
					{
						throw;
					}
				}
			}
		}

		private sealed class WaitOrTimerThunk : Utility.Thunk<WaitOrTimerCallback>
		{
			public WaitOrTimerCallback ThunkFrame
			{
				get
				{
					return new WaitOrTimerCallback(this.UnhandledExceptionFrame);
				}
			}

			public WaitOrTimerThunk(WaitOrTimerCallback callback, Utility utility) : base(callback, utility)
			{
			}

			private void UnhandledExceptionFrame(object state, bool timedOut)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.callback(state, timedOut);
				}
				catch (Exception exception)
				{
					if (!this.utility.HandleAtThreadBase(exception))
					{
						throw;
					}
				}
			}
		}

		private sealed class WaitThunk : Utility.Thunk<WaitCallback>
		{
			public WaitCallback ThunkFrame
			{
				get
				{
					return new WaitCallback(this.UnhandledExceptionFrame);
				}
			}

			public WaitThunk(WaitCallback callback, Utility utility) : base(callback, utility)
			{
			}

			private void UnhandledExceptionFrame(object state)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.callback(state);
				}
				catch (Exception exception)
				{
					if (!this.utility.HandleAtThreadBase(exception))
					{
						throw;
					}
				}
			}
		}
	}
}