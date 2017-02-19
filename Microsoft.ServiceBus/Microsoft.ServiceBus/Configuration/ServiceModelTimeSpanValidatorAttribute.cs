using Microsoft.ServiceBus.Common;
using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	[AttributeUsage(AttributeTargets.Property)]
	internal sealed class ServiceModelTimeSpanValidatorAttribute : ConfigurationValidatorAttribute
	{
		private TimeSpanValidatorAttribute innerValidatorAttribute;

		public TimeSpan MaxValue
		{
			get
			{
				return this.innerValidatorAttribute.MaxValue;
			}
		}

		public string MaxValueString
		{
			get
			{
				return this.innerValidatorAttribute.MaxValueString;
			}
			set
			{
				this.innerValidatorAttribute.MaxValueString = value;
			}
		}

		public TimeSpan MinValue
		{
			get
			{
				return this.innerValidatorAttribute.MinValue;
			}
		}

		public string MinValueString
		{
			get
			{
				return this.innerValidatorAttribute.MinValueString;
			}
			set
			{
				this.innerValidatorAttribute.MinValueString = value;
			}
		}

		public override ConfigurationValidatorBase ValidatorInstance
		{
			get
			{
				return new TimeSpanOrInfiniteValidator(this.MinValue, this.MaxValue);
			}
		}

		public ServiceModelTimeSpanValidatorAttribute()
		{
			this.innerValidatorAttribute = new TimeSpanValidatorAttribute()
			{
				MaxValueString = TimeoutHelper.MaxWait.ToString()
			};
		}
	}
}