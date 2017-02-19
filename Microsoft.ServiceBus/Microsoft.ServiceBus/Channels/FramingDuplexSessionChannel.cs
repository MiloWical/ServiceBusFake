using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Diagnostics;
using System.IdentityModel.Claims;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class FramingDuplexSessionChannel : Microsoft.ServiceBus.Channels.TransportOutputChannel, IDuplexSessionChannel, IDuplexChannel, IInputChannel, IOutputChannel, IChannel, ICommunicationObject, ISessionChannel<IDuplexSession>
	{
		private System.ServiceModel.Channels.BufferManager bufferManager;

		private Microsoft.ServiceBus.Channels.IConnection connection;

		private Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession duplexSession;

		private bool exposeConnectionProperty;

		private bool isInputSessionClosed;

		private bool isOutputSessionClosed;

		private System.ServiceModel.Channels.MessageEncoder messageEncoder;

		private Microsoft.ServiceBus.Channels.SynchronizedMessageSource messageSource;

		private SecurityMessageProperty remoteSecurity;

		private EndpointAddress localAddress;

		private ThreadNeutralSemaphore sendLock = new ThreadNeutralSemaphore(1);

		private Uri localVia;

		protected EventTraceActivity Activity
		{
			get;
			private set;
		}

		protected System.ServiceModel.Channels.BufferManager BufferManager
		{
			get
			{
				return this.bufferManager;
			}
		}

		protected Microsoft.ServiceBus.Channels.IConnection Connection
		{
			get
			{
				return this.connection;
			}
			set
			{
				this.connection = value;
			}
		}

		public EndpointAddress LocalAddress
		{
			get
			{
				return this.localAddress;
			}
		}

		protected Uri LocalVia
		{
			get
			{
				return this.localVia;
			}
		}

		protected System.ServiceModel.Channels.MessageEncoder MessageEncoder
		{
			get
			{
				return this.messageEncoder;
			}
			set
			{
				this.messageEncoder = value;
			}
		}

		public SecurityMessageProperty RemoteSecurity
		{
			get
			{
				return this.remoteSecurity;
			}
			protected set
			{
				this.remoteSecurity = value;
			}
		}

		public IDuplexSession Session
		{
			get
			{
				return this.duplexSession;
			}
		}

		private FramingDuplexSessionChannel(ChannelManagerBase manager, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings, EndpointAddress localAddress, Uri localVia, EndpointAddress remoteAddresss, Uri via, bool exposeConnectionProperty) : base(manager, remoteAddresss, via, settings.ManualAddressing, settings.MessageVersion)
		{
			this.localAddress = localAddress;
			this.localVia = localVia;
			this.exposeConnectionProperty = exposeConnectionProperty;
			this.bufferManager = settings.BufferManager;
			this.Activity = new EventTraceActivity();
		}

		protected FramingDuplexSessionChannel(ChannelManagerBase factory, Microsoft.ServiceBus.Channels.IConnectionOrientedTransportFactorySettings settings, EndpointAddress remoteAddresss, Uri via, bool exposeConnectionProperty) : this(factory, settings, EndpointAddress2.AnonymousAddress, (Uri)InvokeHelper.InvokeInstanceGet(typeof(AddressingVersion), settings.MessageVersion.Addressing, "AnonymousUri"), remoteAddresss, via, exposeConnectionProperty)
		{
			this.duplexSession = Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession.CreateSession(this, settings.Upgrade);
		}

		protected FramingDuplexSessionChannel(Microsoft.ServiceBus.Channels.ConnectionOrientedTransportChannelListener channelListener, EndpointAddress localAddress, Uri localVia, bool exposeConnectionProperty) : this(channelListener, channelListener, localAddress, localVia, EndpointAddress2.AnonymousAddress, (Uri)InvokeHelper.InvokeInstanceGet(typeof(AddressingVersion), channelListener.MessageVersion.Addressing, "AnonymousUri"), exposeConnectionProperty)
		{
			this.duplexSession = Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession.CreateSession(this, channelListener.Upgrade);
		}

		private IAsyncResult BeginCloseOutputSession(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult(this, timeout, callback, state);
		}

		public IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			return this.BeginReceive(base.DefaultReceiveTimeout, callback, state);
		}

		public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			if (this.DoneReceivingInCurrentState())
			{
				return new Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult(callback, state);
			}
			bool flag = true;
			try
			{
				IAsyncResult asyncResult1 = this.messageSource.BeginReceive(timeout, callback, state);
				flag = false;
				asyncResult = asyncResult1;
			}
			finally
			{
				if (flag)
				{
					base.Fault();
				}
			}
			return asyncResult;
		}

		public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult(this, timeout, callback, state);
		}

		public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult asyncResult;
			if (this.DoneReceivingInCurrentState())
			{
				return new Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult(callback, state);
			}
			bool flag = true;
			try
			{
				IAsyncResult asyncResult1 = this.messageSource.BeginWaitForMessage(timeout, callback, state);
				flag = false;
				asyncResult = asyncResult1;
			}
			finally
			{
				if (flag)
				{
					base.Fault();
				}
			}
			return asyncResult;
		}

		private void CloseOutputSession(TimeSpan timeout)
		{
			this.ThrowIfNotOpened();
			this.ThrowIfFaulted();
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			if (!this.sendLock.TryEnter(timeoutHelper.RemainingTime()))
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string closeTimedOut = Resources.CloseTimedOut;
				object[] objArray = new object[] { timeout };
				throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(closeTimedOut, objArray), ThreadNeutralSemaphore.CreateEnterTimedOutException(timeout)));
			}
			try
			{
				this.ThrowIfFaulted();
				if (!this.isOutputSessionClosed)
				{
					this.isOutputSessionClosed = true;
					bool flag = true;
					try
					{
						this.Connection.Write(Microsoft.ServiceBus.Channels.SessionEncoder.EndBytes, 0, (int)Microsoft.ServiceBus.Channels.SessionEncoder.EndBytes.Length, true, timeoutHelper.RemainingTime());
						this.OnOutputSessionClosed(ref timeoutHelper);
						flag = false;
					}
					finally
					{
						if (flag)
						{
							base.Fault();
						}
					}
				}
			}
			finally
			{
				this.sendLock.Exit();
			}
		}

		private void CompleteClose(TimeSpan timeout)
		{
			this.ReturnConnectionIfNecessary(false, timeout);
		}

		private ArraySegment<byte> EncodeMessage(Message message)
		{
			ArraySegment<byte> nums = this.MessageEncoder.WriteMessage(message, 2147483647, this.bufferManager, 6);
			return Microsoft.ServiceBus.Channels.SessionEncoder.EncodeMessageFrame(nums);
		}

		private static void EndCloseOutputSession(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult.End(result);
		}

		public Message EndReceive(IAsyncResult result)
		{
			Message message;
			this.ThrowIfNotOpened();
			if (result == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("result");
			}
			Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult doneReceivingAsyncResult = result as Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult;
			if (doneReceivingAsyncResult != null)
			{
				Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult.End(doneReceivingAsyncResult);
				return null;
			}
			bool flag = true;
			Message message1 = null;
			try
			{
				message1 = this.messageSource.EndReceive(result);
				this.OnReceiveMessage(message1);
				flag = false;
				message = message1;
			}
			finally
			{
				if (flag)
				{
					if (message1 != null)
					{
						message1.Close();
						message1 = null;
					}
					base.Fault();
				}
			}
			return message;
		}

		public bool EndTryReceive(IAsyncResult result, out Message message)
		{
			return Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult.End(result, out message);
		}

		public bool EndWaitForMessage(IAsyncResult result)
		{
			bool flag;
			this.ThrowIfNotOpened();
			if (result == null)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("result");
			}
			Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult doneReceivingAsyncResult = result as Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult;
			if (doneReceivingAsyncResult != null)
			{
				return Microsoft.ServiceBus.Channels.DoneReceivingAsyncResult.End(doneReceivingAsyncResult);
			}
			bool flag1 = true;
			try
			{
				bool flag2 = this.messageSource.EndWaitForMessage(result);
				flag1 = !flag2;
				flag = flag2;
			}
			finally
			{
				if (flag1)
				{
					base.Fault();
				}
			}
			return flag;
		}

		public override T GetProperty<T>()
		where T : class
		{
			if (this.Connection != null)
			{
				T property = this.Connection.GetProperty<T>();
				if ((object)property != (object)default(T))
				{
					return property;
				}
			}
			return base.GetProperty<T>();
		}

		protected override void OnAbort()
		{
			MessagingClientEtwProvider.Provider.RelayChannelAborting(this.Activity, (this.Via == null ? string.Empty : this.Via.AbsoluteUri));
			this.ReturnConnectionIfNecessary(true, TimeSpan.Zero);
		}

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult(this, timeout, callback, state);
		}

		protected override IAsyncResult OnBeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposedOrNotOpen();
			return new Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult(this, message, timeout, callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			this.CloseOutputSession(timeoutHelper.RemainingTime());
			if (!this.isInputSessionClosed)
			{
				Message message = this.messageSource.Receive(timeoutHelper.RemainingTime());
				if (message != null)
				{
					using (message)
					{
						Type type = typeof(ProtocolException);
						object[] objArray = new object[] { message };
						ProtocolException protocolException = (ProtocolException)InvokeHelper.InvokeStaticMethod(type, "ReceiveShutdownReturnedNonNull", objArray);
						throw TraceUtility.ThrowHelperError(protocolException, message);
					}
				}
				this.OnInputSessionClosed();
			}
			this.CompleteClose(timeoutHelper.RemainingTime());
		}

		protected override void OnClosing()
		{
			MessagingClientEtwProvider.Provider.RelayChannelClosing(this.Activity, (this.Via == null ? string.Empty : this.Via.AbsoluteUri));
			base.OnClosing();
		}

		protected override void OnEndClose(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.End(result);
		}

		protected override void OnEndSend(IAsyncResult result)
		{
			Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult.End(result);
		}

		protected override void OnFaulted()
		{
			MessagingClientEtwProvider.Provider.RelayChannelFaulting(this.Activity, (this.Via == null ? string.Empty : this.Via.AbsoluteUri));
			base.OnFaulted();
			this.ReturnConnectionIfNecessary(true, TimeSpan.Zero);
		}

		private void OnInputSessionClosed()
		{
			lock (base.ThisLock)
			{
				if (!this.isInputSessionClosed)
				{
					this.isInputSessionClosed = true;
				}
			}
		}

		protected override void OnOpening()
		{
			MessagingClientEtwProvider.Provider.RelayChannelOpening(this.Activity, base.GetType().Name, this.LocalAddress.Uri.AbsoluteUri);
			base.OnOpening();
		}

		private void OnOutputSessionClosed(ref TimeoutHelper timeoutHelper)
		{
			bool flag = false;
			lock (base.ThisLock)
			{
				if (this.isInputSessionClosed)
				{
					flag = true;
				}
			}
			if (flag)
			{
				this.ReturnConnectionIfNecessary(false, timeoutHelper.RemainingTime());
			}
		}

		private void OnReceiveMessage(Message message)
		{
			if (message == null)
			{
				this.OnInputSessionClosed();
				return;
			}
			this.PrepareMessage(message);
		}

		protected override void OnSend(Message message, TimeSpan timeout)
		{
			base.ThrowIfDisposedOrNotOpen();
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			if (!this.sendLock.TryEnter(timeoutHelper.RemainingTime()))
			{
				ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
				string sendToViaTimedOut = Resources.SendToViaTimedOut;
				object[] via = new object[] { this.Via, timeout };
				throw exceptionUtility.ThrowHelperError(new TimeoutException(Microsoft.ServiceBus.SR.GetString(sendToViaTimedOut, via), ThreadNeutralSemaphore.CreateEnterTimedOutException(timeout)));
			}
			try
			{
				base.ThrowIfDisposedOrNotOpen();
				this.ThrowIfOutputSessionClosed();
				bool flag = false;
				try
				{
					bool allowOutputBatching = message.Properties.AllowOutputBatching;
					ArraySegment<byte> nums = this.EncodeMessage(message);
					this.Connection.Write(nums.Array, nums.Offset, nums.Count, !allowOutputBatching, timeoutHelper.RemainingTime(), this.bufferManager);
					flag = true;
				}
				finally
				{
					if (!flag)
					{
						base.Fault();
					}
				}
			}
			finally
			{
				this.sendLock.Exit();
			}
		}

		protected virtual void PrepareMessage(Message message)
		{
			message.Properties.Via = this.localVia;
			if (this.exposeConnectionProperty)
			{
				message.Properties[Microsoft.ServiceBus.Channels.ConnectionMessageProperty.Name] = this.connection;
			}
			if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
			{
				TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.MessageReceived, MessageTransmitTraceRecord.CreateReceiveTraceRecord(message, this.LocalAddress), this, null, message);
			}
		}

		public Message Receive()
		{
			return this.Receive(base.DefaultReceiveTimeout);
		}

		public Message Receive(TimeSpan timeout)
		{
			Message message;
			Message message1 = null;
			if (this.DoneReceivingInCurrentState())
			{
				return null;
			}
			bool flag = true;
			try
			{
				message1 = this.messageSource.Receive(timeout);
				this.OnReceiveMessage(message1);
				flag = false;
				message = message1;
			}
			finally
			{
				if (flag)
				{
					if (message1 != null)
					{
						message1.Close();
						message1 = null;
					}
					base.Fault();
				}
			}
			return message;
		}

		protected abstract void ReturnConnectionIfNecessary(bool abort, TimeSpan timeout);

		protected void SetMessageSource(Microsoft.ServiceBus.Channels.IMessageSource messageSource)
		{
			this.messageSource = new Microsoft.ServiceBus.Channels.SynchronizedMessageSource(messageSource);
		}

		private void ThrowIfOutputSessionClosed()
		{
			if (this.isOutputSessionClosed)
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.SendCannotBeCalledAfterCloseOutputSession, new object[0])));
			}
		}

		public bool TryReceive(TimeSpan timeout, out Message message)
		{
			bool flag;
			try
			{
				message = this.Receive(timeout);
				flag = true;
			}
			catch (TimeoutException timeoutException1)
			{
				TimeoutException timeoutException = timeoutException1;
				if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
				{
					Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
				}
				message = null;
				flag = false;
			}
			return flag;
		}

		public bool WaitForMessage(TimeSpan timeout)
		{
			bool flag;
			if (this.DoneReceivingInCurrentState())
			{
				return true;
			}
			bool flag1 = true;
			try
			{
				bool flag2 = this.messageSource.WaitForMessage(timeout);
				flag1 = !flag2;
				flag = flag2;
			}
			finally
			{
				if (flag1)
				{
					base.Fault();
				}
			}
			return flag;
		}

		private class CloseAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel;

			private TimeoutHelper timeoutHelper;

			private static AsyncCallback onCloseOutputSession;

			private static AsyncCallback onCloseInputSession;

			private static Action<object> onCompleteCloseScheduled;

			protected internal override EventTraceActivity Activity
			{
				get
				{
					return this.channel.Activity;
				}
			}

			protected override TraceEventType TraceEventType
			{
				get
				{
					return TraceEventType.Warning;
				}
			}

			static CloseAsyncResult()
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.onCloseOutputSession = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.OnCloseOutputSession));
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.onCloseInputSession = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.OnCloseInputSession));
			}

			public CloseAsyncResult(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.channel = channel;
				this.timeoutHelper = new TimeoutHelper(timeout);
				IAsyncResult asyncResult = channel.BeginCloseOutputSession(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.onCloseOutputSession, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return;
				}
				if (!this.HandleCloseOutputSession(asyncResult, true))
				{
					return;
				}
				base.Complete(true);
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult>(result);
			}

			private bool HandleCloseInputSession(IAsyncResult result, bool isStillSynchronous)
			{
				Message message = this.channel.messageSource.EndReceive(result);
				if (message != null)
				{
					using (message)
					{
						Type type = typeof(ProtocolException);
						object[] objArray = new object[] { message };
						ProtocolException protocolException = (ProtocolException)InvokeHelper.InvokeStaticMethod(type, "ReceiveShutdownReturnedNonNull", objArray);
						throw TraceUtility.ThrowHelperError(protocolException, message);
					}
				}
				this.channel.OnInputSessionClosed();
				return this.ScheduleCompleteClose(isStillSynchronous);
			}

			private bool HandleCloseOutputSession(IAsyncResult result, bool isStillSynchronous)
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.EndCloseOutputSession(result);
				if (this.channel.isInputSessionClosed)
				{
					return this.ScheduleCompleteClose(isStillSynchronous);
				}
				IAsyncResult asyncResult = this.channel.messageSource.BeginReceive(this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.onCloseInputSession, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				return this.HandleCloseInputSession(asyncResult, isStillSynchronous);
			}

			private static void OnCloseInputSession(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult asyncState = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult)result.AsyncState;
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = asyncState.HandleCloseInputSession(result, false);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					flag = true;
					exception = exception1;
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnCloseOutputSession(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult asyncState = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult)result.AsyncState;
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = asyncState.HandleCloseOutputSession(result, false);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					flag = true;
					exception = exception1;
				}
				if (flag)
				{
					asyncState.Complete(false, exception);
				}
			}

			private static void OnCompleteCloseScheduled(object state)
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult closeAsyncResult = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult)state;
				Exception exception = null;
				try
				{
					closeAsyncResult.OnCompleteCloseScheduled();
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				closeAsyncResult.Complete(false, exception);
			}

			private void OnCompleteCloseScheduled()
			{
				this.channel.CompleteClose(this.timeoutHelper.RemainingTime());
			}

			private bool ScheduleCompleteClose(bool isStillSynchronous)
			{
				if (!isStillSynchronous)
				{
					this.OnCompleteCloseScheduled();
					return true;
				}
				if (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.onCompleteCloseScheduled == null)
				{
					Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.onCompleteCloseScheduled = new Action<object>(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.OnCompleteCloseScheduled);
				}
				IOThreadScheduler.ScheduleCallbackNoFlow(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseAsyncResult.onCompleteCloseScheduled, this);
				return false;
			}
		}

		private class CloseOutputSessionAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel;

			private TimeoutHelper timeoutHelper;

			private readonly static AsyncCallback onWriteComplete;

			private readonly static Action<object> onEnterComplete;

			static CloseOutputSessionAsyncResult()
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult.onWriteComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult.OnWriteComplete));
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult.onEnterComplete = new Action<object>(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult.OnEnterComplete);
			}

			public CloseOutputSessionAsyncResult(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				channel.ThrowIfNotOpened();
				channel.ThrowIfFaulted();
				this.timeoutHelper = new TimeoutHelper(timeout);
				this.channel = channel;
				if (!channel.sendLock.Enter(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult.onEnterComplete, this))
				{
					return;
				}
				bool flag = false;
				bool flag1 = false;
				try
				{
					flag = this.WriteEndBytes();
					flag1 = true;
				}
				finally
				{
					if (!flag1)
					{
						this.Cleanup(false);
					}
				}
				if (flag)
				{
					this.Cleanup(true);
					base.Complete(true);
				}
			}

			private void Cleanup(bool success)
			{
				try
				{
					if (!success)
					{
						this.channel.Fault();
					}
				}
				finally
				{
					this.channel.sendLock.Exit();
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult>(result);
			}

			private void HandleWriteEndBytesComplete(IAsyncResult result)
			{
				this.channel.Connection.EndWrite(result);
				this.channel.OnOutputSessionClosed(ref this.timeoutHelper);
			}

			private static void OnEnterComplete(object state)
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult closeOutputSessionAsyncResult = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult)state;
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = closeOutputSessionAsyncResult.WriteEndBytes();
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					flag = true;
					exception = exception1;
				}
				if (flag)
				{
					closeOutputSessionAsyncResult.Cleanup(exception == null);
					closeOutputSessionAsyncResult.Complete(false, exception);
				}
			}

			private static void OnWriteComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult asyncState = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.HandleWriteEndBytesComplete(result);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				asyncState.Cleanup(exception == null);
				asyncState.Complete(false, exception);
			}

			private bool WriteEndBytes()
			{
				this.channel.ThrowIfFaulted();
				if (this.channel.isOutputSessionClosed)
				{
					return true;
				}
				this.channel.isOutputSessionClosed = true;
				IAsyncResult asyncResult = this.channel.Connection.BeginWrite(Microsoft.ServiceBus.Channels.SessionEncoder.EndBytes, 0, (int)Microsoft.ServiceBus.Channels.SessionEncoder.EndBytes.Length, true, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.CloseOutputSessionAsyncResult.onWriteComplete, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				this.HandleWriteEndBytesComplete(asyncResult);
				return true;
			}
		}

		private class ConnectionDuplexSession : IDuplexSession, IInputSession, IOutputSession, ISession
		{
			private Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel;

			private string id;

			private static Microsoft.ServiceBus.Channels.UriGenerator uriGenerator;

			public string Id
			{
				get
				{
					if (this.id == null)
					{
						lock (this.channel.ThisLock)
						{
							if (this.id == null)
							{
								this.id = Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession.UriGenerator.Next();
							}
						}
					}
					return this.id;
				}
			}

			private static Microsoft.ServiceBus.Channels.UriGenerator UriGenerator
			{
				get
				{
					if (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession.uriGenerator == null)
					{
						Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession.uriGenerator = new Microsoft.ServiceBus.Channels.UriGenerator();
					}
					return Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession.uriGenerator;
				}
			}

			private ConnectionDuplexSession(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel)
			{
				this.channel = channel;
			}

			public IAsyncResult BeginCloseOutputSession(AsyncCallback callback, object state)
			{
				return this.BeginCloseOutputSession(this.channel.DefaultCloseTimeout, callback, state);
			}

			public IAsyncResult BeginCloseOutputSession(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return this.channel.BeginCloseOutputSession(timeout, callback, state);
			}

			public void CloseOutputSession()
			{
				this.CloseOutputSession(this.channel.DefaultCloseTimeout);
			}

			public void CloseOutputSession(TimeSpan timeout)
			{
				this.channel.CloseOutputSession(timeout);
			}

			public static Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession CreateSession(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel, StreamUpgradeProvider upgrade)
			{
				if (!(upgrade is StreamSecurityUpgradeProvider))
				{
					return new Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession(channel);
				}
				return new Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession.SecureConnectionDuplexSession(channel);
			}

			public void EndCloseOutputSession(IAsyncResult result)
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.EndCloseOutputSession(result);
			}

			private class SecureConnectionDuplexSession : Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.ConnectionDuplexSession, ISecuritySession, ISession
			{
				private EndpointIdentity remoteIdentity;

				EndpointIdentity System.ServiceModel.Security.ISecuritySession.RemoteIdentity
				{
					get
					{
						if (this.remoteIdentity == null)
						{
							SecurityMessageProperty remoteSecurity = this.channel.RemoteSecurity;
							if (remoteSecurity != null && remoteSecurity.ServiceSecurityContext != null && InvokeHelper.InvokeInstanceGet(typeof(ServiceSecurityContext), remoteSecurity.ServiceSecurityContext, "IdentityClaim") != null && remoteSecurity.ServiceSecurityContext.PrimaryIdentity != null)
							{
								this.remoteIdentity = EndpointIdentity.CreateIdentity((Claim)InvokeHelper.InvokeInstanceGet(typeof(ServiceSecurityContext), remoteSecurity.ServiceSecurityContext, "IdentityClaim"));
							}
						}
						return this.remoteIdentity;
					}
				}

				public SecureConnectionDuplexSession(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel) : base(channel)
				{
				}
			}
		}

		private class SendAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel;

			private Message message;

			private byte[] buffer;

			private TimeoutHelper timeoutHelper;

			private readonly static AsyncCallback onWriteComplete;

			private readonly static Action<object> onEnterComplete;

			static SendAsyncResult()
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult.onWriteComplete = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult.OnWriteComplete));
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult.onEnterComplete = new Action<object>(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult.OnEnterComplete);
			}

			public SendAsyncResult(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel, Message message, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.timeoutHelper = new TimeoutHelper(timeout);
				this.channel = channel;
				this.message = message;
				if (!channel.sendLock.Enter(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult.onEnterComplete, this))
				{
					return;
				}
				bool flag = false;
				bool flag1 = false;
				try
				{
					flag = this.WriteCore();
					flag1 = true;
				}
				finally
				{
					if (!flag1)
					{
						this.Cleanup(false);
					}
				}
				if (flag)
				{
					this.Cleanup(true);
					base.Complete(true);
				}
			}

			private void Cleanup(bool success)
			{
				try
				{
					if (!success)
					{
						this.channel.Fault();
					}
				}
				finally
				{
					this.channel.sendLock.Exit();
				}
				if (this.buffer != null)
				{
					this.channel.bufferManager.ReturnBuffer(this.buffer);
					this.buffer = null;
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult>(result);
			}

			private static void OnEnterComplete(object state)
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult sendAsyncResult = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult)state;
				bool flag = false;
				Exception exception = null;
				try
				{
					flag = sendAsyncResult.WriteCore();
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					flag = true;
					exception = exception1;
				}
				if (flag)
				{
					sendAsyncResult.Cleanup(exception == null);
					sendAsyncResult.Complete(false, exception);
				}
			}

			private static void OnWriteComplete(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult asyncState = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.channel.Connection.EndWrite(result);
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				asyncState.Cleanup(exception == null);
				asyncState.Complete(false, exception);
			}

			private bool WriteCore()
			{
				this.channel.ThrowIfDisposedOrNotOpen();
				this.channel.ThrowIfOutputSessionClosed();
				bool allowOutputBatching = this.message.Properties.AllowOutputBatching;
				ArraySegment<byte> nums = this.channel.EncodeMessage(this.message);
				this.message = null;
				this.buffer = nums.Array;
				IAsyncResult asyncResult = this.channel.Connection.BeginWrite(nums.Array, nums.Offset, nums.Count, !allowOutputBatching, this.timeoutHelper.RemainingTime(), Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.SendAsyncResult.onWriteComplete, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				this.channel.Connection.EndWrite(asyncResult);
				return true;
			}
		}

		private class TryReceiveAsyncResult : AsyncResult
		{
			private Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel;

			private static AsyncCallback onReceive;

			private bool receiveSuccess;

			private Message message;

			static TryReceiveAsyncResult()
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult.onReceive = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ThunkAsyncCallback(new AsyncCallback(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult.OnReceive));
			}

			public TryReceiveAsyncResult(Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel channel, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				this.channel = channel;
				bool flag = false;
				try
				{
					IAsyncResult asyncResult = this.channel.BeginReceive(timeout, Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult.onReceive, this);
					if (asyncResult.CompletedSynchronously)
					{
						this.CompleteReceive(asyncResult);
						flag = true;
					}
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
					}
					flag = true;
				}
				if (flag)
				{
					base.Complete(true);
				}
			}

			private void CompleteReceive(IAsyncResult result)
			{
				this.message = this.channel.EndReceive(result);
				this.receiveSuccess = true;
			}

			public static bool End(IAsyncResult result, out Message message)
			{
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult tryReceiveAsyncResult = AsyncResult.End<Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult>(result);
				message = tryReceiveAsyncResult.message;
				return tryReceiveAsyncResult.receiveSuccess;
			}

			private static void OnReceive(IAsyncResult result)
			{
				if (result.CompletedSynchronously)
				{
					return;
				}
				Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult asyncState = (Microsoft.ServiceBus.Channels.FramingDuplexSessionChannel.TryReceiveAsyncResult)result.AsyncState;
				Exception exception = null;
				try
				{
					asyncState.CompleteReceive(result);
				}
				catch (TimeoutException timeoutException1)
				{
					TimeoutException timeoutException = timeoutException1;
					if (Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ShouldTraceInformation)
					{
						Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.TraceHandledException(timeoutException, TraceEventType.Information);
					}
				}
				catch (Exception exception2)
				{
					Exception exception1 = exception2;
					if (Fx.IsFatal(exception1))
					{
						throw;
					}
					exception = exception1;
				}
				asyncState.Complete(false, exception);
			}
		}
	}
}