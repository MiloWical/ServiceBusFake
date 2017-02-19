using System;

namespace Microsoft.ServiceBus.Configuration
{
	[Serializable]
	public enum ServiceBusSectionType
	{
		All,
		NamespaceManager,
		MessagingFactory
	}
}