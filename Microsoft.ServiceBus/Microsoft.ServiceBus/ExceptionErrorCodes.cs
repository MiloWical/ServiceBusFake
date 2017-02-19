using System;

namespace Microsoft.ServiceBus
{
	public enum ExceptionErrorCodes
	{
		BadRequest = 40000,
		UnauthorizedGeneric = 40100,
		NoTransportSecurity = 40101,
		MissingToken = 40102,
		InvalidSignature = 40103,
		InvalidAudience = 40104,
		MalformedToken = 40105,
		ExpiredToken = 40106,
		AudienceNotFound = 40107,
		ExpiresOnNotFound = 40108,
		IssuerNotFound = 40109,
		SignatureNotFound = 40110,
		ForbiddenGeneric = 40300,
		EndpointNotFound = 40400,
		InvalidDestination = 40401,
		NamespaceNotFound = 40402,
		StoreLockLost = 40500,
		SqlFiltersExceeded = 40501,
		CorrelationFiltersExceeded = 40502,
		SubscriptionsExceeded = 40503,
		UpdateConflict = 40504,
		EventHubAtFullCapacity = 40505,
		ConflictGeneric = 40900,
		ConflictOperationInProgress = 40901,
		EntityGone = 41000,
		UnspecifiedInternalError = 50000,
		DataCommunicationError = 50001,
		InternalFailure = 50002,
		ProviderUnreachable = 50003,
		ServerBusy = 50004,
		BadGatewayFailure = 50200,
		GatewayTimeoutFailure = 50400,
		UnknownExceptionDetail = 60000
	}
}