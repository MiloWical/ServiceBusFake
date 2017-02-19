using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ServiceBus.Messaging.Filters
{
	internal static class ParametersValidator
	{
		public static void Validate(bool isFilter, IEnumerable<string> requiredParameters, IDictionary<string, object> suppliedParameters)
		{
			MessagingException filterException;
			MessagingException ruleActionException;
			HashSet<string> strs = new HashSet<string>(suppliedParameters.Keys, StringComparer.OrdinalIgnoreCase);
			foreach (string requiredParameter in requiredParameters)
			{
				if (!suppliedParameters.ContainsKey(requiredParameter))
				{
					string str = SRClient.ParameterNotSpecifiedForSqlExpression(requiredParameter);
					if (isFilter)
					{
						filterException = new FilterException(str);
					}
					else
					{
						filterException = new RuleActionException(str);
					}
					throw filterException;
				}
				strs.Remove(requiredParameter);
			}
			if (strs.Count != 0)
			{
				string str1 = SRClient.ExtraParameterSpecifiedForSqlExpression(strs.First<string>());
				if (isFilter)
				{
					ruleActionException = new FilterException(str1);
				}
				else
				{
					ruleActionException = new RuleActionException(str1);
				}
				throw ruleActionException;
			}
		}
	}
}