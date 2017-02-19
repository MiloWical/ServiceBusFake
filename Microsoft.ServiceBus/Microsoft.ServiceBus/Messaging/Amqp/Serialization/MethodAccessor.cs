using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
	internal abstract class MethodAccessor
	{
		private readonly static Type[] delegateParamsType;

		private bool isStatic;

		private MethodAccessor.MethodDelegate methodDelegate;

		static MethodAccessor()
		{
			MethodAccessor.delegateParamsType = new Type[] { typeof(object), typeof(object[]) };
		}

		protected MethodAccessor()
		{
		}

		public static MethodAccessor Create(MethodInfo methodInfo)
		{
			return new MethodAccessor.TypeMethodAccessor(methodInfo);
		}

		public static MethodAccessor Create(ConstructorInfo constructorInfo)
		{
			return new MethodAccessor.ConstructorAccessor(constructorInfo);
		}

		private Type[] GetParametersType(ParameterInfo[] paramsInfo)
		{
			Type[] typeArray = new Type[(int)paramsInfo.Length];
			for (int i = 0; i < (int)paramsInfo.Length; i++)
			{
				typeArray[i] = (paramsInfo[i].ParameterType.IsByRef ? paramsInfo[i].ParameterType.GetElementType() : paramsInfo[i].ParameterType);
			}
			return typeArray;
		}

		public object Invoke(object[] parameters)
		{
			if (!this.isStatic)
			{
				throw new InvalidOperationException("Instance required to call an instance method.");
			}
			return this.Invoke(null, parameters);
		}

		public object Invoke(object container, object[] parameters)
		{
			if (this.isStatic && container != null)
			{
				throw new InvalidOperationException("Static method must be called with null instance.");
			}
			return this.methodDelegate(container, parameters);
		}

		private void LoadArguments(ILGenerator generator, Type[] paramsType)
		{
			for (int i = 0; i < (int)paramsType.Length; i++)
			{
				generator.Emit(OpCodes.Ldarg_1);
				switch (i)
				{
					case 0:
					{
						generator.Emit(OpCodes.Ldc_I4_0);
						break;
					}
					case 1:
					{
						generator.Emit(OpCodes.Ldc_I4_1);
						break;
					}
					case 2:
					{
						generator.Emit(OpCodes.Ldc_I4_2);
						break;
					}
					case 3:
					{
						generator.Emit(OpCodes.Ldc_I4_3);
						break;
					}
					case 4:
					{
						generator.Emit(OpCodes.Ldc_I4_4);
						break;
					}
					case 5:
					{
						generator.Emit(OpCodes.Ldc_I4_5);
						break;
					}
					case 6:
					{
						generator.Emit(OpCodes.Ldc_I4_6);
						break;
					}
					case 7:
					{
						generator.Emit(OpCodes.Ldc_I4_7);
						break;
					}
					case 8:
					{
						generator.Emit(OpCodes.Ldc_I4_8);
						break;
					}
					default:
					{
						generator.Emit(OpCodes.Ldc_I4, i);
						break;
					}
				}
				generator.Emit(OpCodes.Ldelem_Ref);
				if (paramsType[i].IsValueType)
				{
					generator.Emit(OpCodes.Unbox_Any, paramsType[i]);
				}
				else if (paramsType[i] != typeof(object))
				{
					generator.Emit(OpCodes.Castclass, paramsType[i]);
				}
			}
		}

		private sealed class ConstructorAccessor : MethodAccessor
		{
			public ConstructorAccessor(ConstructorInfo constructorInfo)
			{
				this.isStatic = true;
				DynamicMethod dynamicMethod = new DynamicMethod(string.Concat("ctor_", constructorInfo.DeclaringType.Name), typeof(object), MethodAccessor.delegateParamsType, true);
				Type[] parametersType = base.GetParametersType(constructorInfo.GetParameters());
				ILGenerator lGenerator = dynamicMethod.GetILGenerator();
				base.LoadArguments(lGenerator, parametersType);
				lGenerator.Emit(OpCodes.Newobj, constructorInfo);
				if (constructorInfo.DeclaringType.IsValueType)
				{
					lGenerator.Emit(OpCodes.Box, constructorInfo.DeclaringType);
				}
				lGenerator.Emit(OpCodes.Ret);
				this.methodDelegate = (MethodAccessor.MethodDelegate)dynamicMethod.CreateDelegate(typeof(MethodAccessor.MethodDelegate));
			}
		}

		private delegate object MethodDelegate(object container, object[] parameters);

		private sealed class TypeMethodAccessor : MethodAccessor
		{
			public TypeMethodAccessor(MethodInfo methodInfo)
			{
				Type[] parametersType = base.GetParametersType(methodInfo.GetParameters());
				DynamicMethod dynamicMethod = new DynamicMethod(string.Concat("method_", methodInfo.Name), typeof(object), MethodAccessor.delegateParamsType, true);
				ILGenerator lGenerator = dynamicMethod.GetILGenerator();
				if (!this.isStatic)
				{
					lGenerator.Emit(OpCodes.Ldarg_0);
					if (!methodInfo.DeclaringType.IsValueType)
					{
						lGenerator.Emit(OpCodes.Castclass, methodInfo.DeclaringType);
					}
					else
					{
						lGenerator.Emit(OpCodes.Unbox_Any, methodInfo.DeclaringType);
					}
				}
				base.LoadArguments(lGenerator, parametersType);
				if (!methodInfo.IsFinal)
				{
					lGenerator.Emit(OpCodes.Callvirt, methodInfo);
				}
				else
				{
					lGenerator.Emit(OpCodes.Call, methodInfo);
				}
				if (methodInfo.ReturnType == typeof(void))
				{
					lGenerator.Emit(OpCodes.Ldnull);
				}
				else if (methodInfo.ReturnType.IsValueType)
				{
					lGenerator.Emit(OpCodes.Box, methodInfo.ReturnType);
				}
				lGenerator.Emit(OpCodes.Ret);
				this.methodDelegate = (MethodAccessor.MethodDelegate)dynamicMethod.CreateDelegate(typeof(MethodAccessor.MethodDelegate));
			}
		}
	}
}