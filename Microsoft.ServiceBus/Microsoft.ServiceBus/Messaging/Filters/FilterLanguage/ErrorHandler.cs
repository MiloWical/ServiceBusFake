using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Filters;
using System;

namespace Microsoft.ServiceBus.Messaging.Filters.FilterLanguage
{
	internal sealed class ErrorHandler : IErrorHandler
	{
		public ErrorHandler()
		{
		}

		public void AddError(string message, string token, int line, int column, int length, int severity)
		{
			throw new FilterException((string.IsNullOrEmpty(message) ? SRClient.SQLSyntaxError(line, column, token) : SRClient.SQLSyntaxErrorDetailed(line, column, token, message)));
		}
	}
}