using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Reflection;

namespace Microsoft.ServiceBus.Configuration
{
	internal class ServiceModelEnumValidator : ConfigurationValidatorBase
	{
		private Type enumHelperType;

		private MethodInfo isDefined;

		public ServiceModelEnumValidator(Type enumHelperType)
		{
			this.enumHelperType = enumHelperType;
			this.isDefined = this.enumHelperType.GetMethod("IsDefined", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		public override bool CanValidate(Type type)
		{
			return this.isDefined != null;
		}

		public override void Validate(object value)
		{
			MethodInfo methodInfo = this.isDefined;
			object[] objArray = new object[] { value };
			if (!(bool)methodInfo.Invoke(null, objArray))
			{
				ParameterInfo[] parameters = this.isDefined.GetParameters();
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidEnumArgumentException("value", (int)value, parameters[0].ParameterType));
			}
		}
	}
}