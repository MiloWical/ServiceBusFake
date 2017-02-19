using Microsoft.ServiceBus.Channels;
using System;

namespace Microsoft.ServiceBus
{
	internal class DemuxSocketElement : IConnectionElement
	{
		private readonly static UriPrefixTable<ITransportManagerRegistration> transportManagerTable;

		private readonly DemuxSocketManager demuxManager;

		private readonly IConnectionElement innerElement;

		private readonly string type;

		public UriPrefixTable<ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return DemuxSocketElement.transportManagerTable;
			}
		}

		static DemuxSocketElement()
		{
			DemuxSocketElement.transportManagerTable = new UriPrefixTable<ITransportManagerRegistration>(true);
		}

		public DemuxSocketElement(IConnectionElement innerElement, string type) : this(innerElement, type, null)
		{
		}

		public DemuxSocketElement(IConnectionElement innerElement, string type, DemuxSocketManager demuxManager)
		{
			this.innerElement = innerElement;
			this.type = type;
			this.demuxManager = demuxManager;
		}

		public IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new DemuxSocketInitiator(this.innerElement.CreateInitiator(bufferSize), this.type);
		}

		public IConnectionListener CreateListener(int bufferSize, Uri uri)
		{
			return new DemuxSocketListener(uri, this.type, this.demuxManager);
		}

		public T GetProperty<T>()
		where T : class
		{
			return default(T);
		}

		public bool IsCompatible(IConnectionElement element)
		{
			if (!(element is DemuxSocketElement) || !this.innerElement.IsCompatible(((DemuxSocketElement)element).innerElement))
			{
				return false;
			}
			return this.type == ((DemuxSocketElement)element).type;
		}
	}
}