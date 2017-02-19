using System;
using System.Runtime.InteropServices;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal static class NativeMethods
	{
		private const string ADVAPI32 = "advapi32.dll";

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=false, SetLastError=true)]
		internal static extern SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=false, SetLastError=true)]
		internal static extern bool ReportEvent(SafeHandle hEventLog, ushort type, ushort category, uint eventID, byte[] userSID, ushort numStrings, uint dataLen, HandleRef strings, byte[] rawData);
	}
}