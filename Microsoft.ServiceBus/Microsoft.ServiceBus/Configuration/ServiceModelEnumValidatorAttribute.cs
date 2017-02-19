using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	[AttributeUsage(AttributeTargets.Property)]
	internal sealed class ServiceModelEnumValidatorAttribute : ConfigurationValidatorAttribute
	{
		private Type enumHelperType;

		public Type EnumHelperType
		{
			get
			{
				return this.enumHelperType;
			}
			set
			{
				this.enumHelperType = value;
			}
		}

		public override ConfigurationValidatorBase ValidatorInstance
		{
			get
			{
				return new ServiceModelEnumValidator(this.enumHelperType);
			}
		}

		public ServiceModelEnumValidatorAttribute(Type enumHelperType)
		{
			this.EnumHelperType = enumHelperType;
		}
	}
}