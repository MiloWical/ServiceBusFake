using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal class MessageComparer : IComparer<BrokeredMessage>
	{
		private readonly bool useTransferSequenceNumber;

		public MessageComparer()
		{
			this.useTransferSequenceNumber = false;
		}

		public MessageComparer(bool useTransferSequenceNumber)
		{
			this.useTransferSequenceNumber = useTransferSequenceNumber;
		}

		public int Compare(BrokeredMessage x, BrokeredMessage y)
		{
			if (this.useTransferSequenceNumber)
			{
				return x.TransferSequenceNumber.CompareTo(y.TransferSequenceNumber);
			}
			return x.SequenceNumber.CompareTo(y.SequenceNumber);
		}
	}
}