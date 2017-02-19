using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal abstract class Delivery : IDisposable
	{
		private volatile bool settled;

		private volatile bool stateChanged;

		public bool Batchable
		{
			get;
			set;
		}

		public long BytesTransfered
		{
			get;
			protected set;
		}

		public SequenceNumber DeliveryId
		{
			get;
			set;
		}

		public ArraySegment<byte> DeliveryTag
		{
			get;
			set;
		}

		public AmqpLink Link
		{
			get;
			set;
		}

		public uint? MessageFormat
		{
			get;
			set;
		}

		public Delivery Next
		{
			get;
			set;
		}

		public Delivery Previous
		{
			get;
			set;
		}

		public List<ByteBuffer> RawByteBuffers
		{
			get;
			protected set;
		}

		public bool Settled
		{
			get
			{
				return this.settled;
			}
			set
			{
				this.settled = value;
			}
		}

		public DeliveryState State
		{
			get;
			set;
		}

		public bool StateChanged
		{
			get
			{
				return this.stateChanged;
			}
			set
			{
				this.stateChanged = value;
			}
		}

		public ArraySegment<byte> TxnId
		{
			get;
			set;
		}

		protected Delivery()
		{
		}

		public static void Add(ref Delivery first, ref Delivery last, Delivery delivery)
		{
			if (first == null)
			{
				Delivery delivery1 = delivery;
				Delivery delivery2 = delivery1;
				last = delivery1;
				first = delivery2;
				return;
			}
			last.Next = delivery;
			delivery.Previous = last;
			last = delivery;
		}

		public virtual void AddPayload(ByteBuffer payload, bool isLast)
		{
			throw new InvalidOperationException();
		}

		public void CompletePayload(int payloadSize)
		{
			Delivery bytesTransfered = this;
			bytesTransfered.BytesTransfered = bytesTransfered.BytesTransfered + (long)payloadSize;
			this.OnCompletePayload(payloadSize);
		}

		public void Dispose()
		{
			if (this.RawByteBuffers != null)
			{
				foreach (ByteBuffer rawByteBuffer in this.RawByteBuffers)
				{
					rawByteBuffer.Dispose();
				}
			}
		}

		public abstract ArraySegment<byte>[] GetPayload(int payloadSize, out bool more);

		protected abstract void OnCompletePayload(int payloadSize);

		public void PrepareForSend()
		{
			this.BytesTransfered = (long)0;
		}

		public static void Remove(ref Delivery first, ref Delivery last, Delivery delivery)
		{
			if (delivery == first)
			{
				first = delivery.Next;
				if (first != null)
				{
					first.Previous = null;
				}
				else
				{
					last = null;
				}
			}
			else if (delivery == last)
			{
				last = delivery.Previous;
				last.Next = null;
			}
			else if (delivery.Previous != null && delivery.Next != null)
			{
				delivery.Previous.Next = delivery.Next;
				delivery.Next.Previous = delivery.Previous;
			}
			delivery.Previous = null;
			delivery.Next = null;
		}
	}
}