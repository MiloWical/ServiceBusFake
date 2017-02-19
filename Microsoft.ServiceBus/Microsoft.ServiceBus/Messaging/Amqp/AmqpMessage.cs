using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal abstract class AmqpMessage : Delivery
	{
		private Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header;

		private Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations deliveryAnnotations;

		private Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations messageAnnotations;

		private Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties properties;

		private Microsoft.ServiceBus.Messaging.Amqp.Framing.ApplicationProperties applicationProperties;

		private Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer footer;

		private SectionFlag sectionFlags;

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.ApplicationProperties ApplicationProperties
		{
			get
			{
				this.EnsureInitialized<Microsoft.ServiceBus.Messaging.Amqp.Framing.ApplicationProperties>(ref this.applicationProperties, SectionFlag.ApplicationProperties);
				return this.applicationProperties;
			}
			internal set
			{
				this.applicationProperties = value;
				this.UpdateSectionFlag(value != null, SectionFlag.ApplicationProperties);
			}
		}

		internal long BodySectionLength
		{
			get;
			set;
		}

		internal long BodySectionOffset
		{
			get;
			set;
		}

		public virtual Stream BodyStream
		{
			get
			{
				throw new InvalidOperationException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public SectionFlag BodyType
		{
			get
			{
				this.Deserialize(SectionFlag.All);
				return this.sectionFlags & SectionFlag.Body;
			}
		}

		public virtual IEnumerable<Data> DataBody
		{
			get
			{
				throw new InvalidOperationException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations DeliveryAnnotations
		{
			get
			{
				this.EnsureInitialized<Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations>(ref this.deliveryAnnotations, SectionFlag.DeliveryAnnotations);
				return this.deliveryAnnotations;
			}
			protected set
			{
				this.deliveryAnnotations = value;
				this.UpdateSectionFlag(value != null, SectionFlag.DeliveryAnnotations);
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer Footer
		{
			get
			{
				this.EnsureInitialized<Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer>(ref this.footer, SectionFlag.Footer);
				return this.footer;
			}
			protected set
			{
				this.footer = value;
				this.UpdateSectionFlag(value != null, SectionFlag.Footer);
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Header Header
		{
			get
			{
				this.EnsureInitialized<Microsoft.ServiceBus.Messaging.Amqp.Framing.Header>(ref this.header, SectionFlag.Header);
				return this.header;
			}
			protected set
			{
				this.header = value;
				this.UpdateSectionFlag(value != null, SectionFlag.Header);
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations MessageAnnotations
		{
			get
			{
				this.EnsureInitialized<Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations>(ref this.messageAnnotations, SectionFlag.MessageAnnotations);
				return this.messageAnnotations;
			}
			protected set
			{
				this.messageAnnotations = value;
				this.UpdateSectionFlag(value != null, SectionFlag.MessageAnnotations);
			}
		}

		public Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties Properties
		{
			get
			{
				this.EnsureInitialized<Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties>(ref this.properties, SectionFlag.Properties);
				return this.properties;
			}
			protected set
			{
				this.properties = value;
				this.UpdateSectionFlag(value != null, SectionFlag.Properties);
			}
		}

		public SectionFlag Sections
		{
			get
			{
				this.Deserialize(SectionFlag.All);
				return this.sectionFlags;
			}
		}

		public virtual IEnumerable<AmqpSequence> SequenceBody
		{
			get
			{
				throw new InvalidOperationException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		internal abstract long SerializedMessageSize
		{
			get;
		}

		public virtual AmqpValue ValueBody
		{
			get
			{
				throw new InvalidOperationException();
			}
			set
			{
				throw new InvalidOperationException();
			}
		}

		protected AmqpMessage()
		{
		}

		public AmqpMessage Clone()
		{
			bool flag;
			if (base.RawByteBuffers == null)
			{
				if (base.BytesTransfered > (long)0)
				{
					throw Fx.Exception.AsWarning(new InvalidOperationException(SRAmqp.AmqpCannotCloneSentMessage), null);
				}
				ArraySegment<byte>[] payload = this.GetPayload(2147483647, out flag);
				return new AmqpMessage.AmqpStreamMessage(new BufferListStream(payload), true);
			}
			ArraySegment<byte>[] nums = new ArraySegment<byte>[base.RawByteBuffers.Count];
			for (int i = 0; i < base.RawByteBuffers.Count; i++)
			{
				ByteBuffer byteBuffer = (ByteBuffer)base.RawByteBuffers[i].Clone();
				nums[i] = new ArraySegment<byte>(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length);
			}
			return new AmqpMessage.AmqpStreamMessage(new BufferListStream(nums), true);
		}

		public static AmqpMessage Create()
		{
			return new AmqpMessage.AmqpEmptyMessage();
		}

		public static AmqpMessage Create(Data data)
		{
			return AmqpMessage.Create(new Data[] { data });
		}

		public static AmqpMessage Create(IEnumerable<Data> dataList)
		{
			return new AmqpMessage.AmqpDataMessage(dataList);
		}

		public static AmqpMessage Create(AmqpValue value)
		{
			return new AmqpMessage.AmqpValueMessage(value);
		}

		public static AmqpMessage Create(IEnumerable<AmqpSequence> amqpSequence)
		{
			return new AmqpMessage.AmqpSequenceMessage(amqpSequence);
		}

		public static AmqpMessage Create(Stream stream, bool ownStream)
		{
			return new AmqpMessage.AmqpBodyStreamMessage(stream, ownStream);
		}

		internal static AmqpMessage CreateAmqpStreamMessage(BufferListStream messageStream)
		{
			return new AmqpMessage.AmqpStreamMessage(messageStream);
		}

		internal static AmqpMessage CreateAmqpStreamMessage(BufferListStream messagerStream, bool payloadInitialized)
		{
			return new AmqpMessage.AmqpStreamMessage(messagerStream, payloadInitialized);
		}

		internal static AmqpMessage CreateAmqpStreamMessage(Stream nonBodyStream, Stream bodyStream, bool forceCopyStream)
		{
			return new AmqpMessage.AmqpStreamMessage(nonBodyStream, bodyStream, forceCopyStream);
		}

		internal static AmqpMessage CreateAmqpStreamMessageBody(Stream bodyStream)
		{
			return new AmqpMessage.AmqpStreamMessage(BufferListStream.Create(bodyStream, 512));
		}

		internal static AmqpMessage CreateAmqpStreamMessageHeader(BufferListStream nonBodyStream)
		{
			return new AmqpMessage.AmqpStreamMessageHeader(nonBodyStream);
		}

		internal static AmqpMessage CreateOutputMessage(BufferListStream stream, bool ownStream)
		{
			return new AmqpMessage.AmqpOutputStreamMessage(stream, ownStream);
		}

		internal static AmqpMessage CreateReceivedMessage()
		{
			return new AmqpMessage.AmqpStreamMessage();
		}

		public virtual void Deserialize(SectionFlag desiredSections)
		{
		}

		private void EncodeSection(ByteBuffer buffer, IAmqpSerializable section)
		{
			if (section != null)
			{
				section.Encode(buffer);
			}
		}

		protected virtual void EnsureInitialized<T>(ref T obj, SectionFlag section)
		where T : class, new()
		{
			if (AmqpMessage.EnsureInitialized<T>(ref obj))
			{
				AmqpMessage amqpMessage = this;
				amqpMessage.sectionFlags = amqpMessage.sectionFlags | section;
			}
		}

		private static bool EnsureInitialized<T>(ref T obj)
		where T : class, new()
		{
			if (obj != null)
			{
				return false;
			}
			obj = Activator.CreateInstance<T>();
			return true;
		}

		internal virtual Stream GetBodySectionStream()
		{
			throw new InvalidOperationException();
		}

		internal virtual Stream GetNonBodySectionsStream()
		{
			throw new InvalidOperationException();
		}

		private int GetSectionSize(IAmqpSerializable section)
		{
			if (section == null)
			{
				return 0;
			}
			return section.EncodeSize;
		}

		public void Modify(Modified modified)
		{
			foreach (KeyValuePair<MapKey, object> messageAnnotation in (IEnumerable<KeyValuePair<MapKey, object>>)modified.MessageAnnotations)
			{
				this.MessageAnnotations.Map[messageAnnotation.Key] = messageAnnotation.Value;
			}
		}

		public virtual Stream ToStream()
		{
			throw new InvalidOperationException();
		}

		private void UpdateSectionFlag(bool set, SectionFlag flag)
		{
			if (set)
			{
				AmqpMessage amqpMessage = this;
				amqpMessage.sectionFlags = amqpMessage.sectionFlags | flag;
				return;
			}
			AmqpMessage amqpMessage1 = this;
			amqpMessage1.sectionFlags = amqpMessage1.sectionFlags & ~flag;
		}

		internal virtual long Write(XmlWriter writer)
		{
			throw new InvalidOperationException();
		}

		private sealed class AmqpBodyStreamMessage : AmqpMessage.AmqpBufferedMessage
		{
			private readonly Stream bodyStream;

			private readonly bool ownStream;

			private ArraySegment<byte>[] bodyData;

			private int bodyLength;

			public override Stream BodyStream
			{
				get
				{
					return new BufferListStream(this.bodyData);
				}
				set
				{
					base.BodyStream = value;
				}
			}

			public AmqpBodyStreamMessage(Stream bodyStream, bool ownStream)
			{
				AmqpMessage.AmqpBodyStreamMessage amqpBodyStreamMessage = this;
				amqpBodyStreamMessage.sectionFlags = amqpBodyStreamMessage.sectionFlags | SectionFlag.Data;
				this.bodyStream = bodyStream;
				this.ownStream = ownStream;
			}

			protected override void AddCustomSegments(List<ArraySegment<byte>> segmentList)
			{
				if (this.bodyLength > 0)
				{
					segmentList.Add(Data.GetEncodedPrefix(this.bodyLength));
					segmentList.AddRange(this.bodyData);
				}
			}

			protected override void EncodeBody(ByteBuffer buffer)
			{
			}

			protected override int GetBodySize()
			{
				return 0;
			}

			protected override void OnInitialize()
			{
				this.bodyData = BufferListStream.ReadStream(this.bodyStream, 1024, out this.bodyLength);
				if (this.ownStream)
				{
					this.bodyStream.Dispose();
				}
			}
		}

		private abstract class AmqpBufferedMessage : AmqpMessage
		{
			private BufferListStream bufferStream;

			private bool initialized;

			internal override long SerializedMessageSize
			{
				get
				{
					this.EnsureInitialized();
					return this.bufferStream.Length;
				}
			}

			protected AmqpBufferedMessage()
			{
			}

			protected virtual void AddCustomSegments(List<ArraySegment<byte>> segmentList)
			{
			}

			protected abstract void EncodeBody(ByteBuffer buffer);

			protected void EnsureInitialized()
			{
				if (!this.initialized)
				{
					this.Initialize();
					this.initialized = true;
				}
			}

			protected abstract int GetBodySize();

			public override ArraySegment<byte>[] GetPayload(int payloadSize, out bool more)
			{
				this.EnsureInitialized();
				return this.bufferStream.ReadBuffers(payloadSize, false, out more);
			}

			private void Initialize()
			{
				this.OnInitialize();
				int sectionSize = base.GetSectionSize(this.header) + base.GetSectionSize(this.deliveryAnnotations) + base.GetSectionSize(this.messageAnnotations) + base.GetSectionSize(this.properties) + base.GetSectionSize(this.applicationProperties) + this.GetBodySize() + base.GetSectionSize(this.footer);
				List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>(4);
				if (sectionSize != 0)
				{
					ByteBuffer byteBuffer = new ByteBuffer(new byte[sectionSize]);
					int length = 0;
					base.EncodeSection(byteBuffer, this.header);
					base.EncodeSection(byteBuffer, this.deliveryAnnotations);
					base.EncodeSection(byteBuffer, this.messageAnnotations);
					base.EncodeSection(byteBuffer, this.properties);
					base.EncodeSection(byteBuffer, this.applicationProperties);
					if (byteBuffer.Length > 0)
					{
						arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, byteBuffer.Length));
					}
					length = byteBuffer.Length;
					this.EncodeBody(byteBuffer);
					int num = byteBuffer.Length - length;
					if (num > 0)
					{
						arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, num));
					}
					this.AddCustomSegments(arraySegments);
					if (this.footer != null)
					{
						length = byteBuffer.Length;
						base.EncodeSection(byteBuffer, this.footer);
						arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, byteBuffer.Length - length));
					}
				}
				else
				{
					this.AddCustomSegments(arraySegments);
				}
				this.bufferStream = new BufferListStream(arraySegments.ToArray());
			}

			protected override void OnCompletePayload(int payloadSize)
			{
				long position = this.bufferStream.Position;
				this.bufferStream.Position = position + (long)payloadSize;
			}

			protected virtual void OnInitialize()
			{
			}

			public override Stream ToStream()
			{
				bool flag;
				return new BufferListStream(this.GetPayload(2147483647, out flag));
			}
		}

		private sealed class AmqpDataMessage : AmqpMessage.AmqpBufferedMessage
		{
			private readonly IEnumerable<Data> dataList;

			public override IEnumerable<Data> DataBody
			{
				get
				{
					return this.dataList;
				}
			}

			public AmqpDataMessage(IEnumerable<Data> dataList)
			{
				this.dataList = dataList;
				AmqpMessage.AmqpDataMessage amqpDataMessage = this;
				amqpDataMessage.sectionFlags = amqpDataMessage.sectionFlags | SectionFlag.Data;
			}

			protected override void AddCustomSegments(List<ArraySegment<byte>> segmentList)
			{
				foreach (Data datum in this.dataList)
				{
					ArraySegment<byte> value = (ArraySegment<byte>)datum.Value;
					segmentList.Add(Data.GetEncodedPrefix(value.Count));
					segmentList.Add(value);
				}
			}

			protected override void EncodeBody(ByteBuffer buffer)
			{
			}

			protected override int GetBodySize()
			{
				return 0;
			}
		}

		private sealed class AmqpEmptyMessage : AmqpMessage.AmqpBufferedMessage
		{
			public AmqpEmptyMessage()
			{
			}

			protected override void EncodeBody(ByteBuffer buffer)
			{
			}

			protected override int GetBodySize()
			{
				return 0;
			}
		}

		private sealed class AmqpInputStreamMessage : AmqpMessage
		{
			private readonly BufferListStream bufferStream;

			private bool deserialized;

			private IEnumerable<Data> dataList;

			private IEnumerable<AmqpSequence> sequenceList;

			private AmqpValue amqpValue;

			private Stream bodyStream;

			public override Stream BodyStream
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.bodyStream;
				}
				set
				{
					this.bodyStream = value;
				}
			}

			public override IEnumerable<Data> DataBody
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.dataList;
				}
				set
				{
					this.dataList = value;
					base.UpdateSectionFlag(value != null, SectionFlag.Data);
				}
			}

			public override IEnumerable<AmqpSequence> SequenceBody
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.sequenceList;
				}
				set
				{
					this.sequenceList = value;
					base.UpdateSectionFlag(value != null, SectionFlag.AmqpSequence);
				}
			}

			internal override long SerializedMessageSize
			{
				get
				{
					return this.bufferStream.Length;
				}
			}

			public override AmqpValue ValueBody
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.amqpValue;
				}
				set
				{
					this.amqpValue = value;
					base.UpdateSectionFlag(value != null, SectionFlag.AmqpValue);
				}
			}

			public AmqpInputStreamMessage(BufferListStream bufferStream)
			{
				this.bufferStream = bufferStream;
			}

			public override void Deserialize(SectionFlag desiredSections)
			{
				if (!this.deserialized)
				{
					BufferListStream bufferListStream = (BufferListStream)this.bufferStream.Clone();
					(new AmqpMessage.AmqpMessageReader(bufferListStream)).ReadMessage(this, desiredSections);
					bufferListStream.Dispose();
					this.deserialized = true;
				}
			}

			protected override void EnsureInitialized<T>(ref T obj, SectionFlag section)
			where T : class, new()
			{
				this.Deserialize(SectionFlag.All);
			}

			public override ArraySegment<byte>[] GetPayload(int payloadSize, out bool more)
			{
				more = false;
				throw new InvalidOperationException();
			}

			protected override void OnCompletePayload(int payloadSize)
			{
				throw new InvalidOperationException();
			}

			public override Stream ToStream()
			{
				return this.bufferStream;
			}
		}

		private sealed class AmqpMessageReader
		{
			private static Dictionary<string, ulong> sectionCodeByName;

			private static Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>[] sectionReaders;

			private readonly BufferListStream stream;

			private List<Data> dataList;

			private List<AmqpSequence> sequenceList;

			private AmqpValue amqpValue;

			private List<ArraySegment<byte>> bodyBuffers;

			static AmqpMessageReader()
			{
				Dictionary<string, ulong> strs = new Dictionary<string, ulong>()
				{
					{ Microsoft.ServiceBus.Messaging.Amqp.Framing.Header.Name, Microsoft.ServiceBus.Messaging.Amqp.Framing.Header.Code },
					{ Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations.Name, Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations.Code },
					{ Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations.Name, Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations.Code },
					{ Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.Name, Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties.Code },
					{ Microsoft.ServiceBus.Messaging.Amqp.Framing.ApplicationProperties.Name, Microsoft.ServiceBus.Messaging.Amqp.Framing.ApplicationProperties.Code },
					{ Data.Name, Data.Code },
					{ AmqpSequence.Name, AmqpSequence.Code },
					{ AmqpValue.Name, AmqpValue.Code },
					{ Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer.Name, Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer.Code }
				};
				AmqpMessage.AmqpMessageReader.sectionCodeByName = strs;
				Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>[] action = new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>[] { new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadHeaderSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadDeliveryAnnotationsSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadMessageAnnotationsSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadPropertiesSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadApplicationPropertiesSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadDataSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadAmqpSequenceSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadAmqpValueSection), new Action<AmqpMessage.AmqpMessageReader, AmqpMessage, long>(AmqpMessage.AmqpMessageReader.ReadFooterSection) };
				AmqpMessage.AmqpMessageReader.sectionReaders = action;
			}

			public AmqpMessageReader(BufferListStream stream)
			{
				this.stream = stream;
			}

			private void AddBodyBuffer(ArraySegment<byte> buffer)
			{
				AmqpMessage.EnsureInitialized<List<ArraySegment<byte>>>(ref this.bodyBuffers);
				this.bodyBuffers.Add(buffer);
			}

			private static void ReadAmqpSequenceSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				AmqpMessage.EnsureInitialized<List<AmqpSequence>>(ref reader.sequenceList);
				reader.sequenceList.Add(AmqpMessage.AmqpMessageReader.ReadListSection<AmqpSequence>(reader, true));
			}

			private static void ReadAmqpValueSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				ArraySegment<byte> nums = reader.ReadBytes(2147483647);
				ByteBuffer byteBuffer = new ByteBuffer(nums);
				object obj = AmqpCodec.DecodeObject(byteBuffer);
				reader.amqpValue = new AmqpValue()
				{
					Value = obj
				};
				reader.AddBodyBuffer(nums);
				if (byteBuffer.Length > 0)
				{
					int length = byteBuffer.Length;
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer footer = new Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer();
					footer.Decode(byteBuffer);
					message.Footer = footer;
					message.footer.Offset = reader.stream.Position - (long)length;
					message.footer.Length = (long)length;
				}
			}

			private static void ReadApplicationPropertiesSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				message.ApplicationProperties = AmqpMessage.AmqpMessageReader.ReadMapSection<Microsoft.ServiceBus.Messaging.Amqp.Framing.ApplicationProperties>(reader);
				message.applicationProperties.Offset = startPosition;
				message.applicationProperties.Length = reader.stream.Position - startPosition;
			}

			private ArraySegment<byte> ReadBytes(int count)
			{
				ArraySegment<byte> nums = this.stream.ReadBytes(count);
				if (count != 2147483647 && nums.Count < count)
				{
					throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInsufficientBufferSize(count, nums.Count));
				}
				return nums;
			}

			private static void ReadDataSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				bool flag = reader.ReadFormatCode() == 160;
				ArraySegment<byte> nums = reader.ReadBytes(reader.ReadInt(flag));
				AmqpMessage.EnsureInitialized<List<Data>>(ref reader.dataList);
				reader.dataList.Add(new Data()
				{
					Value = nums
				});
				reader.AddBodyBuffer(nums);
			}

			private static void ReadDeliveryAnnotationsSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				message.DeliveryAnnotations = AmqpMessage.AmqpMessageReader.ReadMapSection<Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations>(reader);
				message.deliveryAnnotations.Offset = startPosition;
				message.deliveryAnnotations.Length = reader.stream.Position - startPosition;
			}

			private ulong ReadDescriptorCode()
			{
				FormatCode formatCode = this.ReadFormatCode();
				ulong num = (ulong)0;
				if (formatCode == 83)
				{
					num = (ulong)this.stream.ReadByte();
				}
				else if (formatCode == 128)
				{
					ArraySegment<byte> nums = this.ReadBytes(8);
					num = AmqpBitConverter.ReadULong(nums.Array, nums.Offset, 8);
				}
				else if (formatCode == 163 || formatCode == 179)
				{
					int num1 = this.ReadInt(formatCode == 163);
					ArraySegment<byte> nums1 = this.ReadBytes(num1);
					string str = System.Text.Encoding.ASCII.GetString(nums1.Array, nums1.Offset, num1);
					AmqpMessage.AmqpMessageReader.sectionCodeByName.TryGetValue(str, out num);
				}
				return num;
			}

			private static void ReadFooterSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				message.Footer = AmqpMessage.AmqpMessageReader.ReadMapSection<Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer>(reader);
				message.footer.Offset = startPosition;
				message.footer.Length = reader.stream.Position - startPosition;
			}

			private FormatCode ReadFormatCode()
			{
				byte num = (byte)this.stream.ReadByte();
				byte num1 = 0;
				if (FormatCode.HasExtType(num))
				{
					num1 = (byte)this.stream.ReadByte();
				}
				return new FormatCode(num, num1);
			}

			private static void ReadHeaderSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				message.Header = AmqpMessage.AmqpMessageReader.ReadListSection<Microsoft.ServiceBus.Messaging.Amqp.Framing.Header>(reader, false);
				message.header.Offset = startPosition;
				message.header.Length = reader.stream.Position - startPosition;
			}

			private int ReadInt(bool smallEncoding)
			{
				if (smallEncoding)
				{
					return this.stream.ReadByte();
				}
				ArraySegment<byte> nums = this.ReadBytes(4);
				return (int)AmqpBitConverter.ReadUInt(nums.Array, nums.Offset, 4);
			}

			private static T ReadListSection<T>(AmqpMessage.AmqpMessageReader reader, bool isBodySection = false)
			where T : DescribedList, new()
			{
				T t = Activator.CreateInstance<T>();
				long position = reader.stream.Position;
				FormatCode formatCode = reader.ReadFormatCode();
				if (formatCode == 69)
				{
					return t;
				}
				bool flag = formatCode == 192;
				int num = reader.ReadInt(flag);
				int num1 = reader.ReadInt(flag);
				if (num1 == 0)
				{
					return t;
				}
				long position1 = reader.stream.Position;
				ArraySegment<byte> nums = reader.ReadBytes(num - (flag ? 1 : 4));
				long position2 = reader.stream.Position;
				t.DecodeValue(new ByteBuffer(nums), num, num1);
				if (isBodySection)
				{
					reader.stream.Position = position;
					ArraySegment<byte> nums1 = reader.stream.ReadBytes((int)(position1 - position));
					reader.stream.Position = position2;
					reader.AddBodyBuffer(nums1);
					reader.AddBodyBuffer(nums);
				}
				return t;
			}

			private static T ReadMapSection<T>(AmqpMessage.AmqpMessageReader reader)
			where T : DescribedMap, new()
			{
				T t = Activator.CreateInstance<T>();
				bool flag = reader.ReadFormatCode() == 193;
				int num = reader.ReadInt(flag);
				int num1 = reader.ReadInt(flag);
				if (num1 > 0)
				{
					ArraySegment<byte> nums = reader.ReadBytes(num - (flag ? 1 : 4));
					t.DecodeValue(new ByteBuffer(nums), num, num1);
				}
				return t;
			}

			public void ReadMessage(AmqpMessage message, SectionFlag sections)
			{
				while (this.ReadSection(message, sections))
				{
				}
				if ((int)(sections & SectionFlag.Body) != 0)
				{
					if (this.dataList != null)
					{
						message.DataBody = this.dataList;
					}
					if (this.sequenceList != null)
					{
						message.SequenceBody = this.sequenceList;
					}
					if (this.amqpValue != null)
					{
						message.ValueBody = this.amqpValue;
					}
					if (this.bodyBuffers != null)
					{
						message.BodyStream = new BufferListStream(this.bodyBuffers.ToArray());
					}
				}
			}

			private static void ReadMessageAnnotationsSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				message.MessageAnnotations = AmqpMessage.AmqpMessageReader.ReadMapSection<Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations>(reader);
				message.messageAnnotations.Offset = startPosition;
				message.messageAnnotations.Length = reader.stream.Position - startPosition;
			}

			private static void ReadPropertiesSection(AmqpMessage.AmqpMessageReader reader, AmqpMessage message, long startPosition)
			{
				message.Properties = AmqpMessage.AmqpMessageReader.ReadListSection<Microsoft.ServiceBus.Messaging.Amqp.Framing.Properties>(reader, false);
				message.properties.Offset = startPosition;
				message.properties.Length = reader.stream.Position - startPosition;
			}

			private bool ReadSection(AmqpMessage message, SectionFlag sections)
			{
				long position = this.stream.Position;
				if (position == this.stream.Length)
				{
					return false;
				}
				FormatCode formatCode = this.ReadFormatCode();
				if (formatCode != 0)
				{
					throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidFormatCode(formatCode, this.stream.Position));
				}
				ulong num = this.ReadDescriptorCode();
				if (num < Microsoft.ServiceBus.Messaging.Amqp.Framing.Header.Code || num > Microsoft.ServiceBus.Messaging.Amqp.Framing.Footer.Code)
				{
					throw AmqpEncoding.GetEncodingException(SRAmqp.AmqpInvalidMessageSectionCode(num));
				}
				int code = (int)(num - Microsoft.ServiceBus.Messaging.Amqp.Framing.Header.Code);
				SectionFlag sectionFlag = (SectionFlag)(1 << (code & 31));
				if ((int)(sectionFlag & sections) == 0)
				{
					this.stream.Position = position;
					return false;
				}
				AmqpMessage.AmqpMessageReader.sectionReaders[code](this, message, position);
				if ((int)(sectionFlag & SectionFlag.Body) != 0)
				{
					message.BodySectionOffset = position;
					message.BodySectionLength = this.stream.Position - position;
				}
				return true;
			}
		}

		private sealed class AmqpOutputStreamMessage : AmqpMessage.AmqpBufferedMessage
		{
			private readonly BufferListStream messageStream;

			private readonly bool ownStream;

			private ArraySegment<byte>[] buffers;

			public AmqpOutputStreamMessage(BufferListStream messageStream, bool ownStream)
			{
				this.messageStream = messageStream;
				this.ownStream = ownStream;
			}

			protected override void AddCustomSegments(List<ArraySegment<byte>> segmentList)
			{
				if (this.buffers != null && (int)this.buffers.Length > 0)
				{
					segmentList.AddRange(this.buffers);
				}
			}

			protected override void EncodeBody(ByteBuffer buffer)
			{
			}

			protected override int GetBodySize()
			{
				return 0;
			}

			protected override void OnInitialize()
			{
				BufferListStream bufferListStream = this.messageStream;
				if (!this.ownStream)
				{
					bufferListStream = (BufferListStream)bufferListStream.Clone();
				}
				try
				{
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header = this.header;
					Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations deliveryAnnotation = this.deliveryAnnotations;
					Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations messageAnnotation = this.messageAnnotations;
					this.header = null;
					this.deliveryAnnotations = null;
					this.messageAnnotations = null;
					(new AmqpMessage.AmqpMessageReader(bufferListStream)).ReadMessage(this, SectionFlag.Header | SectionFlag.DeliveryAnnotations | SectionFlag.MessageAnnotations);
					this.UpdateHeader(header);
					this.UpdateDeliveryAnnotations(deliveryAnnotation);
					this.UpdateMessageAnnotations(messageAnnotation);
					this.properties = null;
					this.applicationProperties = null;
					this.footer = null;
					bool flag = false;
					this.buffers = bufferListStream.ReadBuffers(2147483647, true, out flag);
				}
				finally
				{
					if (!this.ownStream)
					{
						bufferListStream.Dispose();
					}
				}
			}

			private void UpdateDeliveryAnnotations(Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations deliveryAnnotations)
			{
				if (deliveryAnnotations != null)
				{
					if (this.deliveryAnnotations == null)
					{
						base.DeliveryAnnotations = deliveryAnnotations;
						return;
					}
					foreach (KeyValuePair<MapKey, object> map in (IEnumerable<KeyValuePair<MapKey, object>>)this.deliveryAnnotations.Map)
					{
						deliveryAnnotations.Map[map.Key] = map.Value;
					}
					this.deliveryAnnotations = deliveryAnnotations;
				}
			}

			private void UpdateHeader(Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header)
			{
				if (header != null)
				{
					if (this.header == null)
					{
						base.Header = header;
						return;
					}
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header1 = this.header;
					bool? durable = this.header.Durable;
					header1.Durable = (durable.HasValue ? new bool?(durable.GetValueOrDefault()) : header.Durable);
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header2 = this.header;
					byte? priority = this.header.Priority;
					header2.Priority = (priority.HasValue ? new byte?(priority.GetValueOrDefault()) : header.Priority);
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header3 = this.header;
					uint? ttl = this.header.Ttl;
					header3.Ttl = (ttl.HasValue ? new uint?(ttl.GetValueOrDefault()) : header.Ttl);
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header4 = this.header;
					bool? firstAcquirer = this.header.FirstAcquirer;
					header4.FirstAcquirer = (firstAcquirer.HasValue ? new bool?(firstAcquirer.GetValueOrDefault()) : header.FirstAcquirer);
					Microsoft.ServiceBus.Messaging.Amqp.Framing.Header header5 = this.header;
					uint? deliveryCount = this.header.DeliveryCount;
					header5.DeliveryCount = (deliveryCount.HasValue ? new uint?(deliveryCount.GetValueOrDefault()) : header.DeliveryCount);
				}
			}

			private void UpdateMessageAnnotations(Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations messageAnnotations)
			{
				if (messageAnnotations != null)
				{
					if (this.messageAnnotations == null)
					{
						base.MessageAnnotations = messageAnnotations;
						return;
					}
					foreach (KeyValuePair<MapKey, object> map in (IEnumerable<KeyValuePair<MapKey, object>>)this.messageAnnotations.Map)
					{
						messageAnnotations.Map[map.Key] = map.Value;
					}
					this.messageAnnotations = messageAnnotations;
				}
			}
		}

		private sealed class AmqpSequenceMessage : AmqpMessage.AmqpBufferedMessage
		{
			private readonly IEnumerable<AmqpSequence> sequence;

			public override IEnumerable<AmqpSequence> SequenceBody
			{
				get
				{
					return this.sequence;
				}
			}

			public AmqpSequenceMessage(IEnumerable<AmqpSequence> sequence)
			{
				this.sequence = sequence;
				AmqpMessage.AmqpSequenceMessage amqpSequenceMessage = this;
				amqpSequenceMessage.sectionFlags = amqpSequenceMessage.sectionFlags | SectionFlag.AmqpSequence;
			}

			protected override void EncodeBody(ByteBuffer buffer)
			{
				foreach (AmqpSequence amqpSequence in this.sequence)
				{
					base.EncodeSection(buffer, amqpSequence);
				}
			}

			protected override int GetBodySize()
			{
				int sectionSize = 0;
				foreach (AmqpSequence amqpSequence in this.sequence)
				{
					sectionSize = sectionSize + base.GetSectionSize(amqpSequence);
				}
				return sectionSize;
			}
		}

		private sealed class AmqpStreamMessage : AmqpMessage
		{
			private readonly BufferListStream bodySection;

			private BufferListStream messageStream;

			private BufferListStream payloadStream;

			private IEnumerable<Data> dataList;

			private IEnumerable<AmqpSequence> sequenceList;

			private AmqpValue amqpValue;

			private Stream bodyStream;

			private bool payloadInitialized;

			private bool deserialized;

			public override Stream BodyStream
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.bodyStream;
				}
				set
				{
					this.bodyStream = value;
				}
			}

			public override IEnumerable<Data> DataBody
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.dataList;
				}
				set
				{
					this.dataList = value;
					base.UpdateSectionFlag(value != null, SectionFlag.Data);
				}
			}

			public override IEnumerable<AmqpSequence> SequenceBody
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.sequenceList;
				}
				set
				{
					this.sequenceList = value;
					base.UpdateSectionFlag(value != null, SectionFlag.AmqpSequence);
				}
			}

			internal override long SerializedMessageSize
			{
				get
				{
					if (this.payloadInitialized)
					{
						return this.payloadStream.Length;
					}
					return this.messageStream.Length + (this.bodySection == null ? (long)0 : this.bodySection.Length);
				}
			}

			public override AmqpValue ValueBody
			{
				get
				{
					this.Deserialize(SectionFlag.All);
					return this.amqpValue;
				}
				set
				{
					this.amqpValue = value;
					base.UpdateSectionFlag(value != null, SectionFlag.AmqpValue);
				}
			}

			public AmqpStreamMessage()
			{
				base.RawByteBuffers = new List<ByteBuffer>();
			}

			public AmqpStreamMessage(BufferListStream messageStream) : this(messageStream, false)
			{
			}

			public AmqpStreamMessage(BufferListStream messageStream, bool payloadInitialized)
			{
				if (messageStream == null)
				{
					throw FxTrace.Exception.ArgumentNullOrEmpty("bufferStream");
				}
				this.messageStream = messageStream;
				this.payloadInitialized = payloadInitialized;
				if (payloadInitialized)
				{
					this.payloadStream = this.messageStream;
				}
			}

			public AmqpStreamMessage(Stream nonBodySections, Stream bodySection, bool forceCopyStream)
			{
				if (nonBodySections == null)
				{
					throw FxTrace.Exception.ArgumentNull("nonBodySections");
				}
				this.messageStream = BufferListStream.Create(nonBodySections, 512, forceCopyStream);
				if (bodySection != null)
				{
					this.bodySection = BufferListStream.Create(bodySection, 512, forceCopyStream);
				}
			}

			public override void AddPayload(ByteBuffer payload, bool isLast)
			{
				base.RawByteBuffers.Add(payload);
				AmqpMessage.AmqpStreamMessage bytesTransfered = this;
				bytesTransfered.BytesTransfered = bytesTransfered.BytesTransfered + (long)payload.Length;
				if (isLast)
				{
					ArraySegment<byte>[] nums = new ArraySegment<byte>[base.RawByteBuffers.Count];
					for (int i = 0; i < base.RawByteBuffers.Count; i++)
					{
						ByteBuffer item = base.RawByteBuffers[i];
						nums[i] = new ArraySegment<byte>(item.Buffer, item.Offset, item.Length);
					}
					this.messageStream = new BufferListStream(nums);
				}
			}

			public override void Deserialize(SectionFlag desiredSections)
			{
				if (!this.deserialized)
				{
					BufferListStream bufferListStream = (BufferListStream)this.messageStream.Clone();
					(new AmqpMessage.AmqpMessageReader(bufferListStream)).ReadMessage(this, desiredSections);
					bufferListStream.Dispose();
					this.header = this.header ?? new Microsoft.ServiceBus.Messaging.Amqp.Framing.Header();
					this.messageAnnotations = this.messageAnnotations ?? new Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations();
					this.deserialized = true;
				}
			}

			private static BufferListStream Encode(AmqpMessage.AmqpStreamMessage message)
			{
				int sectionSize = message.GetSectionSize(message.header) + message.GetSectionSize(message.deliveryAnnotations) + message.GetSectionSize(message.messageAnnotations) + message.GetSectionSize(message.properties) + message.GetSectionSize(message.applicationProperties) + message.GetSectionSize(message.footer);
				List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>(4);
				ByteBuffer byteBuffer = new ByteBuffer(new byte[sectionSize]);
				int length = 0;
				message.EncodeSection(byteBuffer, message.header);
				message.EncodeSection(byteBuffer, message.deliveryAnnotations);
				message.EncodeSection(byteBuffer, message.messageAnnotations);
				message.EncodeSection(byteBuffer, message.properties);
				message.EncodeSection(byteBuffer, message.applicationProperties);
				if (byteBuffer.Length > 0)
				{
					arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, byteBuffer.Length));
				}
				ArraySegment<byte>[] bodySectionSegments = message.GetBodySectionSegments();
				if (bodySectionSegments != null)
				{
					arraySegments.AddRange(bodySectionSegments);
				}
				if (message.footer != null)
				{
					length = byteBuffer.Length;
					message.EncodeSection(byteBuffer, message.footer);
					arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, byteBuffer.Length - length));
				}
				return new BufferListStream(arraySegments.ToArray());
			}

			private void EncodeSection(ByteBuffer buffer, AmqpDescribed section)
			{
				if (section != null)
				{
					section.Offset = (long)buffer.Length;
					base.EncodeSection(buffer, section);
					section.Length = (long)buffer.Length - section.Offset;
				}
			}

			protected override void EnsureInitialized<T>(ref T obj, SectionFlag section)
			where T : class, new()
			{
				this.Deserialize(SectionFlag.All);
			}

			private ArraySegment<byte>[] GetBodySectionSegments()
			{
				bool flag;
				bool flag1;
				ArraySegment<byte>[] arraySegmentArrays;
				if (this.bodySection == null)
				{
					this.Deserialize(SectionFlag.All);
					if ((int)(this.sectionFlags & SectionFlag.Body) == 0)
					{
						return null;
					}
					using (BufferListStream bodySectionOffset = (BufferListStream)this.messageStream.Clone())
					{
						bodySectionOffset.Position = base.BodySectionOffset;
						arraySegmentArrays = bodySectionOffset.ReadBuffers((int)base.BodySectionLength, true, out flag1);
					}
				}
				else
				{
					using (BufferListStream bufferListStream = (BufferListStream)this.bodySection.Clone())
					{
						arraySegmentArrays = bufferListStream.ReadBuffers(2147483647, false, out flag);
					}
				}
				return arraySegmentArrays;
			}

			internal override Stream GetBodySectionStream()
			{
				bool flag;
				Stream bufferListStream;
				if (this.bodySection != null)
				{
					return (BufferListStream)this.bodySection.Clone();
				}
				this.Deserialize(SectionFlag.All);
				if ((int)(this.sectionFlags & SectionFlag.Body) == 0)
				{
					return null;
				}
				using (BufferListStream bodySectionOffset = (BufferListStream)this.messageStream.Clone())
				{
					bodySectionOffset.Position = base.BodySectionOffset;
					ArraySegment<byte>[] arraySegmentArrays = bodySectionOffset.ReadBuffers((int)base.BodySectionLength, true, out flag);
					bufferListStream = new BufferListStream(arraySegmentArrays);
				}
				return bufferListStream;
			}

			internal override Stream GetNonBodySectionsStream()
			{
				Stream bufferListStream;
				this.Deserialize(SectionFlag.All);
				using (BufferListStream bufferListStream1 = (BufferListStream)this.messageStream.Clone())
				{
					List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>();
					this.ReadSection(bufferListStream1, arraySegments, base.Header);
					this.ReadSection(bufferListStream1, arraySegments, base.DeliveryAnnotations);
					this.ReadSection(bufferListStream1, arraySegments, base.MessageAnnotations);
					this.ReadSection(bufferListStream1, arraySegments, base.Properties);
					this.ReadSection(bufferListStream1, arraySegments, base.ApplicationProperties);
					this.ReadSection(bufferListStream1, arraySegments, base.Footer);
					bufferListStream = new BufferListStream(arraySegments.ToArray());
				}
				return bufferListStream;
			}

			public override ArraySegment<byte>[] GetPayload(int payloadSize, out bool more)
			{
				if (!this.payloadInitialized)
				{
					this.payloadStream = AmqpMessage.AmqpStreamMessage.Encode(this);
					this.payloadInitialized = true;
				}
				return this.payloadStream.ReadBuffers(payloadSize, false, out more);
			}

			protected override void OnCompletePayload(int payloadSize)
			{
				long position = this.payloadStream.Position;
				this.payloadStream.Position = position + (long)payloadSize;
			}

			private void ReadSection(BufferListStream source, List<ArraySegment<byte>> target, AmqpDescribed section)
			{
				bool flag;
				if (section != null && section.Length > (long)0)
				{
					source.Position = section.Offset;
					ArraySegment<byte>[] arraySegmentArrays = source.ReadBuffers((int)section.Length, false, out flag);
					if (arraySegmentArrays != null && (int)arraySegmentArrays.Length > 0)
					{
						target.AddRange(arraySegmentArrays);
					}
				}
			}

			public override Stream ToStream()
			{
				return this.messageStream;
			}
		}

		private sealed class AmqpStreamMessageHeader : AmqpMessage
		{
			private readonly BufferListStream bufferStream;

			private bool deserialized;

			internal override long SerializedMessageSize
			{
				get
				{
					return this.bufferStream.Length;
				}
			}

			public AmqpStreamMessageHeader(BufferListStream headerStream)
			{
				if (headerStream == null)
				{
					throw FxTrace.Exception.ArgumentNullOrEmpty("headerStream");
				}
				this.bufferStream = headerStream;
			}

			public override void Deserialize(SectionFlag desiredSections)
			{
				if (!this.deserialized)
				{
					BufferListStream bufferListStream = (BufferListStream)this.bufferStream.Clone();
					(new AmqpMessage.AmqpMessageReader(bufferListStream)).ReadMessage(this, desiredSections);
					bufferListStream.Dispose();
					this.header = this.header ?? new Microsoft.ServiceBus.Messaging.Amqp.Framing.Header();
					this.deliveryAnnotations = this.deliveryAnnotations ?? new Microsoft.ServiceBus.Messaging.Amqp.Framing.DeliveryAnnotations();
					this.messageAnnotations = this.messageAnnotations ?? new Microsoft.ServiceBus.Messaging.Amqp.Framing.MessageAnnotations();
					this.deserialized = true;
				}
			}

			private static List<ArraySegment<byte>> Encode(AmqpMessage.AmqpStreamMessageHeader message)
			{
				int sectionSize = message.GetSectionSize(message.header) + message.GetSectionSize(message.deliveryAnnotations) + message.GetSectionSize(message.messageAnnotations) + message.GetSectionSize(message.properties) + message.GetSectionSize(message.applicationProperties) + message.GetSectionSize(message.footer);
				List<ArraySegment<byte>> arraySegments = new List<ArraySegment<byte>>(4);
				ByteBuffer byteBuffer = new ByteBuffer(new byte[sectionSize]);
				int length = 0;
				message.EncodeSection(byteBuffer, message.header);
				message.EncodeSection(byteBuffer, message.deliveryAnnotations);
				message.EncodeSection(byteBuffer, message.messageAnnotations);
				message.EncodeSection(byteBuffer, message.properties);
				message.EncodeSection(byteBuffer, message.applicationProperties);
				if (byteBuffer.Length > 0)
				{
					arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, byteBuffer.Length));
				}
				length = byteBuffer.Length;
				int num = byteBuffer.Length - length;
				if (num > 0)
				{
					arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, num));
				}
				if (message.footer != null)
				{
					length = byteBuffer.Length;
					message.EncodeSection(byteBuffer, message.footer);
					arraySegments.Add(new ArraySegment<byte>(byteBuffer.Buffer, length, byteBuffer.Length - length));
				}
				return arraySegments;
			}

			private void EncodeSection(ByteBuffer buffer, AmqpDescribed section)
			{
				if (section != null)
				{
					section.Offset = (long)buffer.Length;
					base.EncodeSection(buffer, section);
					section.Length = (long)buffer.Length - section.Offset;
				}
			}

			protected override void EnsureInitialized<T>(ref T obj, SectionFlag section)
			where T : class, new()
			{
				this.Deserialize(SectionFlag.All);
			}

			internal override Stream GetNonBodySectionsStream()
			{
				return (BufferListStream)this.bufferStream.Clone();
			}

			public override ArraySegment<byte>[] GetPayload(int payloadSize, out bool more)
			{
				more = false;
				throw new InvalidOperationException();
			}

			protected override void OnCompletePayload(int payloadSize)
			{
				throw new InvalidOperationException();
			}

			public override Stream ToStream()
			{
				return this.bufferStream;
			}

			internal override long Write(XmlWriter writer)
			{
				long num;
				this.Deserialize(SectionFlag.All);
				using (BufferListStream bufferListStream = new BufferListStream(AmqpMessage.AmqpStreamMessageHeader.Encode(this).ToArray()))
				{
					long num1 = (long)0;
					num1 = num1 + AmqpMessage.AmqpStreamMessageHeader.Write(bufferListStream, writer, base.Header);
					num1 = num1 + AmqpMessage.AmqpStreamMessageHeader.Write(bufferListStream, writer, base.DeliveryAnnotations);
					num1 = num1 + AmqpMessage.AmqpStreamMessageHeader.Write(bufferListStream, writer, base.MessageAnnotations);
					num1 = num1 + AmqpMessage.AmqpStreamMessageHeader.Write(bufferListStream, writer, base.Properties);
					num1 = num1 + AmqpMessage.AmqpStreamMessageHeader.Write(bufferListStream, writer, base.ApplicationProperties);
					num1 = num1 + AmqpMessage.AmqpStreamMessageHeader.Write(bufferListStream, writer, base.Footer);
					num = num1;
				}
				return num;
			}

			private static long Write(BufferListStream source, XmlWriter target, AmqpDescribed section)
			{
				long length = (long)0;
				if (section != null && section.Length > (long)0)
				{
					source.Position = section.Offset;
					byte[] numArray = new byte[checked((IntPtr)section.Length)];
					int num = source.Read(numArray, 0, (int)numArray.Length);
					target.WriteBase64(numArray, 0, num);
					length = (long)((int)numArray.Length);
				}
				return length;
			}
		}

		private sealed class AmqpValueMessage : AmqpMessage.AmqpBufferedMessage
		{
			private readonly AmqpValue @value;

			public override AmqpValue ValueBody
			{
				get
				{
					return this.@value;
				}
			}

			public AmqpValueMessage(AmqpValue value)
			{
				this.@value = value;
				AmqpMessage.AmqpValueMessage amqpValueMessage = this;
				amqpValueMessage.sectionFlags = amqpValueMessage.sectionFlags | SectionFlag.AmqpValue;
			}

			protected override void EncodeBody(ByteBuffer buffer)
			{
				base.EncodeSection(buffer, this.@value);
			}

			protected override int GetBodySize()
			{
				return base.GetSectionSize(this.@value);
			}
		}
	}
}