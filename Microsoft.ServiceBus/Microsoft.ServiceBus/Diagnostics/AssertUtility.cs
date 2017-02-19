using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal static class AssertUtility
	{
		[Conditional("DEBUG")]
		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.DebugAssert instead")]
		internal static void DebugAssert(bool condition, string message)
		{
		}

		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.NoInlining)]
		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.DebugAssert instead")]
		internal static void DebugAssert(string message)
		{
			AssertUtility.DebugAssertCore(message);
		}

		[Obsolete("For SMDiagnostics.dll use only. Call DiagnosticUtility.DebugAssert instead")]
		internal static void DebugAssertCore(string message)
		{
		}
	}
}