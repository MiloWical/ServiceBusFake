using System;

namespace Microsoft.ServiceBus
{
	public class ConnectivitySettings
	{
		protected ConnectivityMode connectivityMode = ConnectivityMode.AutoDetect;

		protected virtual bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public ConnectivityMode Mode
		{
			get
			{
				return this.connectivityMode;
			}
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(SRClient.ValueVisibility);
				}
				if (this.connectivityMode != value)
				{
					this.connectivityMode = value;
				}
			}
		}

		public ConnectivitySettings()
		{
		}
	}
}