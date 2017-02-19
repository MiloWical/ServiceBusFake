using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class ServerSingletonDecoder : FramingDecoder
	{
		private ViaStringDecoder viaDecoder;

		private ContentTypeStringDecoder contentTypeDecoder;

		private ServerSingletonDecoder.State currentState;

		private string contentType;

		private string upgrade;

		public string ContentType
		{
			get
			{
				if (this.currentState < ServerSingletonDecoder.State.PreUpgradeStart)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.contentType;
			}
		}

		public ServerSingletonDecoder.State CurrentState
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

		public string Upgrade
		{
			get
			{
				if (this.currentState != ServerSingletonDecoder.State.UpgradeRequest)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.upgrade;
			}
		}

		public Uri Via
		{
			get
			{
				if (this.currentState < ServerSingletonDecoder.State.ReadingContentTypeRecord)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.viaDecoder.ValueAsUri;
			}
		}

		public ServerSingletonDecoder(long streamPosition, int maxViaLength, int maxContentTypeLength) : base(streamPosition)
		{
			this.viaDecoder = new ViaStringDecoder(maxViaLength);
			this.contentTypeDecoder = new ContentTypeStringDecoder(maxContentTypeLength);
			this.currentState = ServerSingletonDecoder.State.ReadingViaRecord;
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
					case ServerSingletonDecoder.State.ReadingViaRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						base.ValidateRecordType(FramingRecordType.Via, framingRecordType);
						num = 1;
						this.viaDecoder.Reset();
						this.currentState = ServerSingletonDecoder.State.ReadingViaString;
						break;
					}
					case ServerSingletonDecoder.State.ReadingViaString:
					{
						num = this.viaDecoder.Decode(bytes, offset, size);
						if (!this.viaDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSingletonDecoder.State.ReadingContentTypeRecord;
						break;
					}
					case ServerSingletonDecoder.State.ReadingContentTypeRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.KnownEncoding)
						{
							base.ValidateRecordType(FramingRecordType.ExtensibleEncoding, framingRecordType);
							num = 1;
							this.contentTypeDecoder.Reset();
							this.currentState = ServerSingletonDecoder.State.ReadingContentTypeString;
							break;
						}
						else
						{
							num = 1;
							this.currentState = ServerSingletonDecoder.State.ReadingContentTypeByte;
							break;
						}
					}
					case ServerSingletonDecoder.State.ReadingContentTypeString:
					{
						num = this.contentTypeDecoder.Decode(bytes, offset, size);
						if (!this.contentTypeDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSingletonDecoder.State.PreUpgradeStart;
						this.contentType = this.contentTypeDecoder.Value;
						break;
					}
					case ServerSingletonDecoder.State.ReadingContentTypeByte:
					{
						this.contentType = ContentTypeStringDecoder.GetString((FramingEncodingType)bytes[offset]);
						num = 1;
						this.currentState = ServerSingletonDecoder.State.PreUpgradeStart;
						break;
					}
					case ServerSingletonDecoder.State.PreUpgradeStart:
					{
						num = 0;
						this.currentState = ServerSingletonDecoder.State.ReadingUpgradeRecord;
						break;
					}
					case ServerSingletonDecoder.State.ReadingUpgradeRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						if (framingRecordType != FramingRecordType.UpgradeRequest)
						{
							num = 0;
							this.currentState = ServerSingletonDecoder.State.ReadingPreambleEndRecord;
							break;
						}
						else
						{
							num = 1;
							this.contentTypeDecoder.Reset();
							this.currentState = ServerSingletonDecoder.State.ReadingUpgradeString;
							break;
						}
					}
					case ServerSingletonDecoder.State.ReadingUpgradeString:
					{
						num = this.contentTypeDecoder.Decode(bytes, offset, size);
						if (!this.contentTypeDecoder.IsValueDecoded)
						{
							break;
						}
						this.currentState = ServerSingletonDecoder.State.UpgradeRequest;
						this.upgrade = this.contentTypeDecoder.Value;
						break;
					}
					case ServerSingletonDecoder.State.UpgradeRequest:
					{
						num = 0;
						this.currentState = ServerSingletonDecoder.State.ReadingUpgradeRecord;
						break;
					}
					case ServerSingletonDecoder.State.ReadingPreambleEndRecord:
					{
						framingRecordType = (FramingRecordType)bytes[offset];
						base.ValidateRecordType(FramingRecordType.PreambleEnd, framingRecordType);
						num = 1;
						this.currentState = ServerSingletonDecoder.State.Start;
						break;
					}
					case ServerSingletonDecoder.State.Start:
					{
						num = 0;
						this.currentState = ServerSingletonDecoder.State.ReadingEnvelopeRecord;
						break;
					}
					case ServerSingletonDecoder.State.ReadingEnvelopeRecord:
					{
						base.ValidateRecordType(FramingRecordType.UnsizedEnvelope, (FramingRecordType)bytes[offset]);
						num = 1;
						this.currentState = ServerSingletonDecoder.State.EnvelopeStart;
						break;
					}
					case ServerSingletonDecoder.State.EnvelopeStart:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingAtEnd, new object[0]))));
					}
					default:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
				}
				ServerSingletonDecoder streamPosition = this;
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
			this.currentState = ServerSingletonDecoder.State.ReadingViaRecord;
		}

		public enum State
		{
			ReadingViaRecord,
			ReadingViaString,
			ReadingContentTypeRecord,
			ReadingContentTypeString,
			ReadingContentTypeByte,
			PreUpgradeStart,
			ReadingUpgradeRecord,
			ReadingUpgradeString,
			UpgradeRequest,
			ReadingPreambleEndRecord,
			Start,
			ReadingEnvelopeRecord,
			EnvelopeStart,
			ReadingEnvelopeChunkSize,
			ChunkStart,
			ReadingEnvelopeChunk,
			ChunkEnd,
			End
		}
	}
}