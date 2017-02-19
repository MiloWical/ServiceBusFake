using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class MessagingFaultProperty
	{
		private readonly static string name;

		public string ExceptionMessage
		{
			get;
			set;
		}

		public string ExceptionName
		{
			get;
			set;
		}

		public bool IsMessagingTransientException
		{
			get;
			set;
		}

		public bool IsSuccessKpiException
		{
			get;
			set;
		}

		public static string Name
		{
			get
			{
				return MessagingFaultProperty.name;
			}
		}

		static MessagingFaultProperty()
		{
			MessagingFaultProperty.name = typeof(MessagingFaultProperty).FullName;
		}

		public MessagingFaultProperty()
		{
		}
	}
}