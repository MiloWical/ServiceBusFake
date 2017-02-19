using System;

namespace Microsoft.ServiceBus.Tracing
{
	[Flags]
	public enum EventKeywords : long
	{
		All = -1,
		None = 0,
		WdiContext = 562949953421312,
		WdiDiagnostic = 1125899906842624,
		Sqm = 2251799813685248,
		AuditFailure = 4503599627370496,
		AuditSuccess = 9007199254740992,
		CorrelationHint = 18014398509481984,
		EventLogClassic = 36028797018963968
	}
}