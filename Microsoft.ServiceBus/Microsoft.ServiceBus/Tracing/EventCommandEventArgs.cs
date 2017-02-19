using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Tracing
{
	internal class EventCommandEventArgs : EventArgs
	{
		internal EventSource eventSource;

		internal EventDispatcher dispatcher;

		public IDictionary<string, string> Arguments
		{
			get;
			private set;
		}

		public EventCommand Command
		{
			get;
			private set;
		}

		internal EventCommandEventArgs(EventCommand command, IDictionary<string, string> arguments, EventSource eventSource, EventDispatcher dispatcher)
		{
			this.Command = command;
			this.Arguments = arguments;
			this.eventSource = eventSource;
			this.dispatcher = dispatcher;
		}

		public bool DisableEvent(int eventId)
		{
			if (this.Command != EventCommand.Enable && this.Command != EventCommand.Disable)
			{
				throw new InvalidOperationException();
			}
			return this.eventSource.EnableEventForDispatcher(this.dispatcher, eventId, false);
		}

		public bool EnableEvent(int eventId)
		{
			if (this.Command != EventCommand.Enable && this.Command != EventCommand.Disable)
			{
				throw new InvalidOperationException();
			}
			return this.eventSource.EnableEventForDispatcher(this.dispatcher, eventId, true);
		}
	}
}