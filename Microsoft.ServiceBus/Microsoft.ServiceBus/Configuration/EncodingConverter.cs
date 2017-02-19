using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Text;

namespace Microsoft.ServiceBus.Configuration
{
	internal class EncodingConverter : TypeConverter
	{
		public EncodingConverter()
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
			if (!(value is string))
			{
				return base.ConvertFrom(context, culture, value);
			}
			string str = (string)value;
			Encoding encoding = Encoding.GetEncoding(str);
			if (encoding == null)
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string configInvalidEncodingValue = Resources.ConfigInvalidEncodingValue;
				object[] objArray = new object[] { str };
				throw exceptionUtility.ThrowHelperArgument("value", Microsoft.ServiceBus.SR.GetString(configInvalidEncodingValue, objArray));
			}
			return encoding;
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (typeof(string) == destinationType && value is Encoding)
			{
				return ((Encoding)value).HeaderName;
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}