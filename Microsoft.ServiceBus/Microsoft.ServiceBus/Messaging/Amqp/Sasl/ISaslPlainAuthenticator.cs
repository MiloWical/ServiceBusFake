using System;
using System.Security.Principal;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal interface ISaslPlainAuthenticator
	{
		IPrincipal Authenticate(string identity, string password);
	}
}