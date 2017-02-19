using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal static class SystemPropertyAccessor
	{
		private readonly static Dictionary<string, PropertyInfo> PublicProperties;

		private readonly static Dictionary<string, Func<BrokeredMessage, object>> GetPropertyFunctions;

		private readonly static Dictionary<string, Action<BrokeredMessage, object>> SetPropertyActions;

		static SystemPropertyAccessor()
		{
			SystemPropertyAccessor.PublicProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
			SystemPropertyAccessor.GetPropertyFunctions = new Dictionary<string, Func<BrokeredMessage, object>>(StringComparer.OrdinalIgnoreCase);
			SystemPropertyAccessor.SetPropertyActions = new Dictionary<string, Action<BrokeredMessage, object>>(StringComparer.OrdinalIgnoreCase);
			ParameterExpression parameterExpression = Expression.Parameter(typeof(BrokeredMessage), "message");
			PropertyInfo[] properties = typeof(BrokeredMessage).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
			for (int i = 0; i < (int)properties.Length; i++)
			{
				PropertyInfo propertyInfo = properties[i];
				SystemPropertyAccessor.PublicProperties.Add(propertyInfo.Name, propertyInfo);
				Expression expression = Expression.Property(parameterExpression, propertyInfo);
				if (propertyInfo.GetGetMethod(false) != null)
				{
					Expression expression1 = Expression.Convert(expression, typeof(object));
					ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { parameterExpression };
					Func<BrokeredMessage, object> func = Expression.Lambda<Func<BrokeredMessage, object>>(expression1, parameterExpressionArray).Compile();
					SystemPropertyAccessor.GetPropertyFunctions.Add(propertyInfo.Name, func);
				}
				if (propertyInfo.GetSetMethod(false) != null)
				{
					ParameterExpression parameterExpression1 = Expression.Parameter(typeof(object), "value");
					Expression expression2 = Expression.Assign(expression, Expression.Convert(parameterExpression1, expression.Type));
					ParameterExpression[] parameterExpressionArray1 = new ParameterExpression[] { parameterExpression, parameterExpression1 };
					Action<BrokeredMessage, object> action = Expression.Lambda<Action<BrokeredMessage, object>>(expression2, parameterExpressionArray1).Compile();
					SystemPropertyAccessor.SetPropertyActions.Add(propertyInfo.Name, action);
				}
			}
		}

		public static object GetProperty(BrokeredMessage message, string propertyName)
		{
			Func<BrokeredMessage, object> func;
			if (!SystemPropertyAccessor.GetPropertyFunctions.TryGetValue(propertyName, out func))
			{
				throw new ArgumentException(SRClient.MessageSetPropertyNotFound(propertyName, typeof(BrokeredMessage).Name));
			}
			return func(message);
		}

		public static Type GetPropertyType(string propertyName)
		{
			return SystemPropertyAccessor.PublicProperties[propertyName].PropertyType;
		}

		public static bool HasGetProperty(string propertyName)
		{
			return SystemPropertyAccessor.GetPropertyFunctions.ContainsKey(propertyName);
		}

		public static bool HasSetProperty(string propertyName)
		{
			return SystemPropertyAccessor.SetPropertyActions.ContainsKey(propertyName);
		}

		public static void SetProperty(BrokeredMessage message, string propertyName, object value)
		{
			Action<BrokeredMessage, object> action;
			if (!SystemPropertyAccessor.SetPropertyActions.TryGetValue(propertyName, out action))
			{
				throw new ArgumentException(SRClient.MessageSetPropertyNotFound(propertyName, typeof(BrokeredMessage).Name));
			}
			action(message, value);
		}
	}
}