using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal interface IDirectConnectionControlChannel : IDirectConnectionControl, IChannel, ICommunicationObject
	{

	}
}