using System;

namespace Microsoft.ServiceBus.Management
{
	public enum UnavailableReason
	{
		None,
		InvalidName,
		SubscriptionIsDisabled,
		NameInUse,
		NameInLockdown,
		TooManyNamespaceInCurrentSubscription
	}
}