using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class SingletonMessageDecoder : FramingDecoder
	{
		private IntDecoder sizeDecoder;

		private int chunkBytesNeeded;

		private int chunkSize;

		private SingletonMessageDecoder.State currentState;

		public int ChunkSize
		{
			get
			{
				if (this.currentState < SingletonMessageDecoder.State.ChunkStart)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.chunkSize;
			}
		}

		public SingletonMessageDecoder.State CurrentState
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

		public SingletonMessageDecoder(long streamPosition) : base(streamPosition)
		{
			this.sizeDecoder = new IntDecoder();
			this.currentState = SingletonMessageDecoder.State.ChunkStart;
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
					case SingletonMessageDecoder.State.ReadingEnvelopeChunkSize:
					{
						num = this.sizeDecoder.Decode(bytes, offset, size);
						if (!this.sizeDecoder.IsValueDecoded)
						{
							break;
						}
						this.chunkSize = this.sizeDecoder.Value;
						this.sizeDecoder.Reset();
						if (this.chunkSize != 0)
						{
							this.currentState = SingletonMessageDecoder.State.ChunkStart;
							this.chunkBytesNeeded = this.chunkSize;
							break;
						}
						else
						{
							this.currentState = SingletonMessageDecoder.State.EnvelopeEnd;
							break;
						}
					}
					case SingletonMessageDecoder.State.ChunkStart:
					{
						num = 0;
						this.currentState = SingletonMessageDecoder.State.ReadingEnvelopeBytes;
						break;
					}
					case SingletonMessageDecoder.State.ReadingEnvelopeBytes:
					{
						num = size;
						if (num > this.chunkBytesNeeded)
						{
							num = this.chunkBytesNeeded;
						}
						SingletonMessageDecoder singletonMessageDecoder = this;
						singletonMessageDecoder.chunkBytesNeeded = singletonMessageDecoder.chunkBytesNeeded - num;
						if (this.chunkBytesNeeded != 0)
						{
							break;
						}
						this.currentState = SingletonMessageDecoder.State.ChunkEnd;
						break;
					}
					case SingletonMessageDecoder.State.ChunkEnd:
					{
						num = 0;
						this.currentState = SingletonMessageDecoder.State.ReadingEnvelopeChunkSize;
						break;
					}
					case SingletonMessageDecoder.State.EnvelopeEnd:
					{
						base.ValidateRecordType(FramingRecordType.End, (FramingRecordType)bytes[offset]);
						num = 1;
						this.currentState = SingletonMessageDecoder.State.End;
						break;
					}
					case SingletonMessageDecoder.State.End:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.FramingAtEnd, new object[0]))));
					}
					default:
					{
						throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(base.CreateException(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(Resources.InvalidDecoderStateMachine, new object[0]))));
					}
				}
				SingletonMessageDecoder streamPosition = this;
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
			this.currentState = SingletonMessageDecoder.State.ChunkStart;
		}

		public enum State
		{
			ReadingEnvelopeChunkSize,
			ChunkStart,
			ReadingEnvelopeBytes,
			ChunkEnd,
			EnvelopeEnd,
			End
		}
	}
}