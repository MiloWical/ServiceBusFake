using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal sealed class SafeEventLogWriteHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		private SafeEventLogWriteHandle() : base(true)
		{
		}

		[DllImport("advapi32", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		private static extern bool DeregisterEventSource(IntPtr hEventLog);

		internal static Microsoft.ServiceBus.Diagnostics.SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName)
		{
			Microsoft.ServiceBus.Diagnostics.SafeEventLogWriteHandle safeEventLogWriteHandle = Microsoft.ServiceBus.Diagnostics.NativeMethods.RegisterEventSource(uncServerName, sourceName);
			Marshal.GetLastWin32Error();
			bool isInvalid = safeEventLogWriteHandle.IsInvalid;
			return safeEventLogWriteHandle;
		}

		protected override bool ReleaseHandle()
		{
			return Microsoft.ServiceBus.Diagnostics.SafeEventLogWriteHandle.DeregisterEventSource(this.handle);
		}
	}
}