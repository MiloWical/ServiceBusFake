using System;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal static class DiagnosticStrings
	{
		internal const string DiagnosticsNamespace = "http://schemas.microsoft.com/2004/09/ServiceModel/Diagnostics";

		internal const string ActivityIdName = "E2ETrace.ActivityID";

		internal const string ActivityId = "ActivityId";

		internal const string AppDomain = "AppDomain";

		internal const string DataTag = "Data";

		internal const string DataItemsTag = "DataItems";

		internal const string DescriptionTag = "Description";

		internal const string EventLogTag = "EventLog";

		internal const string ExceptionTag = "Exception";

		internal const string ExceptionTypeTag = "ExceptionType";

		internal const string ExceptionStringTag = "ExceptionString";

		internal const string ExtendedDataTag = "ExtendedData";

		internal const string HeaderTag = "Header";

		internal const string InnerExceptionTag = "InnerException";

		internal const string KeyTag = "Key";

		internal const string MessageTag = "Message";

		internal const string NameTag = "Name";

		internal const string NamespaceTag = "xmlns";

		internal const string NativeErrorCodeTag = "NativeErrorCode";

		internal const string ProcessId = "ProcessId";

		internal const string ProcessName = "ProcessName";

		internal const string RoleTag = "Role";

		internal const string SeverityTag = "Severity";

		internal const string SourceTag = "Source";

		internal const string StackTraceTag = "StackTrace";

		internal const string TraceCodeTag = "TraceIdentifier";

		internal const string TraceRecordTag = "TraceRecord";

		internal const string ValueTag = "Value";

		internal static string[][] HeadersPaths;

		internal static string[] PiiList;

		static DiagnosticStrings()
		{
			string[][] strArrays = new string[2][];
			string[] strArrays1 = new string[] { "TraceRecord", "ExtendedData", "MessageHeaders", "Security" };
			strArrays[0] = strArrays1;
			string[] strArrays2 = new string[] { "TraceRecord", "ExtendedData", "MessageHeaders", "IssuedTokens" };
			strArrays[1] = strArrays2;
			DiagnosticStrings.HeadersPaths = strArrays;
			string[] strArrays3 = new string[] { "BinarySecret", "Entropy", "Password", "Nonce", "Username", "BinarySecurityToken", "NameIdentifier", "SubjectLocality", "AttributeValue" };
			DiagnosticStrings.PiiList = strArrays3;
		}
	}
}