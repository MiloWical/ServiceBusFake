using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface ICheckpointer
	{
		Task CheckpointAsync();

		Task CheckpointAsync(EventData data);
	}
}