using System;

namespace Microsoft.ServiceBus
{
	internal enum TransactionCommitStatus
	{
		Committed = 1,
		Aborted = 2,
		InDoubt = 3
	}
}