using Microsoft.ServiceBus.Common;
using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class FxTrace
	{
		private const string EventSourceName = "Microsoft.ServiceBus";

		private static ExceptionTrace exceptionTrace;

		public static ExceptionTrace Exception
		{
			get
			{
				if (FxTrace.exceptionTrace == null)
				{
					FxTrace.exceptionTrace = new ExceptionTrace("Microsoft.ServiceBus");
				}
				return FxTrace.exceptionTrace;
			}
		}
	}
}