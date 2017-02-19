using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class CollectionQueryResult<T> : IEnumerable<T>, IEnumerable
	where T : EntityDescription
	{
		private IEnumerable<T> results;

		public string ContinuationToken
		{
			get;
			private set;
		}

		internal CollectionQueryResult(IEnumerable<T> results, string continuationToken)
		{
			this.results = results;
			if (this.results == null)
			{
				this.results = new T[0];
			}
			this.ContinuationToken = continuationToken;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.results.GetEnumerator();
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.results.GetEnumerator();
		}
	}
}