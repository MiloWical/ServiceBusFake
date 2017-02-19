using System;

namespace Microsoft.ServiceBus.Channels
{
	internal class ConnectionMessageProperty
	{
		private IConnection connection;

		public IConnection Connection
		{
			get
			{
				return this.connection;
			}
		}

		public static string Name
		{
			get
			{
				return "iconnection";
			}
		}

		public ConnectionMessageProperty(IConnection connection)
		{
			this.connection = connection;
		}
	}
}