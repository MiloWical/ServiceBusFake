using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus
{
	[Serializable]
	public class AuthorizationFailedException : Exception
	{
		public AuthorizationFailedException.FailureCode ErrorCode
		{
			get;
			private set;
		}

		internal AuthorizationFailedException() : this(AuthorizationFailedException.FailureCode.Generic)
		{
		}

		internal AuthorizationFailedException(string message) : this(AuthorizationFailedException.FailureCode.Generic, message)
		{
		}

		internal AuthorizationFailedException(string message, Exception innerException) : this(AuthorizationFailedException.FailureCode.Generic, message, innerException)
		{
		}

		internal AuthorizationFailedException(AuthorizationFailedException.FailureCode failureCode) : this(failureCode, string.Empty)
		{
			this.ErrorCode = failureCode;
		}

		internal AuthorizationFailedException(AuthorizationFailedException.FailureCode failureCode, string message) : base(AuthorizationFailedException.FormatExceptionMessage(failureCode, message))
		{
			this.ErrorCode = failureCode;
		}

		internal AuthorizationFailedException(AuthorizationFailedException.FailureCode failureCode, string message, Exception innerException) : base(AuthorizationFailedException.FormatExceptionMessage(failureCode, message), innerException)
		{
			this.ErrorCode = failureCode;
		}

		internal AuthorizationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			try
			{
				this.ErrorCode = (AuthorizationFailedException.FailureCode)info.GetValue("ErrorCode", typeof(AuthorizationFailedException.FailureCode));
			}
			catch (SerializationException serializationException)
			{
				this.ErrorCode = AuthorizationFailedException.FailureCode.Generic;
			}
		}

		private static string FormatExceptionMessage(AuthorizationFailedException.FailureCode code, string message)
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { code, message };
			return string.Format(invariantCulture, "{0}: {1}", objArray);
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("ErrorCode", this.ErrorCode);
		}

		public enum FailureCode
		{
			Generic,
			MissingToken,
			MalformedToken,
			InvalidSignature,
			InvalidAudience,
			ExpiredToken,
			InvalidClaim,
			MissingAudience,
			MissingExpiresOn,
			MissingIssuer,
			MissingSignature
		}
	}
}