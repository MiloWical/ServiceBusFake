using System;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	public interface ICheckpointManager
	{
		Task CheckpointAsync(Lease lease, string offset, long sequenceNumber);
	}
}