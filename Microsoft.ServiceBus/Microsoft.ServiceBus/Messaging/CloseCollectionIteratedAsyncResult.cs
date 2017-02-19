using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class CloseCollectionIteratedAsyncResult : IteratorAsyncResult<CloseCollectionIteratedAsyncResult>
	{
		private ICommunicationObject currentCommunicationObject;

		private IEnumerable<ICommunicationObject> CommunicationObjects
		{
			get;
			set;
		}

		public CloseCollectionIteratedAsyncResult(IEnumerable<ICommunicationObject> communicationObjects, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.CommunicationObjects = communicationObjects;
			base.Start();
		}

		protected override IEnumerator<IteratorAsyncResult<CloseCollectionIteratedAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			foreach (ICommunicationObject communicationObject in this.CommunicationObjects)
			{
				if (communicationObject == null || communicationObject.State == CommunicationState.Closed)
				{
					continue;
				}
				this.currentCommunicationObject = communicationObject;
				CloseCollectionIteratedAsyncResult closeCollectionIteratedAsyncResult = this;
				IteratorAsyncResult<CloseCollectionIteratedAsyncResult>.BeginCall beginCall = (CloseCollectionIteratedAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.currentCommunicationObject.BeginClose(t, c, s);
				IteratorAsyncResult<CloseCollectionIteratedAsyncResult>.EndCall endCall = (CloseCollectionIteratedAsyncResult thisPtr, IAsyncResult r) => thisPtr.currentCommunicationObject.EndClose(r);
				yield return closeCollectionIteratedAsyncResult.CallAsync(beginCall, endCall, (CloseCollectionIteratedAsyncResult thisPtr, TimeSpan t) => thisPtr.currentCommunicationObject.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (base.LastAsyncStepException == null)
				{
					continue;
				}
				this.currentCommunicationObject.Abort();
			}
		}
	}
}