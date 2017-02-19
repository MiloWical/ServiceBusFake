using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus
{
	[ServiceContract(Name="DirectConnectionControl", Namespace="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect", ConfigurationName="servicebus.DirectConnectionControl.2009.05", SessionMode=SessionMode.Required, CallbackContract=typeof(IDirectConnectionControl))]
	internal interface IDirectConnectionControl
	{
		[OperationContract(Name="Abort", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/Abort")]
		void Abort(AbortMessage request);

		[OperationContract(Name="Connect", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/Connect")]
		void Connect(DirectConnectMessage request);

		[OperationContract(Name="ConnectResponse", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/ConnectResponse")]
		void ConnectResponse(ConnectResponseMessage request);

		[OperationContract(Name="ConnectRetry", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/ConnectRetry")]
		void ConnectRetry(ConnectRetryMessage request);

		[OperationContract(Name="SwitchRoles", IsOneWay=true, Action="http://schemas.microsoft.com/netservices/2009/05/servicebus/directconnect/SwitchRoles")]
		void SwitchRoles(SwitchRolesMessage request);
	}
}