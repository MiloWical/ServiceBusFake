using System;
using System.ComponentModel;

namespace Microsoft.ServiceBus.Security
{
	internal static class SspiWrapper
	{
		internal static int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext refContext, SspiContextFlags inFlags, Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref SspiContextFlags outFlags)
		{
			return SafeDeleteContext.AcceptSecurityContext(credential, ref refContext, inFlags, datarep, inputBuffer, null, outputBuffer, ref outFlags);
		}

		public static SafeFreeCredentials AcquireCredentialsHandle(string package, CredentialUse intent, ref IntPtr ppAuthIdentity)
		{
			SafeFreeCredentials safeFreeCredential = null;
			int num = SafeFreeCredentials.AcquireCredentialsHandle(package, intent, ref ppAuthIdentity, out safeFreeCredential);
			if (num != 0)
			{
				throw new Win32Exception(num);
			}
			return safeFreeCredential;
		}

		internal static int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SspiContextFlags inFlags, Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref SspiContextFlags outFlags)
		{
			return SafeDeleteContext.InitializeSecurityContext(credential, ref context, targetName, inFlags, datarep, inputBuffer, null, outputBuffer, ref outFlags);
		}
	}
}