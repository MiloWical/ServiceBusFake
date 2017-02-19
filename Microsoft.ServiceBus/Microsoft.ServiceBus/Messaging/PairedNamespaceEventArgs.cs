using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	public class PairedNamespaceEventArgs : EventArgs
	{
		public PairedNamespaceOptions Options
		{
			get;
			private set;
		}

		internal PairedNamespaceEventArgs(PairedNamespaceOptions options)
		{
			this.Options = options;
		}
	}
}