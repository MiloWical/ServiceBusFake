using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace Microsoft.ServiceBus
{
	public class ConnectionStatusBehavior : IEndpointBehavior, IConnectionStatus
	{
		private IConnectionStatus status;

		public bool IsOnline
		{
			get
			{
				if (this.status == null)
				{
					return false;
				}
				return this.status.IsOnline;
			}
		}

		public Exception LastError
		{
			get
			{
				if (this.status == null)
				{
					return null;
				}
				return this.status.LastError;
			}
		}

		public ConnectionStatusBehavior()
		{
		}

		private void OnConnecting(object source, EventArgs args)
		{
			EventHandler eventHandler = this.Connecting;
			if (eventHandler != null)
			{
				eventHandler(this, args);
			}
		}

		private void OnOffline(object source, EventArgs args)
		{
			EventHandler eventHandler = this.Offline;
			if (eventHandler != null)
			{
				eventHandler(this, args);
			}
		}

		private void OnOnline(object source, EventArgs args)
		{
			EventHandler eventHandler = this.Online;
			if (eventHandler != null)
			{
				eventHandler(this, args);
			}
		}

		public void Retry()
		{
			throw Fx.Exception.AsError(new NotImplementedException(), null);
		}

		internal void SetConnectionStatus(IConnectionStatus connectionStatus)
		{
			this.status = connectionStatus;
			this.status.Connecting += new EventHandler(this.OnConnecting);
			this.status.Online += new EventHandler(this.OnOnline);
			this.status.Offline += new EventHandler(this.OnOffline);
		}

		void System.ServiceModel.Description.IEndpointBehavior.AddBindingParameters(ServiceEndpoint serviceEndpoint, BindingParameterCollection bindingParameters)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint serviceEndpoint, ClientRuntime behavior)
		{
		}

		void System.ServiceModel.Description.IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint serviceEndpoint, EndpointDispatcher endpointDispatcher)
		{
			IConnectionStatus property = endpointDispatcher.ChannelDispatcher.Listener.GetProperty<IConnectionStatus>();
			if (property == null)
			{
				throw Fx.Exception.AsError(new InvalidOperationException(SRClient.ConnectionStatusBehavior), null);
			}
			this.SetConnectionStatus(property);
		}

		void System.ServiceModel.Description.IEndpointBehavior.Validate(ServiceEndpoint serviceEndpoint)
		{
		}

		public event EventHandler Connecting;

		public event EventHandler Offline;

		public event EventHandler Online;
	}
}