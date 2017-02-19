using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Common
{
	[Serializable]
	internal class AssertionFailedException : Exception
	{
		public AssertionFailedException(string description) : base(SRCore.ShipAssertExceptionMessage(description))
		{
		}

		protected AssertionFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}