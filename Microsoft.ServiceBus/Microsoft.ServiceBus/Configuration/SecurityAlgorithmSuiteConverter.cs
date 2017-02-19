using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.ServiceModel.Security;

namespace Microsoft.ServiceBus.Configuration
{
	internal class SecurityAlgorithmSuiteConverter : TypeConverter
	{
		public SecurityAlgorithmSuiteConverter()
		{
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (typeof(string) == sourceType)
			{
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			if (typeof(InstanceDescriptor) == destinationType)
			{
				return true;
			}
			return base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			object[] fullName;
			string configInvalidClassFactoryValue;
			ExceptionUtility exceptionUtility;
			if (!(value is string))
			{
				return base.ConvertFrom(context, culture, value);
			}
			string str = (string)value;
			SecurityAlgorithmSuite @default = null;
			string str1 = str;
			string str2 = str1;
			if (str1 != null)
			{
				switch (str2)
				{
					case "Default":
					{
						@default = SecurityAlgorithmSuite.Default;
						break;
					}
					case "Basic256":
					{
						@default = SecurityAlgorithmSuite.Basic256;
						break;
					}
					case "Basic192":
					{
						@default = SecurityAlgorithmSuite.Basic192;
						break;
					}
					case "Basic128":
					{
						@default = SecurityAlgorithmSuite.Basic128;
						break;
					}
					case "TripleDes":
					{
						@default = SecurityAlgorithmSuite.TripleDes;
						break;
					}
					case "Basic256Rsa15":
					{
						@default = SecurityAlgorithmSuite.Basic256Rsa15;
						break;
					}
					case "Basic192Rsa15":
					{
						@default = SecurityAlgorithmSuite.Basic192Rsa15;
						break;
					}
					case "Basic128Rsa15":
					{
						@default = SecurityAlgorithmSuite.Basic128Rsa15;
						break;
					}
					case "TripleDesRsa15":
					{
						@default = SecurityAlgorithmSuite.TripleDesRsa15;
						break;
					}
					case "Basic256Sha256":
					{
						@default = SecurityAlgorithmSuite.Basic256Sha256;
						break;
					}
					case "Basic192Sha256":
					{
						@default = SecurityAlgorithmSuite.Basic192Sha256;
						break;
					}
					case "Basic128Sha256":
					{
						@default = SecurityAlgorithmSuite.Basic128Sha256;
						break;
					}
					case "TripleDesSha256":
					{
						@default = SecurityAlgorithmSuite.TripleDesSha256;
						break;
					}
					case "Basic256Sha256Rsa15":
					{
						@default = SecurityAlgorithmSuite.Basic256Sha256Rsa15;
						break;
					}
					case "Basic192Sha256Rsa15":
					{
						@default = SecurityAlgorithmSuite.Basic192Sha256Rsa15;
						break;
					}
					case "Basic128Sha256Rsa15":
					{
						@default = SecurityAlgorithmSuite.Basic128Sha256Rsa15;
						break;
					}
					case "TripleDesSha256Rsa15":
					{
						@default = SecurityAlgorithmSuite.TripleDesSha256Rsa15;
						break;
					}
					default:
					{
						exceptionUtility = DiagnosticUtility.ExceptionUtility;
						configInvalidClassFactoryValue = Resources.ConfigInvalidClassFactoryValue;
						fullName = new object[] { str, typeof(SecurityAlgorithmSuite).FullName };
						throw exceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", Microsoft.ServiceBus.SR.GetString(configInvalidClassFactoryValue, fullName)));
					}
				}
				return @default;
			}
			exceptionUtility = DiagnosticUtility.ExceptionUtility;
			configInvalidClassFactoryValue = Resources.ConfigInvalidClassFactoryValue;
			fullName = new object[] { str, typeof(SecurityAlgorithmSuite).FullName };
			throw exceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", Microsoft.ServiceBus.SR.GetString(configInvalidClassFactoryValue, fullName)));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (!(typeof(string) == destinationType) || !(value is SecurityAlgorithmSuite))
			{
				return base.ConvertTo(context, culture, value, destinationType);
			}
			string str = null;
			SecurityAlgorithmSuite securityAlgorithmSuite = (SecurityAlgorithmSuite)value;
			if (securityAlgorithmSuite == SecurityAlgorithmSuite.Default)
			{
				str = "Default";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic256)
			{
				str = "Basic256";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic192)
			{
				str = "Basic192";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic128)
			{
				str = "Basic128";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.TripleDes)
			{
				str = "TripleDes";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic256Rsa15)
			{
				str = "Basic256Rsa15";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic192Rsa15)
			{
				str = "Basic192Rsa15";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic128Rsa15)
			{
				str = "Basic128Rsa15";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.TripleDesRsa15)
			{
				str = "TripleDesRsa15";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic256Sha256)
			{
				str = "Basic256Sha256";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic192Sha256)
			{
				str = "Basic192Sha256";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic128Sha256)
			{
				str = "Basic128Sha256";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.TripleDesSha256)
			{
				str = "TripleDesSha256";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic256Sha256Rsa15)
			{
				str = "Basic256Sha256Rsa15";
			}
			else if (securityAlgorithmSuite == SecurityAlgorithmSuite.Basic192Sha256Rsa15)
			{
				str = "Basic192Sha256Rsa15";
			}
			else if (securityAlgorithmSuite != SecurityAlgorithmSuite.Basic128Sha256Rsa15)
			{
				if (securityAlgorithmSuite != SecurityAlgorithmSuite.TripleDesSha256Rsa15)
				{
					ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
					string configInvalidClassInstanceValue = Resources.ConfigInvalidClassInstanceValue;
					object[] fullName = new object[] { typeof(SecurityAlgorithmSuite).FullName };
					throw exceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", Microsoft.ServiceBus.SR.GetString(configInvalidClassInstanceValue, fullName)));
				}
				str = "TripleDesSha256Rsa15";
			}
			else
			{
				str = "Basic128Sha256Rsa15";
			}
			return str;
		}
	}
}