using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.ServiceBus.Common.Interop
{
	[SecurityCritical]
	internal sealed class SafeEventLogWriteHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		[SecurityCritical]
		private SafeEventLogWriteHandle() : base(true)
		{
		}

		[DllImport("advapi32", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		private static extern bool DeregisterEventSource(IntPtr hEventLog);

		[SecurityCritical]
		public static Microsoft.ServiceBus.Common.Interop.SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName)
		{
			Microsoft.ServiceBus.Common.Interop.SafeEventLogWriteHandle safeEventLogWriteHandle = UnsafeNativeMethods.RegisterEventSource(uncServerName, sourceName);
			Marshal.GetLastWin32Error();
			bool isInvalid = safeEventLogWriteHandle.IsInvalid;
			return safeEventLogWriteHandle;
		}

		[SecurityCritical]
		protected override bool ReleaseHandle()
		{
			return Microsoft.ServiceBus.Common.Interop.SafeEventLogWriteHandle.DeregisterEventSource(this.handle);
		}
	}
}