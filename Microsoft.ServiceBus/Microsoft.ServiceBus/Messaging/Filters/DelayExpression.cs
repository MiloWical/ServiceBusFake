using Babel.ParserGenerator;
using Microsoft.ServiceBus;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal abstract class DelayExpression
	{
		protected DelayExpression()
		{
		}

		public static DelayExpression Expression(Expression expression)
		{
			return new DelayExpression.NormalExpression(expression);
		}

		public virtual Expression GetExpression()
		{
			throw new InvalidOperationException();
		}

		public static DelayExpression NumericConstant(LexLocation location, string token, TypeCode typeCode)
		{
			return new DelayExpression.NumericConstantExpression(location, token, typeCode);
		}

		public virtual bool TryGetConstantNumericLiteral(ExpressionType expressionType, out Expression expression)
		{
			expression = null;
			return false;
		}

		private sealed class NormalExpression : DelayExpression
		{
			private readonly Expression expression;

			public NormalExpression(Expression expression)
			{
				if (expression == null)
				{
					throw new ArgumentNullException("expression");
				}
				this.expression = expression;
			}

			public override Expression GetExpression()
			{
				return this.expression;
			}
		}

		private sealed class NumericConstantExpression : DelayExpression
		{
			private readonly LexLocation location;

			private readonly string token;

			private readonly TypeCode typeCode;

			public NumericConstantExpression(LexLocation location, string token, TypeCode typeCode)
			{
				if (location == null)
				{
					throw new ArgumentNullException("location");
				}
				if (token == null)
				{
					throw new ArgumentNullException("token");
				}
				this.location = location;
				this.token = token;
				this.typeCode = typeCode;
			}

			public override Expression GetExpression()
			{
				return this.GetExpression(this.token);
			}

			private Expression GetExpression(string literal)
			{
				Expression expression;
				try
				{
					expression = Expression.Constant(Convert.ChangeType(literal, this.typeCode, NumberFormatInfo.InvariantInfo));
				}
				catch (OverflowException overflowException1)
				{
					OverflowException overflowException = overflowException1;
					throw new OverflowException(SRClient.SQLSyntaxErrorDetailed(this.location.sLin, this.location.sCol, literal, overflowException.Message));
				}
				catch (InvalidCastException invalidCastException1)
				{
					InvalidCastException invalidCastException = invalidCastException1;
					throw new InvalidCastException(SRClient.SQLSyntaxErrorDetailed(this.location.sLin, this.location.sCol, literal, invalidCastException.Message));
				}
				catch (FormatException formatException1)
				{
					FormatException formatException = formatException1;
					throw new FormatException(SRClient.SQLSyntaxErrorDetailed(this.location.sLin, this.location.sCol, literal, formatException.Message));
				}
				catch (ArgumentException argumentException1)
				{
					ArgumentException argumentException = argumentException1;
					throw new ArgumentException(SRClient.SQLSyntaxErrorDetailed(this.location.sLin, this.location.sCol, literal, argumentException.Message));
				}
				return expression;
			}

			public override bool TryGetConstantNumericLiteral(ExpressionType expressionType, out Expression expression)
			{
				expression = this.GetExpression((expressionType == ExpressionType.Negate || expressionType == ExpressionType.NegateChecked ? string.Concat("-", this.token) : this.token));
				return true;
			}
		}
	}
}