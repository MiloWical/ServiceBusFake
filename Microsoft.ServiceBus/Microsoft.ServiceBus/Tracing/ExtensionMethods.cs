using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	internal static class ExtensionMethods
	{
		private const string ExceptionIdentifierName = "ExceptionId";

		public static int ToStringLength(this object param)
		{
			return (param != null ? param.ToString().Length : 0);
		}

		public static string ToStringSlim(this Exception exception)
		{
			if (exception.Data != null && exception.Data.Contains("ExceptionId"))
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] item = new object[] { exception.Data["ExceptionId"], exception.GetType(), exception.Message };
				return string.Format(invariantCulture, "ExceptionId: {0}-{1}: {2}", item);
			}
			if (exception.Data == null)
			{
				return exception.ToString();
			}
			string str = Guid.NewGuid().ToString();
			exception.Data["ExceptionId"] = str;
			CultureInfo cultureInfo = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { str, exception.ToString() };
			return string.Format(cultureInfo, "ExceptionId: {0}-{1}", objArray);
		}
	}
}