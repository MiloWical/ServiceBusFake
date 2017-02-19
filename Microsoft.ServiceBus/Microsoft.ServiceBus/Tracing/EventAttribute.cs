using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	[AttributeUsage(AttributeTargets.Method)]
	internal sealed class EventAttribute : Attribute
	{
		public EventChannel Channel
		{
			get;
			set;
		}

		public int EventId
		{
			get;
			private set;
		}

		public EventKeywords Keywords
		{
			get;
			set;
		}

		public EventLevel Level
		{
			get;
			set;
		}

		public string Message
		{
			get;
			set;
		}

		public EventOpcode Opcode
		{
			get;
			set;
		}

		public EventTask Task
		{
			get;
			set;
		}

		public byte Version
		{
			get;
			set;
		}

		public EventAttribute(int eventId)
		{
			this.EventId = eventId;
			this.Level = EventLevel.Informational;
		}
	}
}