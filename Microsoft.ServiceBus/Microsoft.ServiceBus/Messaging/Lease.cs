using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	public class Lease
	{
		public long Epoch
		{
			get;
			set;
		}

		public string Offset
		{
			get;
			set;
		}

		public string Owner
		{
			get;
			set;
		}

		public string PartitionId
		{
			get;
			set;
		}

		public long SequenceNumber
		{
			get;
			set;
		}

		public string Token
		{
			get;
			set;
		}

		public Lease()
		{
		}

		public Lease(Lease source)
		{
			this.PartitionId = source.PartitionId;
			this.Owner = source.Owner;
			this.Token = source.Token;
			this.Epoch = source.Epoch;
			this.Offset = source.Offset;
			this.SequenceNumber = source.SequenceNumber;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj))
			{
				return true;
			}
			Lease lease = obj as Lease;
			if (lease == null)
			{
				return false;
			}
			return string.Equals(this.PartitionId, lease.PartitionId);
		}

		public override int GetHashCode()
		{
			return this.PartitionId.GetHashCode();
		}

		public virtual bool IsExpired()
		{
			return false;
		}
	}
}