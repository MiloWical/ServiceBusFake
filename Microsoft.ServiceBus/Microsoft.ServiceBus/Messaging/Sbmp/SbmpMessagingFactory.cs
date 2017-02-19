using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class SbmpMessagingFactory : MessagingFactory
	{
		private string connectionId;

		private CreateControlLinkSettings acceptMessageSessionForNamespaceLinkSettings;

		private List<Uri> baseAddresses;

		private int nextLinkId;

		internal Uri BaseAddress
		{
			get;
			private set;
		}

		internal IRequestSessionChannel Channel
		{
			get;
			private set;
		}

		private IChannelFactory<IRequestSessionChannel> ChannelFactory
		{
			get;
			set;
		}

		internal string ConnectionId
		{
			get
			{
				return this.connectionId;
			}
		}

		internal Uri FirstInBaseAddress
		{
			get;
			private set;
		}

		internal System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get;
			private set;
		}

		internal SbmpResourceManager ResourceManager
		{
			get;
			private set;
		}

		internal override IServiceBusSecuritySettings ServiceBusSecuritySettings
		{
			get
			{
				return this.Settings;
			}
		}

		internal NetMessagingTransportSettings Settings
		{
			get;
			private set;
		}

		internal bool ShouldIntialize
		{
			get
			{
				if (this.baseAddresses != null)
				{
					return true;
				}
				return false;
			}
		}

		public SbmpMessagingFactory(IEnumerable<Uri> baseAddresses, NetMessagingTransportSettings settings)
		{
			bool flag;
			ConnectivityMode systemConnectivityMode = ConnectivityModeHelper.SystemConnectivityMode;
			this.connectionId = Guid.NewGuid().ToString("N");
			this.Settings = (NetMessagingTransportSettings)settings.Clone();
			if (systemConnectivityMode == ConnectivityMode.AutoDetect && !this.Settings.GatewayMode)
			{
				this.baseAddresses = new List<Uri>(baseAddresses);
				this.FirstInBaseAddress = this.baseAddresses.First<Uri>();
				return;
			}
			flag = (systemConnectivityMode != ConnectivityMode.Http ? false : true);
			this.Initialize(flag, baseAddresses);
		}

		public SbmpMessagingFactory(Uri baseAddress, NetMessagingTransportSettings settings) : this(new List<Uri>()
		{
			baseAddress
		}, settings)
		{
		}

		private void BaseClose(TimeSpan timeout)
		{
			base.OnClose(timeout);
		}

		private IAsyncResult BaseOnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.OnBeginClose(timeout, callback, state);
		}

		private void BaseOnEndClose(IAsyncResult result)
		{
			base.OnEndClose(result);
		}

		internal EndpointAddress CreateEndpointAddress(string entityName)
		{
			return new EndpointAddress(this.CreateUri(entityName), SbmpProtocolDefaults.GetEndpointIdentity(this.BaseAddress), new AddressHeader[0]);
		}

		internal Uri CreateUri(string path)
		{
			UriBuilder uriBuilder = new UriBuilder(this.BaseAddress);
			MessagingUtilities.EnsureTrailingSlash(uriBuilder);
			UriBuilder uriBuilder1 = uriBuilder;
			uriBuilder1.Path = string.Concat(uriBuilder1.Path, path);
			return uriBuilder.Uri;
		}

		internal string GetNextLinkId()
		{
			int num = Interlocked.Increment(ref this.nextLinkId);
			return num.ToString(CultureInfo.InvariantCulture);
		}

		private void Initialize(bool useWebStream, IEnumerable<Uri> baseAddresses)
		{
			this.Initialize(useWebStream, false, baseAddresses);
		}

		private void Initialize(bool useWebStream, bool useHttpsWebStream, IEnumerable<Uri> baseAddresses)
		{
			List<Uri> uris = new List<Uri>();
			foreach (Uri baseAddress in baseAddresses)
			{
				if (base.Address == null)
				{
					base.Address = baseAddress;
				}
				UriBuilder uriBuilder = new UriBuilder(baseAddress);
				if (!this.Settings.GatewayMode && string.Compare(uriBuilder.Scheme, "sb", StringComparison.OrdinalIgnoreCase) != 0)
				{
					ExceptionTrace exception = Microsoft.ServiceBus.Messaging.FxTrace.Exception;
					string invalidUriScheme = Resources.InvalidUriScheme;
					object[] scheme = new object[] { uriBuilder.Scheme, "sb" };
					throw exception.AsError(new ArgumentException(Microsoft.ServiceBus.SR.GetString(invalidUriScheme, scheme)), null);
				}
				if (!useWebStream)
				{
					uriBuilder.Scheme = SbmpProtocolDefaults.TransportUriScheme;
				}
				else
				{
					uriBuilder.Scheme = "sb";
					uriBuilder.Port = (useHttpsWebStream ? RelayEnvironment.RelayHttpsPort : RelayEnvironment.RelayHttpPort);
				}
				if (!this.Settings.GatewayMode && uriBuilder.Port == -1)
				{
					if (!useWebStream)
					{
						uriBuilder.Port = RelayEnvironment.RelayNmfPort;
					}
					else
					{
						uriBuilder.Port = (useHttpsWebStream ? RelayEnvironment.RelayHttpsPort : RelayEnvironment.RelayHttpPort);
					}
				}
				MessagingUtilities.EnsureTrailingSlash(uriBuilder);
				uris.Add(uriBuilder.Uri);
			}
			this.baseAddresses = null;
			this.BaseAddress = uris.First<Uri>();
			bool useSslStreamSecurity = this.Settings.UseSslStreamSecurity;
			CustomBinding customBinding = SbmpProtocolDefaults.CreateBinding(false, useWebStream, useHttpsWebStream, 2147483647, useSslStreamSecurity, this.Settings.EndpointIdentity);
			DuplexRequestBindingElement duplexRequestBindingElement = new DuplexRequestBindingElement()
			{
				ClientMode = !this.Settings.GatewayMode,
				IncludeExceptionDetails = true
			};
			DuplexRequestBindingElement duplexRequestBindingElement1 = duplexRequestBindingElement;
			int num = 0;
			if (!this.Settings.GatewayMode)
			{
				RedirectBindingElement redirectBindingElement = new RedirectBindingElement()
				{
					EnableRedirect = this.Settings.EnableRedirect,
					UseSslStreamSecurity = useSslStreamSecurity,
					IncludeExceptionDetails = true,
					EndpointIdentity = this.Settings.EndpointIdentity
				};
				RedirectBindingElement redirectBindingElement1 = redirectBindingElement;
				int num1 = num;
				num = num1 + 1;
				customBinding.Elements.Insert(num1, new ReconnectBindingElement(uris));
				int num2 = num;
				num = num2 + 1;
				customBinding.Elements.Insert(num2, redirectBindingElement1);
			}
			int num3 = num;
			num = num3 + 1;
			customBinding.Elements.Insert(num3, new ReconnectBindingElement(uris));
			int num4 = num;
			num = num4 + 1;
			customBinding.Elements.Insert(num4, duplexRequestBindingElement1);
			BindingParameterCollection bindingParameterCollection = new BindingParameterCollection();
			if (useSslStreamSecurity)
			{
				ClientCredentials clientCredential = new ClientCredentials();
				clientCredential.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
				clientCredential.ServiceCertificate.Authentication.CustomCertificateValidator = RetriableCertificateValidator.Instance;
				bindingParameterCollection.Add(clientCredential);
			}
			this.ChannelFactory = customBinding.BuildChannelFactory<IRequestSessionChannel>(bindingParameterCollection);
			this.MessageVersion = customBinding.MessageVersion;
			this.ResourceManager = SbmpResourceManager.Instance;
			this.acceptMessageSessionForNamespaceLinkSettings = new CreateControlLinkSettings(this, string.Empty, "||", MessagingEntityType.Namespace, null);
			EventHandler eventHandler = new EventHandler(this.OnInnerFactoryFaulted);
			this.ChannelFactory.SafeAddFaulted(eventHandler);
		}

		protected override void OnAbort()
		{
			base.OnAbort();
			IRequestSessionChannel channel = this.Channel;
			if (channel != null)
			{
				channel.Abort();
			}
			if (this.ChannelFactory != null)
			{
				this.ChannelFactory.Abort();
			}
		}

		protected override IAsyncResult OnBeginAcceptMessageSession(ReceiveMode receiveMode, TimeSpan serverWaitTime, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult acceptMessageSessionForNamespaceAsyncResult;
			if (this.Settings.EnableRedirect)
			{
				throw new NotSupportedException(Microsoft.ServiceBus.SR.GetString(Resources.AcceptMessageSessionRedirectNotSupported, new object[0]));
			}
			try
			{
				acceptMessageSessionForNamespaceAsyncResult = new AcceptMessageSessionForNamespaceAsyncResult(this, receiveMode, this.PrefetchCount, this.acceptMessageSessionForNamespaceLinkSettings, serverWaitTime, timeout, callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException), null);
			}
			return acceptMessageSessionForNamespaceAsyncResult;
		}

		protected override IAsyncResult OnBeginAcceptSessionReceiver(string entityName, string sessionId, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult acceptMessageSessionAsyncResult;
			try
			{
				MessagingEntityType? nullable = null;
				acceptMessageSessionAsyncResult = new Microsoft.ServiceBus.Messaging.Sbmp.AcceptMessageSessionAsyncResult(this, entityName, sessionId, nullable, receiveMode, Constants.DefaultMessageSessionPrefetchCount, null, timeout, timeout, callback, state);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return acceptMessageSessionAsyncResult;
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			try
			{
				asyncResult = (new SbmpMessagingFactory.CloseAsyncResult(this, timeout, callback, state)).Start();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
			return asyncResult;
		}

		protected override IAsyncResult OnBeginCreateMessageReceiver(string entityName, ReceiveMode receiveMode, TimeSpan timeout, AsyncCallback callback, object state)
		{
			MessagingEntityType? nullable = null;
			CreateReceiverLinkSettings createReceiverLinkSetting = new CreateReceiverLinkSettings(this, entityName, entityName, nullable, receiveMode, null, false);
			return new CompletedAsyncResult<SbmpMessageReceiver>(createReceiverLinkSetting.MessageReceiver, callback, state);
		}

		protected override IAsyncResult OnBeginCreateMessageSender(string transferDestinationEntityName, string viaEntityName, TimeSpan timeout, AsyncCallback callback, object state)
		{
			CreateSenderLinkSettings createSenderLinkSetting = new CreateSenderLinkSettings(this, viaEntityName, null, transferDestinationEntityName);
			return new CompletedAsyncResult<SbmpMessageSender>(createSenderLinkSetting.MessageSender, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object asyncState)
		{
			IAsyncResult asyncResult;
			try
			{
				asyncResult = (new SbmpMessagingFactory.OpenAsyncResult(this, timeout, callback, asyncState)).Start();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return asyncResult;
		}

		protected override void OnClose(TimeSpan timeout)
		{
			try
			{
				(new SbmpMessagingFactory.CloseAsyncResult(this, timeout, null, null)).RunSynchronously();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
		}

		protected override EventHubClient OnCreateEventHubClient(string path)
		{
			MessagingUtilities.ThrowIfContainsSubQueueName(path);
			return new SbmpEventHubClient(this, path);
		}

		protected override QueueClient OnCreateQueueClient(string path, ReceiveMode receiveMode)
		{
			return new SbmpQueueClient(this, path, receiveMode);
		}

		protected override SubscriptionClient OnCreateSubscriptionClient(string topicPath, string name, ReceiveMode receiveMode)
		{
			MessagingUtilities.ThrowIfContainsSubQueueName(topicPath);
			return new SbmpSubscriptionClient(this, topicPath, name, receiveMode);
		}

		protected override TopicClient OnCreateTopicClient(string path)
		{
			MessagingUtilities.ThrowIfContainsSubQueueName(path);
			return new SbmpTopicClient(this, path);
		}

		internal override VolatileTopicClient OnCreateVolatileTopicClient(string path, string clientId, Filter filter)
		{
			return new SbmpVolatileTopicClient(this, path, clientId, base.RetryPolicy, filter);
		}

		protected override MessageSession OnEndAcceptMessageSession(IAsyncResult result)
		{
			MessageSession messageSession;
			try
			{
				messageSession = AcceptMessageSessionForNamespaceAsyncResult.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException), null);
			}
			return messageSession;
		}

		protected override MessageSession OnEndAcceptSessionReceiver(IAsyncResult result)
		{
			MessageSession messageSession;
			try
			{
				messageSession = Microsoft.ServiceBus.Messaging.Sbmp.AcceptMessageSessionAsyncResult.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
			return messageSession;
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpMessagingFactory.CloseAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, false), null);
			}
		}

		protected override MessageReceiver OnEndCreateMessageReceiver(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageReceiver>.End(result);
		}

		protected override MessageSender OnEndCreateMessageSender(IAsyncResult result)
		{
			return CompletedAsyncResult<SbmpMessageSender>.End(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			try
			{
				AsyncResult<SbmpMessagingFactory.OpenAsyncResult>.End(result);
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
		}

		private void OnInnerFactoryFaulted(object sender, EventArgs e)
		{
			base.Fault();
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			try
			{
				(new SbmpMessagingFactory.OpenAsyncResult(this, timeout, null, null)).RunSynchronously();
			}
			catch (CommunicationException communicationException1)
			{
				CommunicationException communicationException = communicationException1;
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(MessagingExceptionHelper.Unwrap(communicationException, base.IsClosedOrClosing), null);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception) && base.IsClosedOrClosing)
				{
					throw new OperationCanceledException(SRClient.EntityClosedOrAborted, exception);
				}
				throw;
			}
		}

		public void ScheduleGetRuntimeEntityDescription(TrackingContext trackingContext, MessageClientEntity clientEntity, string entityName, SbmpMessageCreator messageCreator)
		{
			Microsoft.ServiceBus.Messaging.RuntimeEntityDescription runtimeEntityDescription;
			if (!this.Settings.GatewayMode && clientEntity.RuntimeEntityDescription == null && !this.Settings.EnableRedirect && base.IsOpened)
			{
				string str = string.Concat(this.BaseAddress.AbsoluteUri, entityName);
				if (RuntimeEntityDescriptionCache.TryGet(str, out runtimeEntityDescription))
				{
					clientEntity.RuntimeEntityDescription = runtimeEntityDescription;
					return;
				}
				IOThreadScheduler.ScheduleCallbackNoFlow((object s) => (new GetRuntimeEntityDescriptionAsyncResult(trackingContext, clientEntity, str, this, messageCreator, false, Constants.GetRuntimeEntityDescriptionTimeout, null, null)).Start(), null);
			}
		}

		private sealed class CloseAsyncResult : IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>
		{
			private readonly SbmpMessagingFactory factory;

			private readonly IRequestSessionChannel channel;

			public CloseAsyncResult(SbmpMessagingFactory factory, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
				this.channel = factory.Channel;
			}

			private bool ContinueAfterCommunicationException(Exception exception)
			{
				if (exception == null)
				{
					return true;
				}
				CommunicationException communicationException = exception as CommunicationException;
				if (communicationException == null)
				{
					base.Complete(exception);
				}
				else
				{
					base.Complete(MessagingExceptionHelper.Unwrap(communicationException, false));
				}
				return !base.IsCompleted;
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				SbmpMessagingFactory.CloseAsyncResult closeAsyncResult = this;
				IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>.BeginCall beginCall = (SbmpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.BaseOnBeginClose(t, c, s);
				IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>.EndCall endCall = (SbmpMessagingFactory.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.factory.BaseOnEndClose(r);
				yield return closeAsyncResult.CallAsync(beginCall, endCall, (SbmpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t) => thisPtr.factory.BaseClose(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (this.ContinueAfterCommunicationException(base.LastAsyncStepException))
				{
					if (this.channel != null)
					{
						SbmpMessagingFactory.CloseAsyncResult closeAsyncResult1 = this;
						IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>.BeginCall beginCall1 = (SbmpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.channel.BeginClose(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout), c, s);
						IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>.EndCall endCall1 = (SbmpMessagingFactory.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.channel.EndClose(r);
						yield return closeAsyncResult1.CallAsync(beginCall1, endCall1, (SbmpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t) => thisPtr.channel.Close(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout)), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (!this.ContinueAfterCommunicationException(base.LastAsyncStepException))
						{
							goto Label0;
						}
					}
					if (this.factory.ChannelFactory != null)
					{
						SbmpMessagingFactory.CloseAsyncResult closeAsyncResult2 = this;
						IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>.BeginCall beginCall2 = (SbmpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.ChannelFactory.BeginClose(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout), c, s);
						IteratorAsyncResult<SbmpMessagingFactory.CloseAsyncResult>.EndCall endCall2 = (SbmpMessagingFactory.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.factory.ChannelFactory.EndClose(r);
						yield return closeAsyncResult2.CallAsync(beginCall2, endCall2, (SbmpMessagingFactory.CloseAsyncResult thisPtr, TimeSpan t) => thisPtr.factory.ChannelFactory.Close(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout)), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					}
					this.ContinueAfterCommunicationException(base.LastAsyncStepException);
				}
			Label0:
				yield break;
			}
		}

		private sealed class OpenAsyncResult : IteratorAsyncResult<SbmpMessagingFactory.OpenAsyncResult>
		{
			private readonly SbmpMessagingFactory factory;

			public OpenAsyncResult(SbmpMessagingFactory factory, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.factory = factory;
			}

			private bool ContinueAfterCommunicationException(Exception exception)
			{
				if (exception == null)
				{
					return true;
				}
				CommunicationException communicationException = exception as CommunicationException;
				if (communicationException == null)
				{
					base.Complete(exception);
				}
				else
				{
					base.Complete(MessagingExceptionHelper.Unwrap(communicationException, false));
				}
				return !base.IsCompleted;
			}

			protected override IEnumerator<IteratorAsyncResult<SbmpMessagingFactory.OpenAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (this.factory.ShouldIntialize)
				{
					SbmpMessagingFactory.OpenAsyncResult openAsyncResult = this;
					IteratorAsyncResult<SbmpMessagingFactory.OpenAsyncResult>.BeginCall beginCall = (SbmpMessagingFactory.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => Microsoft.ServiceBus.Messaging.NetworkDetector.BeginCheckTcp(thisPtr.factory.baseAddresses, SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout), c, s);
					yield return openAsyncResult.CallAsync(beginCall, (SbmpMessagingFactory.OpenAsyncResult thisPtr, IAsyncResult r) => thisPtr.factory.Initialize(!Microsoft.ServiceBus.Messaging.NetworkDetector.EndCheckTcp(r), true, thisPtr.factory.baseAddresses), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				}
				SbmpMessagingFactory.OpenAsyncResult openAsyncResult1 = this;
				IteratorAsyncResult<SbmpMessagingFactory.OpenAsyncResult>.BeginCall beginCall1 = (SbmpMessagingFactory.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.ChannelFactory.BeginOpen(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout), c, s);
				IteratorAsyncResult<SbmpMessagingFactory.OpenAsyncResult>.EndCall endCall = (SbmpMessagingFactory.OpenAsyncResult thisPtr, IAsyncResult r) => thisPtr.factory.ChannelFactory.EndOpen(r);
				yield return openAsyncResult1.CallAsync(beginCall1, endCall, (SbmpMessagingFactory.OpenAsyncResult thisPtr, TimeSpan t) => thisPtr.factory.ChannelFactory.Open(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout)), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (this.ContinueAfterCommunicationException(base.LastAsyncStepException))
				{
					EndpointAddress endpointAddress = this.factory.CreateEndpointAddress(string.Empty);
					this.factory.Channel = this.factory.ChannelFactory.CreateChannel(endpointAddress);
					SbmpMessagingFactory.OpenAsyncResult openAsyncResult2 = this;
					IteratorAsyncResult<SbmpMessagingFactory.OpenAsyncResult>.BeginCall beginCall2 = (SbmpMessagingFactory.OpenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.Channel.BeginOpen(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout), c, s);
					IteratorAsyncResult<SbmpMessagingFactory.OpenAsyncResult>.EndCall endCall1 = (SbmpMessagingFactory.OpenAsyncResult thisPtr, IAsyncResult r) => thisPtr.factory.Channel.EndOpen(r);
					yield return openAsyncResult2.CallAsync(beginCall2, endCall1, (SbmpMessagingFactory.OpenAsyncResult thisPtr, TimeSpan t) => thisPtr.factory.Channel.Open(SbmpProtocolDefaults.BufferTimeout(t, thisPtr.factory.GetSettings().EnableAdditionalClientTimeout)), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					this.ContinueAfterCommunicationException(base.LastAsyncStepException);
				}
			}
		}
	}
}