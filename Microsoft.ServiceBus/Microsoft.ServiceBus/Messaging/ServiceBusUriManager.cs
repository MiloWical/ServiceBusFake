using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class ServiceBusUriManager
	{
		private readonly List<Uri> uriAddresses;

		private readonly bool roundRobin;

		private int currentUriIndex;

		private int firstUriIndex;

		public Uri Current
		{
			get
			{
				return this.uriAddresses[this.currentUriIndex];
			}
		}

		public ServiceBusUriManager(List<Uri> uriAddresses, bool roundRobin = false)
		{
			this.uriAddresses = uriAddresses;
			this.firstUriIndex = -1;
			this.roundRobin = roundRobin;
		}

		public bool CanRetry()
		{
			if (this.roundRobin)
			{
				return true;
			}
			return !this.IsLastUri();
		}

		private int GetNextUriValue()
		{
			return (this.currentUriIndex + 1) % this.uriAddresses.Count;
		}

		private bool IsLastUri()
		{
			return this.GetNextUriValue() == this.firstUriIndex;
		}

		public bool MoveNextUri()
		{
			if (this.firstUriIndex != -1)
			{
				if (!this.roundRobin && this.IsLastUri())
				{
					return false;
				}
				this.currentUriIndex = this.GetNextUriValue();
			}
			else
			{
				int num = ConcurrentRandom.Next(0, this.uriAddresses.Count);
				int num1 = num;
				this.currentUriIndex = num;
				this.firstUriIndex = num1;
			}
			return true;
		}
	}
}