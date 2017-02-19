using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class IsNullExpression : Expression
	{
		private readonly static MethodInfo IsNullMethodInfo;

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

		public Expression Operand
		{
			get;
			private set;
		}

		public override System.Type Type
		{
			get
			{
				return typeof(bool?);
			}
		}

		static IsNullExpression()
		{
			IsNullExpression.IsNullMethodInfo = typeof(IsNullExpression).GetMethod("IsNullMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public IsNullExpression(Expression operand)
		{
			this.Operand = operand;
		}

		private static bool? IsNullMethod(object operand)
		{
			return new bool?((operand == null ? true : operand is DBNull));
		}

		public override Expression Reduce()
		{
			return Expression.Call(IsNullExpression.IsNullMethodInfo, this.Operand);
		}

		public override string ToString()
		{
			return string.Concat("IS NULL (", this.Operand, ")");
		}
	}
}