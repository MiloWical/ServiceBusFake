using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.ServiceBus
{
	internal static class SecureStringHelper
	{
		public static unsafe SecureString ToSecureString(this string unsecureString)
		{
			if (unsecureString == null)
			{
				return null;
			}
			fixed (string str = unsecureString)
			{
				string* offsetToStringData = &str;
				if (offsetToStringData != null)
				{
					offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
				}
				SecureString secureString = new SecureString((char*)offsetToStringData, unsecureString.Length);
				secureString.MakeReadOnly();
				return secureString;
			}
		}

		public static string ToUnsecureString(this SecureString secureString)
		{
			string stringUni;
			if (secureString == null)
			{
				return null;
			}
			IntPtr zero = IntPtr.Zero;
			try
			{
				zero = Marshal.SecureStringToGlobalAllocUnicode(secureString);
				stringUni = Marshal.PtrToStringUni(zero);
			}
			finally
			{
				Marshal.ZeroFreeGlobalAllocUnicode(zero);
			}
			return stringUni;
		}
	}
}