using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Filters;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Microsoft.ServiceBus.Messaging.Filters.FilterLanguage
{
	internal struct LexValue
	{
		public List<Expression> expressions;

		public Expression expression;

		public DelayExpression delay;

		public string @value;

		public QualifiedPropertyName qname;
	}
}