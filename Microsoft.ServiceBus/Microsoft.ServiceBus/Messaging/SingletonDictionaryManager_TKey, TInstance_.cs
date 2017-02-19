using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal abstract class SingletonDictionaryManager<TKey, TInstance> : CommunicationObject
	{
		private readonly Dictionary<TKey, SingletonDictionaryManager<TKey, TInstance>.SingletonContext> instances;

		private readonly Dictionary<TKey, List<AsyncWaiter>> pendingOperations;

		protected override TimeSpan DefaultCloseTimeout
		{
			get
			{
				return Constants.DefaultOperationTimeout;
			}
		}

		protected override TimeSpan DefaultOpenTimeout
		{
			get
			{
				return Constants.DefaultOperationTimeout;
			}
		}

		protected SingletonDictionaryManager() : this(EqualityComparer<TKey>.Default)
		{
		}

		protected SingletonDictionaryManager(IEqualityComparer<TKey> comparer)
		{
			this.instances = new Dictionary<TKey, SingletonDictionaryManager<TKey, TInstance>.SingletonContext>(comparer);
			this.pendingOperations = new Dictionary<TKey, List<AsyncWaiter>>(comparer);
		}

		protected IAsyncResult BeginGetInstance(TKey key, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposedOrNotOpen();
			return (new SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult(this, key, timeout, callback, state)).Start();
		}

		protected IAsyncResult BeginLoadInstance(TKey key, object loadingContext, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposedOrNotOpen();
			return (new SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult(this, key, loadingContext, timeout, callback, state)).Start();
		}

		protected IAsyncResult BeginUnloadInstance(TKey key, object unloadingContext, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state)
		{
			base.ThrowIfDisposedOrNotOpen();
			return (new SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult(this, key, unloadingContext, shouldAbort, timeout, callback, state)).Start();
		}

		private IAsyncResult BeginWaitPendingOperation(TKey key, TimeSpan timeout, AsyncCallback callback, object state)
		{
			List<AsyncWaiter> asyncWaiters;
			if (!this.pendingOperations.TryGetValue(key, out asyncWaiters))
			{
				asyncWaiters = new List<AsyncWaiter>();
				this.pendingOperations.Add(key, asyncWaiters);
			}
			AsyncWaiter asyncWaiter = new AsyncWaiter(timeout, callback, state);
			asyncWaiters.Add(asyncWaiter);
			return asyncWaiter;
		}

		protected TInstance EndGetInstance(IAsyncResult result)
		{
			return SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult.End(result);
		}

		protected TInstance EndLoadInstance(IAsyncResult result)
		{
			return SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult.End(result);
		}

		protected void EndUnloadInstance(IAsyncResult result)
		{
			SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult.End(result);
		}

		private void EndWaitPendingOperation(IAsyncResult result)
		{
			AsyncWaiter.End(result);
		}

		protected TInstance GetInstance(TKey key, TimeSpan timeout)
		{
			SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult getInstanceAsyncResult = new SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult(this, key, timeout, null, null);
			getInstanceAsyncResult.RunSynchronously();
			return getInstanceAsyncResult.Instance;
		}

		protected IEnumerable<SingletonDictionaryManager<TKey, TInstance>.SingletonContext> GetInstanceContexts()
		{
			IEnumerable<SingletonDictionaryManager<TKey, TInstance>.SingletonContext> list;
			lock (base.ThisLock)
			{
				list = this.instances.Values.ToList<SingletonDictionaryManager<TKey, TInstance>.SingletonContext>();
			}
			return list;
		}

		protected List<TKey> GetInstanceKeys()
		{
			List<TKey> tKeys;
			lock (base.ThisLock)
			{
				tKeys = new List<TKey>(this.instances.Keys);
			}
			return tKeys;
		}

		protected SingletonDictionaryManager<TKey, TInstance>.SingletonContext[] GetInstances()
		{
			SingletonDictionaryManager<TKey, TInstance>.SingletonContext[] array;
			lock (base.ThisLock)
			{
				array = this.instances.Values.ToArray<SingletonDictionaryManager<TKey, TInstance>.SingletonContext>();
			}
			return array;
		}

		internal bool IsInstanceLoadingOrLoaded(TKey key)
		{
			SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext;
			bool flag = false;
			lock (base.ThisLock)
			{
				if (this.instances.TryGetValue(key, out singletonContext) && singletonContext.State != SingletonDictionaryManager<TKey, TInstance>.SingletonState.Unloading)
				{
					flag = true;
				}
			}
			return flag;
		}

		protected TInstance LoadInstance(TKey key, object loadingContext, TimeSpan timeout)
		{
			SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult loadInstanceAsyncResult = new SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult(this, key, loadingContext, timeout, null, null);
			loadInstanceAsyncResult.RunSynchronously();
			return loadInstanceAsyncResult.Instance;
		}

		protected override void OnAbort()
		{
			lock (base.ThisLock)
			{
				foreach (TKey list in this.instances.Keys.ToList<TKey>())
				{
					SingletonDictionaryManager<TKey, TInstance>.SingletonContext item = this.instances[list];
					if (item.State != SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loaded)
					{
						continue;
					}
					this.instances.Remove(list);
					this.OnAbortInstance(item, list, item.Instance, null);
				}
			}
		}

		protected abstract void OnAbortInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, TInstance instance, object unloadingContext);

		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult(this, timeout, callback, state);
		}

		protected abstract IAsyncResult OnBeginCloseInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, TInstance instance, object unloadingContext, TimeSpan timeout, AsyncCallback callback, object state);

		protected abstract IAsyncResult OnBeginCreateInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, object loadingContext, TimeSpan timeout, AsyncCallback callback, object state);

		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected abstract IAsyncResult OnBeginOpenInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, TInstance instance, TimeSpan timeout, AsyncCallback callback, object state);

		protected virtual IAsyncResult OnBeginWaitPendingOperations(TInstance instance, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new CompletedAsyncResult(callback, state);
		}

		protected override void OnClose(TimeSpan timeout)
		{
			this.OnEndClose(this.OnBeginClose(timeout, null, null));
		}

		protected abstract void OnCloseInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, TInstance instance, object unloadingContext, TimeSpan timeout);

		protected abstract TInstance OnCreateInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, object loadingContext, TimeSpan timeout);

		protected override void OnEndClose(IAsyncResult result)
		{
			SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult.End(result);
		}

		protected abstract void OnEndCloseInstance(IAsyncResult result);

		protected abstract TInstance OnEndCreateInstance(IAsyncResult result);

		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected abstract void OnEndOpenInstance(IAsyncResult result);

		protected virtual void OnEndWaitPendingOperations(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		protected virtual void OnGetInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, TInstance instance, object loadingContext)
		{
		}

		protected override void OnOpen(TimeSpan timeout)
		{
		}

		protected abstract void OnOpenInstance(SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext, TKey key, TInstance instance, TimeSpan timeout);

		protected void OnTryRemoveSingletonContext(TKey key)
		{
			lock (base.ThisLock)
			{
				this.instances.Remove(key);
			}
		}

		private void SignalPendingOperations(TKey key)
		{
			List<AsyncWaiter> asyncWaiters;
			lock (base.ThisLock)
			{
				if (this.pendingOperations.TryGetValue(key, out asyncWaiters))
				{
					this.pendingOperations.Remove(key);
				}
			}
			if (asyncWaiters != null)
			{
				foreach (AsyncWaiter asyncWaiter in asyncWaiters)
				{
					IOThreadScheduler.ScheduleCallbackNoFlow((object state) => ((AsyncWaiter)state).Signal(), asyncWaiter);
				}
			}
		}

		protected bool TryGetSingletonContext(TKey key, out SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext)
		{
			bool flag;
			lock (base.ThisLock)
			{
				flag = this.instances.TryGetValue(key, out singletonContext);
			}
			return flag;
		}

		protected void UnloadInstance(TKey key, object unloadingContext, bool shouldAbort, TimeSpan timeout)
		{
			SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult unloadInstanceAsyncResult = new SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult(this, key, unloadingContext, shouldAbort, timeout, null, null);
			unloadInstanceAsyncResult.RunSynchronously();
		}

		private sealed class CloseAsyncResult : AsyncResult
		{
			private readonly static AsyncCallback OnUnloadCompletedCallback;

			private readonly SingletonDictionaryManager<TKey, TInstance> owner;

			private Microsoft.ServiceBus.Common.TimeoutHelper timeoutHelper;

			private readonly object syncRoot;

			private volatile Exception firstException;

			private int waitCount;

			static CloseAsyncResult()
			{
				SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult.OnUnloadCompletedCallback = new AsyncCallback(SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult.OnUnloadCompleted);
			}

			public CloseAsyncResult(SingletonDictionaryManager<TKey, TInstance> owner, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				List<TKey> tKeys;
				this.owner = owner;
				this.syncRoot = new object();
				this.timeoutHelper = new Microsoft.ServiceBus.Common.TimeoutHelper(timeout);
				lock (this.owner.ThisLock)
				{
					tKeys = new List<TKey>(this.owner.instances.Keys);
				}
				this.IncreaseWaitCount();
				foreach (TKey tKey in tKeys)
				{
					try
					{
						this.IncreaseWaitCount();
						(new SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult(this.owner, tKey, null, false, this.timeoutHelper.RemainingTime(), SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult.OnUnloadCompletedCallback, this)).Start();
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.DecreaseWaitCount(exception, true);
					}
				}
				this.DecreaseWaitCount(null, true);
			}

			private void DecreaseWaitCount(Exception exception, bool isCompletedSynchronously)
			{
				bool flag = false;
				lock (this.syncRoot)
				{
					if (this.firstException == null)
					{
						this.firstException = exception;
					}
					SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult closeAsyncResult = this;
					int num = closeAsyncResult.waitCount - 1;
					int num1 = num;
					closeAsyncResult.waitCount = num;
					if (num1 == 0)
					{
						flag = true;
					}
				}
				if (flag)
				{
					base.Complete(isCompletedSynchronously, this.firstException);
				}
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult.End<SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult>(result);
			}

			private void IncreaseWaitCount()
			{
				lock (this.syncRoot)
				{
					SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult closeAsyncResult = this;
					closeAsyncResult.waitCount = closeAsyncResult.waitCount + 1;
				}
			}

			private static void OnUnloadCompleted(IAsyncResult result)
			{
				SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult asyncState = (SingletonDictionaryManager<TKey, TInstance>.CloseAsyncResult)result.AsyncState;
				try
				{
					SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult.End(result);
					asyncState.DecreaseWaitCount(null, result.CompletedSynchronously);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					asyncState.DecreaseWaitCount(exception, result.CompletedSynchronously);
				}
			}
		}

		private sealed class GetInstanceAsyncResult : IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult>
		{
			private readonly SingletonDictionaryManager<TKey, TInstance> owner;

			private readonly TKey key;

			private SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext;

			private volatile bool shouldReleaseLock;

			public TInstance Instance
			{
				get;
				private set;
			}

			public GetInstanceAsyncResult(SingletonDictionaryManager<TKey, TInstance> owner, TKey key, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.key = key;
				this.Instance = default(TInstance);
			}

			public static new TInstance End(IAsyncResult result)
			{
				return AsyncResult<SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult>.End(result).Instance;
			}

			protected override IEnumerator<IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
					Monitor.Enter(this.owner.ThisLock);
					this.shouldReleaseLock = true;
					try
					{
						this.owner.ThrowIfDisposedOrNotOpen();
						if (!this.owner.instances.TryGetValue(this.key, out this.singletonContext))
						{
							break;
						}
						if (this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loaded)
						{
							this.Instance = this.singletonContext.Instance;
							break;
						}
						else if (base.RemainingTime() <= TimeSpan.Zero)
						{
							base.Complete(new TimeoutException(SRCore.TimeoutOnOperation(base.OriginalTimeout)));
							break;
						}
						else if (this.singletonContext.State != SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loading)
						{
							if (this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Unloading)
							{
								goto Label0;
							}
							CultureInfo invariantCulture = CultureInfo.InvariantCulture;
							object[] state = new object[] { this.singletonContext.State, typeof(SingletonDictionaryManager<TKey, TInstance>).Name };
							string str = string.Format(invariantCulture, "{0} was not recognized. This is likely a bug in {1}.", state);
							Fx.AssertAndFailFastService(str);
							break;
						}
						else
						{
							SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult getInstanceAsyncResult = this;
							IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult>.BeginCall beginCall = (SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
								IAsyncResult asyncResult;
								try
								{
									thisPtr.shouldReleaseLock = false;
									asyncResult = thisPtr.owner.BeginWaitPendingOperation(thisPtr.key, t, c, s);
								}
								finally
								{
									Monitor.Exit(thisPtr.owner.ThisLock);
								}
								return asyncResult;
							};
							yield return getInstanceAsyncResult.CallAsync(beginCall, (SingletonDictionaryManager<TKey, TInstance>.GetInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.EndWaitPendingOperation(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						}
					}
					finally
					{
						if (this.shouldReleaseLock)
						{
							Monitor.Exit(this.owner.ThisLock);
						}
					}
				}
			Label1:
				yield break;
			Label0:
				goto Label1;
			}
		}

		private sealed class LoadInstanceAsyncResult : IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> CompletingDelegate;

			private readonly SingletonDictionaryManager<TKey, TInstance> owner;

			private readonly object loadingContext;

			private readonly TKey key;

			private SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext;

			private volatile bool shouldReleaseLock;

			private bool ownsLoading;

			private bool created;

			public TInstance Instance
			{
				get;
				private set;
			}

			static LoadInstanceAsyncResult()
			{
				SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult.CompletingDelegate = new Action<AsyncResult, Exception>(SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult.OnFinally);
			}

			public LoadInstanceAsyncResult(SingletonDictionaryManager<TKey, TInstance> owner, TKey key, object loadingContext, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.owner = owner;
				this.key = key;
				this.loadingContext = loadingContext;
				SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult loadInstanceAsyncResult = this;
				loadInstanceAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(loadInstanceAsyncResult.OnCompleting, SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult.CompletingDelegate);
			}

			public static new TInstance End(IAsyncResult result)
			{
				return AsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>.End(result).Instance;
			}

			protected override IEnumerator<IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
					Monitor.Enter(this.owner.ThisLock);
					this.shouldReleaseLock = true;
					try
					{
						this.owner.ThrowIfDisposedOrNotOpen();
						if (!this.owner.instances.TryGetValue(this.key, out this.singletonContext))
						{
							this.singletonContext = new SingletonDictionaryManager<TKey, TInstance>.SingletonContext(SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loading);
							this.owner.instances.Add(this.key, this.singletonContext);
							this.ownsLoading = true;
							break;
						}
						else if (this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loaded)
						{
							this.Instance = this.singletonContext.Instance;
							break;
						}
						else if (base.RemainingTime() <= TimeSpan.Zero)
						{
							base.Complete(new TimeoutException(SRCore.TimeoutOnOperation(base.OriginalTimeout)));
							goto Label0;
						}
						else if (this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loading || this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Unloading)
						{
							SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult loadInstanceAsyncResult = this;
							IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>.BeginCall beginCall = (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
								IAsyncResult asyncResult;
								try
								{
									thisPtr.shouldReleaseLock = false;
									asyncResult = thisPtr.owner.BeginWaitPendingOperation(thisPtr.key, t, c, s);
								}
								finally
								{
									Monitor.Exit(thisPtr.owner.ThisLock);
								}
								return asyncResult;
							};
							yield return loadInstanceAsyncResult.CallAsync(beginCall, (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.EndWaitPendingOperation(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						}
						else
						{
							CultureInfo invariantCulture = CultureInfo.InvariantCulture;
							object[] state = new object[] { this.singletonContext.State, typeof(SingletonDictionaryManager<TKey, TInstance>).Name };
							string str = string.Format(invariantCulture, "{0} was not recognized. This is likely a bug in {1}.", state);
							Fx.AssertAndFailFastService(str);
							break;
						}
					}
					finally
					{
						if (this.shouldReleaseLock)
						{
							Monitor.Exit(this.owner.ThisLock);
						}
					}
				}
				if (!this.ownsLoading)
				{
					this.owner.OnGetInstance(this.singletonContext, this.key, this.singletonContext.Instance, this.loadingContext);
				}
				else
				{
					SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult loadInstanceAsyncResult1 = this;
					IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>.BeginCall beginCall1 = (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.OnBeginCreateInstance(thisPtr.singletonContext, thisPtr.key, thisPtr.loadingContext, t, c, s);
					IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>.EndCall instance = (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.singletonContext.Instance = thisPtr.owner.OnEndCreateInstance(r);
					yield return loadInstanceAsyncResult1.CallAsync(beginCall1, instance, (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, TimeSpan t) => thisPtr.singletonContext.Instance = thisPtr.owner.OnCreateInstance(thisPtr.singletonContext, thisPtr.key, thisPtr.loadingContext, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					this.created = true;
					SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult loadInstanceAsyncResult2 = this;
					IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>.BeginCall beginCall2 = (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.OnBeginOpenInstance(thisPtr.singletonContext, thisPtr.key, thisPtr.singletonContext.Instance, t, c, s);
					IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult>.EndCall endCall = (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.OnEndOpenInstance(r);
					yield return loadInstanceAsyncResult2.CallAsync(beginCall2, endCall, (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult thisPtr, TimeSpan t) => thisPtr.owner.OnOpenInstance(thisPtr.singletonContext, thisPtr.key, thisPtr.singletonContext.Instance, t), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			Label0:
				yield break;
			}

			private static void OnFinally(AsyncResult result, Exception exception)
			{
				SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult instance = (SingletonDictionaryManager<TKey, TInstance>.LoadInstanceAsyncResult)result;
				if (instance.ownsLoading)
				{
					if (exception != null || instance.owner.State != CommunicationState.Opened)
					{
						try
						{
							if (instance.created)
							{
								instance.owner.OnAbortInstance(instance.singletonContext, instance.key, instance.singletonContext.Instance, null);
							}
						}
						finally
						{
							lock (instance.owner.ThisLock)
							{
								instance.owner.instances.Remove(instance.key);
							}
							instance.owner.SignalPendingOperations(instance.key);
						}
					}
					else
					{
						lock (instance.owner.ThisLock)
						{
							instance.Instance = instance.singletonContext.Instance;
							instance.singletonContext.State = SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loaded;
						}
						instance.owner.SignalPendingOperations(instance.key);
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteSingletonManagerLoadSucceeded(instance.key.ToString()));
					}
				}
			}
		}

		public sealed class SingletonContext : IExtensibleObject<SingletonDictionaryManager<TKey, TInstance>.SingletonContext>
		{
			private IExtensionCollection<SingletonDictionaryManager<TKey, TInstance>.SingletonContext> extensions;

			public IExtensionCollection<SingletonDictionaryManager<TKey, TInstance>.SingletonContext> Extensions
			{
				get
				{
					if (this.extensions == null)
					{
						Interlocked.CompareExchange<IExtensionCollection<SingletonDictionaryManager<TKey, TInstance>.SingletonContext>>(ref this.extensions, new ExtensionCollection<SingletonDictionaryManager<TKey, TInstance>.SingletonContext>(this), null);
					}
					return this.extensions;
				}
			}

			internal TInstance Instance
			{
				get;
				set;
			}

			internal SingletonDictionaryManager<TKey, TInstance>.SingletonState State
			{
				get;
				set;
			}

			public SingletonContext(SingletonDictionaryManager<TKey, TInstance>.SingletonState initialState)
			{
				this.State = initialState;
			}
		}

		internal enum SingletonState
		{
			Loading,
			Loaded,
			Unloading
		}

		private sealed class UnloadInstanceAsyncResult : IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> CompletingDelegate;

			private readonly SingletonDictionaryManager<TKey, TInstance> owner;

			private readonly TKey key;

			private readonly object unloadingContext;

			private readonly bool shouldAbort;

			private readonly TimeSpan closeTimeout;

			private bool ownsUnloading;

			private volatile bool shouldReleaseLock;

			private SingletonDictionaryManager<TKey, TInstance>.SingletonContext singletonContext;

			private TInstance instance;

			private bool waitPendingOperationsCalled;

			static UnloadInstanceAsyncResult()
			{
				SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult.CompletingDelegate = new Action<AsyncResult, Exception>(SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult.OnFinally);
			}

			public UnloadInstanceAsyncResult(SingletonDictionaryManager<TKey, TInstance> owner, TKey key, object unloadingContext, bool shouldAbort, TimeSpan timeout, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				this.owner = owner;
				this.key = key;
				this.unloadingContext = unloadingContext;
				this.shouldAbort = shouldAbort;
				this.closeTimeout = timeout;
				SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult unloadInstanceAsyncResult = this;
				unloadInstanceAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(unloadInstanceAsyncResult.OnCompleting, SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult.CompletingDelegate);
			}

			public static new void End(IAsyncResult result)
			{
				AsyncResult<SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult>.End(result);
			}

			protected override IEnumerator<IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
					Monitor.Enter(this.owner.ThisLock);
					this.shouldReleaseLock = true;
					try
					{
						if (!this.owner.instances.TryGetValue(this.key, out this.singletonContext))
						{
							break;
						}
						if (this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loaded)
						{
							this.singletonContext.State = SingletonDictionaryManager<TKey, TInstance>.SingletonState.Unloading;
							this.instance = this.singletonContext.Instance;
							this.ownsUnloading = true;
							break;
						}
						else if (this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Loading || this.singletonContext.State == SingletonDictionaryManager<TKey, TInstance>.SingletonState.Unloading)
						{
							SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult unloadInstanceAsyncResult = this;
							IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult>.BeginCall beginCall = (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
								IAsyncResult asyncResult;
								try
								{
									thisPtr.shouldReleaseLock = false;
									asyncResult = thisPtr.owner.BeginWaitPendingOperation(thisPtr.key, t, c, s);
								}
								finally
								{
									Monitor.Exit(thisPtr.owner.ThisLock);
								}
								return asyncResult;
							};
							yield return unloadInstanceAsyncResult.CallAsync(beginCall, (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.EndWaitPendingOperation(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						}
						else
						{
							CultureInfo invariantCulture = CultureInfo.InvariantCulture;
							object[] state = new object[] { this.singletonContext.State, typeof(SingletonDictionaryManager<TKey, TInstance>).Name };
							string str = string.Format(invariantCulture, "{0} was not recognized. This is likely a bug in {1}.", state);
							Fx.AssertAndFailFastService(str);
							break;
						}
					}
					finally
					{
						if (this.shouldReleaseLock)
						{
							Monitor.Exit(this.owner.ThisLock);
						}
					}
				}
				if (this.ownsUnloading)
				{
					if (!this.waitPendingOperationsCalled)
					{
						this.waitPendingOperationsCalled = true;
						SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult unloadInstanceAsyncResult1 = this;
						IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult>.BeginCall beginCall1 = (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.OnBeginWaitPendingOperations(thisPtr.instance, thisPtr.closeTimeout, c, s);
						yield return unloadInstanceAsyncResult1.CallAsync(beginCall1, (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.OnEndWaitPendingOperations(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					if (!this.shouldAbort)
					{
						SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult unloadInstanceAsyncResult2 = this;
						IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult>.BeginCall beginCall2 = (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.OnBeginCloseInstance(thisPtr.singletonContext, thisPtr.key, thisPtr.instance, thisPtr.unloadingContext, thisPtr.closeTimeout, c, s);
						IteratorAsyncResult<SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult>.EndCall endCall = (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.owner.OnEndCloseInstance(r);
						yield return unloadInstanceAsyncResult2.CallAsync(beginCall2, endCall, (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult thisPtr, TimeSpan t) => thisPtr.owner.OnCloseInstance(thisPtr.singletonContext, thisPtr.key, thisPtr.instance, thisPtr.unloadingContext, thisPtr.closeTimeout), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
				}
			}

			private static void OnFinally(AsyncResult result, Exception exception)
			{
				SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult unloadInstanceAsyncResult = (SingletonDictionaryManager<TKey, TInstance>.UnloadInstanceAsyncResult)result;
				if (unloadInstanceAsyncResult.ownsUnloading)
				{
					try
					{
						if (exception != null || unloadInstanceAsyncResult.shouldAbort)
						{
							unloadInstanceAsyncResult.owner.OnAbortInstance(unloadInstanceAsyncResult.singletonContext, unloadInstanceAsyncResult.key, unloadInstanceAsyncResult.instance, unloadInstanceAsyncResult.unloadingContext);
						}
					}
					finally
					{
						lock (unloadInstanceAsyncResult.owner.ThisLock)
						{
							unloadInstanceAsyncResult.owner.instances.Remove(unloadInstanceAsyncResult.key);
						}
						unloadInstanceAsyncResult.owner.SignalPendingOperations(unloadInstanceAsyncResult.key);
					}
				}
			}
		}
	}
}