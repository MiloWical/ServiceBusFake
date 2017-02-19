using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.ServiceBus.Security
{
	internal sealed class SafeFreeContextBuffer : SafeHandleZeroOrMinusOneIsInvalid
	{
		private const string SECURITY = "security.dll";

		private SafeFreeContextBuffer() : base(true)
		{
		}

		internal static SafeFreeContextBuffer CreateEmptyHandle()
		{
			return new SafeFreeContextBuffer();
		}

		internal static int EnumeratePackages(out int pkgnum, out SafeFreeContextBuffer pkgArray)
		{
			int num = -1;
			num = SafeFreeContextBuffer.EnumerateSecurityPackagesW(out pkgnum, out pkgArray);
			if (num != 0)
			{
				if (pkgArray != null)
				{
					pkgArray.SetHandleAsInvalid();
				}
				pkgArray = null;
			}
			return num;
		}

		[DllImport("security.dll", CharSet=CharSet.None, ExactSpelling=true, SetLastError=true)]
		internal static extern int EnumerateSecurityPackagesW(out int pkgnum, out SafeFreeContextBuffer handle);

		[DllImport("security.dll", CharSet=CharSet.None, ExactSpelling=true, SetLastError=true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		private static extern int FreeContextBuffer([In] IntPtr contextBuffer);

		protected override bool ReleaseHandle()
		{
			return SafeFreeContextBuffer.FreeContextBuffer(this.handle) == 0;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		internal void Set(IntPtr value)
		{
			this.handle = value;
		}
	}
}