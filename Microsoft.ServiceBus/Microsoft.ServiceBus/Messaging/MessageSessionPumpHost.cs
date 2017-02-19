using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class MessageSessionPumpHost
	{
		private readonly object syncRoot;

		private readonly string entityName;

		private readonly IMessageSessionEntity entity;

		private bool markedForClosed;

		private MessageSessionPump pump;

		private object ThisLock
		{
			get
			{
				return this.syncRoot;
			}
		}

		public MessageSessionPumpHost(object syncRoot, string entityName, IMessageSessionEntity entity)
		{
			this.syncRoot = syncRoot;
			this.entityName = entityName;
			this.entity = entity;
		}

		public void Abort()
		{
			MessageSessionPumpHost.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new MessageSessionPumpHost.CloseOrAbortAsyncResult(this, true, TimeSpan.MaxValue, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new MessageSessionPumpHost.CloseOrAbortAsyncResult(this, false, timeout, callback, state)).Start();
		}

		private IAsyncResult BeginRegisterSessionHandler(IMessageSessionAsyncHandlerFactory factory, SessionHandlerOptions options, AsyncCallback callback, object state)
		{
			return (new MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult(this, factory, options, callback, state)).Start();
		}

		private IAsyncResult BeginRegisterSessionHandler(Type handlerType, SessionHandlerOptions options, AsyncCallback callback, object state)
		{
			IMessageSessionAsyncHandlerFactory messageSessionAsyncHandlerFactory = MessageSessionHandlerFactory.Create(handlerType);
			return (new MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult(this, messageSessionAsyncHandlerFactory, options, callback, state)).Start();
		}

		public void Close(TimeSpan timeout)
		{
			MessageSessionPumpHost.CloseOrAbortAsyncResult closeOrAbortAsyncResult = new MessageSessionPumpHost.CloseOrAbortAsyncResult(this, false, timeout, null, null);
			closeOrAbortAsyncResult.RunSynchronously();
		}

		public void EndClose(IAsyncResult result)
		{
			AsyncResult<MessageSessionPumpHost.CloseOrAbortAsyncResult>.End(result);
		}

		private void EndRegisterSessionHandler(IAsyncResult result)
		{
			AsyncResult<MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult>.End(result);
		}

		public void RegisterSessionHandler(Type handlerType)
		{
			IMessageSessionAsyncHandlerFactory messageSessionAsyncHandlerFactory = MessageSessionHandlerFactory.Create(handlerType);
			MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult registerSessionHandlerFactoryAsyncResult = new MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult(this, messageSessionAsyncHandlerFactory, null, null, null);
			registerSessionHandlerFactoryAsyncResult.RunSynchronously();
		}

		public void RegisterSessionHandler(Type handlerType, SessionHandlerOptions options)
		{
			IMessageSessionAsyncHandlerFactory messageSessionAsyncHandlerFactory = MessageSessionHandlerFactory.Create(handlerType);
			MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult registerSessionHandlerFactoryAsyncResult = new MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult(this, messageSessionAsyncHandlerFactory, options, null, null);
			registerSessionHandlerFactoryAsyncResult.RunSynchronously();
		}

		public Task RegisterSessionHandlerAsync(Type handlerType)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginRegisterSessionHandler(handlerType, null, c, s), (IAsyncResult r) => this.EndRegisterSessionHandler(r));
		}

		public Task RegisterSessionHandlerAsync(Type handlerType, SessionHandlerOptions options)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginRegisterSessionHandler(handlerType, options, c, s), (IAsyncResult r) => this.EndRegisterSessionHandler(r));
		}

		public void RegisterSessionHandlerFactory(IMessageSessionHandlerFactory factory, SessionHandlerOptions options)
		{
			IMessageSessionAsyncHandlerFactory messageSessionAsyncHandlerFactory = MessageSessionHandlerFactory.Create(factory);
			MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult registerSessionHandlerFactoryAsyncResult = new MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult(this, messageSessionAsyncHandlerFactory, options, null, null);
			registerSessionHandlerFactoryAsyncResult.RunSynchronously();
		}

		public void RegisterSessionHandlerFactory(IMessageSessionAsyncHandlerFactory factory, SessionHandlerOptions options)
		{
			MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult registerSessionHandlerFactoryAsyncResult = new MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult(this, factory, options, null, null);
			registerSessionHandlerFactoryAsyncResult.RunSynchronously();
		}

		public Task RegisterSessionHandlerFactoryAsync(IMessageSessionHandlerFactory factory, SessionHandlerOptions options)
		{
			IMessageSessionAsyncHandlerFactory messageSessionAsyncHandlerFactory = MessageSessionHandlerFactory.Create(factory);
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginRegisterSessionHandler(messageSessionAsyncHandlerFactory, options, c, s), (IAsyncResult r) => this.EndRegisterSessionHandler(r));
		}

		public Task RegisterSessionHandlerFactoryAsync(IMessageSessionAsyncHandlerFactory factory, SessionHandlerOptions options)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginRegisterSessionHandler(factory, options, c, s), (IAsyncResult r) => this.EndRegisterSessionHandler(r));
		}

		private sealed class CloseOrAbortAsyncResult : IteratorAsyncResult<MessageSessionPumpHost.CloseOrAbortAsyncResult>
		{
			private readonly MessageSessionPumpHost owner;

			private readonly bool shouldAbort;

			private MessageSessionPump pumpToClose;

			public CloseOrAbortAsyncResult(MessageSessionPumpHost owner, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.shouldAbort = shouldAbort;
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSessionPumpHost.CloseOrAbortAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				object obj = null;
				bool flag = false;
				try
				{
					object thisLock = base.ThisLock;
					object obj1 = thisLock;
					obj = thisLock;
					Monitor.Enter(obj1, ref flag);
					this.owner.markedForClosed = true;
					this.pumpToClose = this.owner.pump;
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(obj);
					}
				}
				if (this.pumpToClose != null)
				{
					if (!this.shouldAbort)
					{
						MessageSessionPumpHost.CloseOrAbortAsyncResult closeOrAbortAsyncResult = this;
						IteratorAsyncResult<MessageSessionPumpHost.CloseOrAbortAsyncResult>.BeginCall beginCall = (MessageSessionPumpHost.CloseOrAbortAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.pumpToClose.BeginClose(t, c, s);
						IteratorAsyncResult<MessageSessionPumpHost.CloseOrAbortAsyncResult>.EndCall endCall = (MessageSessionPumpHost.CloseOrAbortAsyncResult thisPtr, IAsyncResult r) => thisPtr.pumpToClose.EndClose(r);
						yield return closeOrAbortAsyncResult.CallAsync(beginCall, endCall, (MessageSessionPumpHost.CloseOrAbortAsyncResult thisPtr, TimeSpan t) => thisPtr.pumpToClose.Close(t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					else
					{
						this.pumpToClose.Abort();
					}
				}
			}
		}

		private sealed class RegisterSessionHandlerFactoryAsyncResult : IteratorAsyncResult<MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> OnFinally;

			private readonly MessageSessionPumpHost owner;

			private readonly IMessageSessionAsyncHandlerFactory factory;

			private readonly SessionHandlerOptions options;

			private bool ownsRegistration;

			static RegisterSessionHandlerFactoryAsyncResult()
			{
				MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult.OnFinally = new Action<AsyncResult, Exception>(MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult.Finally);
			}

			public RegisterSessionHandlerFactoryAsyncResult(MessageSessionPumpHost owner, IMessageSessionAsyncHandlerFactory factory, SessionHandlerOptions options, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.owner = owner;
				this.factory = factory;
				this.options = options;
				MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult registerSessionHandlerFactoryAsyncResult = this;
				registerSessionHandlerFactoryAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(registerSessionHandlerFactoryAsyncResult.OnCompleting, MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult.OnFinally);
			}

			private static void Finally(AsyncResult asyncResult, Exception exception)
			{
				MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult registerSessionHandlerFactoryAsyncResult = (MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult)asyncResult;
				if (exception != null && registerSessionHandlerFactoryAsyncResult.ownsRegistration && registerSessionHandlerFactoryAsyncResult.owner.pump != null)
				{
					registerSessionHandlerFactoryAsyncResult.owner.pump.Abort();
					lock (registerSessionHandlerFactoryAsyncResult.owner.ThisLock)
					{
						registerSessionHandlerFactoryAsyncResult.owner.pump = null;
					}
				}
			}

			protected override IEnumerator<IteratorAsyncResult<MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				object obj = null;
				bool flag = false;
				try
				{
					object thisLock = this.owner.ThisLock;
					object obj1 = thisLock;
					obj = thisLock;
					Monitor.Enter(obj1, ref flag);
					if (this.owner.markedForClosed)
					{
						throw new OperationCanceledException(SRClient.MessageEntityDisposed);
					}
					if (this.owner.pump != null)
					{
						throw new InvalidOperationException(SRClient.SessionHandlerAlreadyRegistered);
					}
					this.ownsRegistration = true;
					this.owner.pump = new MessageSessionPump(this.owner.entityName, this.owner.entity, this.factory, this.options);
				}
				finally
				{
					if (flag)
					{
						Monitor.Exit(obj);
					}
				}
				MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult registerSessionHandlerFactoryAsyncResult = this;
				IteratorAsyncResult<MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult>.BeginCall beginCall = (MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.pump.BeginOpen(c, s);
				yield return registerSessionHandlerFactoryAsyncResult.CallAsync(beginCall, (MessageSessionPumpHost.RegisterSessionHandlerFactoryAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.pump.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
			}
		}
	}
}