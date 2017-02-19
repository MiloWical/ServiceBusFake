using System;
using System.Globalization;
using System.Reflection;

namespace Microsoft.ServiceBus.Channels
{
	internal class InvokeHelper
	{
		public InvokeHelper()
		{
		}

		public static object InvokeInstanceGet(Type type, object obj, string name)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
			return property.GetValue(obj, new object[0]);
		}

		public static object InvokeInstanceMethod(Type type, object obj, string name, params object[] parameters)
		{
			return type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, parameters);
		}

		public static void InvokeInstanceSet(Type type, object obj, string name, object value)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
			property.SetValue(obj, value, new object[0]);
		}

		public static object InvokeStaticGet(Type type, string name)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic);
			return property.GetValue(null, new object[0]);
		}

		public static object InvokeStaticMethod(Type type, string name, params object[] parameters)
		{
			return type.InvokeMember(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, parameters, CultureInfo.InvariantCulture);
		}

		public static void InvokeStaticSet(Type type, string name, object value)
		{
			PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic);
			property.SetValue(null, value, new object[0]);
		}
	}
}