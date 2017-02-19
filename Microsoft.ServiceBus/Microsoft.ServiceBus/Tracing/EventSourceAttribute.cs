using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	[AttributeUsage(AttributeTargets.Class)]
	internal sealed class EventSourceAttribute : Attribute
	{
		public string Guid
		{
			get;
			set;
		}

		public string LocalizationResources
		{
			get;
			set;
		}

		public string Name
		{
			get;
			set;
		}

		public EventSourceAttribute()
		{
		}
	}
}