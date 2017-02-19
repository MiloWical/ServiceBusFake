using System;

namespace Microsoft.ServiceBus.Tracing
{
	internal enum TraceOperation
	{
		Initialize,
		Create,
		Delete,
		Add,
		Remove,
		Open,
		Close,
		Send,
		Receive,
		Connect,
		Accept,
		Execute,
		Bind,
		Attach,
		Abort,
		Flow,
		ActiveLinkRegistered,
		ActiveLinkUpdated,
		ActiveLinkExpired,
		ActiveLinkRefreshed
	}
}