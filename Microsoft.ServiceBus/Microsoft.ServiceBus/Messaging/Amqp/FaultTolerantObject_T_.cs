using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class FaultTolerantObject<T> : SingletonManager<T>
	where T : AmqpObject
	{
		private const int BufferredOperationTimeout = 10;

		private readonly static TimeSpan MinCreateInstanceTimeout;

		private readonly ICloseable owner;

		private readonly Func<TimeSpan, AsyncCallback, object, IAsyncResult> beginCreateObject;

		private readonly Func<IAsyncResult, T> endCreateObject;

		private readonly EventHandler onObjectClosed;

		private readonly Action<T> disposeObject;

		private T innerObject;

		public T UnsafeInnerObject
		{
			get
			{
				return this.innerObject;
			}
		}

		static FaultTolerantObject()
		{
			FaultTolerantObject<T>.MinCreateInstanceTimeout = TimeSpan.FromSeconds(70);
		}

		public FaultTolerantObject(ICloseable owner, Action<T> disposeObject, Func<TimeSpan, AsyncCallback, object, IAsyncResult> beginCreateObject, Func<IAsyncResult, T> endCreateObject) : base(new object())
		{
			this.owner = owner;
			this.beginCreateObject = beginCreateObject;
			this.endCreateObject = endCreateObject;
			this.disposeObject = disposeObject;
			this.onObjectClosed = new EventHandler(this.OnObjectClosed);
		}

		private static TimeSpan CreateInstanceTimeout(TimeSpan timeout)
		{
			if (timeout >= FaultTolerantObject<T>.MinCreateInstanceTimeout)
			{
				return timeout;
			}
			return FaultTolerantObject<T>.MinCreateInstanceTimeout;
		}

		protected override IAsyncResult OnBeginCreateInstance(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new FaultTolerantObject<T>.CreateAsyncResult(this, timeout, callback, state);
		}

		protected override T OnEndCreateInstance(IAsyncResult asyncResult)
		{
			this.innerObject = AsyncResult<FaultTolerantObject<T>.CreateAsyncResult>.End(asyncResult).Object;
			this.innerObject.Closed += new EventHandler(this.onObjectClosed.Invoke);
			return this.innerObject;
		}

		protected override void OnGetInstance(T instance)
		{
			if (instance.State != AmqpObjectState.Opened)
			{
				base.Invalidate(instance);
			}
		}

		private void OnObjectClosed(object sender, EventArgs e)
		{
			MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteChannelFaulted(sender.ToString()));
			T t = (T)sender;
			this.disposeObject(t);
			base.Invalidate(t);
		}

		public override string ToString()
		{
			if (this.innerObject == null)
			{
				return string.Empty;
			}
			return this.innerObject.ToString();
		}

		public bool TryGetOpenedObject(out T obj)
		{
			obj = default(T);
			if (this.innerObject != null && this.innerObject.State == AmqpObjectState.Opened)
			{
				obj = this.innerObject;
			}
			return obj != null;
		}

		private sealed class CreateAsyncResult : IteratorAsyncResult<FaultTolerantObject<T>.CreateAsyncResult>
		{
			private readonly FaultTolerantObject<T> parent;

			private T innerObject;

			public T Object
			{
				get
				{
					return this.innerObject;
				}
			}

			public CreateAsyncResult(FaultTolerantObject<T> parent, TimeSpan timeout, AsyncCallback callback, object state) : base(FaultTolerantObject<T>.CreateInstanceTimeout(timeout), callback, state)
			{
				this.parent = parent;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<FaultTolerantObject<T>.CreateAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				FaultTolerantObject<T>.CreateAsyncResult createAsyncResult = this;
				IteratorAsyncResult<FaultTolerantObject<T>.CreateAsyncResult>.BeginCall beginCall = (FaultTolerantObject<T>.CreateAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.parent.beginCreateObject(t, c, s);
				yield return createAsyncResult.CallAsync(beginCall, (FaultTolerantObject<T>.CreateAsyncResult thisPtr, IAsyncResult r) => thisPtr.innerObject = thisPtr.parent.endCreateObject(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				Exception lastAsyncStepException = base.LastAsyncStepException;
				if (lastAsyncStepException != null)
				{
					if (this.innerObject != null)
					{
						this.parent.disposeObject(this.innerObject);
					}
					base.Complete(lastAsyncStepException);
				}
				else if (this.parent.owner.IsClosedOrClosing)
				{
					this.parent.disposeObject(this.innerObject);
				}
			}
		}
	}
}