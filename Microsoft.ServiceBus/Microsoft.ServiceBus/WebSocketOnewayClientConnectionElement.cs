using Microsoft.ServiceBus.Channels;
using System;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal class WebSocketOnewayClientConnectionElement : IConnectionElement, ISecureableConnectionElement
	{
		private readonly static UriPrefixTable<ITransportManagerRegistration> transportManagerTable;

		private readonly string webSocketRole;

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
				return WebSocketOnewayClientConnectionElement.transportManagerTable;
			}
		}

		static WebSocketOnewayClientConnectionElement()
		{
			WebSocketOnewayClientConnectionElement.transportManagerTable = new UriPrefixTable<ITransportManagerRegistration>(true);
		}

		public WebSocketOnewayClientConnectionElement(SocketSecurityRole socketSecurityMode, string webSocketRole)
		{
			this.SecurityMode = socketSecurityMode;
			this.webSocketRole = webSocketRole;
		}

		public IConnectionInitiator CreateInitiator(int bufferSize)
		{
			return new WebSocketOnewayConnectionInitiator(this.webSocketRole, bufferSize);
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
			return element is WebSocketOnewayClientConnectionElement;
		}
	}
}