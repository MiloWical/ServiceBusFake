using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Diagnostics;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal class ErrorBehavior
	{
		public ErrorBehavior()
		{
		}

		internal static void ThrowAndCatch(Exception e)
		{
			ErrorBehavior.ThrowAndCatch(e, null);
		}

		internal static void ThrowAndCatch(Exception e, Message message)
		{
			try
			{
				if (Debugger.IsAttached)
				{
					if (message != null)
					{
						throw TraceUtility.ThrowHelperError(e, message);
					}
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(e);
				}
				if (message == null)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(e);
				}
				TraceUtility.ThrowHelperError(e, message);
			}
			catch (Exception exception)
			{
				if (!object.ReferenceEquals(e, exception))
				{
					throw;
				}
			}
		}
	}
}