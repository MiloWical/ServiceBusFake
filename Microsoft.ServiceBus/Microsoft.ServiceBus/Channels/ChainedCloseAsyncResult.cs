using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal class ChainedCloseAsyncResult : ChainedAsyncResult
	{
		private IList<ICommunicationObject> collection;

		public ChainedCloseAsyncResult(TimeSpan timeout, AsyncCallback callback, object state, ChainedBeginHandler begin1, ChainedEndHandler end1, params ICommunicationObject[] objs) : base(timeout, callback, state)
		{
			this.collection = new List<ICommunicationObject>();
			if (objs != null)
			{
				for (int i = 0; i < (int)objs.Length; i++)
				{
					if (objs[i] != null)
					{
						this.collection.Add(objs[i]);
					}
				}
			}
			base.Begin(new ChainedBeginHandler(this.BeginClose), new ChainedEndHandler(this.EndClose), begin1, end1);
		}

		private IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult(timeout, callback, state, this.collection);
		}

		private void EndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult.End((Microsoft.ServiceBus.Channels.CloseCollectionAsyncResult)result);
		}
	}
}