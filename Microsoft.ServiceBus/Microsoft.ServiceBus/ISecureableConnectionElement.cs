namespace Microsoft.ServiceBus
{
	internal interface ISecureableConnectionElement
	{
		SocketSecurityRole SecurityMode
		{
			get;
		}
	}
}