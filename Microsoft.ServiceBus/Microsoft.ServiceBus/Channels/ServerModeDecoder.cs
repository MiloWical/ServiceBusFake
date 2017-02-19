using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerModeDecoder : FramingDecoder
	{
		private ServerModeDecoder.State currentState;

		private int majorVersion;

		private int minorVersion;

		private FramingMode mode;

		public ServerModeDecoder.State CurrentState
		{
			get
			{
				return this.currentState;
			}
		}

		protected override string CurrentStateAsString
		{
			get
			{
				return this.currentState.ToString();
			}
		}

		public int MajorVersion
		{
			get
			{
				if (this.currentState != ServerModeDecoder.State.Done)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.majorVersion;
			}
		}

		public int MinorVersion
		{
			get
			{
				if (this.currentState != ServerModeDecoder.State.Done)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.minorVersion;
			}
		}

		public FramingMode Mode
		{
			get
			{
				if (this.currentState != ServerModeDecoder.State.Done)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.mode;
			}
		}

		public ServerModeDecoder()
		{
			this.currentState = ServerModeDecoder.State.ReadingVersionRecord;
		}

		public int Decode(byte[] bytes, int offset, int size)
		{
			int num;
			int num1;
			DecoderHelper.ValidateSize(size);
			try
			{
				switch (this.currentState)
				{
					case ServerModeDecoder.State.ReadingVersionRecord:
					{
						base.ValidateRecordType(FramingRecordType.Version, (FramingRecordType)bytes[offset]);
						this.currentState = ServerModeDecoder.State.ReadingMajorVersion;
						num = 1;
						break;
					}
					case ServerModeDecoder.State.ReadingMajorVersion:
					{
						this.majorVersion = bytes[offset];
						base.ValidateMajorVersion(this.majorVersion);
						this.currentState = ServerModeDecoder.State.ReadingMinorVersion;
						num = 1;
						break;
					}
					case ServerModeDecoder.State.ReadingMinorVersion:
					{
						this.minorVersion = bytes[offset];
						this.currentState = ServerModeDecoder.State.ReadingModeRecord;
						num = 1;
						break;
					}
					case ServerModeDecoder.State.ReadingModeRecord:
					{
						base.ValidateRecordType(FramingRecordType.Mode, (FramingRecordType)bytes[offset]);
						this.currentState = ServerModeDecoder.State.ReadingModeValue;
						num = 1;
						break;
					}
					case ServerModeDecoder.State.ReadingModeValue:
					{
						this.mode = (FramingMode)bytes[offset];
						base.ValidateFramingMode(this.mode);
						this.currentState = ServerModeDecoder.State.Done;
						num = 1;
						break;
					}
					default:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
				}
				ServerModeDecoder streamPosition = this;
				streamPosition.StreamPosition = streamPosition.StreamPosition + (long)num;
				num1 = num;
			}
			catch (InvalidDataException invalidDataException)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(invalidDataException));
			}
			return num1;
		}

		public void Reset()
		{
			base.StreamPosition = (long)0;
			this.currentState = ServerModeDecoder.State.ReadingVersionRecord;
		}

		public enum State
		{
			ReadingVersionRecord,
			ReadingMajorVersion,
			ReadingMinorVersion,
			ReadingModeRecord,
			ReadingModeValue,
			Done
		}
	}
}