using System;

namespace Microsoft.ServiceBus.Tracing
{
	internal class MessagingClientEtwProvider
	{
		private const int WindowsVistaMajorNumber = 6;

		private readonly static bool IsVistaOrGreater;

		private readonly static object thisLock;

		private static volatile MessagingClientEventSource provider;

		private static volatile EventListener diagnostics;

		public Guid Id
		{
			get
			{
				return MessagingClientEtwProvider.provider.Guid;
			}
		}

		public static MessagingClientEventSource Provider
		{
			get
			{
				if (MessagingClientEtwProvider.provider == null)
				{
					lock (MessagingClientEtwProvider.thisLock)
					{
						if (MessagingClientEtwProvider.provider == null)
						{
							MessagingClientEtwProvider.diagnostics = new ServiceBusEventListener(new Guid("A307C7A2-A4CD-4D22-8093-94DB72934152"));
							MessagingClientEtwProvider.provider = new MessagingClientEventSource(!MessagingClientEtwProvider.IsVistaOrGreater);
						}
					}
				}
				return MessagingClientEtwProvider.provider;
			}
		}

		static MessagingClientEtwProvider()
		{
			MessagingClientEtwProvider.IsVistaOrGreater = Environment.OSVersion.Version.Major >= 6;
			MessagingClientEtwProvider.thisLock = new object();
		}

		public MessagingClientEtwProvider()
		{
		}

		public static void Close()
		{
			lock (MessagingClientEtwProvider.thisLock)
			{
				if (MessagingClientEtwProvider.provider != null)
				{
					MessagingClientEtwProvider.diagnostics.Dispose();
					MessagingClientEtwProvider.provider.Dispose();
					MessagingClientEtwProvider.provider = null;
				}
			}
		}

		public static bool IsEtwEnabled()
		{
			return MessagingClientEtwProvider.IsVistaOrGreater;
		}

		public static void TraceClient(Action action)
		{
			if (MessagingClientEtwProvider.IsVistaOrGreater)
			{
				action();
			}
		}

		public static void TraceClient<T1>(Action<T1> action, T1 state1)
		{
			if (MessagingClientEtwProvider.IsVistaOrGreater)
			{
				action(state1);
			}
		}

		public static void TraceClient<T1, T2>(Action<T1, T2> action, T1 state1, T2 state2)
		{
			if (MessagingClientEtwProvider.IsVistaOrGreater)
			{
				action(state1, state2);
			}
		}

		public static void TraceClient<T1, T2, T3>(Action<T1, T2, T3> action, T1 state1, T2 state2, T3 state3)
		{
			if (MessagingClientEtwProvider.IsVistaOrGreater)
			{
				action(state1, state2, state3);
			}
		}

		public static void TraceClient<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 state1, T2 state2, T3 state3, T4 state4)
		{
			if (MessagingClientEtwProvider.IsVistaOrGreater)
			{
				action(state1, state2, state3, state4);
			}
		}
	}
}