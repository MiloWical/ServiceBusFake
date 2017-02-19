using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerSingletonSizedDecoder : FramingDecoder
	{
		private ViaStringDecoder viaDecoder;

		private ContentTypeStringDecoder contentTypeDecoder;

		private ServerSingletonSizedDecoder.State currentState;

		private string contentType;

		public string ContentType
		{
			get
			{
				if (this.currentState < ServerSingletonSizedDecoder.State.Start)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.contentType;
			}
		}

		public ServerSingletonSizedDecoder.State CurrentState
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

		public Uri Via
		{
			get
			{
				if (this.currentState < ServerSingletonSizedDecoder.State.ReadingContentTypeRecord)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.viaDecoder.ValueAsUri;
			}
		}

		public ServerSingletonSizedDecoder(long streamPosition, int maxViaLength, int maxContentTypeLength) : base(streamPosition)
		{
			this.viaDecoder = new ViaStringDecoder(maxViaLength);
			this.contentTypeDecoder = new ContentTypeStringDecoder(maxContentTypeLength);
			this.currentState = ServerSingletonSizedDecoder.State.ReadingViaRecord;
		}

		public int Decode(byte[] bytes, int offset, int size)
		{
			int num;
			FramingRecordType framingRecordType;
			int num1;
			DecoderHelper.ValidateSize(size);
			try
			{
				switch (this.currentState)
				{
					case ServerSingletonSizedDecoder.State.ReadingViaRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						base.ValidateRecordType(FramingRecordType.Via, framingRecordType);
						num = 1;
						this.viaDecoder.Reset();
						this.currentState = ServerSingletonSizedDecoder.State.ReadingViaString;
						break;
					}
					case ServerSingletonSizedDecoder.State.ReadingViaString:
					{
						num = this.viaDecoder.Decode(bytes, offset, size);
						if (!this.viaDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSingletonSizedDecoder.State.ReadingContentTypeRecord;
						break;
					}
					case ServerSingletonSizedDecoder.State.ReadingContentTypeRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.KnownEncoding)
						{
							base.ValidateRecordType(FramingRecordType.ExtensibleEncoding, framingRecordType);
							num = 1;
							this.contentTypeDecoder.Reset();
							this.currentState = ServerSingletonSizedDecoder.State.ReadingContentTypeString;
							break;
						}
						else
						{
							num = 1;
							this.currentState = ServerSingletonSizedDecoder.State.ReadingContentTypeByte;
							break;
						}
					}
					case ServerSingletonSizedDecoder.State.ReadingContentTypeString:
					{
						num = this.contentTypeDecoder.Decode(bytes, offset, size);
						if (!this.contentTypeDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSingletonSizedDecoder.State.Start;
						this.contentType = this.contentTypeDecoder.Value;
						break;
					}
					case ServerSingletonSizedDecoder.State.ReadingContentTypeByte:
					{
						this.contentType = ContentTypeStringDecoder.GetString((FramingEncodingType)bytes[offset]);
						num = 1;
						this.currentState = ServerSingletonSizedDecoder.State.Start;
						break;
					}
					case ServerSingletonSizedDecoder.State.Start:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingAtEnd, new object[0]))));
					}
					default:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
				}
				ServerSingletonSizedDecoder streamPosition = this;
				streamPosition.StreamPosition = streamPosition.StreamPosition + (long)num;
				num1 = num;
			}
			catch (InvalidDataException invalidDataException)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(invalidDataException));
			}
			return num1;
		}

		public void Reset(long streamPosition)
		{
			base.StreamPosition = streamPosition;
			this.currentState = ServerSingletonSizedDecoder.State.ReadingViaRecord;
		}

		public enum State
		{
			ReadingViaRecord,
			ReadingViaString,
			ReadingContentTypeRecord,
			ReadingContentTypeString,
			ReadingContentTypeByte,
			Start
		}
	}
}