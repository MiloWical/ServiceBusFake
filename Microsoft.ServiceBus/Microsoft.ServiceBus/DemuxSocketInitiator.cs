using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using System;
using System.Text;

namespace Microsoft.ServiceBus
{
	internal class DemuxSocketInitiator : IConnectionInitiator
	{
		private IConnectionInitiator innerInitiator;

		private string type;

		public DemuxSocketInitiator(IConnectionInitiator innerInitiator, string type)
		{
			this.innerInitiator = innerInitiator;
			this.type = type;
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult<IConnection>(this.Connect(uri, timeout), callback, state);
		}

		public IConnection Connect(Uri uri, TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			IConnection connection = this.innerInitiator.Connect(uri, timeoutHelper.RemainingTime());
			byte[] bytes = Encoding.UTF8.GetBytes(this.type);
			byte[] numArray = BitConverter.GetBytes((int)bytes.Length);
			connection.Write(numArray, 0, (int)numArray.Length, true, timeoutHelper.RemainingTime());
			connection.Write(bytes, 0, (int)bytes.Length, true, timeoutHelper.RemainingTime());
			return connection;
		}

		public IConnection EndConnect(IAsyncResult result)
		{
			return CompletedAsyncResult<IConnection>.End(result);
		}
	}
}