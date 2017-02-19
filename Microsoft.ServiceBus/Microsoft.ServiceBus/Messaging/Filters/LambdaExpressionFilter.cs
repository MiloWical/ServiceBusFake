using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class LambdaExpressionFilter : Filter
	{
		private Func<BrokeredMessage, bool?> filterDelegate;

		public Expression<Func<BrokeredMessage, bool?>> Expression
		{
			get;
			private set;
		}

		internal override Microsoft.ServiceBus.Messaging.FilterType FilterType
		{
			get
			{
				return Microsoft.ServiceBus.Messaging.FilterType.SqlFilter;
			}
		}

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		public LambdaExpressionFilter(Expression<Func<BrokeredMessage, bool?>> expression)
		{
			if (expression == null)
			{
				throw Fx.Exception.ArgumentNull("expression");
			}
			this.Expression = expression;
		}

		public override bool Match(BrokeredMessage message)
		{
			bool flag;
			if (message == null)
			{
				throw new ArgumentNullException("message");
			}
			if (this.filterDelegate == null)
			{
				this.filterDelegate = this.Expression.Compile();
			}
			try
			{
				bool? nullable = this.filterDelegate(message);
				flag = (!nullable.HasValue ? false : nullable.Value);
			}
			catch (FilterException filterException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new FilterException(exception.Message, exception);
				}
				throw;
			}
			return flag;
		}

		public override Filter Preprocess()
		{
			return this;
		}

		public override void Validate()
		{
			Expression<Func<BrokeredMessage, bool?>> expression = this.Expression;
			(new LambdaExpressionFilter.ExpressionValidator()).Validate(expression);
		}

		private sealed class ExpressionValidator : ExpressionVisitor
		{
			private int nodeCount;

			private int currentDepth;

			public ExpressionValidator()
			{
			}

			public void Validate(System.Linq.Expressions.Expression expression)
			{
				this.nodeCount = 0;
				this.currentDepth = 0;
				try
				{
					this.Visit(expression);
				}
				catch (FilterException filterException)
				{
					throw filterException;
				}
			}

			public override System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression node)
			{
				System.Linq.Expressions.Expression expression;
				LambdaExpressionFilter.ExpressionValidator expressionValidator = this;
				expressionValidator.nodeCount = expressionValidator.nodeCount + 1;
				LambdaExpressionFilter.ExpressionValidator expressionValidator1 = this;
				expressionValidator1.currentDepth = expressionValidator1.currentDepth + 1;
				if (this.currentDepth > 32)
				{
					throw new FilterException(SRClient.FilterExpressionTooComplex);
				}
				if (this.nodeCount > 1024)
				{
					throw new FilterException(SRClient.FilterExpressionTooComplex);
				}
				try
				{
					expression = base.Visit(node);
				}
				finally
				{
					LambdaExpressionFilter.ExpressionValidator expressionValidator2 = this;
					expressionValidator2.currentDepth = expressionValidator2.currentDepth - 1;
				}
				return expression;
			}
		}
	}
}