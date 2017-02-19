namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface IAmqpProvider : IRuntimeProvider, IConnectionFactory, ISessionFactory, ILinkFactory, INodeFactory
	{

	}
}