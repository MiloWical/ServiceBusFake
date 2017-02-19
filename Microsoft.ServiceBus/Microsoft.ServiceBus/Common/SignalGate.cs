using System;
using System.Threading;

namespace Microsoft.ServiceBus.Common
{
	[Serializable]
	internal class SignalGate
	{
		private int state;

		internal bool IsLocked
		{
			get
			{
				return this.state == 0;
			}
		}

		internal bool IsSignalled
		{
			get
			{
				return this.state == 3;
			}
		}

		public SignalGate()
		{
		}

		public bool Signal()
		{
			int num = this.state;
			if (num == 0)
			{
				num = Interlocked.CompareExchange(ref this.state, 1, 0);
			}
			if (num == 2)
			{
				this.state = 3;
				return true;
			}
			if (num != 0)
			{
				SignalGate.ThrowInvalidSignalGateState();
			}
			return false;
		}

		private static void ThrowInvalidSignalGateState()
		{
			throw Fx.Exception.AsError(new InvalidOperationException(SRCore.InvalidSemaphoreExit), null);
		}

		public bool Unlock()
		{
			int num = this.state;
			if (num == 0)
			{
				num = Interlocked.CompareExchange(ref this.state, 2, 0);
			}
			if (num == 1)
			{
				this.state = 3;
				return true;
			}
			if (num != 0)
			{
				SignalGate.ThrowInvalidSignalGateState();
			}
			return false;
		}

		private static class GateState
		{
			public const int Locked = 0;

			public const int SignalPending = 1;

			public const int Unlocked = 2;

			public const int Signalled = 3;
		}
	}
}