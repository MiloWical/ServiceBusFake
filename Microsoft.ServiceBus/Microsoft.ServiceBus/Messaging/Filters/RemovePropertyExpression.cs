using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class RemovePropertyExpression : Expression
	{
		private readonly static MethodInfo RemovePropertyMethodInfo;

		private readonly ParameterExpression message;

		private readonly QualifiedPropertyName property;

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
				return typeof(void);
			}
		}

		static RemovePropertyExpression()
		{
			RemovePropertyExpression.RemovePropertyMethodInfo = typeof(RemovePropertyExpression).GetMethod("RemoveProperty", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public RemovePropertyExpression(ParameterExpression message, QualifiedPropertyName property)
		{
			if (message == null)
			{
				throw Fx.Exception.ArgumentNull("message");
			}
			this.message = message;
			this.property = property;
		}

		public override Expression Reduce()
		{
			MethodInfo removePropertyMethodInfo = RemovePropertyExpression.RemovePropertyMethodInfo;
			ParameterExpression parameterExpression = this.message;
			ConstantExpression constantExpression = Expression.Constant(this.property.Scope);
			QualifiedPropertyName qualifiedPropertyName = this.property;
			return Expression.Call(removePropertyMethodInfo, parameterExpression, constantExpression, Expression.Constant(qualifiedPropertyName.Name));
		}

		private static void RemoveProperty(BrokeredMessage message, PropertyScope scope, string name)
		{
			if (scope == PropertyScope.System)
			{
				throw new ArgumentException(SRClient.SqlFilterActionCannotRemoveSystemProperty(name), "name");
			}
			if (scope != PropertyScope.User)
			{
				throw new ArgumentException(SRClient.FilterScopeNotSupported(scope), "scope");
			}
			message.BrokerRemoveProperty(name);
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { this.property };
			return string.Format(invariantCulture, "REMOVE {0}", objArray);
		}
	}
}