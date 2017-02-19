using Microsoft.ServiceBus.Common;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class ExceptionHandler
	{
		public ExceptionHandler()
		{
		}

		internal static bool HandleTransportExceptionHelper(Exception exception)
		{
			if (exception == null)
			{
				Fx.AssertAndThrow("Null exception passed to HandleTransportExceptionHelper.");
			}
			return false;
		}
	}
}