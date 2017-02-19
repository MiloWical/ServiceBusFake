using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	internal interface IConfigurationContextProviderInternal
	{
		ContextInformation GetEvaluationContext();

		ContextInformation GetOriginalEvaluationContext();
	}
}