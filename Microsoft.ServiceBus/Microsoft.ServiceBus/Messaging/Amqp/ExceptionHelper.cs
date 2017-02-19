using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class ExceptionHelper
	{
		private const int MaxSizeInInfoMap = 32768;

		public static bool IncludeExceptionDetails;

		static ExceptionHelper()
		{
			ExceptionHelper.IncludeExceptionDetails = true;
		}

		public static Exception GetClientException(Exception exception, string trackingId)
		{
			if (!(exception is OperationCanceledException) && !(exception is SocketException) && !(exception is AmqpException))
			{
				return exception;
			}
			string message = exception.Message;
			if (trackingId != null)
			{
				message = string.Concat(message, SRClient.TrackingIdAndTimestampFormat(trackingId, DateTime.UtcNow));
			}
			return new MessagingException(message, exception);
		}

		private static string GetExceptionMessage(Exception exception, out bool hasBrokerInvalidOperationPrefix)
		{
			hasBrokerInvalidOperationPrefix = false;
			string message = exception.Message;
			if ((exception is InvalidOperationException || exception is MessagingException || exception is ArgumentException || exception is ArgumentOutOfRangeException || exception is NoMatchingSubscriptionException) && !string.IsNullOrEmpty(message) && message.StartsWith("BR0012", StringComparison.OrdinalIgnoreCase))
			{
				message = message.Remove(0, "BR0012".Length);
				hasBrokerInvalidOperationPrefix = true;
			}
			return message;
		}

		public static string GetTrackingId(this AmqpLink link)
		{
			string str = null;
			if (link.Settings.Properties != null && link.Settings.Properties.TryGetValue<string>(ClientConstants.TrackingIdName, out str))
			{
				return str;
			}
			return null;
		}

		public static bool IsAmqpError(Exception exception, AmqpSymbol errorCode)
		{
			AmqpException amqpException = exception as AmqpException;
			if (amqpException != null && amqpException.Error.Condition.Equals(errorCode))
			{
				return true;
			}
			return false;
		}

		public static Error ToAmqpError(Exception exception)
		{
			bool flag;
			if (exception == null)
			{
				return null;
			}
			Error error = new Error()
			{
				Description = ExceptionHelper.GetExceptionMessage(exception, out flag)
			};
			if (exception is AmqpException)
			{
				AmqpException amqpException = (AmqpException)exception;
				error.Condition = amqpException.Error.Condition;
				error.Info = amqpException.Error.Info;
			}
			else if (exception is UnauthorizedAccessException)
			{
				error.Condition = AmqpError.UnauthorizedAccess.Condition;
			}
			else if (exception is TransactionAbortedException)
			{
				error.Condition = AmqpError.TransactionRollback.Condition;
			}
			else if (exception is NotSupportedException)
			{
				error.Condition = AmqpError.NotImplemented.Condition;
			}
			else if (exception is MessagingEntityNotFoundException)
			{
				error.Condition = AmqpError.NotFound.Condition;
			}
			else if (exception is MessagingEntityAlreadyExistsException)
			{
				error.Condition = ClientConstants.EntityAlreadyExistsError;
			}
			else if (exception is AddressAlreadyInUseException)
			{
				error.Condition = ClientConstants.AddressAlreadyInUseError;
			}
			else if (exception is AuthorizationFailedException)
			{
				error.Condition = ClientConstants.AuthorizationFailedError;
			}
			else if (exception is MessageLockLostException)
			{
				error.Condition = ClientConstants.MessageLockLostError;
			}
			else if (exception is SessionLockLostException)
			{
				error.Condition = ClientConstants.SessionLockLostError;
			}
			else if (exception is Microsoft.ServiceBus.Messaging.QuotaExceededException || exception is System.ServiceModel.QuotaExceededException)
			{
				error.Condition = AmqpError.ResourceLimitExceeded.Condition;
			}
			else if (exception is TimeoutException)
			{
				error.Condition = ClientConstants.TimeoutError;
			}
			else if (exception is NoMatchingSubscriptionException)
			{
				error.Condition = ClientConstants.NoMatchingSubscriptionError;
			}
			else if (exception is ServerBusyException)
			{
				error.Condition = ClientConstants.ServerBusyError;
			}
			else if (exception is MessageStoreLockLostException)
			{
				error.Condition = ClientConstants.StoreLockLostError;
			}
			else if (exception is SessionCannotBeLockedException)
			{
				error.Condition = ClientConstants.SessionCannotBeLockedError;
			}
			else if (exception is PartitionNotOwnedException)
			{
				error.Condition = ClientConstants.PartitionNotOwnedError;
			}
			else if (exception is MessagingEntityDisabledException)
			{
				error.Condition = ClientConstants.EntityDisabledError;
			}
			else if (exception is OperationCanceledException)
			{
				error.Condition = ClientConstants.OperationCancelledError;
			}
			else if (exception is RelayNotFoundException)
			{
				error.Condition = ClientConstants.RelayNotFoundError;
			}
			else if (exception is MessageSizeExceededException)
			{
				error.Condition = AmqpError.MessageSizeExceeded.Condition;
			}
			else if (!(exception is ReceiverDisconnectedException))
			{
				error.Condition = AmqpError.InternalError.Condition;
				if (flag)
				{
					if (exception is InvalidOperationException)
					{
						error.Condition = AmqpError.NotAllowed.Condition;
					}
					else if (exception is ArgumentOutOfRangeException)
					{
						error.Condition = ClientConstants.ArgumentOutOfRangeError;
					}
					else if (exception is ArgumentException)
					{
						error.Condition = ClientConstants.ArgumentError;
					}
				}
				error.Description = (ExceptionHelper.IncludeExceptionDetails || flag ? error.Description : SRClient.InternalServerError);
			}
			else
			{
				error.Condition = AmqpError.Stolen.Condition;
			}
			if (ExceptionHelper.IncludeExceptionDetails)
			{
				string stackTrace = exception.StackTrace;
				if (stackTrace != null)
				{
					if (stackTrace.Length > 32768)
					{
						stackTrace = stackTrace.Substring(0, 32768);
					}
					if (error.Info == null)
					{
						error.Info = new Fields();
					}
					error.Info.Add(ClientConstants.StackTraceName, stackTrace);
				}
			}
			return error;
		}

		public static Exception ToCommunicationContract(Exception exception)
		{
			Exception communicationContract = null;
			AmqpException amqpException = exception as AmqpException;
			AmqpException amqpException1 = amqpException;
			if (amqpException != null)
			{
				communicationContract = ExceptionHelper.ToCommunicationContract(amqpException1.Error, amqpException1);
			}
			else if (exception is CommunicationException || exception is TimeoutException)
			{
				communicationContract = exception;
			}
			else
			{
				SocketException socketException = exception as SocketException;
				SocketException socketException1 = socketException;
				if (socketException != null)
				{
					if (socketException1.SocketErrorCode == SocketError.ConnectionRefused || socketException1.SocketErrorCode == SocketError.HostDown || socketException1.SocketErrorCode == SocketError.HostNotFound || socketException1.SocketErrorCode == SocketError.HostUnreachable)
					{
						communicationContract = new EndpointNotFoundException(socketException1.Message, socketException1);
					}
					else if (socketException1.SocketErrorCode == SocketError.TimedOut)
					{
						communicationContract = new TimeoutException(socketException1.Message, socketException1);
					}
				}
			}
			if (communicationContract == null)
			{
				communicationContract = new CommunicationException(exception.Message, exception);
			}
			return communicationContract;
		}

		public static Exception ToCommunicationContract(Error error, Exception innerException)
		{
			if (error == null)
			{
				return new CommunicationException("Unknown error.", innerException);
			}
			string description = error.Description;
			if (error.Condition.Equals(ClientConstants.TimeoutError))
			{
				return new TimeoutException(description, innerException);
			}
			if (error.Condition.Equals(AmqpError.NotFound.Condition))
			{
				return new EndpointNotFoundException(description, innerException);
			}
			if (error.Condition.Equals(AmqpError.ResourceLimitExceeded.Condition))
			{
				return new System.ServiceModel.QuotaExceededException(description, innerException);
			}
			if (error.Condition.Equals(ClientConstants.AddressAlreadyInUseError))
			{
				return new AddressAlreadyInUseException(description);
			}
			return new CommunicationException(description, innerException);
		}

		public static Exception ToMessagingContract(Exception exception, string trackingId)
		{
			AmqpException amqpException = exception as AmqpException;
			if (amqpException != null)
			{
				return ExceptionHelper.ToMessagingContract(amqpException.Error);
			}
			return ExceptionHelper.GetClientException(exception, trackingId);
		}

		public static Exception ToMessagingContract(Error error)
		{
			if (error == null)
			{
				return new MessagingException("Unknown error.");
			}
			string description = error.Description;
			if (error.Condition.Equals(ClientConstants.TimeoutError))
			{
				return new TimeoutException(description);
			}
			if (error.Condition.Equals(AmqpError.NotFound.Condition))
			{
				return new MessagingEntityNotFoundException(description, null);
			}
			if (error.Condition.Equals(AmqpError.NotImplemented.Condition))
			{
				return new NotSupportedException(description);
			}
			if (error.Condition.Equals(ClientConstants.EntityAlreadyExistsError))
			{
				return new MessagingEntityAlreadyExistsException(description, null, null);
			}
			if (error.Condition.Equals(ClientConstants.MessageLockLostError))
			{
				return new MessageLockLostException(description);
			}
			if (error.Condition.Equals(ClientConstants.SessionLockLostError))
			{
				return new SessionLockLostException(description);
			}
			if (error.Condition.Equals(AmqpError.ResourceLimitExceeded.Condition))
			{
				return new Microsoft.ServiceBus.Messaging.QuotaExceededException(description);
			}
			if (error.Condition.Equals(ClientConstants.NoMatchingSubscriptionError))
			{
				return new NoMatchingSubscriptionException(description);
			}
			if (error.Condition.Equals(AmqpError.NotAllowed.Condition))
			{
				return new InvalidOperationException(description);
			}
			if (error.Condition.Equals(AmqpError.UnauthorizedAccess.Condition))
			{
				return new UnauthorizedAccessException(description);
			}
			if (error.Condition.Equals(AmqpError.MessageSizeExceeded.Condition))
			{
				return new MessageSizeExceededException(description);
			}
			if (error.Condition.Equals(ClientConstants.ServerBusyError))
			{
				return new ServerBusyException(description);
			}
			if (error.Condition.Equals(ClientConstants.ArgumentError))
			{
				return new ArgumentException(description);
			}
			if (error.Condition.Equals(ClientConstants.ArgumentOutOfRangeError))
			{
				return new ArgumentOutOfRangeException(description);
			}
			if (error.Condition.Equals(ClientConstants.StoreLockLostError))
			{
				return new MessageStoreLockLostException(description);
			}
			if (error.Condition.Equals(ClientConstants.SessionCannotBeLockedError))
			{
				return new SessionCannotBeLockedException(description);
			}
			if (error.Condition.Equals(ClientConstants.PartitionNotOwnedError))
			{
				return new PartitionNotOwnedException(description);
			}
			if (error.Condition.Equals(ClientConstants.EntityDisabledError))
			{
				return new MessagingEntityDisabledException(description, null);
			}
			if (error.Condition.Equals(AmqpError.Stolen.Condition))
			{
				return new ReceiverDisconnectedException(description);
			}
			return new MessagingException(description);
		}

		public static Exception ToRelayContract(Exception exception)
		{
			AmqpException amqpException = exception as AmqpException;
			if (amqpException != null && amqpException.Error != null)
			{
				if (amqpException.Error.Condition.Equals(ClientConstants.RelayNotFoundError))
				{
					return new RelayNotFoundException(exception.Message, exception);
				}
				if (amqpException.Error.Condition.Equals(ClientConstants.AuthorizationFailedError))
				{
					return new AuthorizationFailedException(exception.Message, exception);
				}
			}
			return ExceptionHelper.ToCommunicationContract(exception);
		}
	}
}