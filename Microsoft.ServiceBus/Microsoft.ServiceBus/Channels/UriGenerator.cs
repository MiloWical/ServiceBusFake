using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Globalization;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal class UriGenerator
	{
		private long id;

		private string prefix;

		public UriGenerator() : this("uuid")
		{
		}

		public UriGenerator(string scheme) : this(scheme, ";")
		{
		}

		public UriGenerator(string scheme, string delimiter)
		{
			if (scheme == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("scheme"));
			}
			if (scheme.Length == 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(Microsoft.ServiceBus.SR.GetString(Resources.UriGeneratorSchemeMustNotBeEmpty, new object[0]), "scheme"));
			}
			string[] str = new string[] { scheme, ":", null, null, null };
			str[2] = Guid.NewGuid().ToString();
			str[3] = delimiter;
			str[4] = "id=";
			this.prefix = string.Concat(str);
		}

		public string Next()
		{
			long num = Interlocked.Increment(ref this.id);
			return string.Concat(this.prefix, num.ToString(CultureInfo.InvariantCulture));
		}
	}
}