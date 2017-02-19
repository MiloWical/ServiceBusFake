using Microsoft.ServiceBus.Channels;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal class WebStreamOnewayClientConnectionElement : IConnectionElement, ISecureableConnectionElement
	{
		private static UriPrefixTable<ITransportManagerRegistration> transportManagerTable;

		private string webStreamRole;

		private bool useHttpsMode;

		public SocketSecurityRole SecurityMode
		{
			get
			{
				return JustDecompileGenerated_get_SecurityMode();
			}
			set
			{
				JustDecompileGenerated_set_SecurityMode(value);
			}
		}

		private SocketSecurityRole JustDecompileGenerated_SecurityMode_k__BackingField;

		public SocketSecurityRole JustDecompileGenerated_get_SecurityMode()
		{
			return this.JustDecompileGenerated_SecurityMode_k__BackingField;
		}

		public void JustDecompileGenerated_set_SecurityMode(SocketSecurityRole value)
		{
			this.JustDecompileGenerated_SecurityMode_k__BackingField = value;
		}

		public UriPrefixTable<ITransportManagerRegistration> TransportManagerTable
		{
			get
			{
				return WebStreamOnewayClientConnectionElement.transportManagerTable;
			}
		}

		static WebStreamOnewayClientConnectionElement()
		{
			WebStreamOnewayClientConnectionElement.transportManagerTable = new UriPrefixTable<ITransportManagerRegistration>(true);
		}

		public WebStreamOnewayClientConnectionElement(SocketSecurityRole socketSecurityMode, string webStreamRole, bool useHttpsMode)
		{
			this.SecurityMode = socketSecurityMode;
			this.webStreamRole = webStreamRole;
			this.useHttpsMode = useHttpsMode;
		}

		public IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new WebStreamOnewayConnectionInitiator(this.webStreamRole, bufferSize, this.useHttpsMode);
		}

		public IConnectionListener CreateListener(int bufferSize, Uri uri)
		{
			throw new InvalidOperationException();
		}

		public T GetProperty<T>()
		where T : class
		{
			return default(T);
		}

		public bool IsCompatible(IConnectionElement element)
		{
			return element is WebStreamOnewayClientConnectionElement;
		}
	}
}