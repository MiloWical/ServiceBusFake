using System;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class ClientFramingDecoder : FramingDecoder
	{
		private ClientFramingDecoderState currentState;

		public ClientFramingDecoderState CurrentState
		{
			get
			{
				return this.currentState;
			}
			protected set
			{
				this.currentState = value;
			}
		}

		protected override string CurrentStateAsString
		{
			get
			{
				return this.currentState.ToString();
			}
		}

		public abstract string Fault
		{
			get;
		}

		protected ClientFramingDecoder(long streamPosition) : base(streamPosition)
		{
			this.currentState = ClientFramingDecoderState.ReadingUpgradeRecord;
		}

		public abstract int Decode(byte[] bytes, int offset, int size);
	}
}