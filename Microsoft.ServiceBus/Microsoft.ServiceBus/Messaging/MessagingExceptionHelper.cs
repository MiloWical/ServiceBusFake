using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class MessagingExceptionHelper
	{
		private readonly static Dictionary<string, HttpStatusCode> ErrorCodes;

		static MessagingExceptionHelper()
		{
			Dictionary<string, HttpStatusCode> strs = new Dictionary<string, HttpStatusCode>()
			{
				{ typeof(TimeoutException).UnderlyingSystemType.Name, HttpStatusCode.RequestTimeout },
				{ MessagingExceptionDetail.ErrorLevelType.ServerError.ToString(), HttpStatusCode.InternalServerError },
				{ MessagingExceptionDetail.ErrorLevelType.UserError.ToString(), HttpStatusCode.Unauthorized },
				{ typeof(ArgumentException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(ArgumentOutOfRangeException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(MessageLockLostException).UnderlyingSystemType.Name, HttpStatusCode.Gone },
				{ typeof(SessionLockLostException).UnderlyingSystemType.Name, HttpStatusCode.Gone },
				{ typeof(MessageNotFoundException).UnderlyingSystemType.Name, HttpStatusCode.NotFound },
				{ typeof(MessageSizeExceededException).UnderlyingSystemType.Name, HttpStatusCode.RequestEntityTooLarge },
				{ typeof(MessagingEntityNotFoundException).UnderlyingSystemType.Name, HttpStatusCode.NotFound },
				{ typeof(MessagingEntityAlreadyExistsException).UnderlyingSystemType.Name, HttpStatusCode.Conflict },
				{ typeof(MessageStoreLockLostException).UnderlyingSystemType.Name, HttpStatusCode.InternalServerError },
				{ typeof(UnauthorizedAccessException).UnderlyingSystemType.Name, HttpStatusCode.Unauthorized },
				{ typeof(TransactionSizeExceededException).UnderlyingSystemType.Name, HttpStatusCode.RequestEntityTooLarge },
				{ typeof(Microsoft.ServiceBus.Messaging.QuotaExceededException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(RequestQuotaExceededException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(RuleActionException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(FilterException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(SessionCannotBeLockedException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(PartitionNotOwnedException).UnderlyingSystemType.Name, HttpStatusCode.InternalServerError },
				{ typeof(ServerBusyException).UnderlyingSystemType.Name, HttpStatusCode.ServiceUnavailable },
				{ typeof(InvalidOperationException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(EndpointNotFoundException).UnderlyingSystemType.Name, HttpStatusCode.InternalServerError },
				{ typeof(MessagingEntityDisabledException).UnderlyingSystemType.Name, HttpStatusCode.Forbidden },
				{ typeof(NoMatchingSubscriptionException).UnderlyingSystemType.Name, HttpStatusCode.NotFound },
				{ typeof(MessagingEntityMovedException).UnderlyingSystemType.Name, HttpStatusCode.InternalServerError },
				{ typeof(MessagingException).UnderlyingSystemType.Name, HttpStatusCode.InternalServerError },
				{ typeof(MessagingCommunicationException).UnderlyingSystemType.Name, HttpStatusCode.InternalServerError },
				{ typeof(InternalServerErrorException).UnderlyingSystemType.Name, HttpStatusCode.InternalServerError },
				{ typeof(InvalidLinkTypeException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(SoapActionNotSupportedException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest },
				{ typeof(TransactionMessagingException).UnderlyingSystemType.Name, HttpStatusCode.BadRequest }
			};
			MessagingExceptionHelper.ErrorCodes = strs;
		}

		private static Exception ConvertExceptionFromDetail(string type, FaultException exceptionDetailFaultException)
		{
			if (string.Equals(type, typeof(TimeoutException).FullName, StringComparison.Ordinal))
			{
				return new TimeoutException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(ArgumentException).FullName, StringComparison.Ordinal))
			{
				return new ArgumentException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(ArgumentOutOfRangeException).FullName, StringComparison.Ordinal))
			{
				return new ArgumentOutOfRangeException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessageLockLostException).FullName, StringComparison.Ordinal))
			{
				return new MessageLockLostException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(SessionLockLostException).FullName, StringComparison.Ordinal))
			{
				return new SessionLockLostException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessageNotFoundException).FullName, StringComparison.Ordinal))
			{
				return new MessageNotFoundException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessagingEntityNotFoundException).FullName, StringComparison.Ordinal))
			{
				return new MessagingEntityNotFoundException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessagingEntityAlreadyExistsException).FullName, StringComparison.Ordinal))
			{
				return new MessagingEntityAlreadyExistsException(exceptionDetailFaultException.Message, null, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessageStoreLockLostException).FullName, StringComparison.Ordinal))
			{
				return new MessageStoreLockLostException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(UnauthorizedAccessException).FullName, StringComparison.Ordinal))
			{
				return new UnauthorizedAccessException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(TransactionSizeExceededException).FullName, StringComparison.Ordinal))
			{
				return new TransactionSizeExceededException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(Microsoft.ServiceBus.Messaging.QuotaExceededException).FullName, StringComparison.Ordinal))
			{
				return new Microsoft.ServiceBus.Messaging.QuotaExceededException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(RuleActionException).FullName, StringComparison.Ordinal))
			{
				return new RuleActionException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(FilterException).FullName, StringComparison.Ordinal))
			{
				return new FilterException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(SessionCannotBeLockedException).FullName, StringComparison.Ordinal))
			{
				return new SessionCannotBeLockedException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(PartitionNotOwnedException).FullName, StringComparison.Ordinal))
			{
				return new PartitionNotOwnedException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(ServerBusyException).FullName, StringComparison.Ordinal))
			{
				return new ServerBusyException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(InvalidOperationException).FullName, StringComparison.Ordinal))
			{
				return new InvalidOperationException(exceptionDetailFaultException.Message.Replace("BR0012", string.Empty), exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(EndpointNotFoundException).FullName, StringComparison.Ordinal))
			{
				return new MessagingException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessagingEntityDisabledException).FullName, StringComparison.Ordinal))
			{
				return new MessagingEntityDisabledException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(NoMatchingSubscriptionException).FullName, StringComparison.Ordinal))
			{
				return new NoMatchingSubscriptionException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessagingEntityMovedException).FullName, StringComparison.Ordinal))
			{
				return new MessagingEntityMovedException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessagingCommunicationException).FullName, StringComparison.Ordinal))
			{
				return new MessagingCommunicationException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(MessageSizeExceededException).FullName, StringComparison.Ordinal))
			{
				return new MessageSizeExceededException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(InternalServerErrorException).FullName, StringComparison.Ordinal))
			{
				return new MessagingException(exceptionDetailFaultException.Message, false, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(InvalidLinkTypeException).FullName, StringComparison.Ordinal))
			{
				return new MessagingException(exceptionDetailFaultException.Message, false, exceptionDetailFaultException);
			}
			if (string.Equals(type, typeof(SoapActionNotSupportedException).FullName, StringComparison.Ordinal))
			{
				return new MessagingException(exceptionDetailFaultException.Message, false, exceptionDetailFaultException);
			}
			if (!string.Equals(type, typeof(TransactionMessagingException).FullName, StringComparison.Ordinal))
			{
				return new MessagingException(exceptionDetailFaultException.Message, exceptionDetailFaultException);
			}
			return new MessagingException(exceptionDetailFaultException.Message, false, exceptionDetailFaultException);
		}

		private static Exception ConvertExceptionFromStatusCode(FaultException faultException)
		{
			if (faultException.Code.SubCode == null)
			{
				return new MessagingException(faultException.Message, faultException);
			}
			if (faultException.Code.SubCode.Name.Equals("ConnectionFailedFault", StringComparison.OrdinalIgnoreCase))
			{
				return new TimeoutException(faultException.Message, faultException);
			}
			if (faultException.Code.SubCode.Name.Equals("EndpointNotFoundFault", StringComparison.OrdinalIgnoreCase))
			{
				return new MessagingEntityNotFoundException(faultException.Message, faultException);
			}
			if (faultException.Code.SubCode.Name.Equals("AuthorizationFailedFault", StringComparison.OrdinalIgnoreCase) || faultException.Code.SubCode.Name.Equals("NoTransportSecurityFault", StringComparison.OrdinalIgnoreCase))
			{
				return new UnauthorizedAccessException(faultException.Message, faultException);
			}
			if (faultException.Code.SubCode.Name.Equals("QuotaExceededFault", StringComparison.OrdinalIgnoreCase))
			{
				return new Microsoft.ServiceBus.Messaging.QuotaExceededException(faultException.Message, faultException);
			}
			if (faultException.Code.SubCode.Name.Equals("PartitionNotOwnedException", StringComparison.OrdinalIgnoreCase))
			{
				return new PartitionNotOwnedException(faultException.Message, faultException);
			}
			return new MessagingException(faultException.Message, faultException);
		}

		public static HttpStatusCode ConvertStatusCodeFromDetail(string type)
		{
			HttpStatusCode httpStatusCode;
			if (MessagingExceptionHelper.ErrorCodes.TryGetValue(type, out httpStatusCode))
			{
				return httpStatusCode;
			}
			MessagingClientEtwProvider.Provider.EventWriteUnexpectedExceptionTelemetry(type);
			return HttpStatusCode.InternalServerError;
		}

		public static CommunicationException ConvertToCommunicationException(MessagingException exception)
		{
			bool flag;
			return MessagingExceptionHelper.ConvertToCommunicationException(exception, out flag);
		}

		public static CommunicationException ConvertToCommunicationException(MessagingException exception, out bool shouldFault)
		{
			shouldFault = false;
			if (exception is MessagingEntityNotFoundException)
			{
				shouldFault = true;
				return new EndpointNotFoundException(exception.Message, exception);
			}
			if (exception is MessagingCommunicationException)
			{
				EndpointNotFoundException innerException = exception.InnerException as EndpointNotFoundException;
				if (innerException != null)
				{
					shouldFault = true;
					return innerException;
				}
			}
			CommunicationException communicationException = exception.InnerException as CommunicationException ?? new CommunicationException(exception.Message, exception);
			return communicationException;
		}

		public static bool IsWrappedExceptionTransient(this Exception exception)
		{
			bool isTransient;
			CommunicationException communicationException = exception as CommunicationException;
			if (communicationException == null)
			{
				isTransient = false;
			}
			else
			{
				Exception exception1 = MessagingExceptionHelper.Unwrap(communicationException);
				MessagingException messagingException = exception1 as MessagingException;
				MessagingException messagingException1 = messagingException;
				if (messagingException == null)
				{
					isTransient = (!(exception1 is TimeoutException) ? false : true);
				}
				else
				{
					isTransient = messagingException1.IsTransient;
				}
			}
			return isTransient;
		}

		public static Exception Unwrap(CommunicationException exception, bool isCancelling)
		{
			if (!isCancelling)
			{
				return MessagingExceptionHelper.Unwrap(exception);
			}
			if (exception is CommunicationObjectAbortedException)
			{
				return new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
			}
			return new OperationCanceledException(SRClient.EntityClosedOrAborted, MessagingExceptionHelper.Unwrap(exception));
		}

		public static Exception Unwrap(CommunicationException exception)
		{
			if (exception == null)
			{
				return null;
			}
			FaultException faultException = exception as FaultException;
			if (faultException != null)
			{
				return MessagingExceptionHelper.Unwrap(faultException);
			}
			EndpointNotFoundException endpointNotFoundException = exception as EndpointNotFoundException;
			if (endpointNotFoundException != null)
			{
				return new MessagingCommunicationException(endpointNotFoundException.Message, endpointNotFoundException);
			}
			if (exception is CommunicationObjectAbortedException)
			{
				return new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
			}
			if (exception is CommunicationObjectFaultedException)
			{
				return new MessagingCommunicationException(SRClient.MessagingCommunicationError, exception);
			}
			return new MessagingCommunicationException(exception.Message, exception);
		}

		private static Exception Unwrap(FaultException faultException)
		{
			FaultException<ExceptionDetailNoStackTrace> faultException1 = faultException as FaultException<ExceptionDetailNoStackTrace>;
			if (faultException1 != null && faultException1.Detail != null)
			{
				return MessagingExceptionHelper.ConvertExceptionFromDetail(faultException1.Detail.Type, faultException1);
			}
			FaultException<ExceptionDetail> faultException2 = faultException as FaultException<ExceptionDetail>;
			if (faultException2 == null || faultException2.Detail == null)
			{
				return MessagingExceptionHelper.ConvertExceptionFromStatusCode(faultException);
			}
			return MessagingExceptionHelper.ConvertExceptionFromDetail(faultException2.Detail.Type, faultException2);
		}
	}
}