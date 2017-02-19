using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.ServiceBus
{
	internal class TimeSpanOrInfiniteConverter : TimeSpanConverter
	{
		public TimeSpanOrInfiniteConverter()
		{
		}

		public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
		{
			if (string.Equals((string)data, "infinite", StringComparison.OrdinalIgnoreCase))
			{
				return TimeSpan.MaxValue;
			}
			return base.ConvertFrom(ctx, ci, data);
		}

		public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
		{
			if (value == null)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
			}
			if (!(value is TimeSpan))
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string sFxWrongType2 = Resources.SFxWrongType2;
				object[] objArray = new object[] { typeof(TimeSpan), value.GetType() };
				throw exceptionUtility.ThrowHelperArgument("value", Microsoft.ServiceBus.SR.GetString(sFxWrongType2, objArray));
			}
			if ((TimeSpan)value == TimeSpan.MaxValue)
			{
				return "Infinite";
			}
			return base.ConvertTo(ctx, ci, value, type);
		}
	}
}