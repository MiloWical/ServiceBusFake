using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	[AttributeUsage(AttributeTargets.Field)]
	internal class ChannelAttribute : Attribute
	{
		public int BufferSize
		{
			get;
			set;
		}

		public bool Enabled
		{
			get;
			set;
		}

		public string ImportChannel
		{
			get;
			set;
		}

		public string Isolation
		{
			get;
			set;
		}

		public string Type
		{
			get;
			set;
		}

		public ChannelAttribute()
		{
		}
	}
}