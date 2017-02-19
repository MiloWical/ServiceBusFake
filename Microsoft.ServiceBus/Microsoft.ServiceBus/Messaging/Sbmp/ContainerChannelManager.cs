using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.Threading;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class ContainerChannelManager : SingletonDictionaryManager<string, IRequestSessionChannel>
	{
		protected IChannelFactory<IRequestSessionChannel> defaultChannelFactory;

		protected IChannelFactory<IRequestSessionChannel> securedChannelFactory;

		private readonly bool clientMode;

		private readonly EventHandler onInnerChannelFaulted;

		public System.ServiceModel.Channels.MessageVersion MessageVersion
		{
			get;
			private set;
		}

		public ContainerChannelManager(bool clientMode, bool useSslStreamSecurity, bool includeExceptionDetails, DnsEndpointIdentity endpointIdentity)
		{
			this.clientMode = clientMode;
			this.onInnerChannelFaulted = new EventHandler(this.OnInnerChannelFaulted);
			this.defaultChannelFactory = this.CreateChannelFactory(useSslStreamSecurity, includeExceptionDetails, endpointIdentity);
			this.securedChannelFactory = this.CreateChannelFactory(true, includeExceptionDetails, endpointIdentity);
			this.defaultChannelFactory.Open();
			this.securedChannelFactory.Open();
		}

		public IAsyncResult BeginGetCorrelator(string containerUri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.BeginLoadInstance(containerUri, null, timeout, callback, state);
		}

		public IAsyncResult BeginGetCorrelator(string containerUri, bool secured, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return base.BeginLoadInstance(containerUri, secured, timeout, callback, state);
		}

		private IChannelFactory<IRequestSessionChannel> CreateChannelFactory(bool useSslStreamSecurity, bool includeExceptionDetails, DnsEndpointIdentity endpointIdentity)
		{
			string str;
			int num = 0;
			CustomBinding customBinding = SbmpProtocolDefaults.CreateBinding(false, false, 2147483647, useSslStreamSecurity, endpointIdentity);
			DuplexRequestBindingElement duplexRequestBindingElement = new DuplexRequestBindingElement()
			{
				IncludeExceptionDetails = includeExceptionDetails,
				ClientMode = this.clientMode
			};
			DuplexRequestBindingElement duplexRequestBindingElement1 = duplexRequestBindingElement;
			int num1 = num;
			num = num1 + 1;
			customBinding.Elements.Insert(num1, duplexRequestBindingElement1);
			BindingParameterCollection bindingParameterCollection = new BindingParameterCollection();
			if (useSslStreamSecurity)
			{
				ClientCredentials clientCredential = new ClientCredentials();
				clientCredential.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.Custom;
				clientCredential.ServiceCertificate.Authentication.CustomCertificateValidator = RetriableCertificateValidator.Instance;
				if (SoapProtocolDefaults.IsAvailableClientCertificateThumbprint(out str))
				{
					clientCredential.ClientCertificate.SetCertificate(StoreLocation.LocalMachine, StoreName.My, X509FindType.FindByThumbprint, str);
				}
				bindingParameterCollection.Add(clientCredential);
			}
			this.MessageVersion = customBinding.MessageVersion;
			return customBinding.BuildChannelFactory<IRequestSessionChannel>(bindingParameterCollection);
		}

		public IRequestSessionChannel EndGetCorrelator(IAsyncResult result)
		{
			return base.EndLoadInstance(result);
		}

		protected override void OnAbortInstance(SingletonDictionaryManager<string, IRequestSessionChannel>.SingletonContext singletonContext, string key, IRequestSessionChannel instance, object unloadingContext)
		{
			instance.Abort();
		}

		protected override IAsyncResult OnBeginCloseInstance(SingletonDictionaryManager<string, IRequestSessionChannel>.SingletonContext singletonContext, string key, IRequestSessionChannel instance, object unloadingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new ContainerChannelManager.CloseInstanceAsyncResult(key, instance, timeout, callback, state)).Start();
		}

		protected override IAsyncResult OnBeginCreateInstance(SingletonDictionaryManager<string, IRequestSessionChannel>.SingletonContext singletonContext, string key, object loadingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			Uri uri = new Uri(key);
			EndpointAddress endpointAddress = new EndpointAddress(uri, SbmpProtocolDefaults.GetEndpointIdentity(uri), new AddressHeader[0]);
			IChannelFactory<IRequestSessionChannel> channelFactory = this.defaultChannelFactory;
			if (!this.clientMode && loadingContext != null && loadingContext is bool && (bool)loadingContext)
			{
				channelFactory = this.securedChannelFactory;
			}
			IRequestSessionChannel requestSessionChannel = channelFactory.CreateChannel(endpointAddress);
			requestSessionChannel.SafeAddFaulted((object s, EventArgs e) => {
				this.RaiseNotifyCleanup(uri);
				try
				{
					base.BeginUnloadInstance(key, null, true, TimeSpan.FromSeconds(10), new AsyncCallback(ContainerChannelManager.UnloadCallback), this);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteExceptionAsWarning(exception.ToString()));
				}
			});
			return new CompletedAsyncResult<IRequestSessionChannel>(requestSessionChannel, callback, state);
		}

		protected override IAsyncResult OnBeginOpenInstance(SingletonDictionaryManager<string, IRequestSessionChannel>.SingletonContext singletonContext, string key, IRequestSessionChannel instance, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new ContainerChannelManager.OpenInstanceAsyncResult(key, instance, timeout, callback, state)).Start();
		}

		protected override void OnCloseInstance(SingletonDictionaryManager<string, IRequestSessionChannel>.SingletonContext singletonContext, string key, IRequestSessionChannel instance, object unloadingContext, TimeSpan timeout)
		{
			(new ContainerChannelManager.CloseInstanceAsyncResult(key, instance, timeout, null, null)).RunSynchronously();
		}

		protected override IRequestSessionChannel OnCreateInstance(SingletonDictionaryManager<string, IRequestSessionChannel>.SingletonContext singletonContext, string key, object loadingContext, TimeSpan timeout)
		{
			IAsyncResult asyncResult = this.OnBeginCreateInstance(singletonContext, key, loadingContext, timeout, null, null);
			return this.OnEndCreateInstance(asyncResult);
		}

		protected override void OnEndCloseInstance(IAsyncResult result)
		{
			AsyncResult<ContainerChannelManager.CloseInstanceAsyncResult>.End(result);
		}

		protected override IRequestSessionChannel OnEndCreateInstance(IAsyncResult result)
		{
			IRequestSessionChannel requestSessionChannel = CompletedAsyncResult<IRequestSessionChannel>.End(result);
			if (this.clientMode)
			{
				return requestSessionChannel;
			}
			return new ContainerChannelManager.WrappedRequestSessionChannel(requestSessionChannel);
		}

		protected override void OnEndOpenInstance(IAsyncResult result)
		{
			AsyncResult<ContainerChannelManager.OpenInstanceAsyncResult>.End(result);
		}

		private void OnInnerChannelFaulted(object sender, EventArgs e)
		{
			((IRequestSessionChannel)sender).Faulted -= this.onInnerChannelFaulted;
		}

		protected override void OnOpenInstance(SingletonDictionaryManager<string, IRequestSessionChannel>.SingletonContext singletonContext, string key, IRequestSessionChannel instance, TimeSpan timeout)
		{
			(new ContainerChannelManager.OpenInstanceAsyncResult(key, instance, timeout, null, null)).RunSynchronously();
		}

		private void RaiseNotifyCleanup(Uri physicalAddress)
		{
			EventHandler eventHandler = this.NotifyCleanup;
			if (eventHandler != null)
			{
				eventHandler(this, new NotifyCleanupEventArgs(physicalAddress));
			}
		}

		public void TryRemoveSingletonContext(string key)
		{
			base.OnTryRemoveSingletonContext(key);
		}

		private static void UnloadCallback(IAsyncResult result)
		{
			ContainerChannelManager asyncState = (ContainerChannelManager)result.AsyncState;
			try
			{
				asyncState.EndUnloadInstance(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteExceptionAsWarning(exception.ToString()));
			}
		}

		public event EventHandler NotifyCleanup;

		private sealed class CloseInstanceAsyncResult : IteratorAsyncResult<ContainerChannelManager.CloseInstanceAsyncResult>
		{
			private readonly IRequestSessionChannel channel;

			public CloseInstanceAsyncResult(string key, IRequestSessionChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.channel = channel;
			}

			protected override IEnumerator<IteratorAsyncResult<ContainerChannelManager.CloseInstanceAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ContainerChannelManager.CloseInstanceAsyncResult closeInstanceAsyncResult = this;
				IteratorAsyncResult<ContainerChannelManager.CloseInstanceAsyncResult>.BeginCall beginCall = (ContainerChannelManager.CloseInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.channel.BeginClose(t, c, s);
				IteratorAsyncResult<ContainerChannelManager.CloseInstanceAsyncResult>.EndCall endCall = (ContainerChannelManager.CloseInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.channel.EndClose(r);
				yield return closeInstanceAsyncResult.CallAsync(beginCall, endCall, (ContainerChannelManager.CloseInstanceAsyncResult thisPtr, TimeSpan t) => thisPtr.channel.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class OpenInstanceAsyncResult : IteratorAsyncResult<ContainerChannelManager.OpenInstanceAsyncResult>
		{
			private readonly IRequestSessionChannel channel;

			public OpenInstanceAsyncResult(string key, IRequestSessionChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.channel = channel;
			}

			protected override IEnumerator<IteratorAsyncResult<ContainerChannelManager.OpenInstanceAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ContainerChannelManager.OpenInstanceAsyncResult openInstanceAsyncResult = this;
				IteratorAsyncResult<ContainerChannelManager.OpenInstanceAsyncResult>.BeginCall beginCall = (ContainerChannelManager.OpenInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.channel.BeginOpen(t, c, s);
				IteratorAsyncResult<ContainerChannelManager.OpenInstanceAsyncResult>.EndCall endCall = (ContainerChannelManager.OpenInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.channel.EndOpen(r);
				yield return openInstanceAsyncResult.CallAsync(beginCall, endCall, (ContainerChannelManager.OpenInstanceAsyncResult thisPtr, TimeSpan t) => thisPtr.channel.Open(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}

		private sealed class WrappedMessage : Message
		{
			private static Action<Message, XmlDictionaryWriter> writeStartHeadersDelegate;

			private static Action<Message, XmlDictionaryWriter> bodyToStringDelegate;

			private readonly Message message;

			private readonly ContainerChannelManager.WrappedXmlBinaryReaderSession readerSession;

			public override MessageHeaders Headers
			{
				get
				{
					return this.message.Headers;
				}
			}

			public override bool IsEmpty
			{
				get
				{
					return this.message.IsEmpty;
				}
			}

			public override bool IsFault
			{
				get
				{
					return this.message.IsFault;
				}
			}

			public override MessageProperties Properties
			{
				get
				{
					return this.message.Properties;
				}
			}

			public override System.ServiceModel.Channels.MessageVersion Version
			{
				get
				{
					return this.message.Version;
				}
			}

			public WrappedMessage(Message message, ContainerChannelManager.WrappedXmlBinaryReaderSession readerSession)
			{
				if (message == null)
				{
					throw new ArgumentNullException("message");
				}
				if (readerSession == null)
				{
					throw new ArgumentNullException("readerSession");
				}
				this.message = message;
				this.readerSession = readerSession;
			}

			private static Action<Message, XmlDictionaryWriter> CompileBodyToStringDelegate()
			{
				Type type = typeof(Message);
				Type[] typeArray = new Type[] { typeof(XmlDictionaryWriter) };
				MethodInfo method = type.GetMethod("BodyToString", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, typeArray, null);
				ParameterExpression parameterExpression = Expression.Parameter(typeof(Message), "@message");
				ParameterExpression parameterExpression1 = Expression.Parameter(typeof(XmlDictionaryWriter), "@writer");
				Expression[] expressionArray = new Expression[] { parameterExpression1 };
				MethodCallExpression methodCallExpression = Expression.Call(parameterExpression, method, expressionArray);
				ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { parameterExpression, parameterExpression1 };
				return Expression.Lambda<Action<Message, XmlDictionaryWriter>>(methodCallExpression, parameterExpressionArray).Compile();
			}

			private static Action<Message, XmlDictionaryWriter> CompileWriteStartHeadersDelegate()
			{
				Type type = typeof(Message);
				Type[] typeArray = new Type[] { typeof(XmlDictionaryWriter) };
				MethodInfo method = type.GetMethod("WriteStartHeaders", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, typeArray, null);
				ParameterExpression parameterExpression = Expression.Parameter(typeof(Message), "@message");
				ParameterExpression parameterExpression1 = Expression.Parameter(typeof(XmlDictionaryWriter), "@writer");
				Expression[] expressionArray = new Expression[] { parameterExpression1 };
				MethodCallExpression methodCallExpression = Expression.Call(parameterExpression, method, expressionArray);
				ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { parameterExpression, parameterExpression1 };
				return Expression.Lambda<Action<Message, XmlDictionaryWriter>>(methodCallExpression, parameterExpressionArray).Compile();
			}

			protected override void OnBodyToString(XmlDictionaryWriter writer)
			{
				if (ContainerChannelManager.WrappedMessage.bodyToStringDelegate == null)
				{
					ContainerChannelManager.WrappedMessage.bodyToStringDelegate = ContainerChannelManager.WrappedMessage.CompileBodyToStringDelegate();
				}
				ContainerChannelManager.WrappedMessage.bodyToStringDelegate(this.message, writer);
			}

			protected override void OnClose()
			{
				this.message.Close();
				base.OnClose();
			}

			protected override MessageBuffer OnCreateBufferedCopy(int maxBufferSize)
			{
				return this.message.CreateBufferedCopy(maxBufferSize);
			}

			protected override string OnGetBodyAttribute(string localName, string ns)
			{
				return this.message.GetBodyAttribute(localName, ns);
			}

			protected override XmlDictionaryReader OnGetReaderAtBodyContents()
			{
				return this.message.GetReaderAtBodyContents();
			}

			protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
			{
				this.message.WriteBodyContents(writer);
			}

			protected override void OnWriteMessage(XmlDictionaryWriter writer)
			{
				XmlDictionaryWriter wrappedXmlDictionaryWriter = new ContainerChannelManager.WrappedXmlDictionaryWriter(writer, this.readerSession);
				this.message.WriteMessage(wrappedXmlDictionaryWriter);
			}

			protected override void OnWriteStartBody(XmlDictionaryWriter writer)
			{
				this.message.WriteStartBody(writer);
			}

			protected override void OnWriteStartEnvelope(XmlDictionaryWriter writer)
			{
				this.message.WriteStartEnvelope(writer);
			}

			protected override void OnWriteStartHeaders(XmlDictionaryWriter writer)
			{
				if (ContainerChannelManager.WrappedMessage.writeStartHeadersDelegate == null)
				{
					ContainerChannelManager.WrappedMessage.writeStartHeadersDelegate = ContainerChannelManager.WrappedMessage.CompileWriteStartHeadersDelegate();
				}
				ContainerChannelManager.WrappedMessage.writeStartHeadersDelegate(this.message, writer);
			}

			public override string ToString()
			{
				return this.message.ToString();
			}
		}

		private sealed class WrappedRequestSessionChannel : IRequestSessionChannel, IRequestChannel, IChannel, ICommunicationObject, ISessionChannel<IOutputSession>
		{
			private readonly IRequestSessionChannel channel;

			private readonly ContainerChannelManager.WrappedXmlBinaryReaderSession readerSession;

			public EndpointAddress RemoteAddress
			{
				get
				{
					return this.channel.RemoteAddress;
				}
			}

			public IOutputSession Session
			{
				get
				{
					return this.channel.Session;
				}
			}

			public CommunicationState State
			{
				get
				{
					return this.channel.State;
				}
			}

			public Uri Via
			{
				get
				{
					return this.channel.Via;
				}
			}

			public WrappedRequestSessionChannel(IRequestSessionChannel channel)
			{
				if (channel == null)
				{
					throw new ArgumentNullException("channel");
				}
				this.channel = channel;
				this.readerSession = new ContainerChannelManager.WrappedXmlBinaryReaderSession();
			}

			public void Abort()
			{
				this.channel.Abort();
			}

			public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.channel.BeginClose(timeout, callback, state);
			}

			public IAsyncResult BeginClose(AsyncCallback callback, object state)
			{
				return this.channel.BeginClose(callback, state);
			}

			public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.channel.BeginOpen(timeout, callback, state);
			}

			public IAsyncResult BeginOpen(AsyncCallback callback, object state)
			{
				return this.channel.BeginOpen(callback, state);
			}

			public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
			{
				this.SanitizeMessage(ref message);
				return this.channel.BeginRequest(message, timeout, callback, state);
			}

			public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
			{
				this.SanitizeMessage(ref message);
				return this.channel.BeginRequest(message, callback, state);
			}

			public void Close(TimeSpan timeout)
			{
				this.channel.Close(timeout);
			}

			public void Close()
			{
				this.channel.Close();
			}

			public void EndClose(IAsyncResult result)
			{
				this.channel.EndClose(result);
			}

			public void EndOpen(IAsyncResult result)
			{
				this.channel.EndOpen(result);
			}

			public Message EndRequest(IAsyncResult result)
			{
				return this.channel.EndRequest(result);
			}

			public T GetProperty<T>()
			where T : class
			{
				return this.channel.GetProperty<T>();
			}

			public void Open()
			{
				this.channel.Open();
			}

			public void Open(TimeSpan timeout)
			{
				this.channel.Open(timeout);
			}

			public Message Request(Message message, TimeSpan timeout)
			{
				this.SanitizeMessage(ref message);
				return this.channel.Request(message, timeout);
			}

			public Message Request(Message message)
			{
				this.SanitizeMessage(ref message);
				return this.channel.Request(message);
			}

			private void SanitizeMessage(ref Message message)
			{
				message = new ContainerChannelManager.WrappedMessage(message, this.readerSession);
			}

			public event EventHandler Closed
			{
				add
				{
					this.channel.Closed += value;
				}
				remove
				{
					this.channel.Closed -= value;
				}
			}

			public event EventHandler Closing
			{
				add
				{
					this.channel.Closing += value;
				}
				remove
				{
					this.channel.Closing -= value;
				}
			}

			public event EventHandler Faulted
			{
				add
				{
					this.channel.Faulted += value;
				}
				remove
				{
					this.channel.Faulted -= value;
				}
			}

			public event EventHandler Opened
			{
				add
				{
					this.channel.Opened += value;
				}
				remove
				{
					this.channel.Opened -= value;
				}
			}

			public event EventHandler Opening
			{
				add
				{
					this.channel.Opening += value;
				}
				remove
				{
					this.channel.Opening -= value;
				}
			}
		}

		private sealed class WrappedXmlBinaryReaderSession : XmlBinaryReaderSession
		{
			private int nextKey;

			public WrappedXmlBinaryReaderSession()
			{
			}

			public XmlDictionaryString Sanitize(XmlDictionaryString value)
			{
				XmlDictionaryString xmlDictionaryString;
				if (value.Dictionary is XmlBinaryReaderSession)
				{
					if (!base.TryLookup(value.Value, out xmlDictionaryString))
					{
						int num = Interlocked.Increment(ref this.nextKey);
						xmlDictionaryString = base.Add(num, value.Value);
					}
					value = xmlDictionaryString;
				}
				return value;
			}
		}

		private sealed class WrappedXmlDictionaryWriter : XmlDictionaryWriter
		{
			private static Action<XmlDictionaryWriter, XmlDictionaryReader, bool> writeTextNodeDelegate;

			private readonly XmlDictionaryWriter writer;

			private readonly ContainerChannelManager.WrappedXmlBinaryReaderSession readerSession;

			public override bool CanCanonicalize
			{
				get
				{
					return this.writer.CanCanonicalize;
				}
			}

			public override XmlWriterSettings Settings
			{
				get
				{
					return this.writer.Settings;
				}
			}

			public override WriteState WriteState
			{
				get
				{
					return this.writer.WriteState;
				}
			}

			public override string XmlLang
			{
				get
				{
					return this.writer.XmlLang;
				}
			}

			public override XmlSpace XmlSpace
			{
				get
				{
					return this.writer.XmlSpace;
				}
			}

			public WrappedXmlDictionaryWriter(XmlDictionaryWriter writer, ContainerChannelManager.WrappedXmlBinaryReaderSession readerSession)
			{
				this.writer = writer;
				this.readerSession = readerSession;
			}

			public override void Close()
			{
				this.Dispose(true);
			}

			private static Action<XmlDictionaryWriter, XmlDictionaryReader, bool> CompileWriteTextNodeDelegate()
			{
				Type type = typeof(XmlDictionaryWriter);
				Type[] typeArray = new Type[] { typeof(XmlDictionaryReader), typeof(bool) };
				MethodInfo method = type.GetMethod("WriteTextNode", BindingFlags.Instance | BindingFlags.NonPublic, null, CallingConventions.Any, typeArray, null);
				ParameterExpression parameterExpression = Expression.Parameter(typeof(XmlDictionaryWriter), "@writer");
				ParameterExpression parameterExpression1 = Expression.Parameter(typeof(XmlDictionaryReader), "@reader");
				ParameterExpression parameterExpression2 = Expression.Parameter(typeof(bool), "@isAttribute");
				MethodCallExpression methodCallExpression = Expression.Call(parameterExpression, method, parameterExpression1, parameterExpression2);
				ParameterExpression[] parameterExpressionArray = new ParameterExpression[] { parameterExpression, parameterExpression1, parameterExpression2 };
				return Expression.Lambda<Action<XmlDictionaryWriter, XmlDictionaryReader, bool>>(methodCallExpression, parameterExpressionArray).Compile();
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					this.writer.Close();
				}
				base.Dispose(disposing);
			}

			public override void EndCanonicalization()
			{
				this.writer.EndCanonicalization();
			}

			public override void Flush()
			{
				this.writer.Flush();
			}

			public override string LookupPrefix(string ns)
			{
				return this.writer.LookupPrefix(ns);
			}

			private XmlDictionaryString Sanitize(XmlDictionaryString value)
			{
				return this.readerSession.Sanitize(value);
			}

			public override void StartCanonicalization(Stream stream, bool includeComments, string[] inclusivePrefixes)
			{
				this.writer.StartCanonicalization(stream, includeComments, inclusivePrefixes);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, bool[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, DateTime[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, decimal[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, double[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, float[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, Guid[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, int[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, long[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, short[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, localName, namespaceUri, array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteArray(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
			{
				this.writer.WriteArray(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri), array, offset, count);
			}

			public override void WriteBase64(byte[] buffer, int index, int count)
			{
				this.writer.WriteBase64(buffer, index, count);
			}

			public override void WriteBinHex(byte[] buffer, int index, int count)
			{
				this.writer.WriteBinHex(buffer, index, count);
			}

			public override void WriteCData(string text)
			{
				this.writer.WriteCData(text);
			}

			public override void WriteCharEntity(char ch)
			{
				this.writer.WriteCharEntity(ch);
			}

			public override void WriteChars(char[] buffer, int index, int count)
			{
				this.writer.WriteChars(buffer, index, count);
			}

			public override void WriteComment(string text)
			{
				this.writer.WriteComment(text);
			}

			public override void WriteDocType(string name, string pubid, string sysid, string subset)
			{
				this.writer.WriteDocType(name, pubid, sysid, subset);
			}

			public override void WriteEndAttribute()
			{
				this.writer.WriteEndAttribute();
			}

			public override void WriteEndDocument()
			{
				this.writer.WriteEndDocument();
			}

			public override void WriteEndElement()
			{
				this.writer.WriteEndElement();
			}

			public override void WriteEntityRef(string name)
			{
				this.writer.WriteEntityRef(name);
			}

			public override void WriteFullEndElement()
			{
				this.writer.WriteFullEndElement();
			}

			public override void WriteName(string name)
			{
				this.writer.WriteName(name);
			}

			public override void WriteNmToken(string name)
			{
				this.writer.WriteNmToken(name);
			}

			public override void WriteProcessingInstruction(string name, string text)
			{
				this.writer.WriteProcessingInstruction(name, text);
			}

			public override void WriteQualifiedName(string localName, string ns)
			{
				this.writer.WriteQualifiedName(localName, ns);
			}

			public override void WriteQualifiedName(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				this.writer.WriteQualifiedName(this.Sanitize(localName), this.Sanitize(namespaceUri));
			}

			public override void WriteRaw(string data)
			{
				this.writer.WriteRaw(data);
			}

			public override void WriteRaw(char[] buffer, int index, int count)
			{
				this.writer.WriteRaw(buffer, index, count);
			}

			public override void WriteStartAttribute(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				this.writer.WriteStartAttribute(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri));
			}

			public override void WriteStartAttribute(string prefix, string localName, string ns)
			{
				this.writer.WriteStartAttribute(prefix, localName, ns);
			}

			public override void WriteStartDocument(bool standalone)
			{
				this.writer.WriteStartDocument(standalone);
			}

			public override void WriteStartDocument()
			{
				this.writer.WriteStartDocument();
			}

			public override void WriteStartElement(string prefix, string localName, string ns)
			{
				this.writer.WriteStartElement(prefix, localName, ns);
			}

			public override void WriteStartElement(string prefix, XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				this.writer.WriteStartElement(prefix, this.Sanitize(localName), this.Sanitize(namespaceUri));
			}

			public override void WriteString(string text)
			{
				this.writer.WriteString(text);
			}

			public override void WriteString(XmlDictionaryString value)
			{
				this.writer.WriteString(this.Sanitize(value));
			}

			public override void WriteSurrogateCharEntity(char lowChar, char highChar)
			{
				this.writer.WriteSurrogateCharEntity(lowChar, highChar);
			}

			protected override void WriteTextNode(XmlDictionaryReader reader, bool isAttribute)
			{
				if (ContainerChannelManager.WrappedXmlDictionaryWriter.writeTextNodeDelegate == null)
				{
					ContainerChannelManager.WrappedXmlDictionaryWriter.writeTextNodeDelegate = ContainerChannelManager.WrappedXmlDictionaryWriter.CompileWriteTextNodeDelegate();
				}
				ContainerChannelManager.WrappedXmlDictionaryWriter.writeTextNodeDelegate(this.writer, reader, isAttribute);
			}

			public override void WriteValue(XmlDictionaryString value)
			{
				this.writer.WriteValue(this.Sanitize(value));
			}

			public override void WriteValue(bool value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(DateTime value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(decimal value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(double value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(float value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(Guid value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(int value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(IStreamProvider value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(long value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(object value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(string value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(TimeSpan value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteValue(UniqueId value)
			{
				this.writer.WriteValue(value);
			}

			public override void WriteWhitespace(string ws)
			{
				this.writer.WriteWhitespace(ws);
			}

			public override void WriteXmlAttribute(string localName, string value)
			{
				this.writer.WriteXmlAttribute(localName, value);
			}

			public override void WriteXmlAttribute(XmlDictionaryString localName, XmlDictionaryString value)
			{
				this.writer.WriteXmlAttribute(localName, value);
			}

			public override void WriteXmlnsAttribute(string prefix, string namespaceUri)
			{
				this.writer.WriteXmlnsAttribute(prefix, namespaceUri);
			}

			public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString namespaceUri)
			{
				this.writer.WriteXmlnsAttribute(prefix, namespaceUri);
			}
		}
	}
}