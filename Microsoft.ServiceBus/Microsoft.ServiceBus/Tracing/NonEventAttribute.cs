using System;

namespace Microsoft.ServiceBus.Tracing
{
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class NonEventAttribute : Attribute
	{
		public NonEventAttribute()
		{
		}
	}
}