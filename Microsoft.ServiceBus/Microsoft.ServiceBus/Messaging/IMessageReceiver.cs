using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IMessageReceiver
	{
		ReceiveMode Mode
		{
			get;
		}

		void Abandon(Guid lockToken);

		void Abandon(Guid lockToken, IDictionary<string, object> propertiesToModify);

		IAsyncResult BeginAbandon(Guid lockToken, AsyncCallback callback, object state);

		IAsyncResult BeginAbandon(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state);

		IAsyncResult BeginComplete(Guid lockToken, AsyncCallback callback, object state);

		IAsyncResult BeginCompleteBatch(IEnumerable<Guid> lockTokens, AsyncCallback callback, object state);

		IAsyncResult BeginDeadLetter(Guid lockToken, AsyncCallback callback, object state);

		IAsyncResult BeginDeadLetter(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state);

		IAsyncResult BeginDeadLetter(Guid lockToken, string deadLetterReason, string deadLetterErrorDescription, AsyncCallback callback, object state);

		IAsyncResult BeginDefer(Guid lockToken, AsyncCallback callback, object state);

		IAsyncResult BeginDefer(Guid lockToken, IDictionary<string, object> propertiesToModify, AsyncCallback callback, object state);

		IAsyncResult BeginReceive(AsyncCallback callback, object state);

		IAsyncResult BeginReceive(TimeSpan serverWaitTime, AsyncCallback callback, object state);

		IAsyncResult BeginReceive(long sequenceNumber, AsyncCallback callback, object state);

		IAsyncResult BeginReceiveBatch(int messageCount, AsyncCallback callback, object state);

		IAsyncResult BeginReceiveBatch(int messageCount, TimeSpan serverWaitTime, AsyncCallback callback, object state);

		IAsyncResult BeginReceiveBatch(IEnumerable<long> sequenceNumbers, AsyncCallback callback, object state);

		void Complete(Guid lockToken);

		Task CompleteAsync(Guid lockToken);

		void CompleteBatch(IEnumerable<Guid> lockTokens);

		void DeadLetter(Guid lockToken);

		void DeadLetter(Guid lockToken, IDictionary<string, object> propertiesToModify);

		void DeadLetter(Guid lockToken, string deadLetterReason, string deadLetterErrorDescription);

		void Defer(Guid lockToken);

		void Defer(Guid lockToken, IDictionary<string, object> propertiesToModify);

		void EndAbandon(IAsyncResult result);

		void EndComplete(IAsyncResult result);

		void EndCompleteBatch(IAsyncResult result);

		void EndDeadLetter(IAsyncResult result);

		void EndDefer(IAsyncResult result);

		BrokeredMessage EndReceive(IAsyncResult result);

		IEnumerable<BrokeredMessage> EndReceiveBatch(IAsyncResult result);

		BrokeredMessage Receive();

		BrokeredMessage Receive(TimeSpan serverWaitTime);

		BrokeredMessage Receive(long sequenceNumber);

		Task<BrokeredMessage> ReceiveAsync();

		Task<BrokeredMessage> ReceiveAsync(TimeSpan serverWaitTime);

		IEnumerable<BrokeredMessage> ReceiveBatch(int messageCount);

		IEnumerable<BrokeredMessage> ReceiveBatch(int messageCount, TimeSpan serverWaitTime);

		IEnumerable<BrokeredMessage> ReceiveBatch(IEnumerable<long> sequenceNumbers);
	}
}