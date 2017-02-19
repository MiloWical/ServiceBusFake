using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class RedirectBindingElement : BindingElement
	{
		public bool EnableRedirect
		{
			get;
			set;
		}

		public DnsEndpointIdentity EndpointIdentity
		{
			get;
			set;
		}

		public bool IncludeExceptionDetails
		{
			get;
			set;
		}

		public bool UseSslStreamSecurity
		{
			get;
			set;
		}

		public RedirectBindingElement()
		{
			this.EnableRedirect = true;
		}

		public RedirectBindingElement(RedirectBindingElement other)
		{
			this.EnableRedirect = other.EnableRedirect;
			this.IncludeExceptionDetails = other.IncludeExceptionDetails;
			this.UseSslStreamSecurity = other.UseSslStreamSecurity;
			this.EndpointIdentity = other.EndpointIdentity;
		}

		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if (!this.CanBuildChannelFactory<TChannel>(context))
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.Argument("TChannel", SRClient.ChannelTypeNotSupported(typeof(TChannel)));
			}
			return new RedirectBindingElement.RedirectContainerChannelFactory<TChannel>(context, this);
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
			return new RedirectBindingElement(this);
		}

		public override T GetProperty<T>(BindingContext context)
		where T : class
		{
			return context.GetInnerProperty<T>();
		}

		private class RedirectContainerChannelFactory<TChannel> : ChannelFactoryBase<TChannel>
		{
			private readonly IChannelFactory<TChannel> innerFactory;

			public RedirectBindingElement BindingElement
			{
				get;
				private set;
			}

			public RedirectContainerChannelFactory(BindingContext context, RedirectBindingElement bindingElement) : base(context.Binding)
			{
				this.BindingElement = bindingElement;
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
				return (TChannel)(new RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel((RedirectBindingElement.RedirectContainerChannelFactory<IRequestSessionChannel>)this, address, via));
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

			private class RedirectContainerSessionChannel : ChannelBase, IRequestSessionChannel, IRequestChannel, IChannel, ICommunicationObject, ISessionChannel<IOutputSession>
			{
				private readonly EventHandler onCorrelatorNotifyCleanup;

				private readonly IChannelFactory<IRequestSessionChannel> innerFactory;

				private readonly IRequestSessionChannel primaryChannel;

				private readonly ContainerChannelManager correlatorManager;

				private readonly ConcurrentDictionary<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey, RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkValue> entityMap;

				private readonly RedirectBindingElement bindingElement;

				private ContainerNameResolutionMode containerNameResolutionMode;

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

				public RedirectContainerSessionChannel(RedirectBindingElement.RedirectContainerChannelFactory<IRequestSessionChannel> factory, EndpointAddress address, Uri via) : base(factory)
				{
					this.innerFactory = factory.innerFactory;
					this.bindingElement = factory.BindingElement;
					this.RemoteAddress = address;
					this.Via = via;
					this.entityMap = new ConcurrentDictionary<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey, RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkValue>();
					this.primaryChannel = this.innerFactory.CreateChannel(address, via);
					RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.OutputSession outputSession = new RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.OutputSession()
					{
						Id = this.primaryChannel.Session.Id
					};
					this.Session = outputSession;
					this.onCorrelatorNotifyCleanup = new EventHandler(this.OnCorrelatorNotifyCleanup);
					this.correlatorManager = new ContainerChannelManager(true, this.bindingElement.UseSslStreamSecurity, this.bindingElement.IncludeExceptionDetails, this.bindingElement.EndpointIdentity);
					this.correlatorManager.NotifyCleanup += new EventHandler(this.OnCorrelatorNotifyCleanup);
				}

				public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
				{
					return this.BeginRequest(message, base.DefaultSendTimeout, callback, state);
				}

				public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
				{
					return (new RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult(this, message, timeout, callback, state)).Start();
				}

				public Message EndRequest(IAsyncResult result)
				{
					return AsyncResult<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult>.End(result).ResponseMessage;
				}

				protected override void OnAbort()
				{
					this.correlatorManager.NotifyCleanup -= this.onCorrelatorNotifyCleanup;
					this.primaryChannel.Abort();
					this.correlatorManager.Abort();
				}

				protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
				{
					this.correlatorManager.NotifyCleanup -= this.onCorrelatorNotifyCleanup;
					ICommunicationObject[] communicationObjectArray = new ICommunicationObject[] { this.primaryChannel, this.correlatorManager };
					return new CloseCollectionIteratedAsyncResult(communicationObjectArray, timeout, callback, state);
				}

				protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
				{
					ICommunicationObject[] communicationObjectArray = new ICommunicationObject[] { this.primaryChannel, this.correlatorManager };
					return new OpenCollectionIteratedAsyncResult(communicationObjectArray, timeout, callback, state);
				}

				protected override void OnClose(TimeSpan timeout)
				{
					this.correlatorManager.NotifyCleanup -= this.onCorrelatorNotifyCleanup;
					TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
					this.primaryChannel.Close(timeoutHelper.RemainingTime());
					this.correlatorManager.Close(timeoutHelper.RemainingTime());
				}

				private void OnCorrelatorNotifyCleanup(object sender, EventArgs args)
				{
					RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkValue entityLinkValue;
					Uri channelAddress = (args as NotifyCleanupEventArgs).ChannelAddress;
					IEnumerable<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey> containerLocation = 
						from  in this.entityMap
						where kvp.Value.ContainerLocation == channelAddress.AbsoluteUri
						select kvp.Key;
					foreach (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey entityLinkKey in containerLocation)
					{
						this.entityMap.TryRemove(entityLinkKey, out entityLinkValue);
					}
				}

				protected override void OnEndClose(IAsyncResult result)
				{
					AsyncResult<CloseCollectionIteratedAsyncResult>.End(result);
				}

				protected override void OnEndOpen(IAsyncResult result)
				{
					AsyncResult<OpenCollectionIteratedAsyncResult>.End(result);
				}

				protected override void OnOpen(TimeSpan timeout)
				{
					TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
					this.primaryChannel.Open(timeoutHelper.RemainingTime());
					this.correlatorManager.Open(timeoutHelper.RemainingTime());
				}

				public Message Request(Message message)
				{
					return this.Request(message, base.DefaultSendTimeout);
				}

				public Message Request(Message message, TimeSpan timeout)
				{
					return this.EndRequest(this.BeginRequest(message, timeout, null, null));
				}

				private struct EntityLinkKey : IEquatable<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey>
				{
					private readonly string entityName;

					private readonly string linkId;

					public string EntityName
					{
						get
						{
							return this.entityName;
						}
					}

					public EntityLinkKey(string entityName, string linkId)
					{
						this.entityName = entityName;
						this.linkId = linkId;
					}

					public bool Equals(RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey other)
					{
						if (!StringComparer.OrdinalIgnoreCase.Equals(this.entityName, other.entityName))
						{
							return false;
						}
						return StringComparer.OrdinalIgnoreCase.Equals(this.linkId, other.linkId);
					}

					public override bool Equals(object obj)
					{
						if (!(obj is RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey))
						{
							return false;
						}
						return this.Equals((RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey)obj);
					}

					public override int GetHashCode()
					{
						int hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(this.entityName);
						int num = StringComparer.OrdinalIgnoreCase.GetHashCode(this.linkId);
						return HashCode.CombineHashCodes(hashCode, num);
					}
				}

				private class EntityLinkValue
				{
					public string ContainerLocation
					{
						get;
						private set;
					}

					public LinkInfo UpdatedLinkInfo
					{
						get;
						private set;
					}

					public EntityLinkValue(string containerLocation, LinkInfo updatedLinkInfo)
					{
						this.ContainerLocation = containerLocation;
						this.UpdatedLinkInfo = updatedLinkInfo;
					}
				}

				private sealed class OutputSession : IOutputSession, ISession
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

				private sealed class RequestAsyncResult : IteratorAsyncResult<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult>
				{
					private readonly RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel sessionChannel;

					private readonly Message incomingMessage;

					private readonly string incomingMessageAction;

					public Message ResponseMessage
					{
						get;
						private set;
					}

					public RequestAsyncResult(RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel parent, Message incomingMessage, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
					{
						this.sessionChannel = parent;
						this.incomingMessage = incomingMessage;
						this.incomingMessageAction = this.incomingMessage.Headers.Action;
					}

					protected override IEnumerator<IteratorAsyncResult<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult>.AsyncStep> GetAsyncSteps()
					{
						RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkValue entityLinkValue;
						RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkValue entityLinkValue1;
						string str;
						object obj;
						this.ThrowIfNotSupportedClientSettings();
						Message message = null;
						int num = this.incomingMessage.Headers.FindHeader("Authorization", "http://schemas.microsoft.com/servicebus/2010/08/protocol/");
						string empty = string.Empty;
						if (this.incomingMessage.Properties != null && this.incomingMessage.Properties.TryGetValue("ParentLinkId", out obj))
						{
							empty = (string)obj;
						}
						LinkInfo header = LinkInfo.GetHeader(this.incomingMessage.Headers);
						bool flag = !string.IsNullOrWhiteSpace(empty);
						str = (flag ? empty : header.LinkId);
						RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey entityLinkKey = new RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkKey(header.EntityName, str);
						RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkValue entityLinkValue2 = null;
						IRequestSessionChannel requestSessionChannel = null;
						if (this.sessionChannel.containerNameResolutionMode != ContainerNameResolutionMode.DisableRedirect && !string.IsNullOrEmpty(entityLinkKey.EntityName) && !this.sessionChannel.entityMap.TryGetValue(entityLinkKey, out entityLinkValue2))
						{
							if (!this.sessionChannel.bindingElement.EnableRedirect)
							{
								RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult requestAsyncResult = this;
								IteratorAsyncResult<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult>.BeginCall beginCall = (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sessionChannel.primaryChannel.BeginRequest(thisPtr.incomingMessage, t, c, s);
								IteratorAsyncResult<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult>.EndCall endCall = (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, IAsyncResult r) => message = thisPtr.sessionChannel.primaryChannel.EndRequest(r);
								yield return requestAsyncResult.CallAsync(beginCall, endCall, (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, TimeSpan t) => message = thisPtr.sessionChannel.primaryChannel.Request(thisPtr.incomingMessage, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
							}
							else
							{
								RedirectCommand redirectCommand = new RedirectCommand();
								Message to = Message.CreateMessage(this.incomingMessage.Version, "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpRedirect/Redirect", redirectCommand);
								to.Headers.To = this.incomingMessage.Headers.To;
								header.AddTo(to.Headers);
								if (num >= 0)
								{
									to.Headers.CopyHeaderFrom(this.incomingMessage.Headers, num);
								}
								RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult requestAsyncResult1 = this;
								IteratorAsyncResult<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult>.BeginCall beginCall1 = (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sessionChannel.primaryChannel.BeginRequest(to, t, c, s);
								yield return requestAsyncResult1.CallAsync(beginCall1, (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, IAsyncResult r) => message = thisPtr.sessionChannel.primaryChannel.EndRequest(r), (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, TimeSpan t) => message = thisPtr.sessionChannel.primaryChannel.Request(to, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
							}
							if (message == null || !string.Equals(message.Headers.Action, "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpRedirect/RedirectResponse", StringComparison.OrdinalIgnoreCase))
							{
								if (base.LastAsyncStepException != null)
								{
									goto Label1;
								}
								this.ResponseMessage = message;
								if (!flag && string.Equals(this.incomingMessageAction, "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CloseLink", StringComparison.OrdinalIgnoreCase))
								{
									this.sessionChannel.entityMap.TryRemove(entityLinkKey, out entityLinkValue);
									goto Label0;
								}
								else
								{
									goto Label0;
								}
							}
							else
							{
								LinkInfo linkInfo = LinkInfo.GetHeader(message.Headers);
								RedirectResponseCommand body = message.GetBody<RedirectResponseCommand>();
								entityLinkValue2 = new RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.EntityLinkValue(body.RedirectTo, linkInfo);
								this.sessionChannel.containerNameResolutionMode = body.ContainerNameResolutionMode;
								this.ThrowIfNotSupportedClientSettings();
								if (body.ContainerNameResolutionMode != ContainerNameResolutionMode.DisableRedirect)
								{
									this.sessionChannel.entityMap.TryAdd(entityLinkKey, entityLinkValue2);
								}
							}
						}
					Label1:
						if (entityLinkValue2 == null || this.sessionChannel.containerNameResolutionMode == ContainerNameResolutionMode.DisableRedirect || !this.sessionChannel.bindingElement.EnableRedirect && this.sessionChannel.containerNameResolutionMode == ContainerNameResolutionMode.AllowRedirect)
						{
							requestSessionChannel = this.sessionChannel.primaryChannel;
						}
						else
						{
							RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult requestAsyncResult2 = this;
							IteratorAsyncResult<RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult>.BeginCall beginCall2 = (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.sessionChannel.correlatorManager.BeginGetCorrelator(entityLinkValue2.ContainerLocation, t, c, s);
							yield return requestAsyncResult2.CallAsync(beginCall2, (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, IAsyncResult a) => requestSessionChannel = thisPtr.sessionChannel.correlatorManager.EndGetCorrelator(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
							this.incomingMessage.Headers.RemoveAll("LinkInfo", "http://schemas.microsoft.com/netservices/2011/06/servicebus");
							header.TransferDestinationEntityAddress = entityLinkValue2.UpdatedLinkInfo.TransferDestinationEntityAddress;
							header.TransferDestinationMessagingInstanceHandle = entityLinkValue2.UpdatedLinkInfo.TransferDestinationMessagingInstanceHandle;
							header.TransferDestinationResourceResourceId = entityLinkValue2.UpdatedLinkInfo.TransferDestinationResourceResourceId;
							header.EntityType = entityLinkValue2.UpdatedLinkInfo.EntityType;
							header.EntityName = entityLinkValue2.UpdatedLinkInfo.EntityName;
							header.AddTo(this.incomingMessage.Headers);
						}
						if (!flag && string.Equals(this.incomingMessageAction, "http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CloseLink", StringComparison.OrdinalIgnoreCase))
						{
							this.sessionChannel.entityMap.TryRemove(entityLinkKey, out entityLinkValue1);
						}
						yield return base.CallAsync((RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => requestSessionChannel.BeginRequest(thisPtr.incomingMessage, t, c, s), (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, IAsyncResult r) => thisPtr.ResponseMessage = requestSessionChannel.EndRequest(r), (RedirectBindingElement.RedirectContainerChannelFactory<TChannel>.RedirectContainerSessionChannel.RequestAsyncResult thisPtr, TimeSpan t) => thisPtr.ResponseMessage = requestSessionChannel.Request(thisPtr.incomingMessage, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					Label0:
						yield break;
					}

					private void ThrowIfNotSupportedClientSettings()
					{
						if (!this.sessionChannel.bindingElement.EnableRedirect && this.sessionChannel.containerNameResolutionMode == ContainerNameResolutionMode.AlwaysRedirect)
						{
							base.Complete(new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.SbmpClientMustEnableRedirect, new object[0])));
						}
					}
				}
			}
		}
	}
}