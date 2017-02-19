using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class CloseEntityCollectionIteratedAsyncResult : IteratorAsyncResult<CloseEntityCollectionIteratedAsyncResult>
	{
		private MessageClientEntity currentEntity;

		private IEnumerable<MessageClientEntity> ClientEntities
		{
			get;
			set;
		}

		public CloseEntityCollectionIteratedAsyncResult(IEnumerable<MessageClientEntity> entities, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.ClientEntities = entities;
			base.Start();
		}

		protected override IEnumerator<IteratorAsyncResult<CloseEntityCollectionIteratedAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			foreach (MessageClientEntity clientEntity in this.ClientEntities)
			{
				this.currentEntity = clientEntity;
				CloseEntityCollectionIteratedAsyncResult closeEntityCollectionIteratedAsyncResult = this;
				IteratorAsyncResult<CloseEntityCollectionIteratedAsyncResult>.BeginCall beginCall = (CloseEntityCollectionIteratedAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.currentEntity.BeginClose(t, c, s);
				IteratorAsyncResult<CloseEntityCollectionIteratedAsyncResult>.EndCall endCall = (CloseEntityCollectionIteratedAsyncResult thisPtr, IAsyncResult r) => thisPtr.currentEntity.EndClose(r);
				yield return closeEntityCollectionIteratedAsyncResult.CallAsync(beginCall, endCall, (CloseEntityCollectionIteratedAsyncResult thisPtr, TimeSpan t) => thisPtr.currentEntity.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (base.LastAsyncStepException == null)
				{
					continue;
				}
				this.currentEntity.Abort();
			}
		}
	}
}