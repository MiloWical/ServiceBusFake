using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;

namespace Microsoft.ServiceBus.Messaging
{
	internal delegate void TransactionStateChangedDelegate(TrackingContext trackingContext, string transactionId, TransactionCommitStatus transactionCommitStatus);
}