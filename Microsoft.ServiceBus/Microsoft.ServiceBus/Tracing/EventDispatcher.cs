using System;

namespace Microsoft.ServiceBus.Tracing
{
	internal class EventDispatcher
	{
		internal readonly EventListener m_Listener;

		internal bool[] m_EventEnabled;

		internal EventDispatcher m_Next;

		internal bool m_ManifestSent;

		internal EventDispatcher(EventDispatcher next, bool[] eventEnabled, EventListener listener)
		{
			this.m_Next = next;
			this.m_EventEnabled = eventEnabled;
			this.m_Listener = listener;
		}
	}
}