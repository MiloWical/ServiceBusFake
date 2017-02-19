using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
	internal abstract class SerializableType
	{
		private readonly AmqpContractSerializer serializer;

		private readonly Type type;

		private readonly bool hasDefaultCtor;

		public virtual EncodingType Encoding
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		public virtual SerialiableMember[] Members
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		protected SerializableType(AmqpContractSerializer serializer, Type type)
		{
			this.serializer = serializer;
			this.type = type;
			this.hasDefaultCtor = type.GetConstructor(Type.EmptyTypes) != null;
		}

		public static SerializableType CreateAmqpSerializableType(AmqpContractSerializer serializer, Type type)
		{
			return new SerializableType.AmqpSerializableType(serializer, type);
		}

		public static SerializableType CreateDescribedListType(AmqpContractSerializer serializer, Type type, SerializableType baseType, string descriptorName, ulong? descriptorCode, SerialiableMember[] members, Dictionary<Type, SerializableType> knownTypes, MethodAccessor onDesrialized)
		{
			return new SerializableType.DescribedListType(serializer, type, baseType, descriptorName, descriptorCode, members, knownTypes, onDesrialized);
		}

		public static SerializableType CreateDescribedMapType(AmqpContractSerializer serializer, Type type, SerializableType baseType, string descriptorName, ulong? descriptorCode, SerialiableMember[] members, Dictionary<Type, SerializableType> knownTypes, MethodAccessor onDesrialized)
		{
			return new SerializableType.DescribedMapType(serializer, type, baseType, descriptorName, descriptorCode, members, knownTypes, onDesrialized);
		}

		public static SerializableType CreateDescribedValueType<TValue, TAs>(string symbol, Func<TValue, TAs> getter, Func<TAs, TValue> setter)
		{
			return new SerializableType.DescribedValueType<TValue, TAs>(symbol, getter, setter);
		}

		public static SerializableType CreateListType(AmqpContractSerializer serializer, Type type, Type itemType, MethodAccessor addAccessor)
		{
			return new SerializableType.ListType(serializer, type, itemType, addAccessor);
		}

		public static SerializableType CreateMapType(AmqpContractSerializer serializer, Type type, MemberAccessor keyAccessor, MemberAccessor valueAccessor, MethodAccessor addAccessor)
		{
			return new SerializableType.MapType(serializer, type, keyAccessor, valueAccessor, addAccessor);
		}

		public static SerializableType CreateObjectType(Type type)
		{
			return new SerializableType.AmqpObjectType(type);
		}

		public static SerializableType CreateSingleValueType(Type type)
		{
			return new SerializableType.SingleValueType(type, AmqpEncoding.GetEncoding(type));
		}

		public abstract object ReadObject(ByteBuffer buffer);

		public abstract void WriteObject(ByteBuffer buffer, object graph);

		private sealed class AmqpObjectType : SerializableType
		{
			public AmqpObjectType(Type type) : base(null, type)
			{
			}

			public override object ReadObject(ByteBuffer buffer)
			{
				return AmqpEncoding.DecodeObject(buffer);
			}

			public override void WriteObject(ByteBuffer buffer, object value)
			{
				AmqpEncoding.EncodeObject(value, buffer);
			}
		}

		private sealed class AmqpSerializableType : SerializableType
		{
			public AmqpSerializableType(AmqpContractSerializer serializer, Type type) : base(serializer, type)
			{
			}

			public override object ReadObject(ByteBuffer buffer)
			{
				buffer.Validate(false, 1);
				if (buffer.Buffer[buffer.Offset] == 64)
				{
					buffer.Complete(1);
					return null;
				}
				object obj = (this.hasDefaultCtor ? Activator.CreateInstance(this.type) : FormatterServices.GetUninitializedObject(this.type));
				((IAmqpSerializable)obj).Decode(buffer);
				return obj;
			}

			public override void WriteObject(ByteBuffer buffer, object value)
			{
				if (value == null)
				{
					AmqpEncoding.EncodeNull(buffer);
					return;
				}
				((IAmqpSerializable)value).Encode(buffer);
			}
		}

		private abstract class CollectionType : SerializableType
		{
			protected CollectionType(AmqpContractSerializer serializer, Type type) : base(serializer, type)
			{
			}

			protected abstract void Initialize(ByteBuffer buffer, FormatCode formatCode, out int size, out int count, out int encodeWidth, out SerializableType.CollectionType effectiveType);

			public abstract void ReadMembers(ByteBuffer buffer, object container, ref int count);

			public override object ReadObject(ByteBuffer buffer)
			{
				int num;
				int num1;
				int num2;
				SerializableType.CollectionType collectionType;
				FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
				if (formatCode == 64)
				{
					return null;
				}
				this.Initialize(buffer, formatCode, out num, out num1, out num2, out collectionType);
				int offset = buffer.Offset;
				object obj = (collectionType.hasDefaultCtor ? Activator.CreateInstance(collectionType.type) : FormatterServices.GetUninitializedObject(collectionType.type));
				if (num1 > 0)
				{
					collectionType.ReadMembers(buffer, obj, ref num1);
					if (num1 > 0)
					{
						buffer.Complete(num - (buffer.Offset - offset) - num2);
					}
				}
				return obj;
			}

			protected abstract bool WriteFormatCode(ByteBuffer buffer);

			public abstract int WriteMembers(ByteBuffer buffer, object container);

			public override void WriteObject(ByteBuffer buffer, object graph)
			{
				if (graph == null)
				{
					AmqpEncoding.EncodeNull(buffer);
					return;
				}
				if (!this.WriteFormatCode(buffer))
				{
					return;
				}
				int writePos = buffer.WritePos;
				AmqpBitConverter.WriteULong(buffer, (ulong)0);
				int num = this.WriteMembers(buffer, graph);
				AmqpBitConverter.WriteUInt(buffer.Buffer, writePos, (uint)(buffer.WritePos - writePos - 4));
				AmqpBitConverter.WriteUInt(buffer.Buffer, writePos + 4, (uint)num);
			}
		}

		private sealed class DescribedListType : SerializableType.DescribedType
		{
			protected override byte Code
			{
				get
				{
					return (byte)208;
				}
			}

			public override EncodingType Encoding
			{
				get
				{
					return EncodingType.List;
				}
			}

			public DescribedListType(AmqpContractSerializer serializer, Type type, SerializableType baseType, string descriptorName, ulong? descriptorCode, SerialiableMember[] members, Dictionary<Type, SerializableType> knownTypes, MethodAccessor onDesrialized) : base(serializer, type, baseType, descriptorName, descriptorCode, members, knownTypes, onDesrialized)
			{
			}

			public override void ReadMembers(ByteBuffer buffer, object container, ref int count)
			{
				int num = 0;
				while (num < (int)this.Members.Length && count > 0)
				{
					object obj = this.Members[num].Type.ReadObject(buffer);
					this.Members[num].Accessor.Set(container, obj);
					num++;
					count = count - 1;
				}
				base.InvokeDeserialized(container);
			}

			public override int WriteMembers(ByteBuffer buffer, object container)
			{
				SerialiableMember[] members = this.Members;
				for (int i = 0; i < (int)members.Length; i++)
				{
					SerialiableMember serialiableMember = members[i];
					object obj = serialiableMember.Accessor.Get(container);
					if (obj != null)
					{
						SerializableType type = serialiableMember.Type;
						if (obj.GetType() != type.type)
						{
							type = this.serializer.GetType(obj.GetType());
						}
						type.WriteObject(buffer, obj);
					}
					else
					{
						AmqpEncoding.EncodeNull(buffer);
					}
				}
				return (int)this.Members.Length;
			}
		}

		private sealed class DescribedMapType : SerializableType.DescribedType
		{
			private readonly Dictionary<string, SerialiableMember> membersMap;

			protected override byte Code
			{
				get
				{
					return (byte)209;
				}
			}

			public override EncodingType Encoding
			{
				get
				{
					return EncodingType.Map;
				}
			}

			public DescribedMapType(AmqpContractSerializer serializer, Type type, SerializableType baseType, string descriptorName, ulong? descriptorCode, SerialiableMember[] members, Dictionary<Type, SerializableType> knownTypes, MethodAccessor onDesrialized) : base(serializer, type, baseType, descriptorName, descriptorCode, members, knownTypes, onDesrialized)
			{
				this.membersMap = new Dictionary<string, SerialiableMember>();
				SerialiableMember[] serialiableMemberArray = members;
				for (int i = 0; i < (int)serialiableMemberArray.Length; i++)
				{
					SerialiableMember serialiableMember = serialiableMemberArray[i];
					this.membersMap.Add(serialiableMember.Name, serialiableMember);
				}
			}

			public override void ReadMembers(ByteBuffer buffer, object container, ref int count)
			{
				SerialiableMember serialiableMember;
				if (base.BaseType != null)
				{
					base.BaseType.ReadMembers(buffer, container, ref count);
				}
				int num = 0;
				while (num < this.membersMap.Count && count > 0)
				{
					AmqpSymbol amqpSymbol = AmqpCodec.DecodeSymbol(buffer);
					if (this.membersMap.TryGetValue(amqpSymbol.Value, out serialiableMember))
					{
						object obj = serialiableMember.Type.ReadObject(buffer);
						serialiableMember.Accessor.Set(container, obj);
					}
					num++;
					count = count - 2;
				}
				base.InvokeDeserialized(container);
			}

			public override int WriteMembers(ByteBuffer buffer, object container)
			{
				int num = 0;
				if (base.BaseType != null)
				{
					base.BaseType.WriteMembers(buffer, container);
				}
				SerialiableMember[] members = this.Members;
				for (int i = 0; i < (int)members.Length; i++)
				{
					SerialiableMember serialiableMember = members[i];
					object obj = serialiableMember.Accessor.Get(container);
					if (obj != null)
					{
						AmqpCodec.EncodeSymbol(serialiableMember.Name, buffer);
						SerializableType type = serialiableMember.Type;
						if (obj.GetType() != type.type)
						{
							type = this.serializer.GetType(obj.GetType());
						}
						type.WriteObject(buffer, obj);
						num = num + 2;
					}
				}
				return num;
			}
		}

		private abstract class DescribedType : SerializableType.CollectionType
		{
			private readonly SerializableType.DescribedType baseType;

			private readonly AmqpSymbol descriptorName;

			private readonly ulong? descriptorCode;

			private readonly SerialiableMember[] members;

			private readonly MethodAccessor onDeserialized;

			private readonly KeyValuePair<Type, SerializableType>[] knownTypes;

			protected SerializableType.DescribedType BaseType
			{
				get
				{
					return this.baseType;
				}
			}

			protected abstract byte Code
			{
				get;
			}

			public override SerialiableMember[] Members
			{
				get
				{
					return this.members;
				}
			}

			protected DescribedType(AmqpContractSerializer serializer, Type type, SerializableType baseType, string descriptorName, ulong? descriptorCode, SerialiableMember[] members, Dictionary<Type, SerializableType> knownTypes, MethodAccessor onDesrialized) : base(serializer, type)
			{
				this.baseType = (SerializableType.DescribedType)baseType;
				this.descriptorName = descriptorName;
				this.descriptorCode = descriptorCode;
				this.members = members;
				this.onDeserialized = onDesrialized;
				this.knownTypes = SerializableType.DescribedType.GetKnownTypes(knownTypes);
			}

			private bool AreEqual(ulong? code1, AmqpSymbol symbol1, ulong? code2, AmqpSymbol symbol2)
			{
				if (code1.HasValue && code2.HasValue)
				{
					return code1.Value == code2.Value;
				}
				if (symbol1.Value == null || symbol2.Value == null)
				{
					return false;
				}
				return symbol1.Value == symbol2.Value;
			}

			private static KeyValuePair<Type, SerializableType>[] GetKnownTypes(Dictionary<Type, SerializableType> types)
			{
				if (types == null || types.Count == 0)
				{
					return null;
				}
				KeyValuePair<Type, SerializableType>[] keyValuePairArray = new KeyValuePair<Type, SerializableType>[types.Count];
				int num = 0;
				foreach (KeyValuePair<Type, SerializableType> type in types)
				{
					int num1 = num;
					num = num1 + 1;
					keyValuePairArray[num1] = type;
				}
				return keyValuePairArray;
			}

			protected override void Initialize(ByteBuffer buffer, FormatCode formatCode, out int size, out int count, out int encodeWidth, out SerializableType.CollectionType effectiveType)
			{
				object valueOrDefault;
				if (formatCode != 0)
				{
					throw new AmqpException(AmqpError.InvalidField, SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
				}
				effectiveType = null;
				formatCode = AmqpEncoding.ReadFormatCode(buffer);
				ulong? nullable = null;
				AmqpSymbol amqpSymbol = new AmqpSymbol();
				if (formatCode == 68)
				{
					nullable = new ulong?((ulong)0);
				}
				else if (formatCode == 128 || formatCode == 83)
				{
					nullable = ULongEncoding.Decode(buffer, formatCode);
				}
				else if (formatCode == 163 || formatCode == 179)
				{
					amqpSymbol = SymbolEncoding.Decode(buffer, formatCode);
				}
				if (this.AreEqual(this.descriptorCode, this.descriptorName, nullable, amqpSymbol))
				{
					effectiveType = this;
				}
				else if (this.knownTypes != null)
				{
					int num = 0;
					while (num < (int)this.knownTypes.Length)
					{
						KeyValuePair<Type, SerializableType> keyValuePair = this.knownTypes[num];
						if (keyValuePair.Value == null)
						{
							SerializableType type = this.serializer.GetType(keyValuePair.Key);
							keyValuePair = new KeyValuePair<Type, SerializableType>(keyValuePair.Key, type);
							KeyValuePair<Type, SerializableType> keyValuePair1 = keyValuePair;
							keyValuePair = keyValuePair1;
							this.knownTypes[num] = keyValuePair1;
						}
						SerializableType.DescribedType value = (SerializableType.DescribedType)keyValuePair.Value;
						if (!this.AreEqual(value.descriptorCode, value.descriptorName, nullable, amqpSymbol))
						{
							num++;
						}
						else
						{
							effectiveType = value;
							break;
						}
					}
				}
				if (effectiveType == null)
				{
					ulong? nullable1 = nullable;
					if (nullable1.HasValue)
					{
						valueOrDefault = nullable1.GetValueOrDefault();
					}
					else
					{
						valueOrDefault = amqpSymbol.Value;
					}
					throw new SerializationException(SRAmqp.AmqpUnknownDescriptor(valueOrDefault, this.type.Name));
				}
				formatCode = AmqpEncoding.ReadFormatCode(buffer);
				if (this.Code != 208)
				{
					encodeWidth = (formatCode == 193 ? 1 : 4);
					AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 193, 209, out size, out count);
					return;
				}
				if (formatCode == 69)
				{
					int num1 = 0;
					int num2 = num1;
					encodeWidth = num1;
					int num3 = num2;
					int num4 = num3;
					count = num3;
					size = num4;
					return;
				}
				encodeWidth = (formatCode == 192 ? 1 : 4);
				AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 192, 208, out size, out count);
			}

			protected void InvokeDeserialized(object container)
			{
				if (this.baseType != null)
				{
					this.baseType.InvokeDeserialized(container);
				}
				if (this.onDeserialized != null)
				{
					MethodAccessor methodAccessor = this.onDeserialized;
					object[] objArray = new object[] { new StreamingContext() };
					methodAccessor.Invoke(container, objArray);
				}
			}

			protected override bool WriteFormatCode(ByteBuffer buffer)
			{
				AmqpBitConverter.WriteUByte(buffer, 0);
				if (!this.descriptorCode.HasValue)
				{
					SymbolEncoding.Encode(this.descriptorName, buffer);
				}
				else
				{
					ULongEncoding.Encode(this.descriptorCode, buffer);
				}
				AmqpBitConverter.WriteUByte(buffer, this.Code);
				return true;
			}
		}

		private sealed class DescribedValueType<TValue, TAs> : SerializableType
		{
			private readonly AmqpSymbol symbol;

			private readonly EncodingBase encoder;

			private readonly Func<TValue, TAs> getter;

			private readonly Func<TAs, TValue> setter;

			public DescribedValueType(string symbol, Func<TValue, TAs> getter, Func<TAs, TValue> setter) : base(null, typeof(TValue))
			{
				this.symbol = symbol;
				this.encoder = AmqpEncoding.GetEncoding(typeof(TAs));
				this.getter = getter;
				this.setter = setter;
			}

			public override object ReadObject(ByteBuffer buffer)
			{
				Microsoft.ServiceBus.Messaging.Amqp.Encoding.DescribedType describedType = DescribedEncoding.Decode(buffer);
				if (describedType == null)
				{
					return null;
				}
				if (!this.symbol.Equals(describedType.Descriptor))
				{
					throw new SerializationException(describedType.Descriptor.ToString());
				}
				return this.setter((TAs)describedType.Value);
			}

			public override void WriteObject(ByteBuffer buffer, object value)
			{
				if (value == null)
				{
					AmqpEncoding.EncodeNull(buffer);
					return;
				}
				AmqpBitConverter.WriteUByte(buffer, 0);
				SymbolEncoding.Encode(this.symbol, buffer);
				this.encoder.EncodeObject(this.getter((TValue)value), false, buffer);
			}
		}

		private sealed class ListType : SerializableType.CollectionType
		{
			private readonly SerializableType itemType;

			private readonly MethodAccessor addMethodAccessor;

			public ListType(AmqpContractSerializer serializer, Type type, Type itemType, MethodAccessor addAccessor) : base(serializer, type)
			{
				this.itemType = serializer.GetType(itemType);
				this.addMethodAccessor = addAccessor;
			}

			protected override void Initialize(ByteBuffer buffer, FormatCode formatCode, out int size, out int count, out int encodeWidth, out SerializableType.CollectionType effectiveType)
			{
				if (formatCode == 69)
				{
					int num = 0;
					int num1 = num;
					encodeWidth = num;
					int num2 = num1;
					int num3 = num2;
					count = num2;
					size = num3;
					effectiveType = this;
					return;
				}
				if (formatCode != 208 && formatCode != 192)
				{
					throw new AmqpException(AmqpError.InvalidField, SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
				}
				encodeWidth = (formatCode == 192 ? 1 : 4);
				AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 192, 208, out size, out count);
				effectiveType = this;
			}

			public override void ReadMembers(ByteBuffer buffer, object container, ref int count)
			{
				while (count > 0)
				{
					object obj = this.itemType.ReadObject(buffer);
					MethodAccessor methodAccessor = this.addMethodAccessor;
					object[] objArray = new object[] { obj };
					methodAccessor.Invoke(container, objArray);
					count = count - 1;
				}
			}

			protected override bool WriteFormatCode(ByteBuffer buffer)
			{
				AmqpBitConverter.WriteUByte(buffer, 208);
				return true;
			}

			public override int WriteMembers(ByteBuffer buffer, object container)
			{
				int num = 0;
				foreach (object obj in (IEnumerable)container)
				{
					if (obj != null)
					{
						SerializableType type = this.itemType;
						if (obj.GetType() != type.type)
						{
							type = this.serializer.GetType(obj.GetType());
						}
						type.WriteObject(buffer, obj);
					}
					else
					{
						AmqpEncoding.EncodeNull(buffer);
					}
					num++;
				}
				return num;
			}
		}

		private sealed class MapType : SerializableType.CollectionType
		{
			private readonly SerializableType keyType;

			private readonly SerializableType valueType;

			private readonly MemberAccessor keyAccessor;

			private readonly MemberAccessor valueAccessor;

			private readonly MethodAccessor addMethodAccessor;

			public MapType(AmqpContractSerializer serializer, Type type, MemberAccessor keyAccessor, MemberAccessor valueAccessor, MethodAccessor addAccessor) : base(serializer, type)
			{
				this.keyType = this.serializer.GetType(keyAccessor.Type);
				this.valueType = this.serializer.GetType(valueAccessor.Type);
				this.keyAccessor = keyAccessor;
				this.valueAccessor = valueAccessor;
				this.addMethodAccessor = addAccessor;
			}

			protected override void Initialize(ByteBuffer buffer, FormatCode formatCode, out int size, out int count, out int encodeWidth, out SerializableType.CollectionType effectiveType)
			{
				if (formatCode != 209 && formatCode != 193)
				{
					throw new AmqpException(AmqpError.InvalidField, SRAmqp.AmqpInvalidFormatCode(formatCode, buffer.Offset));
				}
				encodeWidth = (formatCode == 193 ? 1 : 4);
				AmqpEncoding.ReadSizeAndCount(buffer, formatCode, 193, 209, out size, out count);
				effectiveType = this;
			}

			public override void ReadMembers(ByteBuffer buffer, object container, ref int count)
			{
				while (count > 0)
				{
					object obj = this.keyType.ReadObject(buffer);
					object obj1 = this.valueType.ReadObject(buffer);
					MethodAccessor methodAccessor = this.addMethodAccessor;
					object[] objArray = new object[] { obj, obj1 };
					methodAccessor.Invoke(container, objArray);
					count = count - 2;
				}
			}

			protected override bool WriteFormatCode(ByteBuffer buffer)
			{
				AmqpBitConverter.WriteUByte(buffer, 209);
				return true;
			}

			public override int WriteMembers(ByteBuffer buffer, object container)
			{
				int num = 0;
				foreach (object obj in (IEnumerable)container)
				{
					object obj1 = this.keyAccessor.Get(obj);
					object obj2 = this.valueAccessor.Get(obj);
					if (obj2 == null)
					{
						continue;
					}
					this.keyType.WriteObject(buffer, obj1);
					SerializableType type = this.valueType;
					if (obj2.GetType() != type.type)
					{
						type = this.serializer.GetType(obj2.GetType());
					}
					type.WriteObject(buffer, obj2);
					num = num + 2;
				}
				return num;
			}
		}

		private sealed class SingleValueType : SerializableType
		{
			private readonly EncodingBase encoder;

			public SingleValueType(Type type, EncodingBase encoder) : base(null, type)
			{
				this.encoder = encoder;
			}

			public override object ReadObject(ByteBuffer buffer)
			{
				return this.encoder.DecodeObject(buffer, 0);
			}

			public override void WriteObject(ByteBuffer buffer, object value)
			{
				this.encoder.EncodeObject(value, false, buffer);
			}
		}
	}
}