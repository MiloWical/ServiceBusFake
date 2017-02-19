using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	public class EventHubRuntimeInformation
	{
		public DateTime CreatedAt
		{
			get;
			set;
		}

		public int PartitionCount
		{
			get;
			set;
		}

		public string[] PartitionIds
		{
			get;
			set;
		}

		public string Path
		{
			get;
			set;
		}

		public EventHubRuntimeInformation()
		{
		}
	}
}