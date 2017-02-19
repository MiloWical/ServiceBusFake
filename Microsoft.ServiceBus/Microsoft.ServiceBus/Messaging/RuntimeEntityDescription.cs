using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class RuntimeEntityDescription
	{
		public bool EnableMessagePartitioning
		{
			get;
			set;
		}

		public bool EnableSubscriptionPartitioning
		{
			get;
			set;
		}

		public short PartitionCount
		{
			get;
			set;
		}

		public bool RequiresDuplicateDetection
		{
			get;
			set;
		}

		public RuntimeEntityDescription()
		{
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] enableMessagePartitioning = new object[] { this.EnableMessagePartitioning, this.RequiresDuplicateDetection, this.PartitionCount, this.EnableSubscriptionPartitioning };
			return string.Format(invariantCulture, "EnableMessagePartitioning={0}, RequiresDuplicateDetection={1}, PartitionCount={2}, EnableSubscriptionPartitioning={3}", enableMessagePartitioning);
		}
	}
}