using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
	internal sealed class AmqpContractSerializer
	{
		private readonly static Dictionary<Type, SerializableType> builtInTypes;

		private readonly static AmqpContractSerializer Instance;

		private readonly ConcurrentDictionary<Type, SerializableType> customTypeCache;

		static AmqpContractSerializer()
		{
			Dictionary<Type, SerializableType> types = new Dictionary<Type, SerializableType>()
			{
				{ typeof(bool), SerializableType.CreateSingleValueType(typeof(bool)) },
				{ typeof(byte), SerializableType.CreateSingleValueType(typeof(byte)) },
				{ typeof(ushort), SerializableType.CreateSingleValueType(typeof(ushort)) },
				{ typeof(uint), SerializableType.CreateSingleValueType(typeof(uint)) },
				{ typeof(ulong), SerializableType.CreateSingleValueType(typeof(ulong)) },
				{ typeof(sbyte), SerializableType.CreateSingleValueType(typeof(sbyte)) },
				{ typeof(short), SerializableType.CreateSingleValueType(typeof(short)) },
				{ typeof(int), SerializableType.CreateSingleValueType(typeof(int)) },
				{ typeof(long), SerializableType.CreateSingleValueType(typeof(long)) },
				{ typeof(float), SerializableType.CreateSingleValueType(typeof(float)) },
				{ typeof(double), SerializableType.CreateSingleValueType(typeof(double)) },
				{ typeof(decimal), SerializableType.CreateSingleValueType(typeof(decimal)) },
				{ typeof(char), SerializableType.CreateSingleValueType(typeof(char)) },
				{ typeof(DateTime), SerializableType.CreateSingleValueType(typeof(DateTime)) },
				{ typeof(Guid), SerializableType.CreateSingleValueType(typeof(Guid)) },
				{ typeof(ArraySegment<byte>), SerializableType.CreateSingleValueType(typeof(ArraySegment<byte>)) },
				{ typeof(string), SerializableType.CreateSingleValueType(typeof(string)) },
				{ typeof(AmqpSymbol), SerializableType.CreateSingleValueType(typeof(AmqpSymbol)) },
				{ typeof(TimeSpan), SerializableType.CreateDescribedValueType<TimeSpan, long>("com.microsoft:timespan", (TimeSpan ts) => ts.Ticks, (long l) => TimeSpan.FromTicks(l)) },
				{ typeof(Uri), SerializableType.CreateDescribedValueType<Uri, string>("com.microsoft:uri", (Uri u) => u.AbsoluteUri, (string s) => new Uri(s)) },
				{ typeof(DateTimeOffset), SerializableType.CreateDescribedValueType<DateTimeOffset, long>("com.microsoft:datetime-offset", (DateTimeOffset d) => d.UtcTicks, (long l) => new DateTimeOffset(new DateTime(l, DateTimeKind.Utc))) },
				{ typeof(object), SerializableType.CreateObjectType(typeof(object)) }
			};
			AmqpContractSerializer.builtInTypes = types;
			AmqpContractSerializer.Instance = new AmqpContractSerializer();
		}

		internal AmqpContractSerializer()
		{
			this.customTypeCache = new ConcurrentDictionary<Type, SerializableType>();
		}

		private SerializableType CompileInterfaceTypes(Type type)
		{
			bool isArray = type.IsArray;
			bool flag = false;
			bool flag1 = false;
			MemberAccessor memberAccessor = null;
			MemberAccessor memberAccessor1 = null;
			MethodAccessor methodAccessor = null;
			Type type1 = null;
			if (type.GetInterface(typeof(IAmqpSerializable).Name, false) != null)
			{
				return SerializableType.CreateAmqpSerializableType(this, type);
			}
			Type[] interfaces = type.GetInterfaces();
			for (int i = 0; i < (int)interfaces.Length; i++)
			{
				Type type2 = interfaces[i];
				if (type2.IsGenericType)
				{
					Type genericTypeDefinition = type2.GetGenericTypeDefinition();
					if (genericTypeDefinition == typeof(IDictionary<,>))
					{
						flag = true;
						Type[] genericArguments = type2.GetGenericArguments();
						type1 = typeof(KeyValuePair<,>).MakeGenericType(genericArguments);
						memberAccessor = MemberAccessor.Create(type1.GetProperty("Key"), false);
						memberAccessor1 = MemberAccessor.Create(type1.GetProperty("Value"), false);
						methodAccessor = MethodAccessor.Create(type.GetMethod("Add", genericArguments));
						break;
					}
					else if (genericTypeDefinition == typeof(IList<>))
					{
						flag1 = true;
						Type[] typeArray = type2.GetGenericArguments();
						type1 = typeArray[0];
						methodAccessor = MethodAccessor.Create(type.GetMethod("Add", typeArray));
						break;
					}
				}
			}
			if (flag)
			{
				return SerializableType.CreateMapType(this, type, memberAccessor, memberAccessor1, methodAccessor);
			}
			if (isArray || !flag1)
			{
				return null;
			}
			return SerializableType.CreateListType(this, type, type1, methodAccessor);
		}

		private SerializableType CompileNonContractTypes(Type type)
		{
			return this.CompileNullableTypes(type) ?? this.CompileInterfaceTypes(type);
		}

		private SerializableType CompileNullableTypes(Type type)
		{
			if (!type.IsGenericType || !(type.GetGenericTypeDefinition() == typeof(Nullable<>)))
			{
				return null;
			}
			return this.GetType(type.GetGenericArguments()[0]);
		}

		private SerializableType CompileType(Type type, bool describedOnly)
		{
			int valueOrDefault;
			object[] customAttributes = type.GetCustomAttributes(typeof(AmqpContractAttribute), false);
			if ((int)customAttributes.Length == 0)
			{
				if (describedOnly)
				{
					return null;
				}
				return this.CompileNonContractTypes(type);
			}
			AmqpContractAttribute amqpContractAttribute = (AmqpContractAttribute)customAttributes[0];
			SerializableType serializableType = null;
			if (type.BaseType != typeof(object))
			{
				serializableType = this.CompileType(type.BaseType, true);
				if (serializableType != null)
				{
					if (serializableType.Encoding != amqpContractAttribute.Encoding)
					{
						throw new SerializationException(SRAmqp.AmqpEncodingTypeMismatch(type.Name, amqpContractAttribute.Encoding, type.BaseType.Name, serializableType.Encoding));
					}
					this.customTypeCache.TryAdd(type.BaseType, serializableType);
				}
			}
			string name = amqpContractAttribute.Name;
			ulong? internalCode = amqpContractAttribute.InternalCode;
			if (name == null && !internalCode.HasValue)
			{
				name = type.FullName;
			}
			List<SerialiableMember> serialiableMembers = new List<SerialiableMember>();
			if (amqpContractAttribute.Encoding == EncodingType.List && serializableType != null)
			{
				serialiableMembers.AddRange(serializableType.Members);
			}
			int count = serialiableMembers.Count + 1;
			MemberInfo[] members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MethodAccessor methodAccessor = null;
			MemberInfo[] memberInfoArray = members;
			for (int i = 0; i < (int)memberInfoArray.Length; i++)
			{
				MemberInfo memberInfo = memberInfoArray[i];
				if (memberInfo.DeclaringType == type)
				{
					if (memberInfo.MemberType == MemberTypes.Field || memberInfo.MemberType == MemberTypes.Property)
					{
						object[] objArray = memberInfo.GetCustomAttributes(typeof(AmqpMemberAttribute), true);
						if ((int)objArray.Length == 1)
						{
							AmqpMemberAttribute amqpMemberAttribute = (AmqpMemberAttribute)objArray[0];
							SerialiableMember serialiableMember = new SerialiableMember()
							{
								Name = amqpMemberAttribute.Name ?? memberInfo.Name
							};
							SerialiableMember serialiableMember1 = serialiableMember;
							int? internalOrder = amqpMemberAttribute.InternalOrder;
							if (internalOrder.HasValue)
							{
								valueOrDefault = internalOrder.GetValueOrDefault();
							}
							else
							{
								valueOrDefault = count;
								count = valueOrDefault + 1;
							}
							serialiableMember1.Order = valueOrDefault;
							serialiableMember.Mandatory = amqpMemberAttribute.Mandatory;
							serialiableMember.Accessor = MemberAccessor.Create(memberInfo, true);
							serialiableMember.Type = this.GetType((memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType));
							serialiableMembers.Add(serialiableMember);
						}
					}
					else if (memberInfo.MemberType == MemberTypes.Method && (int)memberInfo.GetCustomAttributes(typeof(OnDeserializedAttribute), false).Length == 1)
					{
						methodAccessor = MethodAccessor.Create((MethodInfo)memberInfo);
					}
				}
			}
			if (amqpContractAttribute.Encoding == EncodingType.List)
			{
				serialiableMembers.Sort(AmqpContractSerializer.MemberOrderComparer.Instance);
				int order = -1;
				foreach (SerialiableMember serialiableMember2 in serialiableMembers)
				{
					if (order > 0 && serialiableMember2.Order == order)
					{
						throw new SerializationException(SRAmqp.AmqpDuplicateMemberOrder(order, type.Name));
					}
					order = serialiableMember2.Order;
				}
			}
			SerialiableMember[] array = serialiableMembers.ToArray();
			Dictionary<Type, SerializableType> types = null;
			object[] customAttributes1 = type.GetCustomAttributes(typeof(KnownTypeAttribute), false);
			for (int j = 0; j < (int)customAttributes1.Length; j++)
			{
				KnownTypeAttribute knownTypeAttribute = (KnownTypeAttribute)customAttributes1[j];
				if ((int)knownTypeAttribute.Type.GetCustomAttributes(typeof(AmqpContractAttribute), false).Length > 0)
				{
					if (types == null)
					{
						types = new Dictionary<Type, SerializableType>();
					}
					types.Add(knownTypeAttribute.Type, null);
				}
			}
			if (amqpContractAttribute.Encoding == EncodingType.List)
			{
				return SerializableType.CreateDescribedListType(this, type, serializableType, name, internalCode, array, types, methodAccessor);
			}
			if (amqpContractAttribute.Encoding != EncodingType.Map)
			{
				throw new NotSupportedException(amqpContractAttribute.Encoding.ToString());
			}
			return SerializableType.CreateDescribedMapType(this, type, serializableType, name, internalCode, array, types, methodAccessor);
		}

		private SerializableType GetOrCompileType(Type type, bool describedOnly)
		{
			SerializableType serializableType = null;
			if (!this.TryGetSerializableType(type, out serializableType))
			{
				serializableType = this.CompileType(type, describedOnly);
				if (serializableType != null)
				{
					this.customTypeCache.TryAdd(type, serializableType);
				}
			}
			if (serializableType == null)
			{
				throw new NotSupportedException(type.FullName);
			}
			return serializableType;
		}

		internal SerializableType GetType(Type type)
		{
			return this.GetOrCompileType(type, false);
		}

		public static T ReadObject<T>(Stream stream)
		{
			return AmqpContractSerializer.Instance.ReadObjectInternal<T, T>(stream);
		}

		public static TAs ReadObject<T, TAs>(Stream stream)
		{
			return AmqpContractSerializer.Instance.ReadObjectInternal<T, TAs>(stream);
		}

		internal T ReadObjectInternal<T>(Stream stream)
		{
			return this.ReadObjectInternal<T, T>(stream);
		}

		internal TAs ReadObjectInternal<T, TAs>(Stream stream)
		{
			TAs tA;
			if (!stream.CanSeek)
			{
				throw new AmqpException(AmqpError.DecodeError, "stream.CanSeek must be true.");
			}
			SerializableType type = this.GetType(typeof(T));
			ByteBuffer byteBuffer = null;
			long position = stream.Position;
			BufferListStream bufferListStream = stream as BufferListStream;
			if (bufferListStream == null)
			{
				byteBuffer = new ByteBuffer((int)stream.Length, false);
				int num = stream.Read(byteBuffer.Buffer, 0, byteBuffer.Capacity);
				byteBuffer.Append(num);
			}
			else
			{
				ArraySegment<byte> nums = bufferListStream.ReadBytes(2147483647);
				byteBuffer = new ByteBuffer(nums.Array, nums.Offset, nums.Count);
			}
			using (byteBuffer)
			{
				TAs tA1 = (TAs)type.ReadObject(byteBuffer);
				if (byteBuffer.Length > 0)
				{
					stream.Position = position + (long)byteBuffer.Offset;
				}
				tA = tA1;
			}
			return tA;
		}

		internal TAs ReadObjectInternal<T, TAs>(ByteBuffer buffer)
		{
			return (TAs)this.GetType(typeof(T)).ReadObject(buffer);
		}

		private bool TryGetSerializableType(Type type, out SerializableType serializableType)
		{
			serializableType = null;
			if (AmqpContractSerializer.builtInTypes.TryGetValue(type, out serializableType))
			{
				return true;
			}
			if (this.customTypeCache.TryGetValue(type, out serializableType))
			{
				return true;
			}
			return false;
		}

		public static void WriteObject(Stream stream, object graph)
		{
			AmqpContractSerializer.Instance.WriteObjectInternal(stream, graph);
		}

		internal void WriteObjectInternal(Stream stream, object graph)
		{
			if (graph == null)
			{
				stream.WriteByte(64);
				return;
			}
			SerializableType type = this.GetType(graph.GetType());
			using (ByteBuffer byteBuffer = new ByteBuffer(1024, true))
			{
				type.WriteObject(byteBuffer, graph);
				stream.Write(byteBuffer.Buffer, byteBuffer.Offset, byteBuffer.Length);
			}
		}

		internal void WriteObjectInternal(ByteBuffer buffer, object graph)
		{
			if (graph == null)
			{
				AmqpEncoding.EncodeNull(buffer);
				return;
			}
			this.GetType(graph.GetType()).WriteObject(buffer, graph);
		}

		private sealed class MemberOrderComparer : IComparer<SerialiableMember>
		{
			public readonly static AmqpContractSerializer.MemberOrderComparer Instance;

			static MemberOrderComparer()
			{
				AmqpContractSerializer.MemberOrderComparer.Instance = new AmqpContractSerializer.MemberOrderComparer();
			}

			public MemberOrderComparer()
			{
			}

			public int Compare(SerialiableMember m1, SerialiableMember m2)
			{
				if (m1.Order == m2.Order)
				{
					return 0;
				}
				if (m1.Order <= m2.Order)
				{
					return -1;
				}
				return 1;
			}
		}
	}
}