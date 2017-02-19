using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Common
{
	internal static class ExceptionExtensionMethods
	{
		private static MethodInfo prepForRemotingMethodInfo;

		public static Exception DisablePrepareForRethrow(this Exception exception)
		{
			exception.Data["DisablePrepareForRethrow"] = string.Empty;
			return exception;
		}

		public static Exception PrepareForRethrow(this Exception exception)
		{
			if (!ExceptionExtensionMethods.ShouldPrepareForRethrow(exception))
			{
				return exception;
			}
			if (PartialTrustHelpers.UnsafeIsInFullTrust())
			{
				if (ExceptionExtensionMethods.prepForRemotingMethodInfo == null)
				{
					ExceptionExtensionMethods.prepForRemotingMethodInfo = typeof(Exception).GetMethod("PrepForRemoting", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], new ParameterModifier[0]);
				}
				if (ExceptionExtensionMethods.prepForRemotingMethodInfo != null)
				{
					ExceptionExtensionMethods.prepForRemotingMethodInfo.Invoke(exception, new object[0]);
				}
			}
			return exception;
		}

		private static bool ShouldPrepareForRethrow(Exception exception)
		{
			while (exception != null)
			{
				if (exception.Data != null && exception.Data.Contains("DisablePrepareForRethrow"))
				{
					return false;
				}
				exception = exception.InnerException;
			}
			return true;
		}
	}
}