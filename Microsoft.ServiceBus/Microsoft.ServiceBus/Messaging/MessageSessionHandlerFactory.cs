using Microsoft.ServiceBus;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class MessageSessionHandlerFactory
	{
		public static IMessageSessionAsyncHandlerFactory Create(IMessageSessionHandlerFactory factory)
		{
			return new MessageSessionHandlerFactory.SyncFactoryAdapter(factory);
		}

		public static IMessageSessionAsyncHandlerFactory Create(Type handlerType)
		{
			if (handlerType == null)
			{
				throw new ArgumentNullException("handlerType");
			}
			bool flag = false;
			bool flag1 = false;
			Type[] interfaces = handlerType.GetInterfaces();
			for (int i = 0; i < (int)interfaces.Length; i++)
			{
				Type type = interfaces[i];
				if (type == typeof(IMessageSessionAsyncHandler))
				{
					flag1 = true;
				}
				else if (type == typeof(IMessageSessionHandler))
				{
					flag = true;
				}
			}
			if (!flag1 && !flag)
			{
				string str = SRClient.SessionHandlerMissingInterfaces(handlerType.Name, typeof(IMessageSessionAsyncHandler).Name, typeof(IMessageSessionHandler).Name);
				throw new ArgumentException(str, "handlerType");
			}
			ConstructorInfo constructor = handlerType.GetConstructor(Type.EmptyTypes);
			if (constructor == null)
			{
				throw new ArgumentException(SRClient.SessionHandlerDoesNotHaveDefaultConstructor(handlerType.Name), "handlerType");
			}
			Func<object> func = Expression.Lambda<Func<object>>(Expression.New(constructor), new ParameterExpression[0]).Compile();
			return new MessageSessionHandlerFactory.DefaultMessageSessionHandlerFactory(func, !flag1);
		}

		private sealed class DefaultMessageSessionHandlerFactory : IMessageSessionAsyncHandlerFactory
		{
			private readonly Func<object> factoryFunction;

			private readonly bool wrapSyncHandler;

			public DefaultMessageSessionHandlerFactory(Func<object> factoryFunction, bool wrapSyncHandler)
			{
				this.factoryFunction = factoryFunction;
				this.wrapSyncHandler = wrapSyncHandler;
			}

			public IMessageSessionAsyncHandler CreateInstance(MessageSession session, BrokeredMessage message)
			{
				if (!this.wrapSyncHandler)
				{
					return (IMessageSessionAsyncHandler)this.factoryFunction();
				}
				return new MessageSessionHandlerFactory.SyncHandlerAdapter((IMessageSessionHandler)this.factoryFunction(), true);
			}

			public void DisposeInstance(IMessageSessionAsyncHandler handler)
			{
				IDisposable disposable = handler as IDisposable;
				if (disposable != null)
				{
					disposable.Dispose();
				}
			}
		}

		private sealed class SyncFactoryAdapter : IMessageSessionAsyncHandlerFactory
		{
			private readonly IMessageSessionHandlerFactory syncFactory;

			public SyncFactoryAdapter(IMessageSessionHandlerFactory syncFactory)
			{
				this.syncFactory = syncFactory;
			}

			IMessageSessionAsyncHandler Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandlerFactory.CreateInstance(MessageSession session, BrokeredMessage message)
			{
				return new MessageSessionHandlerFactory.SyncHandlerAdapter(this.syncFactory.CreateInstance(session, message), false);
			}

			void Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandlerFactory.DisposeInstance(IMessageSessionAsyncHandler handler)
			{
				MessageSessionHandlerFactory.SyncHandlerAdapter syncHandlerAdapter = (MessageSessionHandlerFactory.SyncHandlerAdapter)handler;
				this.syncFactory.DisposeInstance(syncHandlerAdapter.SyncHandler);
			}
		}

		private sealed class SyncHandlerAdapter : IMessageSessionAsyncHandler, IDisposable
		{
			private readonly IMessageSessionHandler syncHandler;

			private readonly bool isDefaultFactory;

			public IMessageSessionHandler SyncHandler
			{
				get
				{
					return this.syncHandler;
				}
			}

			public SyncHandlerAdapter(IMessageSessionHandler syncHandler, bool isDefaultFactory)
			{
				this.syncHandler = syncHandler;
				this.isDefaultFactory = isDefaultFactory;
			}

			public void Dispose()
			{
				if (this.isDefaultFactory)
				{
					IDisposable disposable = this.syncHandler as IDisposable;
					if (disposable != null)
					{
						disposable.Dispose();
					}
				}
			}

			Task Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandler.OnCloseSessionAsync(MessageSession session)
			{
				return Task.Factory.StartNew(() => this.syncHandler.OnCloseSession(session));
			}

			Task Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandler.OnMessageAsync(MessageSession session, BrokeredMessage message)
			{
				return Task.Factory.StartNew(() => this.syncHandler.OnMessage(session, message));
			}

			Task Microsoft.ServiceBus.Messaging.IMessageSessionAsyncHandler.OnSessionLostAsync(Exception e)
			{
				return Task.Factory.StartNew(() => this.syncHandler.OnSessionLost(e));
			}
		}
	}
}