using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class ExceptionReceivedEventArgs : EventArgs
	{
		public string Action
		{
			get;
			private set;
		}

		public System.Exception Exception
		{
			get;
			private set;
		}

		public ExceptionReceivedEventArgs(System.Exception exception, string action)
		{
			this.Exception = exception;
			this.Action = action;
		}
	}
}