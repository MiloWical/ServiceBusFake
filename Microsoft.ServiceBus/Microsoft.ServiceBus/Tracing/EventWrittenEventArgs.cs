using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	internal class EventWrittenEventArgs : EventArgs
	{
		private Microsoft.ServiceBus.Tracing.EventSource m_eventSource;

		public EventChannel Channel
		{
			get
			{
				return (EventChannel)this.m_eventSource.m_eventData[this.EventId].Descriptor.Channel;
			}
		}

		public int EventId
		{
			get;
			internal set;
		}

		public Microsoft.ServiceBus.Tracing.EventSource EventSource
		{
			get
			{
				return this.m_eventSource;
			}
		}

		public EventKeywords Keywords
		{
			get
			{
				return (EventKeywords)this.m_eventSource.m_eventData[this.EventId].Descriptor.Keywords;
			}
		}

		public EventLevel Level
		{
			get
			{
				if (this.EventId >= (int)this.m_eventSource.m_eventData.Length)
				{
					return EventLevel.LogAlways;
				}
				return (EventLevel)this.m_eventSource.m_eventData[this.EventId].Descriptor.Level;
			}
		}

		public string Message
		{
			get
			{
				return this.m_eventSource.m_eventData[this.EventId].Message;
			}
		}

		public EventOpcode Opcode
		{
			get
			{
				return (EventOpcode)this.m_eventSource.m_eventData[this.EventId].Descriptor.Opcode;
			}
		}

		public IEnumerable<object> Payload
		{
			get;
			internal set;
		}

		public EventTask Task
		{
			get
			{
				return (EventTask)this.m_eventSource.m_eventData[this.EventId].Descriptor.Task;
			}
		}

		public byte Version
		{
			get
			{
				return this.m_eventSource.m_eventData[this.EventId].Descriptor.Version;
			}
		}

		internal EventWrittenEventArgs(Microsoft.ServiceBus.Tracing.EventSource eventSource)
		{
			this.m_eventSource = eventSource;
		}
	}
}