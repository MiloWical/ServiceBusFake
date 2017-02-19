using System;

namespace Microsoft.ServiceBus.Management
{
	public enum NamespaceState
	{
		Unknown,
		Creating,
		Created,
		Activating,
		Enabling,
		Active,
		Disabling,
		Disabled,
		SoftDeleting,
		SoftDeleted,
		Removing,
		Removed
	}
}