using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class InExpression : Expression
	{
		private readonly static MethodInfo InMethodInfo;

		public override bool CanReduce
		{
			get
			{
				return true;
			}
		}

		public Expression Left
		{
			get;
			private set;
		}

		public override ExpressionType NodeType
		{
			get
			{
				return ExpressionType.Extension;
			}
		}

		public IEnumerable<Expression> Right
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

		static InExpression()
		{
			InExpression.InMethodInfo = typeof(InExpression).GetMethod("InMethod", BindingFlags.Static | BindingFlags.NonPublic);
		}

		public InExpression(Expression operand, IEnumerable<Expression> elementList)
		{
			this.Left = operand;
			this.Right = elementList;
		}

		private static bool? InMethod(object left, List<object> rightList)
		{
			bool? nullable;
			if (left is DBNull)
			{
				return null;
			}
			dynamic obj = left;
			List<object>.Enumerator enumerator = rightList.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					if (obj != enumerator.Current)
					{
						continue;
					}
					nullable = new bool?(true);
					return nullable;
				}
				return new bool?(false);
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return nullable;
		}

		public override Expression Reduce()
		{
			Expression expression = Expression.ListInit(Expression.New(typeof(List<object>)), this.Right);
			return Expression.Call(InExpression.InMethodInfo, this.Left, expression);
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder(string.Concat(this.Left.ToString(), " IN ("));
			int num = 0;
			foreach (Expression right in this.Right)
			{
				if (num != 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(right);
				num++;
			}
			stringBuilder.Append(')');
			return stringBuilder.ToString();
		}
	}
}