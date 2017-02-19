using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	[ServiceContract(Name="ConnectContract", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect", ConfigurationName="servicebus.ConnectContract.2009.05", SessionMode=SessionMode.Required, CallbackContract=typeof(IRelayedOnewayCallback))]
	internal interface IRelayedOneway
	{
		[OperationContract(Name="Send", IsOneWay=true, AsyncPattern=true, Action="*", IsInitiating=false)]
		IAsyncResult BeginSend(Message message, AsyncCallback callback, object state);

		[OperationContract(Name="Listen", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/Listen", IsInitiating=true, AsyncPattern=true)]
		IAsyncResult BeginSubscribe(Message message, AsyncCallback callback, object state);

		[OperationContract(Name="Connect", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/Connect", IsInitiating=true)]
		void Connect(Message message);

		void EndSend(IAsyncResult result);

		void EndSubscribe(IAsyncResult result);

		[OperationContract(Name="OnewayPing", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/connect/OnewayPing", IsInitiating=true)]
		void OnewayPing(Message message);
	}
}