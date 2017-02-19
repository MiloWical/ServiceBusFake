using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal class ChainedOpenAsyncResult : ChainedAsyncResult
	{
		private IList<ICommunicationObject> collection;

		public ChainedOpenAsyncResult(TimeSpan timeout, AsyncCallback callback, object state, ChainedBeginHandler begin1, ChainedEndHandler end1, params ICommunicationObject[] objs) : base(timeout, callback, state)
		{
			this.collection = new List<ICommunicationObject>();
			for (int i = 0; i < (int)objs.Length; i++)
			{
				if (objs[i] != null)
				{
					this.collection.Add(objs[i]);
				}
			}
			base.Begin(begin1, end1, new ChainedBeginHandler(this.BeginOpen), new ChainedEndHandler(this.EndOpen));
		}

		private IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult(timeout, callback, state, this.collection);
		}

		private void EndOpen(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult.End((Microsoft.ServiceBus.Channels.OpenCollectionAsyncResult)result);
		}
	}
}