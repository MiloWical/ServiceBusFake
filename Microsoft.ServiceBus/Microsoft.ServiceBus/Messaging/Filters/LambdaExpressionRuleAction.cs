using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class LambdaExpressionRuleAction : RuleAction
	{
		private Action<BrokeredMessage> compiledExpression;

		private bool requiresPreprocessing;

		public Expression<Action<BrokeredMessage>> Expression
		{
			get;
			private set;
		}

		public override bool RequiresPreprocessing
		{
			get
			{
				return this.requiresPreprocessing;
			}
		}

		public LambdaExpressionRuleAction(Expression<Action<BrokeredMessage>> expression) : this(expression, true)
		{
		}

		public LambdaExpressionRuleAction(Expression<Action<BrokeredMessage>> expression, bool requiresPreprocessing)
		{
			if (expression == null)
			{
				throw Fx.Exception.ArgumentNull("expression");
			}
			this.Expression = expression;
			this.requiresPreprocessing = requiresPreprocessing;
		}

		public override BrokeredMessage Execute(BrokeredMessage message)
		{
			BrokeredMessage brokeredMessage;
			if (this.compiledExpression == null)
			{
				this.compiledExpression = this.Expression.Compile();
			}
			try
			{
				this.compiledExpression(message);
				brokeredMessage = message;
			}
			catch (RuleActionException ruleActionException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new RuleActionException(exception.Message, exception);
				}
				throw;
			}
			return brokeredMessage;
		}

		internal override BrokeredMessage Execute(BrokeredMessage message, RuleExecutionContext context)
		{
			return this.Execute(message);
		}

		public override RuleAction Preprocess()
		{
			if (!this.RequiresPreprocessing)
			{
				return this;
			}
			return new LambdaExpressionRuleAction(this.Expression, false);
		}

		public override void Validate()
		{
		}
	}
}