using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Common
{
	internal static class WaitCallbackActionItem
	{
		internal static bool ShouldUseActivity
		{
			get;
			set;
		}
	}
}