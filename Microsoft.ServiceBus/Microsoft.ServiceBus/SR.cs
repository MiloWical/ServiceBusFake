using Microsoft.ServiceBus.Properties;
using System;

namespace Microsoft.ServiceBus
{
	internal sealed class SR : Resources
	{
		public SR()
		{
		}

		internal static string GetString(string value, params object[] args)
		{
			if (args == null || (int)args.Length <= 0)
			{
				return value;
			}
			return string.Format(Resources.Culture, value, args);
		}
	}
}