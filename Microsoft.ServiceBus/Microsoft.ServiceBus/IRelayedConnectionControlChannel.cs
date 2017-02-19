using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal interface IRelayedConnectionControlChannel : IRelayedConnectionControl, IChannel, ICommunicationObject
	{

	}
}