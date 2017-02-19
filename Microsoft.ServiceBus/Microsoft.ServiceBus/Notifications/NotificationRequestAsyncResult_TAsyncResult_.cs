using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Notifications
{
	internal abstract class NotificationRequestAsyncResult<TAsyncResult> : IteratorAsyncResult<TAsyncResult>
	where TAsyncResult : NotificationRequestAsyncResult<TAsyncResult>
	{
		private readonly bool needRequestStream;

		public NotificationHubManager Manager
		{
			get;
			private set;
		}

		public HttpWebRequest Request
		{
			get;
			set;
		}

		public Stream RequestStream
		{
			get;
			set;
		}

		public HttpWebResponse Response
		{
			get;
			set;
		}

		public Microsoft.ServiceBus.Tracing.TrackingContext TrackingContext
		{
			get;
			private set;
		}

		public NotificationRequestAsyncResult(NotificationHubManager manager, bool needRequestStream, AsyncCallback callback, object state) : base(ConnectConstants.ConnectionInitiateTimeout, callback, state)
		{
			this.TrackingContext = Microsoft.ServiceBus.Tracing.TrackingContext.GetInstance(Guid.NewGuid(), manager.notificationHubPath);
			this.Manager = manager;
			this.needRequestStream = needRequestStream;
		}

		protected override IEnumerator<IteratorAsyncResult<TAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			this.PrepareRequest();
			if (this.needRequestStream)
			{
				NotificationRequestAsyncResult<TAsyncResult> notificationRequestAsyncResult = this;
				IteratorAsyncResult<TAsyncResult>.BeginCall beginCall = (TAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Request.BeginGetRequestStream(c, s);
				yield return notificationRequestAsyncResult.CallAsync(beginCall, (TAsyncResult thisPtr, IAsyncResult r) => thisPtr.RequestStream = thisPtr.Request.EndGetRequestStream(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (base.LastAsyncStepException == null)
				{
					try
					{
						this.WriteToStream();
					}
					catch (WebException webException1)
					{
						WebException webException = webException1;
						base.Complete(ServiceBusResourceOperations.ConvertWebException(this.TrackingContext, webException, this.Request.Timeout, false));
						goto Label0;
					}
				}
				else
				{
					Exception lastAsyncStepException = base.LastAsyncStepException;
					WebException webException2 = lastAsyncStepException as WebException;
					if (webException2 != null)
					{
						lastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.TrackingContext, webException2, this.Request.Timeout, false);
					}
					base.Complete(lastAsyncStepException);
					goto Label0;
				}
			}
			NotificationRequestAsyncResult<TAsyncResult> notificationRequestAsyncResult1 = this;
			IteratorAsyncResult<TAsyncResult>.BeginCall beginCall1 = (TAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Request.BeginGetResponse(c, s);
			yield return notificationRequestAsyncResult1.CallAsync(beginCall1, (TAsyncResult thisPtr, IAsyncResult r) => thisPtr.Response = (HttpWebResponse)thisPtr.Request.EndGetResponse(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
			if (base.LastAsyncStepException == null)
			{
				try
				{
					using (this.Response)
					{
						if (this.Response.StatusCode == HttpStatusCode.Created || this.Response.StatusCode == HttpStatusCode.OK)
						{
							this.ProcessResponse();
						}
						else
						{
							base.Complete(ServiceBusResourceOperations.ConvertWebException(this.TrackingContext, new WebException(this.Response.StatusDescription, null, WebExceptionStatus.ProtocolError, this.Response), this.Request.Timeout, false));
							goto Label0;
						}
					}
				}
				catch (Exception exception)
				{
					base.Complete(exception);
				}
			}
			else
			{
				Exception lastAsyncStepException1 = base.LastAsyncStepException;
				WebException webException3 = lastAsyncStepException1 as WebException;
				if (webException3 != null)
				{
					lastAsyncStepException1 = ServiceBusResourceOperations.ConvertWebException(this.TrackingContext, webException3, this.Request.Timeout, false);
				}
				base.Complete(lastAsyncStepException1);
			}
		Label0:
			yield break;
		}

		protected abstract void PrepareRequest();

		protected abstract void ProcessResponse();

		protected abstract void WriteToStream();
	}
}