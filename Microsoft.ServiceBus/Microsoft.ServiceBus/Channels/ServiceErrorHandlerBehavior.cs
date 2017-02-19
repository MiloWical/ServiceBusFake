using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace Microsoft.ServiceBus.Channels
{
	internal sealed class ServiceErrorHandlerBehavior : IServiceBehavior, IErrorHandler
	{
		public ServiceErrorHandlerBehavior()
		{
		}

		void System.ServiceModel.Description.IServiceBehavior.AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{
		}

		void System.ServiceModel.Description.IServiceBehavior.ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
			{
				channelDispatcher.ErrorHandlers.Add(this);
			}
		}

		void System.ServiceModel.Description.IServiceBehavior.Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
		}

		bool System.ServiceModel.Dispatcher.IErrorHandler.HandleError(Exception error)
		{
			EventHandler<ServiceErrorEventArgs> eventHandler = this.HandleError;
			if (eventHandler == null)
			{
				return false;
			}
			ServiceErrorEventArgs serviceErrorEventArg = new ServiceErrorEventArgs()
			{
				Exception = error
			};
			eventHandler(this, serviceErrorEventArg);
			return serviceErrorEventArg.Handled;
		}

		void System.ServiceModel.Dispatcher.IErrorHandler.ProvideFault(Exception error, MessageVersion version, ref Message fault)
		{
		}

		public event EventHandler<ServiceErrorEventArgs> HandleError;
	}
}