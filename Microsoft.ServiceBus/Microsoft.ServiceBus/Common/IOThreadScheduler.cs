using System;
using System.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	internal class IOThreadScheduler
	{
		private const int MaximumCapacity = 32768;

		private static IOThreadScheduler current;

		private readonly IOThreadScheduler.ScheduledOverlapped overlapped;

		[SecurityCritical]
		private readonly IOThreadScheduler.Slot[] slots;

		[SecurityCritical]
		private readonly IOThreadScheduler.Slot[] slotsLowPri;

		private int headTail = -131072;

		private int headTailLowPri = -65536;

		private int SlotMask
		{
			[SecurityCritical]
			get
			{
				return (int)this.slots.Length - 1;
			}
		}

		private int SlotMaskLowPri
		{
			[SecurityCritical]
			get
			{
				return (int)this.slotsLowPri.Length - 1;
			}
		}

		static IOThreadScheduler()
		{
			IOThreadScheduler.current = new IOThreadScheduler(32, 32);
		}

		private IOThreadScheduler(int capacity, int capacityLowPri)
		{
			this.slots = new IOThreadScheduler.Slot[capacity];
			this.slotsLowPri = new IOThreadScheduler.Slot[capacityLowPri];
			this.overlapped = new IOThreadScheduler.ScheduledOverlapped();
		}

		private void Cleanup()
		{
			if (this.overlapped != null)
			{
				this.overlapped.Cleanup();
			}
		}

		[SecurityCritical]
		private void CompletionCallback(out Action<object> callback, out object state)
		{
			int num;
			int num1 = this.headTail;
			while (true)
			{
				bool flag = IOThreadScheduler.Bits.Count(num1) == 0;
				if (flag)
				{
					num = this.headTailLowPri;
					while (IOThreadScheduler.Bits.CountNoIdle(num) != 0)
					{
						int num2 = num;
						int num3 = Interlocked.CompareExchange(ref this.headTailLowPri, IOThreadScheduler.Bits.IncrementLo(num), num);
						num = num3;
						if (num2 != num3)
						{
							continue;
						}
						this.overlapped.Post(this);
						this.slotsLowPri[num & this.SlotMaskLowPri].DequeueWorkItem(out callback, out state);
						return;
					}
				}
				int num4 = num1;
				int num5 = Interlocked.CompareExchange(ref this.headTail, IOThreadScheduler.Bits.IncrementLo(num1), num1);
				num1 = num5;
				if (num4 == num5)
				{
					if (!flag)
					{
						this.overlapped.Post(this);
						this.slots[num1 & this.SlotMask].DequeueWorkItem(out callback, out state);
						return;
					}
					num = this.headTailLowPri;
					if (IOThreadScheduler.Bits.CountNoIdle(num) == 0)
					{
						break;
					}
					num1 = IOThreadScheduler.Bits.IncrementLo(num1);
					if (num1 != Interlocked.CompareExchange(ref this.headTail, num1 + 65536, num1))
					{
						break;
					}
					num1 = num1 + 65536;
				}
			}
			callback = null;
			state = null;
		}

		~IOThreadScheduler()
		{
			if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload())
			{
				this.Cleanup();
			}
		}

		[SecurityCritical]
		private bool ScheduleCallbackHelper(Action<object> callback, object state)
		{
			bool flag;
			int num = Interlocked.Add(ref this.headTail, 65536);
			bool flag1 = IOThreadScheduler.Bits.Count(num) == 0;
			if (flag1)
			{
				num = Interlocked.Add(ref this.headTail, 65536);
			}
			if (IOThreadScheduler.Bits.Count(num) == -1)
			{
				throw Fx.AssertAndThrowFatal("Head/Tail overflow!");
			}
			bool flag2 = this.slots[num >> 16 & this.SlotMask].TryEnqueueWorkItem(callback, state, out flag);
			if (flag)
			{
				IOThreadScheduler oThreadScheduler = new IOThreadScheduler(Math.Min((int)this.slots.Length * 2, 32768), (int)this.slotsLowPri.Length);
				Interlocked.CompareExchange<IOThreadScheduler>(ref IOThreadScheduler.current, oThreadScheduler, this);
			}
			if (flag1)
			{
				this.overlapped.Post(this);
			}
			return flag2;
		}

		[SecurityCritical]
		private bool ScheduleCallbackLowPriHelper(Action<object> callback, object state)
		{
			bool flag;
			int num = Interlocked.Add(ref this.headTailLowPri, 65536);
			bool flag1 = false;
			if (IOThreadScheduler.Bits.CountNoIdle(num) == 1)
			{
				int num1 = this.headTail;
				if (IOThreadScheduler.Bits.Count(num1) == -1 && num1 == Interlocked.CompareExchange(ref this.headTail, num1 + 65536, num1))
				{
					flag1 = true;
				}
			}
			if (IOThreadScheduler.Bits.CountNoIdle(num) == 0)
			{
				throw Fx.AssertAndThrowFatal("Low-priority Head/Tail overflow!");
			}
			bool flag2 = this.slotsLowPri[num >> 16 & this.SlotMaskLowPri].TryEnqueueWorkItem(callback, state, out flag);
			if (flag)
			{
				IOThreadScheduler oThreadScheduler = new IOThreadScheduler((int)this.slots.Length, Math.Min((int)this.slotsLowPri.Length * 2, 32768));
				Interlocked.CompareExchange<IOThreadScheduler>(ref IOThreadScheduler.current, oThreadScheduler, this);
			}
			if (flag1)
			{
				this.overlapped.Post(this);
			}
			return flag2;
		}

		[SecurityCritical]
		public static void ScheduleCallbackLowPriNoFlow(Action<object> callback, object state)
		{
			if (callback == null)
			{
				throw Fx.Exception.ArgumentNull("callback");
			}
			bool flag = false;
			while (!flag)
			{
				try
				{
				}
				finally
				{
					flag = IOThreadScheduler.current.ScheduleCallbackLowPriHelper(callback, state);
				}
			}
		}

		[SecurityCritical]
		public static void ScheduleCallbackNoFlow(Action<object> callback, object state)
		{
			if (callback == null)
			{
				throw Fx.Exception.ArgumentNull("callback");
			}
			bool flag = false;
			while (!flag)
			{
				try
				{
				}
				finally
				{
					flag = IOThreadScheduler.current.ScheduleCallbackHelper(callback, state);
				}
			}
		}

		[SecurityCritical]
		private bool TryCoalesce(out Action<object> callback, out object state)
		{
			int num;
			int num1;
			int num2 = this.headTail;
			do
			{
			Label0:
				if (IOThreadScheduler.Bits.Count(num2) <= 0)
				{
					int num3 = this.headTailLowPri;
					if (IOThreadScheduler.Bits.CountNoIdle(num3) <= 0)
					{
						callback = null;
						state = null;
						return false;
					}
					int num4 = num3;
					int num5 = Interlocked.CompareExchange(ref this.headTailLowPri, IOThreadScheduler.Bits.IncrementLo(num3), num3);
					num3 = num5;
					if (num4 == num5)
					{
						this.slotsLowPri[num3 & this.SlotMaskLowPri].DequeueWorkItem(out callback, out state);
						return true;
					}
					num2 = this.headTail;
					goto Label0;
				}
				else
				{
					num1 = num2;
					num = Interlocked.CompareExchange(ref this.headTail, IOThreadScheduler.Bits.IncrementLo(num2), num2);
					num2 = num;
				}
			}
			while (num1 != num);
			this.slots[num2 & this.SlotMask].DequeueWorkItem(out callback, out state);
			return true;
		}

		private static class Bits
		{
			public const int HiShift = 16;

			public const int HiOne = 65536;

			public const int LoHiBit = 32768;

			public const int HiHiBit = -2147483648;

			public const int LoCountMask = 32767;

			public const int HiCountMask = 2147418112;

			public const int LoMask = 65535;

			public const int HiMask = -65536;

			public const int HiBits = -2147450880;

			public static int Count(int slot)
			{
				return ((slot >> 16) - slot + 2 & 65535) - 1;
			}

			public static int CountNoIdle(int slot)
			{
				return (slot >> 16) - slot + 1 & 65535;
			}

			public static int IncrementLo(int slot)
			{
				return slot + 1 & 65535 | slot & -65536;
			}

			public static bool IsComplete(int gate)
			{
				return (gate & -65536) == gate << 16;
			}
		}

		[SecurityCritical]
		private class ScheduledOverlapped
		{
			private readonly unsafe NativeOverlapped* nativeOverlapped;

			private IOThreadScheduler scheduler;

			public ScheduledOverlapped()
			{
				this.nativeOverlapped = (new Overlapped()).UnsafePack(Fx.ThunkCallback(new IOCompletionCallback(this.IOCallback)), null);
			}

			public void Cleanup()
			{
				if (this.scheduler != null)
				{
					throw Fx.AssertAndThrowFatal("Cleanup called on an overlapped that is in-flight.");
				}
				Overlapped.Free(this.nativeOverlapped);
			}

			private unsafe void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlappedCallback)
			{
				Action<object> action = null;
				object obj = null;
				IOThreadScheduler oThreadScheduler = this.scheduler;
				this.scheduler = null;
				try
				{
				}
				finally
				{
					oThreadScheduler.CompletionCallback(out action, out obj);
				}
				bool flag = true;
				while (flag)
				{
					if (action != null)
					{
						action(obj);
					}
					try
					{
					}
					finally
					{
						flag = oThreadScheduler.TryCoalesce(out action, out obj);
					}
				}
			}

			public void Post(IOThreadScheduler iots)
			{
				this.scheduler = iots;
				ThreadPool.UnsafeQueueNativeOverlapped(this.nativeOverlapped);
			}
		}

		private struct Slot
		{
			private int gate;

			private Action<object> heldCallback;

			private object heldState;

			public void DequeueWorkItem(out Action<object> callback, out object state)
			{
				int num = Interlocked.Add(ref this.gate, 65536);
				if ((num & 32768) == 0)
				{
					callback = null;
					state = null;
					return;
				}
				if ((num & 2147418112) != 65536)
				{
					callback = null;
					state = null;
					if (IOThreadScheduler.Bits.IsComplete(num))
					{
						Interlocked.CompareExchange(ref this.gate, 0, num);
					}
				}
				else
				{
					callback = this.heldCallback;
					state = this.heldState;
					this.heldState = null;
					this.heldCallback = null;
					if ((num & 32767) != 1 || Interlocked.CompareExchange(ref this.gate, 0, num) != num)
					{
						num = Interlocked.Add(ref this.gate, -2147483648);
						if (IOThreadScheduler.Bits.IsComplete(num))
						{
							Interlocked.CompareExchange(ref this.gate, 0, num);
							return;
						}
					}
				}
			}

			public bool TryEnqueueWorkItem(Action<object> callback, object state, out bool wrapped)
			{
				int num = Interlocked.Increment(ref this.gate);
				wrapped = (num & 32767) != 1;
				if (wrapped)
				{
					if ((num & 32768) != 0 && IOThreadScheduler.Bits.IsComplete(num))
					{
						Interlocked.CompareExchange(ref this.gate, 0, num);
					}
					return false;
				}
				this.heldState = state;
				this.heldCallback = callback;
				num = Interlocked.Add(ref this.gate, 32768);
				if ((num & 2147418112) == 0)
				{
					return true;
				}
				this.heldState = null;
				this.heldCallback = null;
				if (num >> 16 != (num & 32767) || Interlocked.CompareExchange(ref this.gate, 0, num) != num)
				{
					num = Interlocked.Add(ref this.gate, -2147483648);
					if (IOThreadScheduler.Bits.IsComplete(num))
					{
						Interlocked.CompareExchange(ref this.gate, 0, num);
					}
				}
				return false;
			}
		}
	}
}