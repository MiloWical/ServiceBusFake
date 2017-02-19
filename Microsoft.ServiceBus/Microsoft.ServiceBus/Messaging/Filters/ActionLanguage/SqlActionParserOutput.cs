using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Filters.ActionLanguage
{
	internal sealed class SqlActionParserOutput
	{
		public Expression<Action<BrokeredMessage, IDictionary<string, object>>> ExpressionTree
		{
			get;
			private set;
		}

		public IEnumerable<string> RequiredParameters
		{
			get;
			private set;
		}

		public SqlActionParserOutput(Expression<Action<BrokeredMessage, IDictionary<string, object>>> expressionTree, IEnumerable<string> requiredParameters)
		{
			this.ExpressionTree = expressionTree;
			this.RequiredParameters = requiredParameters;
		}
	}
}