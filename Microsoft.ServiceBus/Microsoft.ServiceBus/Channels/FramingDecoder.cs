using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class FramingDecoder
	{
		private long streamPosition;

		protected abstract string CurrentStateAsString
		{
			get;
		}

		public long StreamPosition
		{
			get
			{
				return this.streamPosition;
			}
			set
			{
				this.streamPosition = value;
			}
		}

		protected FramingDecoder()
		{
		}

		protected FramingDecoder(long streamPosition)
		{
			this.streamPosition = streamPosition;
		}

		protected Exception CreateException(InvalidDataException innerException, string framingFault)
		{
			Exception exception = this.CreateException(innerException);
			FramingEncodingString.AddFaultString(exception, framingFault);
			return exception;
		}

		protected Exception CreateException(InvalidDataException innerException)
		{
			string framingError = Resources.FramingError;
			object[] streamPosition = new object[] { this.StreamPosition, this.CurrentStateAsString };
			return new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingError, streamPosition), innerException);
		}

		private static Exception CreateInvalidRecordTypeException(FramingRecordType expectedType, FramingRecordType foundType)
		{
			string framingRecordTypeMismatch = Resources.FramingRecordTypeMismatch;
			object[] str = new object[] { expectedType.ToString(), foundType.ToString() };
			return new InvalidDataException(Microsoft.ServiceBus.SR.GetString(framingRecordTypeMismatch, str));
		}

		public Exception CreatePrematureEOFException()
		{
			return this.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingPrematureEOF, new object[0])));
		}

		protected void ValidateFramingMode(FramingMode mode)
		{
			switch (mode)
			{
				case FramingMode.Singleton:
				case FramingMode.Duplex:
				case FramingMode.Simplex:
				case FramingMode.SingletonSized:
				{
					return;
				}
				default:
				{
					string framingModeNotSupported = Resources.FramingModeNotSupported;
					object[] str = new object[] { mode.ToString() };
					Exception exception = this.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(framingModeNotSupported, str)), "http://schemas.microsoft.com/ws/2006/05/framing/faults/UnsupportedMode");
					throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(exception);
				}
			}
		}

		protected void ValidateMajorVersion(int majorVersion)
		{
			if (majorVersion != 1)
			{
				string framingVersionNotSupported = Resources.FramingVersionNotSupported;
				object[] objArray = new object[] { majorVersion };
				Exception exception = this.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(framingVersionNotSupported, objArray)), "http://schemas.microsoft.com/ws/2006/05/framing/faults/UnsupportedVersion");
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(exception);
			}
		}

		protected void ValidatePreambleAck(FramingRecordType foundType)
		{
			string str;
			if (foundType != FramingRecordType.PreambleAck)
			{
				Exception exception = FramingDecoder.CreateInvalidRecordTypeException(FramingRecordType.PreambleAck, foundType);
				str = ((byte)foundType == 104 || (byte)foundType == 72 ? Microsoft.ServiceBus.SR.GetString(Resources.PreambleAckIncorrectMaybeHttp, new object[0]) : Microsoft.ServiceBus.SR.GetString(Resources.PreambleAckIncorrect, new object[0]));
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ProtocolException(str, exception));
			}
		}

		protected void ValidateRecordType(FramingRecordType expectedType, FramingRecordType foundType)
		{
			if (foundType != expectedType)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(FramingDecoder.CreateInvalidRecordTypeException(expectedType, foundType));
			}
		}
	}
}