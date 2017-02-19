using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IMessageSessionEntity : IMessageClientEntity
	{
		int PrefetchCount
		{
			get;
			set;
		}

		MessageSession AcceptMessageSession();

		MessageSession AcceptMessageSession(TimeSpan serverWaitTime);

		MessageSession AcceptMessageSession(string sessionId);

		MessageSession AcceptMessageSession(string sessionId, TimeSpan serverWaitTime);

		IAsyncResult BeginAcceptMessageSession(AsyncCallback callback, object state);

		IAsyncResult BeginAcceptMessageSession(TimeSpan serverWaitTime, AsyncCallback callback, object state);

		IAsyncResult BeginAcceptMessageSession(string sessionId, AsyncCallback callback, object state);

		IAsyncResult BeginAcceptMessageSession(string sessionId, TimeSpan serverWaitTime, AsyncCallback callback, object state);

		IAsyncResult BeginGetMessageSessions(AsyncCallback callback, object state);

		IAsyncResult BeginGetMessageSessions(DateTime lastUpdatedTime, AsyncCallback callback, object state);

		MessageSession EndAcceptMessageSession(IAsyncResult result);

		IEnumerable<MessageSession> EndGetMessageSessions(IAsyncResult result);

		IEnumerable<MessageSession> GetMessageSessions();

		IEnumerable<MessageSession> GetMessageSessions(DateTime lastUpdatedTime);
	}
}