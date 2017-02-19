using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class NetworkDetector
	{
		private static Dictionary<int, Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext> cache;

		private readonly static Dictionary<int, List<AsyncWaiter>> pendingOperations;

		private static object ThisLock;

		static NetworkDetector()
		{
			Microsoft.ServiceBus.Messaging.NetworkDetector.cache = new Dictionary<int, Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext>();
			Microsoft.ServiceBus.Messaging.NetworkDetector.pendingOperations = new Dictionary<int, List<AsyncWaiter>>();
			NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler((object o, NetworkAvailabilityEventArgs ea) => Microsoft.ServiceBus.Messaging.NetworkDetector.Reset());
			NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler((object o, EventArgs ea) => Microsoft.ServiceBus.Messaging.NetworkDetector.Reset());
			Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock = new object();
		}

		internal static IAsyncResult BeginCheckTcp(IEnumerable<Uri> addresses, TimeSpan timeout, AsyncCallback callback, object state)
		{
			if (addresses != null)
			{
				Uri uri = addresses.FirstOrDefault<Uri>();
				if (uri != null)
				{
					return Microsoft.ServiceBus.Messaging.NetworkDetector.BeginInternalCheckTcp(uri.DnsSafeHost, (uri.Port != -1 ? uri.Port : RelayEnvironment.RelayNmfPort), timeout, callback, state);
				}
			}
			return new CompletedAsyncResult<bool>(true, callback, state);
		}

		private static IAsyncResult BeginInternalCheckTcp(string host, int port, TimeSpan timeout, AsyncCallback callback, object state)
		{
			IAsyncResult checkAsyncResult;
			lock (Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock)
			{
				checkAsyncResult = new Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult(host, port, Microsoft.ServiceBus.Messaging.NetworkDetector.GetHashCode(host, port), Microsoft.ServiceBus.Messaging.NetworkDetector.cache, timeout, callback, state);
			}
			return checkAsyncResult;
		}

		private static IAsyncResult BeginWaitPendingOperation(int key, TimeSpan timeout, AsyncCallback callback, object state)
		{
			List<AsyncWaiter> asyncWaiters;
			if (!Microsoft.ServiceBus.Messaging.NetworkDetector.pendingOperations.TryGetValue(key, out asyncWaiters))
			{
				asyncWaiters = new List<AsyncWaiter>();
				Microsoft.ServiceBus.Messaging.NetworkDetector.pendingOperations.Add(key, asyncWaiters);
			}
			AsyncWaiter asyncWaiter = new AsyncWaiter(timeout, callback, state);
			asyncWaiters.Add(asyncWaiter);
			return asyncWaiter;
		}

		internal static bool EndCheckTcp(IAsyncResult asyncResult)
		{
			return Microsoft.ServiceBus.Messaging.NetworkDetector.EndInternalCheckTcp(asyncResult);
		}

		private static bool EndInternalCheckTcp(IAsyncResult asyncResult)
		{
			if (asyncResult is Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult)
			{
				return Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult.End(asyncResult);
			}
			return CompletedAsyncResult<bool>.End(asyncResult);
		}

		private static void EndWaitPendingOperation(IAsyncResult result)
		{
			AsyncWaiter.End(result);
		}

		private static int GetHashCode(string host, int port)
		{
			return HashCode.CombineHashCodes(host.GetHashCode(), port);
		}

		private static void LogResult(string host, int port, bool result)
		{
			if (result)
			{
				return;
			}
			MessagingClientEtwProvider.Provider.DetectConnectivityModeFailed(string.Concat(host, ":", Convert.ToString(port, CultureInfo.InvariantCulture)), "Tcp");
		}

		private static void Reset()
		{
			lock (Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock)
			{
				Microsoft.ServiceBus.Messaging.NetworkDetector.cache = new Dictionary<int, Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext>();
			}
		}

		private static void SignalPendingOperations(int key)
		{
			List<AsyncWaiter> asyncWaiters;
			lock (Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock)
			{
				if (Microsoft.ServiceBus.Messaging.NetworkDetector.pendingOperations.TryGetValue(key, out asyncWaiters))
				{
					Microsoft.ServiceBus.Messaging.NetworkDetector.pendingOperations.Remove(key);
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

		private sealed class CheckAsyncResult : AsyncResult
		{
			private readonly static Action<object> timerCallback;

			private readonly static AsyncCallback checkConnectCallback;

			private IOThreadTimer timer;

			private bool @value;

			static CheckAsyncResult()
			{
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult.timerCallback = new Action<object>(Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult.TimerCallback);
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult.checkConnectCallback = new AsyncCallback(Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult.CheckConnectCallback);
			}

			public CheckAsyncResult(string host, int port, int key, Dictionary<int, Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext> cache, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult checkConnectAsyncResult = new Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult(host, port, key, cache, timeout, Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult.checkConnectCallback, this);
				if (timeout != TimeSpan.MaxValue)
				{
					this.timer = new IOThreadTimer(Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult.timerCallback, this, true);
					this.timer.Set(timeout);
				}
			}

			private static void CheckConnectCallback(IAsyncResult ar)
			{
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult asyncState = (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult)ar.AsyncState;
				if (asyncState.timer == null || asyncState.timer.Cancel())
				{
					asyncState.@value = Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult.End(ar);
					asyncState.TryComplete(ar.CompletedSynchronously);
				}
			}

			public static new bool End(IAsyncResult result)
			{
				return AsyncResult.End<Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult>(result).@value;
			}

			private static void TimerCallback(object state)
			{
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult checkAsyncResult = (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckAsyncResult)state;
				checkAsyncResult.@value = false;
				checkAsyncResult.TryComplete(false);
			}
		}

		private sealed class CheckConnectAsyncResult : IteratorAsyncResult<Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult>
		{
			private readonly static Action<AsyncResult, Exception> CompletingDelegate;

			private readonly int key;

			private string host;

			private int port;

			private TcpClient client;

			private Dictionary<int, Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext> cache;

			private Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext connectCheckContext;

			private volatile bool shouldReleaseLock;

			private bool ownsLoading;

			public bool Value
			{
				get;
				private set;
			}

			static CheckConnectAsyncResult()
			{
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult.CompletingDelegate = new Action<AsyncResult, Exception>(Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult.OnFinally);
			}

			public CheckConnectAsyncResult(string host, int port, int key, Dictionary<int, Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext> cache, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.host = host;
				this.port = port;
				this.key = key;
				this.cache = cache;
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult checkConnectAsyncResult = this;
				checkConnectAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(checkConnectAsyncResult.OnCompleting, Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult.CompletingDelegate);
				base.Start();
			}

			public static new bool End(IAsyncResult result)
			{
				return AsyncResult<Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult>.End(result).Value;
			}

			protected override IEnumerator<IteratorAsyncResult<Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
					Monitor.Enter(Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock);
					this.shouldReleaseLock = true;
					try
					{
						if (!this.cache.TryGetValue(this.key, out this.connectCheckContext))
						{
							this.connectCheckContext = new Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckContext(Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckState.Checking);
							this.cache.Add(this.key, this.connectCheckContext);
							this.ownsLoading = true;
							break;
						}
						else if (this.connectCheckContext.State == Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckState.Checked)
						{
							this.Value = this.connectCheckContext.Value;
							break;
						}
						else if (base.RemainingTime() > TimeSpan.Zero)
						{
							Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult checkConnectAsyncResult = this;
							IteratorAsyncResult<Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult>.BeginCall beginCall = (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
								IAsyncResult asyncResult;
								try
								{
									thisPtr.shouldReleaseLock = false;
									asyncResult = Microsoft.ServiceBus.Messaging.NetworkDetector.BeginWaitPendingOperation(thisPtr.key, t, c, s);
								}
								finally
								{
									Monitor.Exit(Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock);
								}
								return asyncResult;
							};
							yield return checkConnectAsyncResult.CallAsync(beginCall, (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult thisPtr, IAsyncResult r) => Microsoft.ServiceBus.Messaging.NetworkDetector.EndWaitPendingOperation(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
						}
						else
						{
							this.Value = false;
							goto Label0;
						}
					}
					finally
					{
						if (this.shouldReleaseLock)
						{
							Monitor.Exit(Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock);
						}
					}
				}
				if (this.ownsLoading)
				{
					this.client = new TcpClient();
					Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult checkConnectAsyncResult1 = this;
					IteratorAsyncResult<Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult>.BeginCall beginCall1 = (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.client.BeginConnect(this.host, this.port, c, s);
					IteratorAsyncResult<Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult>.EndCall endCall = (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult thisPtr, IAsyncResult r) => {
						try
						{
							thisPtr.client.EndConnect(r);
							thisPtr.connectCheckContext.Value = true;
						}
						catch (Exception exception)
						{
							thisPtr.connectCheckContext.Value = false;
						}
					};
					yield return checkConnectAsyncResult1.CallAsync(beginCall1, endCall, (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult thisPtr, TimeSpan t) => thisPtr.client.Close(), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
			Label0:
				yield break;
			}

			private static void OnFinally(AsyncResult result, Exception exception)
			{
				Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult value = (Microsoft.ServiceBus.Messaging.NetworkDetector.CheckConnectAsyncResult)result;
				lock (Microsoft.ServiceBus.Messaging.NetworkDetector.ThisLock)
				{
					value.Value = value.connectCheckContext.Value;
					if (value.ownsLoading)
					{
						value.client.Close();
						value.connectCheckContext.State = Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckState.Checked;
						Microsoft.ServiceBus.Messaging.NetworkDetector.SignalPendingOperations(value.key);
						Microsoft.ServiceBus.Messaging.NetworkDetector.LogResult(value.host, value.port, value.Value);
					}
				}
			}
		}

		private sealed class ConnectCheckContext
		{
			public Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckState State
			{
				get;
				set;
			}

			public bool Value
			{
				get;
				set;
			}

			public ConnectCheckContext(Microsoft.ServiceBus.Messaging.NetworkDetector.ConnectCheckState initialState)
			{
				this.State = initialState;
			}
		}

		private enum ConnectCheckState
		{
			Checking,
			Checked
		}
	}
}