using Microsoft.ServiceBus;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public sealed class MessagingExceptionDetail
	{
		public int ErrorCode
		{
			get;
			private set;
		}

		public MessagingExceptionDetail.ErrorLevelType ErrorLevel
		{
			get;
			private set;
		}

		public string Message
		{
			get;
			private set;
		}

		private MessagingExceptionDetail(int errorCode, string message) : this(errorCode, message, MessagingExceptionDetail.ErrorLevelType.UserError)
		{
		}

		private MessagingExceptionDetail(int errorCode, string message, MessagingExceptionDetail.ErrorLevelType errorLevel)
		{
			this.ErrorCode = errorCode;
			this.Message = message;
			this.ErrorLevel = errorLevel;
		}

		public static MessagingExceptionDetail CorrelationFiltersExceeded(string message)
		{
			return new MessagingExceptionDetail(40502, message);
		}

		public static MessagingExceptionDetail EntityConflict(string message)
		{
			return new MessagingExceptionDetail(40900, message);
		}

		public static MessagingExceptionDetail EntityConflictOperationInProgress(string entityName)
		{
			return new MessagingExceptionDetail(40901, SRClient.MessagingEntityRequestConflict(entityName));
		}

		public static MessagingExceptionDetail EntityGone(string message)
		{
			return new MessagingExceptionDetail(41000, message);
		}

		public static MessagingExceptionDetail EntityNotFound(string message)
		{
			return new MessagingExceptionDetail(40400, message);
		}

		public static MessagingExceptionDetail EntityUpdateConflict(string entityName)
		{
			return new MessagingExceptionDetail(40504, SRClient.MessagingEntityUpdateConflict(entityName));
		}

		public static MessagingExceptionDetail EventHubAtFullCapacity(string message)
		{
			return new MessagingExceptionDetail(40505, message, MessagingExceptionDetail.ErrorLevelType.ServerError);
		}

		public static MessagingExceptionDetail ReconstructExceptionDetail(int errorCode, string message, MessagingExceptionDetail.ErrorLevelType errorLevel)
		{
			return new MessagingExceptionDetail(errorCode, message, errorLevel);
		}

		public static MessagingExceptionDetail ServerBusy(string message)
		{
			return new MessagingExceptionDetail(50004, message, MessagingExceptionDetail.ErrorLevelType.ServerError);
		}

		public static MessagingExceptionDetail SqlFiltersExceeded(string message)
		{
			return new MessagingExceptionDetail(40501, message);
		}

		public static MessagingExceptionDetail StoreLockLost(string message)
		{
			return new MessagingExceptionDetail(40500, message);
		}

		public static MessagingExceptionDetail SubscriptionsExceeded(string message)
		{
			return new MessagingExceptionDetail(40503, message);
		}

		public static MessagingExceptionDetail UnknownDetail(string message)
		{
			return new MessagingExceptionDetail(60000, message);
		}

		public static MessagingExceptionDetail UnspecifiedInternalError(string message)
		{
			return new MessagingExceptionDetail(50000, message, MessagingExceptionDetail.ErrorLevelType.ServerError);
		}

		public enum ErrorLevelType
		{
			UserError,
			ServerError
		}
	}
}