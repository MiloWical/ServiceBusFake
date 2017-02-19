using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[Serializable]
	public class LeaseLostException : Exception
	{
		public Microsoft.ServiceBus.Messaging.Lease Lease
		{
			get;
			private set;
		}

		public LeaseLostException()
		{
		}

		public LeaseLostException(Microsoft.ServiceBus.Messaging.Lease lease)
		{
			this.Lease = lease;
		}

		public LeaseLostException(Microsoft.ServiceBus.Messaging.Lease lease, Exception innerException) : base(null, innerException)
		{
			this.Lease = lease;
		}

		public LeaseLostException(string message) : base(message)
		{
		}

		public LeaseLostException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected LeaseLostException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			this.Lease = (Microsoft.ServiceBus.Messaging.Lease)info.GetValue("Lease", typeof(Microsoft.ServiceBus.Messaging.Lease));
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			if (this.Lease != null)
			{
				info.AddValue("Lease", this.Lease);
			}
		}
	}
}