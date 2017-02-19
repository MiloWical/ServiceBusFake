using System;
using System.Configuration;

namespace Microsoft.ServiceBus.Configuration
{
	internal class TimeSpanOrInfiniteValidator : TimeSpanValidator
	{
		public TimeSpanOrInfiniteValidator(TimeSpan minValue, TimeSpan maxValue) : base(minValue, maxValue)
		{
		}

		public override void Validate(object value)
		{
			if (value.GetType() == typeof(TimeSpan) && (TimeSpan)value == TimeSpan.MaxValue)
			{
				return;
			}
			base.Validate(value);
		}
	}
}