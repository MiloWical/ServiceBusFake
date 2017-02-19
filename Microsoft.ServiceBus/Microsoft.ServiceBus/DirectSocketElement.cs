using Microsoft.ServiceBus.Channels;
using System;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal class DirectSocketElement : IConnectionElement
	{
		private static Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> transportManagerTable;

		private Binding innerBinding;

		public Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return DirectSocketElement.transportManagerTable;
			}
		}

		static DirectSocketElement()
		{
			DirectSocketElement.transportManagerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration>(true);
		}

		public DirectSocketElement(Binding innerBinding)
		{
			this.innerBinding = innerBinding;
		}

		public Microsoft.ServiceBus.Channels.IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new DirectSocketInitiator(bufferSize, this.innerBinding);
		}

		public Microsoft.ServiceBus.Channels.IConnectionListener CreateListener(int bufferSize, Uri uri)
		{
			return new DirectSocketListener(bufferSize, uri, this.innerBinding);
		}

		public T GetProperty<T>()
		where T : class
		{
			return default(T);
		}

		public bool IsCompatible(IConnectionElement element)
		{
			if (!(element is DirectSocketElement))
			{
				return false;
			}
			return this.innerBinding == ((DirectSocketElement)element).innerBinding;
		}
	}
}