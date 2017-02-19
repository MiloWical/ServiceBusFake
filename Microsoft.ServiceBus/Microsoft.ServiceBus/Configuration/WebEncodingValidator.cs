using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Configuration;
using System.Text;

namespace Microsoft.ServiceBus.Configuration
{
	internal class WebEncodingValidator : ConfigurationValidatorBase
	{
		public WebEncodingValidator()
		{
		}

		public override bool CanValidate(Type type)
		{
			return type == typeof(Encoding);
		}

		public override void Validate(object value)
		{
			Encoding encoding = value as Encoding;
			if (encoding == null || encoding != Encoding.UTF8 && encoding != Encoding.Unicode && encoding != Encoding.BigEndianUnicode)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("value", "JsonEncodingNotSupported");
			}
		}
	}
}