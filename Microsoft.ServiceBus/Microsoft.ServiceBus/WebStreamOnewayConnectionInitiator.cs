using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus
{
	internal class WebStreamOnewayConnectionInitiator : IConnectionInitiator
	{
		private readonly int bufferSize;

		private readonly string webStreamRole;

		private readonly bool useHttpsMode;

		public WebStreamOnewayConnectionInitiator(string webStreamRole, int bufferSize, bool useHttpsMode)
		{
			this.webStreamRole = webStreamRole;
			this.bufferSize = bufferSize;
			this.useHttpsMode = useHttpsMode;
		}

		public IAsyncResult BeginConnect(Uri uri, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (new WebStreamOnewayConnectionInitiator.ConnectAsyncResult(this, uri, timeout, callback, state)).Start();
		}

		public IConnection Connect(Uri uri, TimeSpan timeout)
		{
			WebStreamOnewayConnectionInitiator.ConnectAsyncResult connectAsyncResult = new WebStreamOnewayConnectionInitiator.ConnectAsyncResult(this, uri, timeout, null, null);
			return connectAsyncResult.RunSynchronously().Connection;
		}

		public IConnection EndConnect(IAsyncResult result)
		{
			return AsyncResult<WebStreamOnewayConnectionInitiator.ConnectAsyncResult>.End(result).Connection;
		}

		private sealed class ConnectAsyncResult : IteratorAsyncResult<WebStreamOnewayConnectionInitiator.ConnectAsyncResult>
		{
			private readonly WebStreamOnewayConnectionInitiator initiator;

			private readonly EventTraceActivity activity;

			private readonly Uri address;

			private readonly BeginEndAsyncWaitHandle asyncWaitHandle;

			private readonly WebStream webStream;

			private Exception CompleteException
			{
				get;
				set;
			}

			public IConnection Connection
			{
				get;
				private set;
			}

			public ConnectAsyncResult(WebStreamOnewayConnectionInitiator initiator, Uri address, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.initiator = initiator;
				this.address = address;
				this.asyncWaitHandle = new BeginEndAsyncWaitHandle();
				this.activity = new EventTraceActivity();
				this.webStream = new WebStream(this.address, this.initiator.webStreamRole, this.initiator.useHttpsMode, this.activity, this.address);
			}

			private void Finally()
			{
				if (this.CompleteException != null)
				{
					try
					{
						this.webStream.Dispose();
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						Fx.Exception.TraceHandled(exception, "WebStreamOnewayConnectionInitiator.ConnectAsyncResult.Finally", null);
					}
				}
				base.Complete(this.CompleteException);
			}

			protected override IEnumerator<IteratorAsyncResult<WebStreamOnewayConnectionInitiator.ConnectAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (base.RemainingTime() > TimeSpan.Zero)
				{
					IOThreadScheduler.ScheduleCallbackNoFlow((object s) => {
						WebStreamOnewayConnectionInitiator.ConnectAsyncResult webStreamConnection = (WebStreamOnewayConnectionInitiator.ConnectAsyncResult)s;
						try
						{
							try
							{
								webStreamConnection.webStream.Open();
								webStreamConnection.Connection = new WebStreamConnection(webStreamConnection.address, webStreamConnection.initiator.bufferSize, webStreamConnection.activity, webStreamConnection.webStream, webStreamConnection.address);
							}
							catch (Exception exception)
							{
								webStreamConnection.CompleteException = exception;
							}
						}
						finally
						{
							webStreamConnection.asyncWaitHandle.Set();
						}
					}, this);
					WebStreamOnewayConnectionInitiator.ConnectAsyncResult connectAsyncResult = this;
					IteratorAsyncResult<WebStreamOnewayConnectionInitiator.ConnectAsyncResult>.BeginCall beginCall = (WebStreamOnewayConnectionInitiator.ConnectAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.asyncWaitHandle.BeginWait(thisPtr.activity, t, c, s);
					yield return connectAsyncResult.CallAsync(beginCall, (WebStreamOnewayConnectionInitiator.ConnectAsyncResult thisPtr, IAsyncResult r) => thisPtr.asyncWaitHandle.EndWait(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null && this.Connection == null)
					{
						this.CompleteException = new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout), base.LastAsyncStepException);
					}
					this.Finally();
				}
				else
				{
					base.Complete(new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout)));
				}
			}
		}
	}
}