using System;

namespace Microsoft.ServiceBus.Common
{
	internal struct TracePayload
	{
		private string serializedException;

		private string eventSource;

		private string appDomainFriendlyName;

		private string extendedData;

		private string hostReference;

		public string AppDomainFriendlyName
		{
			get
			{
				return this.appDomainFriendlyName;
			}
		}

		public string EventSource
		{
			get
			{
				return this.eventSource;
			}
		}

		public string ExtendedData
		{
			get
			{
				return this.extendedData;
			}
		}

		public string HostReference
		{
			get
			{
				return this.hostReference;
			}
		}

		public string SerializedException
		{
			get
			{
				return this.serializedException;
			}
		}

		public TracePayload(string serializedException, string eventSource, string appDomainFriendlyName, string extendedData, string hostReference)
		{
			this.serializedException = serializedException;
			this.eventSource = eventSource;
			this.appDomainFriendlyName = appDomainFriendlyName;
			this.extendedData = extendedData;
			this.hostReference = hostReference;
		}
	}
}