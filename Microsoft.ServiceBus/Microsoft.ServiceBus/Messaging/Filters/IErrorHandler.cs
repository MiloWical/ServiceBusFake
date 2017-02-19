using System;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal interface IErrorHandler
	{
		void AddError(string message, string token, int line, int column, int length, int severity);
	}
}