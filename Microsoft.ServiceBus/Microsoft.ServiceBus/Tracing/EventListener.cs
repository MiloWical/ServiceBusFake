using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.ServiceBus.Tracing
{
	internal abstract class EventListener : IDisposable
	{
		internal volatile EventListener m_Next;

		internal static EventListener s_Listeners;

		internal static List<WeakReference> s_EventSources;

		internal static object EventListenersLock
		{
			get
			{
				if (EventListener.s_EventSources == null)
				{
					Interlocked.CompareExchange<List<WeakReference>>(ref EventListener.s_EventSources, new List<WeakReference>(2), null);
				}
				return EventListener.s_EventSources;
			}
		}

		protected EventListener()
		{
			lock (EventListener.EventListenersLock)
			{
				foreach (WeakReference sEventSource in EventListener.s_EventSources)
				{
					EventSource target = sEventSource.Target as EventSource;
					if (target == null)
					{
						continue;
					}
					target.AddListener(this);
				}
				this.m_Next = EventListener.s_Listeners;
				EventListener.s_Listeners = this;
			}
		}

		internal static void AddEventSource(EventSource newEventSource)
		{
			WeakReference item;
			lock (EventListener.EventListenersLock)
			{
				if (EventListener.s_EventSources == null)
				{
					EventListener.s_EventSources = new List<WeakReference>(2);
				}
				int count = -1;
				if (EventListener.s_EventSources.Count % 64 == 63)
				{
					int num = EventListener.s_EventSources.Count;
					do
					{
						if (0 >= num)
						{
							goto Label0;
						}
						num--;
						item = EventListener.s_EventSources[num];
					}
					while (item.IsAlive);
					count = num;
					item.Target = newEventSource;
				}
			Label0:
				if (count < 0)
				{
					count = EventListener.s_EventSources.Count;
					EventListener.s_EventSources.Add(new WeakReference(newEventSource));
				}
				newEventSource.m_id = count;
				for (EventListener i = EventListener.s_Listeners; i != null; i = i.m_Next)
				{
					newEventSource.AddListener(i);
				}
			}
		}

		public void DisableEvents(EventSource eventSource)
		{
			if (eventSource == null)
			{
				throw new ArgumentNullException("eventSource");
			}
			EventSource.SendCommand(eventSource, this, EventCommand.Update, false, EventLevel.LogAlways, (EventKeywords)((long)0), null);
		}

		public virtual void Dispose()
		{
			EventListener mNext;
			lock (EventListener.EventListenersLock)
			{
				if (EventListener.s_Listeners != null)
				{
					if (this != EventListener.s_Listeners)
					{
						EventListener sListeners = EventListener.s_Listeners;
						while (true)
						{
							mNext = sListeners.m_Next;
							if (mNext == null)
							{
								goto Label0;
							}
							if (mNext == this)
							{
								break;
							}
							sListeners = mNext;
						}
						sListeners.m_Next = mNext.m_Next;
						EventListener.RemoveReferencesToListenerInEventSources(mNext);
					}
					else
					{
						EventListener.s_Listeners = this.m_Next;
					}
				}
			Label0:
			}
		}

		public void EnableEvents(EventSource eventSource, EventLevel level)
		{
			this.EnableEvents(eventSource, level, (EventKeywords)((long)0));
		}

		public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
		{
			this.EnableEvents(eventSource, level, matchAnyKeyword, null);
		}

		public void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> arguments)
		{
			if (eventSource == null)
			{
				throw new ArgumentNullException("eventSource");
			}
			EventSource.SendCommand(eventSource, this, EventCommand.Update, true, level, matchAnyKeyword, arguments);
		}

		protected static int EventSourceIndex(EventSource eventSource)
		{
			return eventSource.m_id;
		}

		protected internal virtual void OnEventSourceCreated(EventSource eventSource)
		{
		}

		protected internal abstract void OnEventWritten(EventWrittenEventArgs eventData);

		private static void RemoveReferencesToListenerInEventSources(EventListener listenerToRemove)
		{
			EventDispatcher mNext;
		Label0:
			foreach (WeakReference sEventSource in EventListener.s_EventSources)
			{
				EventSource target = sEventSource.Target as EventSource;
				if (target == null)
				{
					continue;
				}
				if (target.m_Dispatchers.m_Listener != listenerToRemove)
				{
					EventDispatcher mDispatchers = target.m_Dispatchers;
					while (true)
					{
						mNext = mDispatchers.m_Next;
						if (mNext == null)
						{
							goto Label0;
						}
						if (mNext.m_Listener == listenerToRemove)
						{
							break;
						}
						mDispatchers = mNext;
					}
					mDispatchers.m_Next = mNext.m_Next;
				}
				else
				{
					target.m_Dispatchers = target.m_Dispatchers.m_Next;
				}
			}
		}

		[Conditional("DEBUG")]
		internal static void Validate()
		{
			lock (EventListener.EventListenersLock)
			{
				Dictionary<EventListener, bool> eventListeners = new Dictionary<EventListener, bool>();
				for (EventListener i = EventListener.s_Listeners; i != null; i = i.m_Next)
				{
					eventListeners.Add(i, true);
				}
				int num = -1;
				foreach (WeakReference sEventSource in EventListener.s_EventSources)
				{
					num++;
					EventSource target = sEventSource.Target as EventSource;
					if (target == null)
					{
						continue;
					}
					EventDispatcher mDispatchers = target.m_Dispatchers;
					while (mDispatchers != null)
					{
						mDispatchers = mDispatchers.m_Next;
					}
					foreach (EventListener key in eventListeners.Keys)
					{
						mDispatchers = target.m_Dispatchers;
						while (mDispatchers.m_Listener != key)
						{
							mDispatchers = mDispatchers.m_Next;
						}
					}
				}
			}
		}
	}
}