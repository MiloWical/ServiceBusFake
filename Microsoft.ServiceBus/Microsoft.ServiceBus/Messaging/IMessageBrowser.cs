using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IMessageBrowser
	{
		IAsyncResult BeginPeek(AsyncCallback callback, object state);

		IAsyncResult BeginPeek(long fromSequenceNumber, AsyncCallback callback, object state);

		IAsyncResult BeginPeekBatch(int messageCount, AsyncCallback callback, object state);

		IAsyncResult BeginPeekBatch(long fromSequenceNumber, int messageCount, AsyncCallback callback, object state);

		BrokeredMessage EndPeek(IAsyncResult result);

		IEnumerable<BrokeredMessage> EndPeekBatch(IAsyncResult result);

		BrokeredMessage Peek();

		BrokeredMessage Peek(long fromSequenceNumber);

		IEnumerable<BrokeredMessage> PeekBatch(int messageCount);

		IEnumerable<BrokeredMessage> PeekBatch(long fromSequenceNumber, int messageCount);
	}
}