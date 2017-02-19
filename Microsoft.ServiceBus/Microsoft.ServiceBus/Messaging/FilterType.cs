using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal enum FilterType
	{
		SqlFilter,
		CorrelationFilter,
		LambdaExpressionFilter
	}
}