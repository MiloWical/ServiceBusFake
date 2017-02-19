using Microsoft.ServiceBus.Common;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics.Eventing;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace Microsoft.ServiceBus.Common.Interop
{
	[SuppressUnmanagedCodeSecurity]
	internal static class UnsafeNativeMethods
	{
		public const string KERNEL32 = "kernel32.dll";

		public const string ADVAPI32 = "advapi32.dll";

		public const string WS2_32 = "ws2_32.dll";

		public const int ERROR_SUCCESS = 0;

		public const int ERROR_INVALID_HANDLE = 6;

		public const int ERROR_OUTOFMEMORY = 14;

		public const int ERROR_MORE_DATA = 234;

		public const int ERROR_ARITHMETIC_OVERFLOW = 534;

		public const int ERROR_NOT_ENOUGH_MEMORY = 8;

		public const int ERROR_OPERATION_ABORTED = 995;

		public const int ERROR_IO_PENDING = 997;

		public const int ERROR_NO_SYSTEM_RESOURCES = 1450;

		public const int STATUS_PENDING = 259;

		public const int WSAACCESS = 10013;

		public const int WSAEMFILE = 10024;

		public const int WSAEMSGSIZE = 10040;

		public const int WSAEADDRINUSE = 10048;

		public const int WSAEADDRNOTAVAIL = 10049;

		public const int WSAENETDOWN = 10050;

		public const int WSAENETUNREACH = 10051;

		public const int WSAENETRESET = 10052;

		public const int WSAECONNABORTED = 10053;

		public const int WSAECONNRESET = 10054;

		public const int WSAENOBUFS = 10055;

		public const int WSAESHUTDOWN = 10058;

		public const int WSAETIMEDOUT = 10060;

		public const int WSAECONNREFUSED = 10061;

		public const int WSAEHOSTDOWN = 10064;

		public const int WSAEHOSTUNREACH = 10065;

		[DllImport("kernel32.dll", BestFitMapping=false, CharSet=CharSet.Auto, ExactSpelling=false)]
		[SecurityCritical]
		public static extern SafeWaitHandle CreateWaitableTimer(IntPtr mustBeZero, bool manualReset, string timerName);

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		[SecurityCritical]
		internal static extern void DebugBreak();

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
		[SecurityCritical]
		internal static extern uint EventActivityIdControl([In] int ControlCode, [In][Out] ref Guid ActivityId);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
		[SecurityCritical]
		internal static extern unsafe uint EventRegister([In] ref Guid providerId, [In] UnsafeNativeMethods.EtwEnableCallback enableCallback, [In] void* callbackContext, [In][Out] ref long registrationHandle);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
		[SecurityCritical]
		internal static extern uint EventUnregister([In] long registrationHandle);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
		[SecurityCritical]
		internal static extern unsafe uint EventWrite([In] long registrationHandle, [In] ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor, [In] uint userDataCount, [In] UnsafeNativeMethods.EventData* userData);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
		[SecurityCritical]
		internal static extern unsafe uint EventWriteString([In] long registrationHandle, [In] byte level, [In] long keywords, [In] char* message);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
		[SecurityCritical]
		internal static extern unsafe uint EventWriteTransfer([In] long registrationHandle, [In] ref System.Diagnostics.Eventing.EventDescriptor eventDescriptor, [In] ref Guid activityId, [In] ref Guid relatedActivityId, [In] uint userDataCount, [In] UnsafeNativeMethods.EventData* userData);

		[SecurityCritical]
		internal static string GetComputerName(ComputerNameFormat nameType)
		{
			int num = 0;
			if (!UnsafeNativeMethods.GetComputerNameEx(nameType, null, ref num))
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (lastWin32Error != 234)
				{
					throw Fx.Exception.AsError(new Win32Exception(lastWin32Error), null);
				}
			}
			if (num < 0)
			{
				Fx.AssertAndThrow(string.Concat("GetComputerName returned an invalid length: ", num));
			}
			StringBuilder stringBuilder = new StringBuilder(num);
			if (!UnsafeNativeMethods.GetComputerNameEx(nameType, stringBuilder, ref num))
			{
				int lastWin32Error1 = Marshal.GetLastWin32Error();
				throw Fx.Exception.AsError(new Win32Exception(lastWin32Error1), null);
			}
			return stringBuilder.ToString();
		}

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		[SecurityCritical]
		private static extern bool GetComputerNameEx([In] ComputerNameFormat nameType, [In][Out] StringBuilder lpBuffer, [In][Out] ref int size);

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		[SecurityCritical]
		public static extern uint GetSystemTimeAdjustment(out int adjustment, out uint increment, out uint adjustmentDisabled);

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		[SecurityCritical]
		public static extern void GetSystemTimeAsFileTime(out long time);

		internal static unsafe bool HasOverlappedIoCompleted(NativeOverlapped* overlapped)
		{
			return (*overlapped).InternalLow != (IntPtr)259;
		}

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false)]
		[SecurityCritical]
		internal static extern bool IsDebuggerPresent();

		[DllImport("kernel32.dll", CharSet=CharSet.Unicode, ExactSpelling=false)]
		[SecurityCritical]
		internal static extern void OutputDebugString(string lpOutputString);

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		[SecurityCritical]
		public static extern int QueryPerformanceCounter(out long time);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=false, SetLastError=true)]
		[SecurityCritical]
		internal static extern Microsoft.ServiceBus.Common.Interop.SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName);

		[DllImport("advapi32.dll", CharSet=CharSet.Unicode, ExactSpelling=false, SetLastError=true)]
		[SecurityCritical]
		internal static extern bool ReportEvent(SafeHandle hEventLog, ushort type, ushort category, uint eventID, byte[] userSID, ushort numStrings, uint dataLen, HandleRef strings, byte[] rawData);

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=true)]
		[SecurityCritical]
		public static extern bool SetWaitableTimer(SafeWaitHandle handle, ref long dueTime, int period, IntPtr mustBeZero, IntPtr mustBeZeroAlso, bool resume);

		[DllImport("ws2_32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		internal static extern unsafe bool WSAGetOverlappedResult(IntPtr socketHandle, NativeOverlapped* overlapped, out int bytesTransferred, bool wait, out uint flags);

		[DllImport("ws2_32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		internal static extern unsafe int WSARecv(IntPtr handle, UnsafeNativeMethods.WSABuffer* buffers, int bufferCount, out int bytesTransferred, ref int socketFlags, NativeOverlapped* nativeOverlapped, IntPtr completionRoutine);

		[SecurityCritical]
		internal delegate void EtwEnableCallback([In] ref Guid sourceId, [In] int isEnabled, [In] byte level, [In] long matchAnyKeywords, [In] long matchAllKeywords, [In] void* filterData, [In] void* callbackContext);

		[StructLayout(LayoutKind.Explicit)]
		public struct EventData
		{
			[FieldOffset(0)]
			internal ulong DataPointer;

			[FieldOffset(8)]
			internal uint Size;

			[FieldOffset(12)]
			internal int Reserved;
		}

		internal struct WSABuffer
		{
			public int length;

			public IntPtr buffer;
		}
	}
}