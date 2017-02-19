using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using System;

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
	internal static class TxnCapabilities
	{
		public readonly static AmqpSymbol LocalTransactions;

		public readonly static AmqpSymbol DistributedTxn;

		public readonly static AmqpSymbol PrototableTransactions;

		public readonly static AmqpSymbol MultiTxnsPerSsn;

		public readonly static AmqpSymbol MultiSsnsPerTxn;

		static TxnCapabilities()
		{
			TxnCapabilities.LocalTransactions = "amqp:local-transactions";
			TxnCapabilities.DistributedTxn = "amqp:distributed-transactions";
			TxnCapabilities.PrototableTransactions = "amqp:prototable-transactions";
			TxnCapabilities.MultiTxnsPerSsn = "amqp:multi-txns-per-ssn";
			TxnCapabilities.MultiSsnsPerTxn = "amqp:multi-ssns-per-txn";
		}
	}
}