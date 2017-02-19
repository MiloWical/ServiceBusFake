using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.ServiceBus.Security
{
	internal class SafeFreeCredentials : SafeHandle
	{
		private const string SECURITY = "security.Dll";

		internal SSPIHandle _handle;

		public override bool IsInvalid
		{
			get
			{
				if (base.IsClosed)
				{
					return true;
				}
				return this._handle.IsZero;
			}
		}

		protected SafeFreeCredentials() : base(IntPtr.Zero, true)
		{
			this._handle = new SSPIHandle();
		}

		public static int AcquireCredentialsHandle(string package, CredentialUse intent, ref IntPtr ppAuthIdentity, out SafeFreeCredentials outCredential)
		{
			unsafe
			{
				long num;
				int num1 = -1;
				outCredential = new SafeFreeCredentials();
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
				}
				finally
				{
					num1 = SafeFreeCredentials.AcquireCredentialsHandleW(null, package, (int)intent, 0, ppAuthIdentity, 0, 0, ref outCredential._handle, out num);
					if (num1 != 0)
					{
						outCredential.SetHandleAsInvalid();
					}
				}
				return num1;
			}
		}

		[DllImport("security.Dll", CharSet=CharSet.Unicode, ExactSpelling=true, SetLastError=true)]
		private static extern unsafe int AcquireCredentialsHandleW([In] string principal, [In] string moduleName, [In] int usage, [In] void* logonID, [In] IntPtr zero, [In] void* keyCallback, [In] void* keyArgument, ref SSPIHandle handlePtr, out long timeStamp);

		[DllImport("security.Dll", CharSet=CharSet.None, ExactSpelling=true, SetLastError=true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		internal static extern int FreeCredentialsHandle(ref SSPIHandle handlePtr);

		protected override bool ReleaseHandle()
		{
			return SafeFreeCredentials.FreeCredentialsHandle(ref this._handle) == 0;
		}
	}
}