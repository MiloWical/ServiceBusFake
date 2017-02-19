using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal static class AmqpError
	{
		private const int MaxSizeInInfoMap = 32768;

		private static Dictionary<string, Error> errors;

		public static Error InternalError;

		public static Error NotFound;

		public static Error UnauthorizedAccess;

		public static Error DecodeError;

		public static Error ResourceLimitExceeded;

		public static Error NotAllowed;

		public static Error InvalidField;

		public static Error NotImplemented;

		public static Error ResourceLocked;

		public static Error PreconditionFailed;

		public static Error ResourceDeleted;

		public static Error IllegalState;

		public static Error FrameSizeTooSmall;

		public static Error ConnectionForced;

		public static Error FramingError;

		public static Error ConnectionRedirect;

		public static Error WindowViolation;

		public static Error ErrantLink;

		public static Error HandleInUse;

		public static Error UnattachedHandle;

		public static Error DetachForced;

		public static Error TransferLimitExceeded;

		public static Error MessageSizeExceeded;

		public static Error LinkRedirect;

		public static Error Stolen;

		public static Error TransactionUnknownId;

		public static Error TransactionRollback;

		public static Error TransactionTimeout;

		static AmqpError()
		{
			AmqpError.InternalError = new Error()
			{
				Condition = "amqp:internal-error"
			};
			AmqpError.NotFound = new Error()
			{
				Condition = "amqp:not-found"
			};
			AmqpError.UnauthorizedAccess = new Error()
			{
				Condition = "amqp:unauthorized-access"
			};
			AmqpError.DecodeError = new Error()
			{
				Condition = "amqp:decode-error"
			};
			AmqpError.ResourceLimitExceeded = new Error()
			{
				Condition = "amqp:resource-limit-exceeded"
			};
			AmqpError.NotAllowed = new Error()
			{
				Condition = "amqp:not-allowed"
			};
			AmqpError.InvalidField = new Error()
			{
				Condition = "amqp:invalid-field"
			};
			AmqpError.NotImplemented = new Error()
			{
				Condition = "amqp:not-implemented"
			};
			AmqpError.ResourceLocked = new Error()
			{
				Condition = "amqp:resource-locked"
			};
			AmqpError.PreconditionFailed = new Error()
			{
				Condition = "amqp:precondition-failed"
			};
			AmqpError.ResourceDeleted = new Error()
			{
				Condition = "amqp:resource-deleted"
			};
			AmqpError.IllegalState = new Error()
			{
				Condition = "amqp:illegal-state"
			};
			AmqpError.FrameSizeTooSmall = new Error()
			{
				Condition = "amqp:frame-size-too-small"
			};
			AmqpError.ConnectionForced = new Error()
			{
				Condition = "amqp:connection:forced"
			};
			AmqpError.FramingError = new Error()
			{
				Condition = "amqp:connection:framing-error"
			};
			AmqpError.ConnectionRedirect = new Error()
			{
				Condition = "amqp:connection:redirect"
			};
			AmqpError.WindowViolation = new Error()
			{
				Condition = "amqp:session:window-violation"
			};
			AmqpError.ErrantLink = new Error()
			{
				Condition = "amqp:session-errant-link"
			};
			AmqpError.HandleInUse = new Error()
			{
				Condition = "amqp:session:handle-in-use"
			};
			AmqpError.UnattachedHandle = new Error()
			{
				Condition = "amqp:session:unattached-handle"
			};
			AmqpError.DetachForced = new Error()
			{
				Condition = "amqp:link:detach-forced"
			};
			AmqpError.TransferLimitExceeded = new Error()
			{
				Condition = "amqp:link:transfer-limit-exceeded"
			};
			AmqpError.MessageSizeExceeded = new Error()
			{
				Condition = "amqp:link:message-size-exceeded"
			};
			AmqpError.LinkRedirect = new Error()
			{
				Condition = "amqp:link:redirect"
			};
			AmqpError.Stolen = new Error()
			{
				Condition = "amqp:link:stolen"
			};
			AmqpError.TransactionUnknownId = new Error()
			{
				Condition = "amqp:transaction:unknown-id"
			};
			AmqpError.TransactionRollback = new Error()
			{
				Condition = "amqp:transaction:rollback"
			};
			AmqpError.TransactionTimeout = new Error()
			{
				Condition = "amqp:transaction:timeout"
			};
			Dictionary<string, Error> strs = new Dictionary<string, Error>()
			{
				{ AmqpError.InternalError.Condition.Value, AmqpError.InternalError },
				{ AmqpError.NotFound.Condition.Value, AmqpError.NotFound },
				{ AmqpError.UnauthorizedAccess.Condition.Value, AmqpError.UnauthorizedAccess },
				{ AmqpError.DecodeError.Condition.Value, AmqpError.DecodeError },
				{ AmqpError.ResourceLimitExceeded.Condition.Value, AmqpError.ResourceLimitExceeded },
				{ AmqpError.NotAllowed.Condition.Value, AmqpError.NotAllowed },
				{ AmqpError.InvalidField.Condition.Value, AmqpError.InvalidField },
				{ AmqpError.NotImplemented.Condition.Value, AmqpError.NotImplemented },
				{ AmqpError.ResourceLocked.Condition.Value, AmqpError.ResourceLocked },
				{ AmqpError.PreconditionFailed.Condition.Value, AmqpError.PreconditionFailed },
				{ AmqpError.ResourceDeleted.Condition.Value, AmqpError.ResourceDeleted },
				{ AmqpError.IllegalState.Condition.Value, AmqpError.IllegalState },
				{ AmqpError.FrameSizeTooSmall.Condition.Value, AmqpError.FrameSizeTooSmall },
				{ AmqpError.ConnectionForced.Condition.Value, AmqpError.ConnectionForced },
				{ AmqpError.FramingError.Condition.Value, AmqpError.FramingError },
				{ AmqpError.ConnectionRedirect.Condition.Value, AmqpError.ConnectionRedirect },
				{ AmqpError.WindowViolation.Condition.Value, AmqpError.WindowViolation },
				{ AmqpError.ErrantLink.Condition.Value, AmqpError.ErrantLink },
				{ AmqpError.HandleInUse.Condition.Value, AmqpError.HandleInUse },
				{ AmqpError.UnattachedHandle.Condition.Value, AmqpError.UnattachedHandle },
				{ AmqpError.DetachForced.Condition.Value, AmqpError.DetachForced },
				{ AmqpError.TransferLimitExceeded.Condition.Value, AmqpError.TransferLimitExceeded },
				{ AmqpError.MessageSizeExceeded.Condition.Value, AmqpError.MessageSizeExceeded },
				{ AmqpError.LinkRedirect.Condition.Value, AmqpError.LinkRedirect },
				{ AmqpError.Stolen.Condition.Value, AmqpError.Stolen },
				{ AmqpError.TransactionUnknownId.Condition.Value, AmqpError.TransactionUnknownId },
				{ AmqpError.TransactionRollback.Condition.Value, AmqpError.TransactionRollback },
				{ AmqpError.TransactionTimeout.Condition.Value, AmqpError.TransactionTimeout }
			};
			AmqpError.errors = strs;
		}

		public static Error FromException(Exception exception, bool includeDetail = true)
		{
			if (exception is AmqpException)
			{
				return ((AmqpException)exception).Error;
			}
			Error error = new Error();
			if (exception is UnauthorizedAccessException)
			{
				error.Condition = AmqpError.UnauthorizedAccess.Condition;
			}
			else if (exception is InvalidOperationException)
			{
				error.Condition = AmqpError.NotAllowed.Condition;
			}
			else if (exception is TransactionAbortedException)
			{
				error.Condition = AmqpError.TransactionRollback.Condition;
			}
			else if (!(exception is NotImplementedException))
			{
				error.Condition = AmqpError.InternalError.Condition;
			}
			else
			{
				error.Condition = AmqpError.NotImplemented.Condition;
			}
			error.Description = exception.Message;
			if (includeDetail)
			{
				error.Info = new Fields();
				string str = exception.ToString();
				if (str.Length > 32768)
				{
					str = str.Substring(0, 32768);
				}
				error.Info.Add("exception", str);
			}
			return error;
		}

		public static Error GetError(AmqpSymbol condition)
		{
			Error internalError = null;
			if (!AmqpError.errors.TryGetValue(condition.Value, out internalError))
			{
				internalError = AmqpError.InternalError;
			}
			return internalError;
		}
	}
}