using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Channels
{
	internal sealed class ServiceErrorEventArgs : EventArgs
	{
		public System.Exception Exception
		{
			get;
			internal set;
		}

		public bool Handled
		{
			get;
			set;
		}

		public ServiceErrorEventArgs()
		{
		}
	}
}