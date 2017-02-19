using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
	internal sealed class SerialiableMember
	{
		public MemberAccessor Accessor
		{
			get;
			set;
		}

		public bool Mandatory
		{
			get;
			set;
		}

		public string Name
		{
			get;
			set;
		}

		public int Order
		{
			get;
			set;
		}

		public SerializableType Type
		{
			get;
			set;
		}

		public SerialiableMember()
		{
		}
	}
}