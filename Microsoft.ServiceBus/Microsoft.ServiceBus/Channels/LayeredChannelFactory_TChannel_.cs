using Microsoft.ServiceBus.Common;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class LayeredChannelFactory<TChannel> : ChannelFactoryBase<TChannel>
	{
		private IChannelFactory innerChannelFactory;

		protected IChannelFactory InnerChannelFactory
		{
			get
			{
				return this.innerChannelFactory;
			}
		}

		public LayeredChannelFactory(IDefaultCommunicationTimeouts timeouts, IChannelFactory innerChannelFactory) : base(timeouts)
		{
			this.innerChannelFactory = innerChannelFactory;
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(IChannelFactory<TChannel>))
			{
				return (T)this;
			}
			T property = base.GetProperty<T>();
			if (property != null)
			{
				return property;
			}
			return this.innerChannelFactory.GetProperty<T>();
		}

		protected override void OnAbort()
		{
			base.OnAbort();
			this.innerChannelFactory.Abort();
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			Microsoft.ServiceBus.Common.ChainedBeginHandler chainedBeginHandler = new Microsoft.ServiceBus.Common.ChainedBeginHandler(this.OnBeginClose);
			Microsoft.ServiceBus.Common.ChainedEndHandler chainedEndHandler = new Microsoft.ServiceBus.Common.ChainedEndHandler(this.OnEndClose);
			ICommunicationObject[] communicationObjectArray = new ICommunicationObject[] { this.innerChannelFactory };
			return new Microsoft.ServiceBus.Channels.ChainedCloseAsyncResult(timeout, callback, state, chainedBeginHandler, chainedEndHandler, communicationObjectArray);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.innerChannelFactory.BeginOpen(timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			base.OnClose(timeoutHelper.RemainingTime());
			this.innerChannelFactory.Close(timeoutHelper.RemainingTime());
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Common.ChainedAsyncResult.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			this.innerChannelFactory.EndOpen(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			this.innerChannelFactory.Open(timeout);
		}
	}
}