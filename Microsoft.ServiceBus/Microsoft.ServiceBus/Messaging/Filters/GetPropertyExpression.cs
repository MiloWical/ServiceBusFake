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
	internal sealed class GetPropertyExpression : Expression
	{
		private readonly static MethodInfo GetPropertyMethodInfo;

		private readonly ParameterExpression message;

		private readonly Expression propertyScope;

		private readonly Expression propertyName;

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
				return Constants.ObjectType;
			}
		}

		static GetPropertyExpression()
		{
			GetPropertyExpression.GetPropertyMethodInfo = typeof(GetPropertyExpression).GetMethod("GetPropertyMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public GetPropertyExpression(ParameterExpression message, QualifiedPropertyName qualifiedPropertyName)
		{
			if (message == null)
			{
				throw Fx.Exception.ArgumentNull("message");
			}
			if (qualifiedPropertyName.Scope == PropertyScope.System && !SystemPropertyAccessor.HasGetProperty(qualifiedPropertyName.Name))
			{
				throw new ArgumentException(SRClient.MessageGetPropertyNotFound(qualifiedPropertyName.Name, typeof(BrokeredMessage)), "qualifiedPropertyName");
			}
			this.message = message;
			this.propertyScope = Expression.Constant(qualifiedPropertyName.Scope);
			this.propertyName = Expression.Constant(qualifiedPropertyName.Name);
		}

		public GetPropertyExpression(ParameterExpression message, Expression propertyScope, Expression propertyName)
		{
			if (message == null)
			{
				throw Fx.Exception.ArgumentNull("message");
			}
			if (propertyScope == null)
			{
				throw Fx.Exception.ArgumentNull("propertyScope");
			}
			if (propertyName == null)
			{
				throw Fx.Exception.ArgumentNull("propertyName");
			}
			this.message = message;
			this.propertyScope = propertyScope;
			this.propertyName = propertyName;
			if (propertyName.Type != typeof(string))
			{
				this.propertyName = Expression.Convert(propertyName, typeof(string));
			}
		}

		private static object GetPropertyMethod(BrokeredMessage message, PropertyScope scope, string name)
		{
			if (scope == PropertyScope.System)
			{
				return GetPropertyExpression.GetSystemProperty(message, name);
			}
			if (scope != PropertyScope.User)
			{
				throw new ArgumentException(SRClient.FilterScopeNotSupported(scope), "scope");
			}
			return GetPropertyExpression.GetUserProperty(message, name);
		}

		private static object GetSystemProperty(BrokeredMessage message, string propertyName)
		{
			return SystemPropertyAccessor.GetProperty(message, propertyName);
		}

		private static object GetUserProperty(BrokeredMessage message, string propertyName)
		{
			object obj;
			if (!message.Properties.TryGetValue(propertyName, out obj))
			{
				return DBNull.Value;
			}
			return obj;
		}

		public override Expression Reduce()
		{
			return Expression.Call(GetPropertyExpression.GetPropertyMethodInfo, this.message, this.propertyScope, this.propertyName);
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { this.propertyScope, this.propertyName };
			return string.Format(invariantCulture, "GetProperty({0}, {1})", objArray);
		}
	}
}