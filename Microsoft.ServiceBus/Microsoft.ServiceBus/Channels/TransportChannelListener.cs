using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class TransportChannelListener : ChannelListenerBase, Microsoft.ServiceBus.Channels.ITransportFactorySettings, IDefaultCommunicationTimeouts, ICommunicationObjectInternals, ICommunicationObject
	{
		private static string exactGeneratedAddressPrefix;

		private static string strongWildcardGeneratedAddressPrefix;

		private static string weakWildcardGeneratedAddressPrefix;

		private static object staticLock;

		private System.Uri baseUri;

		private System.ServiceModel.Channels.BufferManager bufferManager;

		private HostNameComparisonMode hostNameComparisonMode;

		private bool inheritBaseAddressSettings;

		private bool manualAddressing;

		private long maxBufferPoolSize;

		private long maxReceivedMessageSize;

		private System.ServiceModel.Channels.MessageEncoderFactory messageEncoderFactory;

		private System.ServiceModel.Channels.MessageVersion messageVersion;

		private System.Uri uri;

		private string hostedVirtualPath;

		private MessageReceivedCallback messageReceivedCallback;

		private ServiceSecurityAuditBehavior auditBehavior;

		private Microsoft.ServiceBus.Diagnostics.ServiceModelActivity activity;

		private Microsoft.ServiceBus.Channels.TransportManagerContainer transportManagerContainer;

		internal ServiceSecurityAuditBehavior AuditBehavior
		{
			get
			{
				return this.auditBehavior;
			}
		}

		internal System.Uri BaseUri
		{
			get
			{
				return this.baseUri;
			}
		}

		public System.ServiceModel.Channels.BufferManager BufferManager
		{
			get
			{
				return this.bufferManager;
			}
		}

		private string GeneratedAddressPrefix
		{
			get
			{
				switch (this.hostNameComparisonMode)
				{
					case HostNameComparisonMode.StrongWildcard:
					{
						return Microsoft.ServiceBus.Channels.TransportChannelListener.GetGeneratedAddressPrefix(ref Microsoft.ServiceBus.Channels.TransportChannelListener.strongWildcardGeneratedAddressPrefix);
					}
					case HostNameComparisonMode.Exact:
					{
						return Microsoft.ServiceBus.Channels.TransportChannelListener.GetGeneratedAddressPrefix(ref Microsoft.ServiceBus.Channels.TransportChannelListener.exactGeneratedAddressPrefix);
					}
					case HostNameComparisonMode.WeakWildcard:
					{
						return Microsoft.ServiceBus.Channels.TransportChannelListener.GetGeneratedAddressPrefix(ref Microsoft.ServiceBus.Channels.TransportChannelListener.weakWildcardGeneratedAddressPrefix);
					}
				}
				Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert("invalid HostnameComparisonMode value");
				return null;
			}
		}

		internal string HostedVirtualPath
		{
			get
			{
				return this.hostedVirtualPath;
			}
		}

		internal HostNameComparisonMode HostNameComparisonModeInternal
		{
			get
			{
				return this.hostNameComparisonMode;
			}
		}

		internal bool InheritBaseAddressSettings
		{
			get
			{
				return this.inheritBaseAddressSettings;
			}
			set
			{
				this.inheritBaseAddressSettings = value;
			}
		}

		public bool ManualAddressing
		{
			get
			{
				return this.manualAddressing;
			}
		}

		public long MaxBufferPoolSize
		{
			get
			{
				return this.maxBufferPoolSize;
			}
		}

		public virtual long MaxReceivedMessageSize
		{
			get
			{
				return this.maxReceivedMessageSize;
			}
		}

		public System.ServiceModel.Channels.MessageEncoderFactory MessageEncoderFactory
		{
			get
			{
				return this.messageEncoderFactory;
			}
		}

		public System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get
			{
				return this.messageVersion;
			}
		}

		System.ServiceModel.Channels.BufferManager Microsoft.ServiceBus.Channels.ITransportFactorySettings.BufferManager
		{
			get
			{
				return this.BufferManager;
			}
		}

		bool Microsoft.ServiceBus.Channels.ITransportFactorySettings.ManualAddressing
		{
			get
			{
				return this.ManualAddressing;
			}
		}

		long Microsoft.ServiceBus.Channels.ITransportFactorySettings.MaxReceivedMessageSize
		{
			get
			{
				return this.MaxReceivedMessageSize;
			}
		}

		System.ServiceModel.Channels.MessageEncoderFactory Microsoft.ServiceBus.Channels.ITransportFactorySettings.MessageEncoderFactory
		{
			get
			{
				return this.MessageEncoderFactory;
			}
		}

		public abstract string Scheme
		{
			get;
		}

		internal Microsoft.ServiceBus.Diagnostics.ServiceModelActivity ServiceModelActivity
		{
			get
			{
				return this.activity;
			}
			set
			{
				this.activity = value;
			}
		}

		internal abstract Microsoft.ServiceBus.Channels.UriPrefixTable<Microsoft.ServiceBus.Channels.ITransportManagerRegistration> TransportManagerTable
		{
			get;
		}

		public override System.Uri Uri
		{
			get
			{
				return this.uri;
			}
		}

		static TransportChannelListener()
		{
			Microsoft.ServiceBus.Channels.TransportChannelListener.staticLock = new object();
		}

		protected TransportChannelListener(TransportBindingElement bindingElement, BindingContext context) : this(bindingElement, context, Microsoft.ServiceBus.Channels.TransportDefaults.GetDefaultMessageEncoderFactory())
		{
		}

		protected TransportChannelListener(TransportBindingElement bindingElement, BindingContext context, System.ServiceModel.Channels.MessageEncoderFactory defaultMessageEncoderFactory) : this(bindingElement, context, defaultMessageEncoderFactory, HostNameComparisonMode.Exact)
		{
		}

		protected TransportChannelListener(TransportBindingElement bindingElement, BindingContext context, HostNameComparisonMode hostNameComparisonMode) : this(bindingElement, context, Microsoft.ServiceBus.Channels.TransportDefaults.GetDefaultMessageEncoderFactory(), hostNameComparisonMode)
		{
		}

		protected TransportChannelListener(TransportBindingElement bindingElement, BindingContext context, System.ServiceModel.Channels.MessageEncoderFactory defaultMessageEncoderFactory, HostNameComparisonMode hostNameComparisonMode) : base(context.Binding)
		{
			Microsoft.ServiceBus.Channels.HostNameComparisonModeHelper.Validate(hostNameComparisonMode);
			this.hostNameComparisonMode = hostNameComparisonMode;
			this.manualAddressing = bindingElement.ManualAddressing;
			this.maxBufferPoolSize = bindingElement.MaxBufferPoolSize;
			this.maxReceivedMessageSize = bindingElement.MaxReceivedMessageSize;
			Collection<MessageEncodingBindingElement> messageEncodingBindingElements = context.BindingParameters.FindAll<MessageEncodingBindingElement>();
			if (messageEncodingBindingElements.Count > 1)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.MultipleMebesInParameters, new object[0])));
			}
			if (messageEncodingBindingElements.Count != 1)
			{
				this.messageEncoderFactory = defaultMessageEncoderFactory;
			}
			else
			{
				this.messageEncoderFactory = messageEncodingBindingElements[0].CreateMessageEncoderFactory();
				context.BindingParameters.Remove<MessageEncodingBindingElement>();
			}
			if (this.messageEncoderFactory == null)
			{
				this.messageVersion = System.ServiceModel.Channels.MessageVersion.None;
			}
			else
			{
				this.messageVersion = this.messageEncoderFactory.MessageVersion;
			}
			ServiceSecurityAuditBehavior serviceSecurityAuditBehavior = context.BindingParameters.Find<ServiceSecurityAuditBehavior>();
			if (serviceSecurityAuditBehavior == null)
			{
				this.auditBehavior = new ServiceSecurityAuditBehavior();
			}
			else
			{
				this.auditBehavior = (ServiceSecurityAuditBehavior)InvokeHelper.InvokeInstanceMethod(typeof(ServiceSecurityAuditBehavior), serviceSecurityAuditBehavior, "Clone", new object[0]);
			}
			if (context.ListenUriMode == ListenUriMode.Unique && context.ListenUriBaseAddress == null)
			{
				UriBuilder uriBuilder = new UriBuilder(this.Scheme, Microsoft.ServiceBus.Channels.DnsCache.MachineName)
				{
					Path = this.GeneratedAddressPrefix
				};
				context.ListenUriBaseAddress = uriBuilder.Uri;
			}
			Microsoft.ServiceBus.Channels.UriSchemeKeyedCollection.ValidateBaseAddress(context.ListenUriBaseAddress, "baseAddress");
			if (context.ListenUriBaseAddress.Scheme != this.Scheme && !context.ListenUriBaseAddress.Scheme.Equals("sbwss") && string.Compare(context.ListenUriBaseAddress.Scheme, this.Scheme, StringComparison.OrdinalIgnoreCase) != 0)
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string invalidUriScheme = Resources.InvalidUriScheme;
				object[] scheme = new object[] { context.ListenUriBaseAddress.Scheme, this.Scheme };
				throw exceptionUtility.ThrowHelperArgument("context.ListenUriBaseAddress", Microsoft.ServiceBus.SR.GetString(invalidUriScheme, scheme));
			}
			Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.DebugAssert(context.ListenUriRelativeAddress != null, "");
			if (context.ListenUriMode != ListenUriMode.Explicit)
			{
				string listenUriRelativeAddress = context.ListenUriRelativeAddress;
				if (listenUriRelativeAddress.Length > 0 && !listenUriRelativeAddress.EndsWith("/", StringComparison.Ordinal))
				{
					listenUriRelativeAddress = string.Concat(listenUriRelativeAddress, "/");
				}
				System.Uri listenUriBaseAddress = context.ListenUriBaseAddress;
				Guid guid = Guid.NewGuid();
				this.SetUri(listenUriBaseAddress, string.Concat(listenUriRelativeAddress, guid.ToString()));
			}
			else
			{
				this.SetUri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
			}
			this.transportManagerContainer = new Microsoft.ServiceBus.Channels.TransportManagerContainer(this);
		}

		internal virtual void ApplyHostedContext(VirtualPathExtension virtualPathExtension, bool isMetadataListener)
		{
			this.hostedVirtualPath = virtualPathExtension.VirtualPath;
		}

		internal virtual Microsoft.ServiceBus.Channels.ITransportManagerRegistration CreateTransportManagerRegistration()
		{
			return this.CreateTransportManagerRegistration(this.BaseUri);
		}

		internal abstract Microsoft.ServiceBus.Channels.ITransportManagerRegistration CreateTransportManagerRegistration(System.Uri listenUri);

		internal static void FixIpv6Hostname(UriBuilder uriBuilder, System.Uri originalUri)
		{
			if (originalUri.HostNameType == UriHostNameType.IPv6)
			{
				string dnsSafeHost = originalUri.DnsSafeHost;
				uriBuilder.Host = string.Concat("[", dnsSafeHost, "]");
			}
		}

		private static string GetGeneratedAddressPrefix(ref string generatedAddressPrefix)
		{
			if (generatedAddressPrefix == null)
			{
				lock (Microsoft.ServiceBus.Channels.TransportChannelListener.staticLock)
				{
					if (generatedAddressPrefix == null)
					{
						Guid guid = Guid.NewGuid();
						generatedAddressPrefix = string.Concat("Temporary_Listen_Addresses/", guid.ToString());
					}
				}
			}
			return generatedAddressPrefix;
		}

		internal virtual int GetMaxBufferSize()
		{
			if (this.MaxReceivedMessageSize > (long)2147483647)
			{
				return 2147483647;
			}
			return (int)this.MaxReceivedMessageSize;
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (typeof(T) == typeof(System.ServiceModel.Channels.MessageVersion))
			{
				return (T)this.MessageVersion;
			}
			if (typeof(T) != typeof(FaultConverter))
			{
				return base.GetProperty<T>();
			}
			if (this.MessageEncoderFactory == null)
			{
				return default(T);
			}
			return this.MessageEncoderFactory.Encoder.GetProperty<T>();
		}

		internal Microsoft.ServiceBus.Channels.TransportManagerContainer GetTransportManagers()
		{
			return Microsoft.ServiceBus.Channels.TransportManagerContainer.TransferTransportManagers(this.transportManagerContainer);
		}

		internal bool IsScopeIdCompatible(HostNameComparisonMode hostNameComparisonMode, System.Uri uri)
		{
			if (this.hostNameComparisonMode != hostNameComparisonMode)
			{
				return false;
			}
			if (hostNameComparisonMode == HostNameComparisonMode.Exact && uri.HostNameType == UriHostNameType.IPv6)
			{
				if (this.Uri.HostNameType != UriHostNameType.IPv6)
				{
					return false;
				}
				IPAddress pAddress = IPAddress.Parse(this.Uri.DnsSafeHost);
				IPAddress pAddress1 = IPAddress.Parse(uri.DnsSafeHost);
				if (pAddress.ScopeId != pAddress1.ScopeId)
				{
					return false;
				}
			}
			return true;
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposed()
		{
			base.ThrowIfDisposed();
		}

		void Microsoft.ServiceBus.Messaging.Channels.ICommunicationObjectInternals.ThrowIfDisposedOrNotOpen()
		{
			base.ThrowIfDisposedOrNotOpen();
		}

		protected override void OnAbort()
		{
			this.transportManagerContainer.Close(this.DefaultCloseTimeout);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return this.transportManagerContainer.BeginClose(timeout, callback, state);
		}

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			Microsoft.ServiceBus.Channels.TransportChannelListener transportChannelListener = this;
			return this.transportManagerContainer.BeginOpen(timeout, new Microsoft.ServiceBus.Channels.SelectTransportManagersCallback(transportChannelListener.SelectTransportManagers), callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.transportManagerContainer.Close(timeout);
		}

		protected override void OnClosed()
		{
			base.OnClosed();
			if (this.bufferManager != null)
			{
				this.bufferManager.Clear();
			}
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			this.transportManagerContainer.EndClose(result);
		}

		protected override void OnEndOpen(IAsyncResult result)
		{
			this.transportManagerContainer.EndOpen(result);
		}

		protected override void OnOpen(TimeSpan timeout)
		{
			Microsoft.ServiceBus.Channels.TransportChannelListener transportChannelListener = this;
			this.transportManagerContainer.Open(timeout, new Microsoft.ServiceBus.Channels.SelectTransportManagersCallback(transportChannelListener.SelectTransportManagers));
		}

		protected override void OnOpened()
		{
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
			{
				TraceUtility.TraceEvent(TraceEventType.Verbose, TraceCode.OpenedListener, new Microsoft.ServiceBus.Channels.UriTraceRecord(this.Uri), this, null);
			}
			base.OnOpened();
		}

		protected override void OnOpening()
		{
			base.OnOpening();
			this.bufferManager = System.ServiceModel.Channels.BufferManager.CreateBufferManager(this.MaxBufferPoolSize, this.GetMaxBufferSize());
		}

		internal void RaiseMessageReceived()
		{
			if (this.messageReceivedCallback != null)
			{
				this.messageReceivedCallback();
			}
		}

		internal virtual IList<Microsoft.ServiceBus.Channels.TransportManager> SelectTransportManagers()
		{
			Microsoft.ServiceBus.Channels.ITransportManagerRegistration transportManagerRegistration;
			IList<Microsoft.ServiceBus.Channels.TransportManager> transportManagers = null;
			if (!this.TryGetTransportManagerRegistration(out transportManagerRegistration))
			{
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceVerbose)
				{
					TraceUtility.TraceEvent(TraceEventType.Verbose, TraceCode.NoExistingTransportManager, new Microsoft.ServiceBus.Channels.UriTraceRecord(this.Uri), this, null);
				}
				if (this.HostedVirtualPath == null)
				{
					transportManagerRegistration = this.CreateTransportManagerRegistration();
					this.TransportManagerTable.RegisterUri(transportManagerRegistration.ListenUri, this.hostNameComparisonMode, transportManagerRegistration);
				}
			}
			if (transportManagerRegistration != null)
			{
				transportManagers = transportManagerRegistration.Select(this);
				if (transportManagers == null)
				{
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.IncompatibleExistingTransportManager, new Microsoft.ServiceBus.Channels.UriTraceRecord(this.Uri), this, null);
					}
					if (this.HostedVirtualPath == null)
					{
						System.Uri uri = this.Uri;
						if (uri != null)
						{
							transportManagerRegistration = this.CreateTransportManagerRegistration(uri);
							this.TransportManagerTable.RegisterUri(uri, this.hostNameComparisonMode, transportManagerRegistration);
							transportManagers = transportManagerRegistration.Select(this);
						}
					}
				}
			}
			if (transportManagers == null)
			{
				this.ThrowTransportManagersNotFound();
			}
			return transportManagers;
		}

		internal void SetMessageReceivedCallback(MessageReceivedCallback messageReceivedCallback)
		{
			this.messageReceivedCallback = messageReceivedCallback;
		}

		protected void SetUri(System.Uri baseAddress, string relativeAddress)
		{
			System.Uri uri = baseAddress;
			if (relativeAddress != string.Empty)
			{
				if (!baseAddress.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
				{
					UriBuilder uriBuilder = new UriBuilder(baseAddress);
					Microsoft.ServiceBus.Channels.TransportChannelListener.FixIpv6Hostname(uriBuilder, baseAddress);
					uriBuilder.Path = string.Concat(uriBuilder.Path, "/");
					baseAddress = uriBuilder.Uri;
				}
				uri = new System.Uri(baseAddress, relativeAddress);
				if (!baseAddress.IsBaseOf(uri))
				{
					baseAddress = uri;
				}
			}
			if (!baseAddress.Scheme.Equals("sbwss"))
			{
				this.baseUri = baseAddress;
				this.ValidateUri(uri);
				this.uri = uri;
				return;
			}
			UriBuilder uriBuilder1 = new UriBuilder(baseAddress)
			{
				Scheme = "sb",
				Port = RelayEnvironment.RelayHttpsPort
			};
			UriBuilder uriBuilder2 = uriBuilder1;
			this.baseUri = uriBuilder2.Uri;
			UriBuilder uriBuilder3 = new UriBuilder(uri)
			{
				Scheme = "sb",
				Port = RelayEnvironment.RelayHttpsPort
			};
			uriBuilder2 = uriBuilder3;
			this.ValidateUri(uriBuilder2.Uri);
			this.uri = uriBuilder2.Uri;
		}

		private void ThrowTransportManagersNotFound()
		{
			if (this.HostedVirtualPath != null)
			{
				if (string.Compare(this.Uri.Scheme, System.Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(this.Uri.Scheme, System.Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) == 0)
				{
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string hostingNoHttpTransportManagerForUri = Resources.Hosting_NoHttpTransportManagerForUri;
					object[] uri = new object[] { this.Uri };
					throw exceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(hostingNoHttpTransportManagerForUri, uri)));
				}
				if (string.Compare(this.Uri.Scheme, System.Uri.UriSchemeNetTcp, StringComparison.OrdinalIgnoreCase) == 0 || string.Compare(this.Uri.Scheme, System.Uri.UriSchemeNetPipe, StringComparison.OrdinalIgnoreCase) == 0)
				{
					ExceptionUtility exceptionUtility1 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string hostingNoTcpPipeTransportManagerForUri = Resources.Hosting_NoTcpPipeTransportManagerForUri;
					object[] objArray = new object[] { this.Uri };
					throw exceptionUtility1.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(hostingNoTcpPipeTransportManagerForUri, objArray)));
				}
			}
			ExceptionUtility exceptionUtility2 = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
			string noCompatibleTransportManagerForUri = Resources.NoCompatibleTransportManagerForUri;
			object[] uri1 = new object[] { this.Uri };
			throw exceptionUtility2.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(noCompatibleTransportManagerForUri, uri1)));
		}

		private bool TryGetTransportManagerRegistration(out Microsoft.ServiceBus.Channels.ITransportManagerRegistration registration)
		{
			if (!this.InheritBaseAddressSettings)
			{
				return this.TryGetTransportManagerRegistration(this.hostNameComparisonMode, out registration);
			}
			if (this.TryGetTransportManagerRegistration(HostNameComparisonMode.StrongWildcard, out registration))
			{
				return true;
			}
			if (this.TryGetTransportManagerRegistration(HostNameComparisonMode.Exact, out registration))
			{
				return true;
			}
			if (this.TryGetTransportManagerRegistration(HostNameComparisonMode.WeakWildcard, out registration))
			{
				return true;
			}
			registration = null;
			return false;
		}

		protected virtual bool TryGetTransportManagerRegistration(HostNameComparisonMode hostNameComparisonMode, out Microsoft.ServiceBus.Channels.ITransportManagerRegistration registration)
		{
			return this.TransportManagerTable.TryLookupUri(this.Uri, hostNameComparisonMode, out registration);
		}

		protected virtual void ValidateUri(System.Uri uri)
		{
		}
	}
}