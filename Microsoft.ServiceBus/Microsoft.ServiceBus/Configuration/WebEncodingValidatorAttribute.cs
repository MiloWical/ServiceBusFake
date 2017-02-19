using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	[AttributeUsage(AttributeTargets.Property)]
	internal sealed class WebEncodingValidatorAttribute : ConfigurationValidatorAttribute
	{
		public override ConfigurationValidatorBase ValidatorInstance
		{
			get
			{
				return new WebEncodingValidator();
			}
		}

		public WebEncodingValidatorAttribute()
		{
		}
	}
}