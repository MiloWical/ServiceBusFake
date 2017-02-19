using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Management
{
	public class MetricValue
	{
		public float Average
		{
			get;
			set;
		}

		public long Max
		{
			get;
			set;
		}

		public long Min
		{
			get;
			set;
		}

		[Key]
		public DateTime Timestamp
		{
			get;
			set;
		}

		public long Total
		{
			get;
			set;
		}

		public MetricValue()
		{
		}
	}
}