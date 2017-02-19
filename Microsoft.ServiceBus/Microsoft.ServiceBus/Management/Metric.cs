using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Management
{
	public class Metric
	{
		public string DisplayName
		{
			get;
			set;
		}

		internal string MetricKeyName
		{
			get;
			set;
		}

		[Key]
		public string Name
		{
			get;
			set;
		}

		public string PrimaryAggregation
		{
			get;
			set;
		}

		public ICollection<MetricRollup> Rollups
		{
			get;
			set;
		}

		public string Unit
		{
			get;
			set;
		}

		public Metric()
		{
		}
	}
}