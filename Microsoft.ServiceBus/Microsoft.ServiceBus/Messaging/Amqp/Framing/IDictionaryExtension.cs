using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
	internal static class IDictionaryExtension
	{
		public static void Add(this IDictionary<string, object> dictionary, bool condition, string key, object value)
		{
			if (condition)
			{
				dictionary.Add(key, value);
			}
		}
	}
}