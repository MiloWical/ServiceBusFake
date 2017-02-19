using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal class ReconnectBindingElement : BindingElement
	{
		private IEnumerable<Uri> viaAddresses;

		public ReconnectBindingElement(IEnumerable<Uri> viaAddresses)
		{
			this.viaAddresses = viaAddresses;
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (!this.CanBuildChannelFactory<TChannel>(context))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("TChannel", SRClient.ChannelTypeNotSupported(typeof(TChannel)));
			}
			return new ReconnectBindingElement.ReconnectChannelFactory<TChannel>(context, this.viaAddresses);
		}

		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			if (context == null)
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("context");
			}
			if (typeof(TChannel) != typeof(IRequestSessionChannel))
			{
				return false;
			}
			return base.CanBuildChannelFactory<TChannel>(context);
		}

		public override BindingElement Clone()
		{
			return new ReconnectBindingElement(this.viaAddresses);
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			return context.GetInnerProperty<T>();
		}

		public static bool IsRetryable(Message message, Exception lastException)
		{
			if (!(lastException is CommunicationObjectFaultedException) && !(lastException is CommunicationObjectAbortedException))
			{
				return false;
			}
			return message.State == System.ServiceModel.Channels.MessageState.Created;
		}

		private class ReconnectChannelFactory<TChannel> : ChannelFactoryBase<TChannel>
		{
			private readonly IChannelFactory<TChannel> innerFactory;

			private readonly IEnumerable<Uri> viaAddresses;

			public ReconnectChannelFactory(BindingContext context, IEnumerable<Uri> viaAddresses) : base(context.Binding)
			{
				this.viaAddresses = viaAddresses;
				this.innerFactory = context.BuildInnerChannelFactory<TChannel>();
			}

			public override T GetProperty<T>()
			where T : class
			{
				return this.innerFactory.GetProperty<T>();
			}

			protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerFactory.BeginClose(timeout, callback, state);
			}

			protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.innerFactory.BeginOpen(timeout, callback, state);
			}

			protected override void OnClose(TimeSpan timeout)
			{
				this.innerFactory.Close(timeout);
			}

			protected override TChannel OnCreateChannel(EndpointAddress address, Uri via)
			{
				return (TChannel)(new ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel((ReconnectBindingElement.ReconnectChannelFactory<IRequestSessionChannel>)this, address, this.viaAddresses));
			}

			protected override void OnEndClose(IAsyncResult result)
			{
				this.innerFactory.EndClose(result);
			}

			protected override void OnEndOpen(IAsyncResult result)
			{
				this.innerFactory.EndOpen(result);
			}

			private void OnInnerFactoryFaulted(object sender, EventArgs e)
			{
				base.Fault();
			}

			protected override void OnOpen(TimeSpan timeout)
			{
				this.innerFactory.Open(timeout);
			}

			protected override void OnOpening()
			{
				base.OnOpening();
				this.innerFactory.SafeAddFaulted(new EventHandler(this.OnInnerFactoryFaulted));
			}

			private class RequestSessionChannel : ChannelBase, IRequestSessionChannel, IRequestChannel, IChannel, ICommunicationObject, ISessionChannel<IOutputSession>
			{
				private IChannelFactory<IRequestSessionChannel> innerFactory;

				private SharedChannel<IRequestSessionChannel> sharedChannel;

				public EndpointAddress RemoteAddress
				{
					get
					{
						return get_RemoteAddress();
					}
					set
					{
						set_RemoteAddress(value);
					}
				}

				private EndpointAddress <RemoteAddress>k__BackingField;

				public EndpointAddress get_RemoteAddress()
				{
					return this.<RemoteAddress>k__BackingField;
				}

				private void set_RemoteAddress(EndpointAddress value)
				{
					this.<RemoteAddress>k__BackingField = value;
				}

				public IOutputSession Session
				{
					get
					{
						return get_Session();
					}
					set
					{
						set_Session(value);
					}
				}

				private IOutputSession <Session>k__BackingField;

				public IOutputSession get_Session()
				{
					return this.<Session>k__BackingField;
				}

				private void set_Session(IOutputSession value)
				{
					this.<Session>k__BackingField = value;
				}

				public Uri Via
				{
					get
					{
						return get_Via();
					}
					set
					{
						set_Via(value);
					}
				}

				private Uri <Via>k__BackingField;

				public Uri get_Via()
				{
					return this.<Via>k__BackingField;
				}

				private void set_Via(Uri value)
				{
					this.<Via>k__BackingField = value;
				}

				public RequestSessionChannel(ReconnectBindingElement.ReconnectChannelFactory<IRequestSessionChannel> factory, EndpointAddress address, IEnumerable<Uri> viaAddresses) : base(factory)
				{
					this.innerFactory = factory.innerFactory;
					this.RemoteAddress = address;
					this.Via = viaAddresses.First<Uri>();
					ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.OutputSession outputSession = new ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.OutputSession()
					{
						Id = Guid.NewGuid().ToString()
					};
					this.Session = outputSession;
					this.sharedChannel = new SharedChannel<IRequestSessionChannel>(this.innerFactory, viaAddresses);
				}

				public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
				{
					return this.BeginRequest(message, base.DefaultSendTimeout, callback, state);
				}

				public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
				{
					TimeoutHelper.ThrowIfNegativeArgument(timeout);
					return (new ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult(this, message, timeout, callback, state)).Start();
				}

				public Message EndRequest(IAsyncResult result)
				{
					return AsyncResult<ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult>.End(result).ResponseMessage;
				}

				protected override void OnAbort()
				{
					this.sharedChannel.Abort();
				}

				protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
				{
					return this.sharedChannel.BeginClose(timeout, callback, state);
				}

				protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
				{
					return new CompletedAsyncResult(callback, state);
				}

				protected override void OnClose(TimeSpan timeout)
				{
					this.sharedChannel.Close(timeout);
				}

				protected override void OnEndClose(IAsyncResult result)
				{
					this.sharedChannel.EndClose(result);
				}

				protected override void OnEndOpen(IAsyncResult result)
				{
					CompletedAsyncResult.End(result);
				}

				protected override void OnOpen(TimeSpan timeout)
				{
				}

				public Message Request(Message message)
				{
					return this.Request(message, base.DefaultSendTimeout);
				}

				public Message Request(Message message, TimeSpan timeout)
				{
					return this.EndRequest(this.BeginRequest(message, timeout, null, null));
				}

				private class OutputSession : IOutputSession, ISession
				{
					public string Id
					{
						get
						{
							return get_Id();
						}
						set
						{
							set_Id(value);
						}
					}

					private string <Id>k__BackingField;

					public string get_Id()
					{
						return this.<Id>k__BackingField;
					}

					public void set_Id(string value)
					{
						this.<Id>k__BackingField = value;
					}

					public OutputSession()
					{
					}
				}

				private class RequestAsyncResult : IteratorAsyncResult<ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult>
				{
					private const int MaxRetriesOnCommunicationException = 1;

					private readonly ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel requestSessionChannel;

					private readonly Message message;

					private IRequestSessionChannel innerChannel;

					public Message ResponseMessage
					{
						get;
						private set;
					}

					public RequestAsyncResult(ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel requestSessionChannel, Message message, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
					{
						this.requestSessionChannel = requestSessionChannel;
						this.message = message;
					}

					protected override IEnumerator<IteratorAsyncResult<ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult>.AsyncStep> GetAsyncSteps()
					{
						int num = 0;
						TimeSpan timeSpan = base.RemainingTime();
						while (true)
						{
							if (timeSpan <= TimeSpan.Zero)
							{
								TimeSpan originalTimeout = base.OriginalTimeout;
								throw new TimeoutException(SRClient.OperationRequestTimedOut(originalTimeout.TotalMilliseconds));
							}
							ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult requestAsyncResult = this;
							IteratorAsyncResult<ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult>.BeginCall beginCall = (ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.requestSessionChannel.sharedChannel.BeginGetInstance(timeSpan, c, s);
							yield return requestAsyncResult.CallAsync(beginCall, (ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult thisPtr, IAsyncResult r) => thisPtr.innerChannel = thisPtr.requestSessionChannel.sharedChannel.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
							ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult requestAsyncResult1 = this;
							IteratorAsyncResult<ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult>.BeginCall beginCall1 = (ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.innerChannel.BeginRequest(thisPtr.message, timeSpan, c, s);
							IteratorAsyncResult<ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult>.EndCall responseMessage = (ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult thisPtr, IAsyncResult r) => thisPtr.ResponseMessage = thisPtr.innerChannel.EndRequest(r);
							yield return requestAsyncResult1.CallAsync(beginCall1, responseMessage, (ReconnectBindingElement.ReconnectChannelFactory<TChannel>.RequestSessionChannel.RequestAsyncResult thisPtr, TimeSpan t) => thisPtr.ResponseMessage = thisPtr.innerChannel.Request(thisPtr.message, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
							Exception lastAsyncStepException = base.LastAsyncStepException;
							if (lastAsyncStepException == null)
							{
								break;
							}
							if (!ReconnectBindingElement.IsRetryable(this.message, lastAsyncStepException))
							{
								base.Complete(lastAsyncStepException);
								break;
							}
							else if (num >= 1 || this.requestSessionChannel.State != CommunicationState.Opened)
							{
								if (this.message != null)
								{
									this.message.SafeClose();
								}
								base.Complete(lastAsyncStepException);
								break;
							}
							else
							{
								num++;
								MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryOperation("SharedChannel.Request", num, lastAsyncStepException.Message));
								yield return base.CallAsyncSleep(TimeSpan.FromMilliseconds(500));
								timeSpan = base.RemainingTime();
							}
						}
					}
				}
			}
		}
	}
}