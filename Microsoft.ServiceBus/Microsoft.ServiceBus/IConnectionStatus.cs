using System;

namespace Microsoft.ServiceBus
{
	public interface IConnectionStatus
	{
		bool IsOnline
		{
			get;
		}

		Exception LastError
		{
			get;
		}

		event EventHandler Connecting;

		event EventHandler Offline;

		event EventHandler Online;
	}
}