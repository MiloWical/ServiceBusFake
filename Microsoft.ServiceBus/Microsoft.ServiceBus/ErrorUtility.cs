using System;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class ErrorUtility
	{
		public ErrorUtility()
		{
		}

		public static bool CanRetry(Exception exception)
		{
			if (exception == null)
			{
				return true;
			}
			if (exception is AddressNotFoundException || exception is ConnectionLostException || exception is EndpointNotFoundException || exception is QuotaExceededException || exception is ServerErrorException || exception is TimeoutException || exception is CommunicationException)
			{
				return true;
			}
			return exception is SecurityTokenException;
		}

		public static Exception ConvertToError(MessageFault fault)
		{
			if (fault.Code.Namespace != "http://schemas.microsoft.com/netservices/2009/05/servicebus/relay")
			{
				return FaultException.CreateFault(fault, new Type[0]);
			}
			if (fault.Code.Name == "AddressAlreadyInUseFault")
			{
				return new AddressAlreadyInUseException(string.Concat("The specified address already exists.", fault.Reason));
			}
			if (fault.Code.Name == "AddressNotFoundFault")
			{
				return new AddressNotFoundException("The address was not found. Please check whether the service at the specified name is currently running.");
			}
			if (fault.Code.Name == "AddressReplacedFault")
			{
				return new AddressReplacedException("The address was re-registered by a service at another location. Please check whether another instance of this service is now running.");
			}
			if (fault.Code.Name == "AuthorizationFailedFault")
			{
				return new AuthorizationFailedException(AuthorizationFailedException.FailureCode.Generic, string.Concat("There was an authorization failure. Make sure you have specified the correct SharedSecret, SimpleWebToken, SharedAccessSignature, or Saml transport client credentials. ", fault.Reason));
			}
			if (fault.Code.Name == "EndpointNotFoundFault")
			{
				string str = string.Concat("The endpoint was not found. ", fault.Reason);
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { str, 9350 };
				return new EndpointNotFoundException(string.Format(invariantCulture, "{0}. Please ensure that you can connect to the internet using HTTP port 80 and TCP port {1}.", objArray));
			}
			if (fault.Code.Name == "RelayNotFoundFault")
			{
				return new RelayNotFoundException(fault.Reason.ToString());
			}
			if (fault.Code.Name == "NoTransportSecurityFault")
			{
				return new NoTransportSecurityException(string.Concat("Transport security is required for this connection. ", fault.Reason));
			}
			if (fault.Code.Name == "InvalidRequestFault")
			{
				return new InvalidRequestException(string.Concat("The request was rejected by the server as invalid. ", fault.Reason));
			}
			if (fault.Code.Name == "ConnectionFailedFault")
			{
				return new InvalidRequestException(string.Concat("The connection was failed. ", fault.Reason));
			}
			if (fault.Code.Name == "QuotaExceededFault")
			{
				return new QuotaExceededException(string.Concat("User quota was exceeded. ", fault.Reason));
			}
			return new ServerErrorException(string.Concat("The server had an error while processing request. ", fault.Reason));
		}
	}
}