using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal sealed class DelegateFilter : Filter
	{
		private readonly Func<BrokeredMessage, IDictionary<string, object>, bool?> function;

		private readonly IDictionary<string, object> parameters;

		internal override Microsoft.ServiceBus.Messaging.FilterType FilterType
		{
			get
			{
				return Microsoft.ServiceBus.Messaging.FilterType.LambdaExpressionFilter;
			}
		}

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		public DelegateFilter(Func<BrokeredMessage, IDictionary<string, object>, bool?> function, IDictionary<string, object> parameters)
		{
			if (function == null)
			{
				throw new ArgumentNullException("function");
			}
			this.function = function;
			this.parameters = parameters;
		}

		public override bool Match(BrokeredMessage message)
		{
			bool flag;
			try
			{
				bool? nullable = this.function(message, this.parameters);
				flag = (nullable.HasValue ? nullable.GetValueOrDefault() : false);
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
		}
	}
}