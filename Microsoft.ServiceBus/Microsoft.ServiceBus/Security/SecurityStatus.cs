using System;

namespace Microsoft.ServiceBus.Security
{
	internal enum SecurityStatus
	{
		OutOfMemory = -2146893056,
		InvalidHandle = -2146893055,
		Unsupported = -2146893054,
		TargetUnknown = -2146893053,
		InternalError = -2146893052,
		PackageNotFound = -2146893051,
		NotOwner = -2146893050,
		CannotInstall = -2146893049,
		InvalidToken = -2146893048,
		LogonDenied = -2146893044,
		UnknownCredential = -2146893043,
		NoCredentials = -2146893042,
		MessageAltered = -2146893041,
		IncompleteMessage = -2146893032,
		IncompleteCred = -2146893024,
		BufferNotEnough = -2146893023,
		WrongPrincipal = -2146893022,
		UntrustedRoot = -2146893019,
		UnknownCertificate = -2146893017,
		OK = 0,
		ContinueNeeded = 590610,
		CompleteNeeded = 590611,
		CompAndContinue = 590612,
		ContextExpired = 590615,
		CredentialsNeeded = 590624,
		Renegotiate = 590625
	}
}