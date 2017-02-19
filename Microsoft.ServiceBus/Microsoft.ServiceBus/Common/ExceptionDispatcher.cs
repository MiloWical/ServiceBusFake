using System;

namespace Microsoft.ServiceBus.Common
{
	internal sealed class ExceptionDispatcher
	{
		public ExceptionDispatcher()
		{
		}

		public static void Throw(Exception exception)
		{
			exception.PrepareForRethrow();
			throw exception;
		}
	}
}