using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	[ServiceContract(Name="RelayedConnectionControl", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect", ConfigurationName="servicebus.RelayedConnectionControl.2009.05", SessionMode=SessionMode.NotAllowed)]
	internal interface IRelayedConnectionControl
	{
		[OperationContract(Name="RelayedConnect", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/relayedconnect/RelayedConnect", AsyncPattern=true)]
		IAsyncResult BeginConnect(RelayedConnectMessage request, AsyncCallback callback, object state);

		void EndConnect(IAsyncResult result);
	}
}