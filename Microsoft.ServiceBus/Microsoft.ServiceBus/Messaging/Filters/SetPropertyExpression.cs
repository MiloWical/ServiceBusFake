using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class SetPropertyExpression : Expression
	{
		private readonly static MethodInfo SetPropertyMethodInfo;

		private readonly ParameterExpression message;

		private readonly QualifiedPropertyName property;

		private readonly Expression @value;

		public override bool CanReduce
		{
			get
			{
				return true;
			}
		}

		public override ExpressionType NodeType
		{
			get
			{
				return ExpressionType.Extension;
			}
		}

		public override System.Type Type
		{
			get
			{
				return typeof(object);
			}
		}

		static SetPropertyExpression()
		{
			SetPropertyExpression.SetPropertyMethodInfo = typeof(SetPropertyExpression).GetMethod("SetPropertyMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public SetPropertyExpression(ParameterExpression message, QualifiedPropertyName property, Expression value)
		{
			if (message == null)
			{
				throw Fx.Exception.ArgumentNull("message");
			}
			if (value == null)
			{
				throw Fx.Exception.ArgumentNull("value");
			}
			if (property.Scope == PropertyScope.System && !SystemPropertyAccessor.HasSetProperty(property.Name))
			{
				throw new ArgumentException(SRClient.MessageSetPropertyNotFound(property.Name, typeof(BrokeredMessage)), "property");
			}
			this.message = message;
			this.property = property;
			this.@value = value;
		}

		private static object ImplicitConvert(object value, System.Type conversionType)
		{
			if (conversionType == typeof(Guid) && value is string)
			{
				return new Guid((string)value);
			}
			if (conversionType == typeof(Uri) && value is string)
			{
				return new Uri((string)value, UriKind.RelativeOrAbsolute);
			}
			if (conversionType == typeof(DateTimeOffset) && value is string)
			{
				return DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture);
			}
			if (conversionType == typeof(TimeSpan) && value is string)
			{
				return TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
			}
			if (conversionType == typeof(string) && (value is Guid || value is DateTimeOffset || value is TimeSpan || value is Uri))
			{
				return Convert.ToString(value, CultureInfo.InvariantCulture);
			}
			return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
		}

		public override Expression Reduce()
		{
			MethodInfo setPropertyMethodInfo = SetPropertyExpression.SetPropertyMethodInfo;
			ParameterExpression parameterExpression = this.message;
			ConstantExpression constantExpression = Expression.Constant(this.property.Scope);
			QualifiedPropertyName qualifiedPropertyName = this.property;
			return Expression.Call(setPropertyMethodInfo, parameterExpression, constantExpression, Expression.Constant(qualifiedPropertyName.Name), this.@value);
		}

		private static object SetPropertyMethod(BrokeredMessage message, PropertyScope scope, string name, object value)
		{
			if (value is DBNull)
			{
				value = null;
			}
			if (scope == PropertyScope.System)
			{
				return SetPropertyExpression.SetSystemProperty(message, name, value);
			}
			if (scope != PropertyScope.User)
			{
				throw new ArgumentException(SRClient.FilterScopeNotSupported(scope), "scope");
			}
			return SetPropertyExpression.SetUserProperty(message, name, value);
		}

		private static object SetSystemProperty(BrokeredMessage message, string propertyName, object value)
		{
			object obj;
			System.Type propertyType = SystemPropertyAccessor.GetPropertyType(propertyName);
			obj = (value == null || !(value.GetType() != propertyType) ? value : SetPropertyExpression.ImplicitConvert(value, propertyType));
			SystemPropertyAccessor.SetProperty(message, propertyName, obj);
			return obj;
		}

		private static object SetUserProperty(BrokeredMessage message, string propertyName, object value)
		{
			object obj;
			System.Type type = null;
			if (message.Properties.TryGetValue(propertyName, out obj) && obj != null)
			{
				type = obj.GetType();
			}
			object obj1 = (type == null ? value : SetPropertyExpression.ImplicitConvert(value, type));
			message.BrokerUpdateProperty(propertyName, obj1);
			return obj1;
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { this.property, this.@value };
			return string.Format(invariantCulture, "SET {0} = ({1})", objArray);
		}
	}
}