using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal interface IRelayedOnewayCallback
	{
		[OperationContract(Name="Send", IsOneWay=true, Action="*", AsyncPattern=true)]
		IAsyncResult BeginSend(Message message, AsyncCallback callback, object state);

		void EndSend(IAsyncResult result);
	}
}