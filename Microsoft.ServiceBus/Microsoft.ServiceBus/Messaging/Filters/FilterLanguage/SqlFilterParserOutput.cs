using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Filters.FilterLanguage
{
	internal sealed class SqlFilterParserOutput
	{
		public Expression<Func<BrokeredMessage, IDictionary<string, object>, bool?>> ExpressionTree
		{
			get;
			private set;
		}

		public IEnumerable<string> RequiredParameters
		{
			get;
			private set;
		}

		public SqlFilterParserOutput(Expression<Func<BrokeredMessage, IDictionary<string, object>, bool?>> expressionTree, IEnumerable<string> requiredParameters)
		{
			this.ExpressionTree = expressionTree;
			this.RequiredParameters = requiredParameters;
		}
	}
}