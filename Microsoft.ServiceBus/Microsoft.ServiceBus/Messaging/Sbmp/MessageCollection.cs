using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	[CollectionDataContract(Name="messages", ItemName="Message", Namespace="http://schemas.datacontract.org/2004/07/Microsoft.ApplicationServer.Messaging")]
	internal class MessageCollection : ICollection<BrokeredMessage>, IEnumerable<BrokeredMessage>, IEnumerable
	{
		private ICollection<BrokeredMessage> innerCollection;

		public int Count
		{
			get
			{
				return this.innerCollection.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return this.innerCollection.IsReadOnly;
			}
		}

		public MessageCollection()
		{
			this.innerCollection = new List<BrokeredMessage>();
		}

		public MessageCollection(IEnumerable<BrokeredMessage> enumerable)
		{
			ICollection<BrokeredMessage> brokeredMessages = enumerable as ICollection<BrokeredMessage>;
			ICollection<BrokeredMessage> brokeredMessages1 = brokeredMessages;
			this.innerCollection = brokeredMessages;
			if (brokeredMessages1 == null)
			{
				this.innerCollection = new List<BrokeredMessage>(enumerable);
			}
		}

		public void Add(BrokeredMessage message)
		{
			this.innerCollection.Add(message);
		}

		public void Clear()
		{
			this.innerCollection.Clear();
		}

		public bool Contains(BrokeredMessage message)
		{
			return this.innerCollection.Contains(message);
		}

		public void CopyTo(BrokeredMessage[] array, int arrayIndex)
		{
			this.innerCollection.CopyTo(array, arrayIndex);
		}

		public IEnumerator<BrokeredMessage> GetEnumerator()
		{
			return this.innerCollection.GetEnumerator();
		}

		public bool Remove(BrokeredMessage message)
		{
			return this.innerCollection.Remove(message);
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.innerCollection.GetEnumerator();
		}

		internal static MessageCollection Wrap(IEnumerable<BrokeredMessage> enumerableMessages)
		{
			return enumerableMessages as MessageCollection ?? new MessageCollection(enumerableMessages);
		}
	}
}