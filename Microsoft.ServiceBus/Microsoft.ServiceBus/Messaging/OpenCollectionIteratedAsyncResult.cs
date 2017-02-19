using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class OpenCollectionIteratedAsyncResult : IteratorAsyncResult<OpenCollectionIteratedAsyncResult>
	{
		private ICommunicationObject currentCommunicationObject;

		private IEnumerable<ICommunicationObject> CommunicationObjects
		{
			get;
			set;
		}

		public OpenCollectionIteratedAsyncResult(IEnumerable<ICommunicationObject> communicationObjects, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.CommunicationObjects = communicationObjects;
			base.Start();
		}

		protected override IEnumerator<IteratorAsyncResult<OpenCollectionIteratedAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			foreach (ICommunicationObject communicationObject in this.CommunicationObjects)
			{
				this.currentCommunicationObject = communicationObject;
				OpenCollectionIteratedAsyncResult openCollectionIteratedAsyncResult = this;
				IteratorAsyncResult<OpenCollectionIteratedAsyncResult>.BeginCall beginCall = (OpenCollectionIteratedAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.currentCommunicationObject.BeginOpen(t, c, s);
				IteratorAsyncResult<OpenCollectionIteratedAsyncResult>.EndCall endCall = (OpenCollectionIteratedAsyncResult thisPtr, IAsyncResult r) => thisPtr.currentCommunicationObject.EndOpen(r);
				yield return openCollectionIteratedAsyncResult.CallAsync(beginCall, endCall, (OpenCollectionIteratedAsyncResult thisPtr, TimeSpan t) => thisPtr.currentCommunicationObject.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}
	}
}