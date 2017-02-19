using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Amqp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class EventData : IDisposable
	{
		private volatile BrokeredMessage receivedMessage;

		private volatile Stream bodyStream;

		private bool disposed;

		private bool ownsBodyStream;

		private int getBodyCalled;

		internal Stream BodyStream
		{
			get
			{
				return this.bodyStream;
			}
		}

		internal ArraySegment<byte> DeliveryTag
		{
			get;
			set;
		}

		public DateTime EnqueuedTimeUtc
		{
			get
			{
				return this.GetSystemProperty<DateTime>("EnqueuedTimeUtc");
			}
			internal set
			{
				this.SystemProperties["EnqueuedTimeUtc"] = value;
			}
		}

		public string Offset
		{
			get
			{
				return this.GetSystemProperty<string>("Offset");
			}
			internal set
			{
				this.SystemProperties["Offset"] = value;
			}
		}

		public string PartitionKey
		{
			get
			{
				return this.GetSystemProperty<string>("PartitionKey");
			}
			set
			{
				this.SystemProperties["PartitionKey"] = value;
			}
		}

		public IDictionary<string, object> Properties
		{
			get;
			private set;
		}

		internal string Publisher
		{
			get
			{
				return this.GetSystemProperty<string>("Publisher");
			}
			set
			{
				this.SystemProperties["Publisher"] = value;
			}
		}

		public long SequenceNumber
		{
			get
			{
				return this.GetSystemProperty<long>("SequenceNumber");
			}
			internal set
			{
				this.SystemProperties["SequenceNumber"] = value;
			}
		}

		public IDictionary<string, object> SystemProperties
		{
			get;
			private set;
		}

		public EventData()
		{
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			this.SystemProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
			this.InitializeWithStream(Stream.Null, true);
		}

		public EventData(Stream stream) : this()
		{
			if (stream != null)
			{
				this.InitializeWithStream(stream, false);
			}
		}

		public EventData(byte[] byteArray) : this(new MemoryStream(byteArray))
		{
			this.ownsBodyStream = true;
		}

		public EventData(object content, XmlObjectSerializer serializer) : this()
		{
			if (content == null)
			{
				this.InitializeWithStream(Stream.Null, true);
				return;
			}
			if (content.GetType().IsSubclassOf(typeof(Stream)) && serializer == null)
			{
				this.InitializeWithStream((Stream)content, false);
				return;
			}
			if (serializer == null)
			{
				throw Fx.Exception.ArgumentNull("serializer");
			}
			MemoryStream memoryStream = new MemoryStream();
			serializer.WriteObject(memoryStream, content);
			memoryStream.Seek((long)0, SeekOrigin.Begin);
			this.InitializeWithStream(memoryStream, true);
		}

		internal EventData(BrokeredMessage brokeredMessage) : this()
		{
			if (brokeredMessage == null)
			{
				throw Fx.Exception.ArgumentNull("brokeredMessage");
			}
			this.receivedMessage = brokeredMessage;
			this.EnqueuedTimeUtc = brokeredMessage.EnqueuedTimeUtc;
			this.PartitionKey = brokeredMessage.PartitionKey;
			this.SequenceNumber = brokeredMessage.SequenceNumber;
			this.Offset = brokeredMessage.EnqueuedSequenceNumber.ToString(NumberFormatInfo.InvariantInfo);
			this.Publisher = brokeredMessage.Publisher;
			foreach (KeyValuePair<string, object> property in brokeredMessage.Properties)
			{
				this.Properties.Add(property);
			}
		}

		internal EventData(AmqpMessage amqpMessage) : this()
		{
			if (amqpMessage == null)
			{
				throw Fx.Exception.ArgumentNull("amqpMessage");
			}
			MessageConverter.UpdateEventDataHeaderAndProperties(amqpMessage, this);
			this.InitializeWithStream(amqpMessage.BodyStream, true);
		}

		public EventData Clone()
		{
			this.ThrowIfDisposed();
			EventData eventDatum = new EventData();
			if (this.bodyStream != null)
			{
				EventData eventDatum1 = new EventData(EventData.CloneStream(this.bodyStream))
				{
					ownsBodyStream = true
				};
				eventDatum = eventDatum1;
			}
			foreach (KeyValuePair<string, object> systemProperty in this.SystemProperties)
			{
				eventDatum.SystemProperties.Add(systemProperty);
			}
			foreach (KeyValuePair<string, object> property in this.Properties)
			{
				eventDatum.Properties.Add(property);
			}
			if (this.receivedMessage != null)
			{
				eventDatum.receivedMessage = this.receivedMessage.Clone();
			}
			return eventDatum;
		}

		private static Stream CloneStream(Stream originalStream)
		{
			if (originalStream == null)
			{
				return null;
			}
			MemoryStream memoryStream = originalStream as MemoryStream;
			MemoryStream memoryStream1 = memoryStream;
			if (memoryStream != null)
			{
				return new MemoryStream(memoryStream1.ToArray(), 0, (int)memoryStream1.Length, false, true);
			}
			ICloneable cloneable = originalStream as ICloneable;
			ICloneable cloneable1 = cloneable;
			if (cloneable != null)
			{
				return (Stream)cloneable1.Clone();
			}
			if (originalStream.Length != (long)0)
			{
				throw Fx.AssertAndThrow(string.Concat("Does not support cloning of Stream Type: ", originalStream.GetType()));
			}
			return Stream.Null;
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.receivedMessage != null)
					{
						this.receivedMessage.Dispose();
						this.receivedMessage = null;
					}
					if (this.bodyStream != null && this.ownsBodyStream)
					{
						this.bodyStream.Dispose();
						this.bodyStream = null;
					}
				}
				this.disposed = true;
			}
		}

		public T GetBody<T>()
		where T : Stream
		{
			return (T)this.GetBodyStream();
		}

		public T GetBody<T>(XmlObjectSerializer bodySerializer)
		{
			this.ThrowIfDisposed();
			if (typeof(T).IsSubclassOf(typeof(Stream)))
			{
				throw new InvalidOperationException("Should not use a serializer to get a stream.");
			}
			if (bodySerializer == null)
			{
				throw new ArgumentNullException("bodySerializer");
			}
			this.SetGetBodyCalled();
			if (this.bodyStream == null || this.bodyStream == Stream.Null)
			{
				if (this.receivedMessage == null)
				{
					return default(T);
				}
				return this.receivedMessage.GetBody<T>(bodySerializer);
			}
			if (this.bodyStream.CanSeek)
			{
				this.bodyStream.Seek((long)0, SeekOrigin.Begin);
			}
			return (T)bodySerializer.ReadObject(this.bodyStream);
		}

		public Stream GetBodyStream()
		{
			this.ThrowIfDisposed();
			this.SetGetBodyCalled();
			if (this.bodyStream != null)
			{
				return this.bodyStream;
			}
			if (this.receivedMessage == null)
			{
				return Stream.Null;
			}
			return this.receivedMessage.GetBody<Stream>();
		}

		public byte[] GetBytes()
		{
			this.ThrowIfDisposed();
			this.SetGetBodyCalled();
			if (this.bodyStream == null && this.receivedMessage == null)
			{
				return new byte[0];
			}
			if (this.receivedMessage != null)
			{
				this.bodyStream = this.receivedMessage.GetBody<Stream>();
			}
			BufferListStream bufferListStream = this.bodyStream as BufferListStream;
			BufferListStream bufferListStream1 = bufferListStream;
			if (bufferListStream == null)
			{
				return EventData.ReadFullStream(this.bodyStream);
			}
			byte[] numArray = new byte[checked((IntPtr)bufferListStream1.Length)];
			bufferListStream1.Read(numArray, 0, (int)numArray.Length);
			return numArray;
		}

		private T GetSystemProperty<T>(string key)
		{
			if (!this.SystemProperties.ContainsKey(key))
			{
				return default(T);
			}
			return (T)this.SystemProperties[key];
		}

		private void InitializeWithStream(Stream stream, bool ownsStream)
		{
			this.bodyStream = stream;
			this.ownsBodyStream = ownsStream;
		}

		private AmqpMessage PopulateAmqpMessageForSend(AmqpMessage message)
		{
			MessageConverter.UpdateAmqpMessageHeadersAndProperties(message, this, true);
			return message;
		}

		private BrokeredMessage PopulateSbmpMessageForSend(BrokeredMessage message)
		{
			message.PartitionKey = this.PartitionKey;
			message.Publisher = this.Publisher;
			foreach (KeyValuePair<string, object> property in this.Properties)
			{
				message.Properties.Add(property);
			}
			return message;
		}

		private static byte[] ReadFullStream(Stream inputStream)
		{
			byte[] array;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				inputStream.CopyTo(memoryStream);
				array = memoryStream.ToArray();
			}
			return array;
		}

		internal void ResetGetBodyCalled()
		{
			Interlocked.Exchange(ref this.getBodyCalled, 0);
			if (this.bodyStream != null && this.bodyStream.CanSeek)
			{
				this.bodyStream.Seek((long)0, SeekOrigin.Begin);
			}
		}

		private void SetGetBodyCalled()
		{
			if (1 == Interlocked.Exchange(ref this.getBodyCalled, 1))
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.MessageBodyConsumed), null);
			}
		}

		private void ThrowIfDisposed()
		{
			if (this.disposed)
			{
				throw Fx.Exception.ObjectDisposed(SRClient.EventDataDisposed);
			}
		}

		internal AmqpMessage ToAmqpMessage()
		{
			this.ThrowIfDisposed();
			return this.PopulateAmqpMessageForSend((this.bodyStream == null ? AmqpMessage.Create() : AmqpMessage.Create(this.bodyStream, false)));
		}

		internal BrokeredMessage ToBrokeredMessage()
		{
			this.ThrowIfDisposed();
			if (this.bodyStream == null || this.bodyStream.Length == (long)0)
			{
				return this.PopulateSbmpMessageForSend(new BrokeredMessage());
			}
			return this.PopulateSbmpMessageForSend(new BrokeredMessage(this.bodyStream, this.ownsBodyStream));
		}
	}
}