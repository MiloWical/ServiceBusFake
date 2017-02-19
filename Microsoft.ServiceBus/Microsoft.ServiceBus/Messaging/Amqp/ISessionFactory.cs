namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal interface ISessionFactory
	{
		AmqpSession CreateSession(AmqpConnection connection, AmqpSessionSettings settings);
	}
}