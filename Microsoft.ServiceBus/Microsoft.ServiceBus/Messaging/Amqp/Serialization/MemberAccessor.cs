using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
	internal abstract class MemberAccessor
	{
		private readonly System.Type type;

		private Func<object, object> getter;

		private Action<object, object> setter;

		public System.Type Type
		{
			get
			{
				return this.type;
			}
		}

		protected MemberAccessor(System.Type type)
		{
			this.type = type;
		}

		public static MemberAccessor Create(MemberInfo memberInfo, bool requiresSetter)
		{
			if (memberInfo.MemberType == MemberTypes.Field)
			{
				return new MemberAccessor.FieldMemberAccessor((FieldInfo)memberInfo);
			}
			if (memberInfo.MemberType != MemberTypes.Property)
			{
				throw new NotSupportedException(memberInfo.MemberType.ToString());
			}
			return new MemberAccessor.PropertyMemberAccessor((PropertyInfo)memberInfo, requiresSetter);
		}

		private static void EmitCall(ILGenerator generator, MethodInfo method)
		{
			generator.EmitCall((method.IsStatic || method.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt), method, null);
		}

		private static void EmitTypeConversion(ILGenerator generator, System.Type castType, bool isContainer)
		{
			if (castType == typeof(object))
			{
				return;
			}
			if (!castType.IsValueType)
			{
				generator.Emit(OpCodes.Castclass, castType);
				return;
			}
			generator.Emit((isContainer ? OpCodes.Unbox : OpCodes.Unbox_Any), castType);
		}

		public object Get(object container)
		{
			return this.getter(container);
		}

		private static string GetAccessorName(bool isGetter, string name)
		{
			return string.Concat((isGetter ? "get_" : "set_"), name);
		}

		public void Set(object container, object value)
		{
			this.setter(container, value);
		}

		private sealed class FieldMemberAccessor : MemberAccessor
		{
			public FieldMemberAccessor(FieldInfo fieldInfo) : base(fieldInfo.FieldType)
			{
				this.InitializeGetter(fieldInfo);
				this.InitializeSetter(fieldInfo);
			}

			private void InitializeGetter(FieldInfo fieldInfo)
			{
				string accessorName = MemberAccessor.GetAccessorName(true, fieldInfo.Name);
				System.Type type = typeof(object);
				System.Type[] typeArray = new System.Type[] { typeof(object) };
				DynamicMethod dynamicMethod = new DynamicMethod(accessorName, type, typeArray, true);
				ILGenerator lGenerator = dynamicMethod.GetILGenerator();
				lGenerator.Emit(OpCodes.Ldarg_0);
				MemberAccessor.EmitTypeConversion(lGenerator, fieldInfo.DeclaringType, true);
				lGenerator.Emit(OpCodes.Ldfld, fieldInfo);
				if (fieldInfo.FieldType.IsValueType)
				{
					lGenerator.Emit(OpCodes.Box, fieldInfo.FieldType);
				}
				lGenerator.Emit(OpCodes.Ret);
				this.getter = (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
			}

			private void InitializeSetter(FieldInfo fieldInfo)
			{
				string accessorName = MemberAccessor.GetAccessorName(false, fieldInfo.Name);
				System.Type type = typeof(void);
				System.Type[] typeArray = new System.Type[] { typeof(object), typeof(object) };
				DynamicMethod dynamicMethod = new DynamicMethod(accessorName, type, typeArray, true);
				ILGenerator lGenerator = dynamicMethod.GetILGenerator();
				lGenerator.Emit(OpCodes.Ldarg_0);
				MemberAccessor.EmitTypeConversion(lGenerator, fieldInfo.DeclaringType, true);
				lGenerator.Emit(OpCodes.Ldarg_1);
				MemberAccessor.EmitTypeConversion(lGenerator, fieldInfo.FieldType, false);
				lGenerator.Emit(OpCodes.Stfld, fieldInfo);
				lGenerator.Emit(OpCodes.Ret);
				this.setter = (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
			}
		}

		private sealed class PropertyMemberAccessor : MemberAccessor
		{
			public PropertyMemberAccessor(PropertyInfo propertyInfo, bool requiresSetter) : base(propertyInfo.PropertyType)
			{
				this.InitializeGetter(propertyInfo);
				this.InitializeSetter(propertyInfo, requiresSetter);
			}

			private void InitializeGetter(PropertyInfo propertyInfo)
			{
				string accessorName = MemberAccessor.GetAccessorName(true, propertyInfo.Name);
				System.Type type = typeof(object);
				System.Type[] typeArray = new System.Type[] { typeof(object) };
				DynamicMethod dynamicMethod = new DynamicMethod(accessorName, type, typeArray, true);
				ILGenerator lGenerator = dynamicMethod.GetILGenerator();
				lGenerator.DeclareLocal(typeof(object));
				lGenerator.Emit(OpCodes.Ldarg_0);
				MemberAccessor.EmitTypeConversion(lGenerator, propertyInfo.DeclaringType, true);
				MemberAccessor.EmitCall(lGenerator, propertyInfo.GetGetMethod(true));
				if (propertyInfo.PropertyType.IsValueType)
				{
					lGenerator.Emit(OpCodes.Box, propertyInfo.PropertyType);
				}
				lGenerator.Emit(OpCodes.Ret);
				this.getter = (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
			}

			private void InitializeSetter(PropertyInfo propertyInfo, bool requiresSetter)
			{
				MethodInfo setMethod = propertyInfo.GetSetMethod(true);
				if (setMethod == null)
				{
					if (requiresSetter)
					{
						throw new SerializationException("Property annotated with AmqpMemberAttribute must have a setter.");
					}
					return;
				}
				string accessorName = MemberAccessor.GetAccessorName(false, propertyInfo.Name);
				System.Type type = typeof(void);
				System.Type[] typeArray = new System.Type[] { typeof(object), typeof(object) };
				DynamicMethod dynamicMethod = new DynamicMethod(accessorName, type, typeArray, true);
				ILGenerator lGenerator = dynamicMethod.GetILGenerator();
				lGenerator.Emit(OpCodes.Ldarg_0);
				MemberAccessor.EmitTypeConversion(lGenerator, propertyInfo.DeclaringType, true);
				lGenerator.Emit(OpCodes.Ldarg_1);
				MemberAccessor.EmitTypeConversion(lGenerator, propertyInfo.PropertyType, false);
				MemberAccessor.EmitCall(lGenerator, setMethod);
				lGenerator.Emit(OpCodes.Ret);
				this.setter = (Action<object, object>)dynamicMethod.CreateDelegate(typeof(Action<object, object>));
			}
		}
	}
}