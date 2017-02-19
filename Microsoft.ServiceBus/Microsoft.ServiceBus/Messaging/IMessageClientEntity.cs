using System;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal interface IMessageClientEntity
	{
		bool IsClosed
		{
			get;
		}

		void Abort();

		void Close();

		Task CloseAsync();
	}
}