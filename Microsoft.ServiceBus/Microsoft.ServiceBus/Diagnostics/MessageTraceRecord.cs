using Microsoft.ServiceBus.Common.Diagnostics;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class MessageTraceRecord : TraceRecord
	{
		internal MessageTraceRecord(Message message)
		{
		}
	}
}