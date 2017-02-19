using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Management
{
	public class MetricRollup
	{
		public TimeSpan Retention
		{
			get;
			set;
		}

		[Key]
		public TimeSpan TimeGrain
		{
			get;
			set;
		}

		public ICollection<MetricValue> Values
		{
			get;
			set;
		}

		public MetricRollup()
		{
		}
	}
}