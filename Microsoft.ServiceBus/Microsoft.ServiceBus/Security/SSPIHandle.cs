using System;
using System.Runtime.ConstrainedExecution;

namespace Microsoft.ServiceBus.Security
{
	internal struct SSPIHandle
	{
		private IntPtr HandleHi;

		private IntPtr HandleLo;

		public bool IsZero
		{
			get
			{
				if (this.HandleHi != IntPtr.Zero)
				{
					return false;
				}
				return this.HandleLo == IntPtr.Zero;
			}
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal void SetToInvalid()
		{
			this.HandleHi = IntPtr.Zero;
			this.HandleLo = IntPtr.Zero;
		}

		public override string ToString()
		{
			return string.Concat(this.HandleHi.ToString("x"), ":", this.HandleLo.ToString("x"));
		}
	}
}