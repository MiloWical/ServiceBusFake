using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal class SharedChannel<TChannel> : SingletonManager<TChannel>
	where TChannel : class, IRequestSessionChannel
	{
		private readonly IChannelFactory<TChannel> innerFactory;

		private readonly EventHandler onInnerChannelFaulted;

		private readonly IEnumerable<Uri> viaAddresses;

		public SharedChannel(IChannelFactory<TChannel> innerFactory, IEnumerable<Uri> viaAddresses) : base(new object())
		{
			this.innerFactory = innerFactory;
			this.viaAddresses = viaAddresses;
			this.onInnerChannelFaulted = new EventHandler(this.OnInnerChannelFaulted);
		}

		public void Abort()
		{
			(new SharedChannel<TChannel>.CloseOrAbortAsyncResult(this, true, TimeSpan.Zero, null, null)).RunSynchronously();
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new SharedChannel<TChannel>.CloseOrAbortAsyncResult(this, false, timeout, callback, state)).Start();
		}

		public void Close(TimeSpan timeout)
		{
			(new SharedChannel<TChannel>.CloseOrAbortAsyncResult(this, false, timeout, null, null)).RunSynchronously();
		}

		public void EndClose(IAsyncResult result)
		{
			AsyncResult<SharedChannel<TChannel>.CloseOrAbortAsyncResult>.End(result);
		}

		protected override IAsyncResult OnBeginCreateInstance(TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (timeout > Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.OpenTimeout)
			{
				timeout = Microsoft.ServiceBus.Messaging.Channels.TransportDefaults.OpenTimeout;
			}
			return (new SharedChannel<TChannel>.CreateChannelAsyncResult(this, timeout, callback, state)).Start();
		}

		protected override TChannel OnEndCreateInstance(IAsyncResult asyncResult)
		{
			TChannel channel = AsyncResult<SharedChannel<TChannel>.CreateChannelAsyncResult>.End(asyncResult).Channel;
			channel.SafeAddFaulted(this.onInnerChannelFaulted);
			return channel;
		}

		protected override void OnGetInstance(TChannel channel)
		{
			if (channel.State != CommunicationState.Opened)
			{
				base.Invalidate(channel);
			}
		}

		private void OnInnerChannelFaulted(object sender, EventArgs e)
		{
			TChannel tChannel = (TChannel)sender;
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] fullName = new object[] { typeof(TChannel).FullName, null, null, null };
			fullName[1] = (tChannel.Session != null ? tChannel.Session.Id : "null");
			fullName[2] = (tChannel.Via != null ? tChannel.Via.AbsoluteUri : "null");
			fullName[3] = (!(tChannel.RemoteAddress != null) || !(tChannel.RemoteAddress.Uri != null) ? "null" : tChannel.RemoteAddress.Uri.AbsoluteUri);
			string str = string.Format(invariantCulture, "{0}, Session = {1}, Via = {2}, RemoveAddress = {3}", fullName);
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteChannelFaulted(str));
			base.Invalidate(tChannel);
			tChannel.Abort();
			((ICommunicationObject)(object)tChannel).Faulted -= this.onInnerChannelFaulted;
		}

		private class CloseOrAbortAsyncResult : IteratorAsyncResult<SharedChannel<TChannel>.CloseOrAbortAsyncResult>
		{
			private readonly TChannel channel;

			private readonly bool abort;

			public CloseOrAbortAsyncResult(SharedChannel<TChannel> sharedChannel, bool abort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.abort = abort;
				sharedChannel.Invalidate(out this.channel);
			}

			protected override IEnumerator<IteratorAsyncResult<SharedChannel<TChannel>.CloseOrAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (this.channel != null)
				{
					if (!this.abort)
					{
						SharedChannel<TChannel>.CloseOrAbortAsyncResult closeOrAbortAsyncResult = this;
						IteratorAsyncResult<SharedChannel<TChannel>.CloseOrAbortAsyncResult>.BeginCall beginCall = (SharedChannel<TChannel>.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.channel.BeginClose(t, c, s);
						IteratorAsyncResult<SharedChannel<TChannel>.CloseOrAbortAsyncResult>.EndCall endCall = (SharedChannel<TChannel>.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.channel.EndClose(r);
						yield return closeOrAbortAsyncResult.CallAsync(beginCall, endCall, (SharedChannel<TChannel>.CloseOrAbortAsyncResult thisPtr, TimeSpan t) => thisPtr.channel.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					else
					{
						this.channel.Abort();
					}
				}
			}
		}

		private class CreateChannelAsyncResult : IteratorAsyncResult<SharedChannel<TChannel>.CreateChannelAsyncResult>
		{
			private readonly SharedChannel<TChannel> sharedChannel;

			public TChannel Channel
			{
				get;
				private set;
			}

			public CreateChannelAsyncResult(SharedChannel<TChannel> sharedChannel, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.sharedChannel = sharedChannel;
			}

			protected override IEnumerator<IteratorAsyncResult<SharedChannel<TChannel>.CreateChannelAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ServiceBusUriManager serviceBusUriManager = new ServiceBusUriManager(this.sharedChannel.viaAddresses.ToList<Uri>(), false);
				while (true)
				{
					if (serviceBusUriManager.MoveNextUri())
					{
						this.Channel = this.sharedChannel.innerFactory.CreateChannel(new EndpointAddress(serviceBusUriManager.Current, new AddressHeader[0]), serviceBusUriManager.Current);
						SharedChannel<TChannel>.CreateChannelAsyncResult createChannelAsyncResult = this;
						IteratorAsyncResult<SharedChannel<TChannel>.CreateChannelAsyncResult>.BeginCall beginCall = (SharedChannel<TChannel>.CreateChannelAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Channel.BeginOpen(t, c, s);
						yield return createChannelAsyncResult.CallAsync(beginCall, (SharedChannel<TChannel>.CreateChannelAsyncResult thisPtr, IAsyncResult r) => thisPtr.Channel.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							break;
						}
						this.Channel.Abort();
					}
					else
					{
						if (base.LastAsyncStepException == null)
						{
							break;
						}
						if (!(base.LastAsyncStepException is TimeoutException))
						{
							throw base.LastAsyncStepException;
						}
						throw new CommunicationException(SRClient.OpenChannelFailed(base.OriginalTimeout), base.LastAsyncStepException);
					}
				}
			}
		}
	}
}