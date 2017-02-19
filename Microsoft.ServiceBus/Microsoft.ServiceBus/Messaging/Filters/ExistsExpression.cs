using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class ExistsExpression : Expression
	{
		private readonly static MethodInfo ExistsMethodInfo;

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

		static ExistsExpression()
		{
			ExistsExpression.ExistsMethodInfo = typeof(ExistsExpression).GetMethod("ExistsMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public ExistsExpression(Expression operand)
		{
			this.Operand = operand;
		}

		private static bool? ExistsMethod(object operand)
		{
			return new bool?(!(operand is DBNull));
		}

		public override Expression Reduce()
		{
			return Expression.Call(ExistsExpression.ExistsMethodInfo, this.Operand);
		}

		public override string ToString()
		{
			return string.Concat("EXISTS (", this.Operand, ")");
		}
	}
}