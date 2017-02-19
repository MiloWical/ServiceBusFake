using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal sealed class GetRuntimeEntityDescriptionAsyncResult : IteratorAsyncResult<GetRuntimeEntityDescriptionAsyncResult>
	{
		private const int oneMinuteInSeconds = 60;

		private readonly static Action<AsyncResult, Exception> onFinally;

		private readonly MessageClientEntity clientEntity;

		private readonly string entityAddress;

		private readonly SbmpMessagingFactory factory;

		private readonly SbmpMessageCreator messageCreator;

		private readonly TrackingContext trackingContext;

		private readonly bool executeOnce;

		private int attempt;

		private MessageBuffer messageBuffer;

		private Message request;

		private Message response;

		private RuntimeEntityDescription runtimeEntityDescription;

		static GetRuntimeEntityDescriptionAsyncResult()
		{
			GetRuntimeEntityDescriptionAsyncResult.onFinally = new Action<AsyncResult, Exception>(GetRuntimeEntityDescriptionAsyncResult.OnFinally);
		}

		public GetRuntimeEntityDescriptionAsyncResult(TrackingContext trackingContext, MessageClientEntity clientEntity, string entityAddress, SbmpMessagingFactory factory, SbmpMessageCreator messageCreator, bool executeOnce, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
		{
			this.clientEntity = clientEntity;
			this.entityAddress = entityAddress;
			this.factory = factory;
			this.messageCreator = messageCreator;
			this.trackingContext = trackingContext ?? TrackingContext.GetInstance(Guid.NewGuid());
			this.executeOnce = executeOnce;
			GetRuntimeEntityDescriptionAsyncResult getRuntimeEntityDescriptionAsyncResult = this;
			getRuntimeEntityDescriptionAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(getRuntimeEntityDescriptionAsyncResult.OnCompleting, GetRuntimeEntityDescriptionAsyncResult.onFinally);
		}

		private static RuntimeEntityDescription BuildRuntimeEntityDescription(Message response)
		{
			GetRuntimeEntityDescriptionResponseCommand body = response.GetBody<GetRuntimeEntityDescriptionResponseCommand>();
			RuntimeEntityDescription runtimeEntityDescription = new RuntimeEntityDescription()
			{
				EnableMessagePartitioning = body.EnablePartitioning,
				RequiresDuplicateDetection = body.RequiresDuplicateDetection,
				PartitionCount = body.PartitionCount,
				EnableSubscriptionPartitioning = body.EnableSubscriptionPartitioning
			};
			return runtimeEntityDescription;
		}

		private Message CreateOrGetRequestMessage()
		{
			if (this.messageBuffer == null)
			{
				RequestInfo requestInfo = new RequestInfo()
				{
					ServerTimeout = new TimeSpan?(this.factory.OperationTimeout)
				};
				RequestInfo requestInfo1 = requestInfo;
				Message message = this.messageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpGetRuntimeEntityDescription/GetRuntimeEntityDescription", null, null, RetryPolicy.NoRetry, this.trackingContext, requestInfo1);
				this.messageBuffer = message.CreateBufferedCopy(65536);
			}
			return this.messageBuffer.CreateMessage();
		}

		public static new RuntimeEntityDescription End(IAsyncResult result)
		{
			return AsyncResult<GetRuntimeEntityDescriptionAsyncResult>.End(result).runtimeEntityDescription;
		}

		protected override IEnumerator<IteratorAsyncResult<GetRuntimeEntityDescriptionAsyncResult>.AsyncStep> GetAsyncSteps()
		{
			MessagingClientEtwProvider.TraceClient(() => {
			});
			if (!this.executeOnce)
			{
				while (this.ShouldGetEntityInfo(MessagingExceptionHelper.Unwrap(base.LastAsyncStepException as CommunicationException)))
				{
					if (!RuntimeEntityDescriptionCache.TryGet(this.entityAddress, out this.runtimeEntityDescription))
					{
						this.request = this.CreateOrGetRequestMessage();
						GetRuntimeEntityDescriptionAsyncResult getRuntimeEntityDescriptionAsyncResult = this;
						IteratorAsyncResult<GetRuntimeEntityDescriptionAsyncResult>.BeginCall beginCall = (GetRuntimeEntityDescriptionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.Channel.BeginRequest(thisPtr.request, t, c, s);
						yield return getRuntimeEntityDescriptionAsyncResult.CallAsync(beginCall, (GetRuntimeEntityDescriptionAsyncResult thisPtr, IAsyncResult a) => thisPtr.response = thisPtr.factory.Channel.EndRequest(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						if (base.LastAsyncStepException == null)
						{
							this.runtimeEntityDescription = GetRuntimeEntityDescriptionAsyncResult.BuildRuntimeEntityDescription(this.response);
							RuntimeEntityDescriptionCache.AddOrUpdate(this.entityAddress, this.runtimeEntityDescription);
							this.clientEntity.RuntimeEntityDescription = this.runtimeEntityDescription;
						}
						else
						{
							if (!base.LastAsyncStepException.IsWrappedExceptionTransient())
							{
								yield return base.CallAsyncSleep(Constants.GetRuntimeEntityDescriptionNonTransientSleepTimeout);
							}
							else
							{
								yield return base.CallAsyncSleep(TimeSpan.FromSeconds((double)(this.attempt % 60)) + TimeSpan.FromMilliseconds((double)ConcurrentRandom.Next(1, 1000)));
							}
							GetRuntimeEntityDescriptionAsyncResult getRuntimeEntityDescriptionAsyncResult1 = this;
							getRuntimeEntityDescriptionAsyncResult1.attempt = getRuntimeEntityDescriptionAsyncResult1.attempt + 1;
						}
					}
					else
					{
						this.clientEntity.RuntimeEntityDescription = this.runtimeEntityDescription;
					}
				}
			}
			else
			{
				if (!RuntimeEntityDescriptionCache.TryGet(this.entityAddress, out this.runtimeEntityDescription))
				{
					this.request = this.CreateOrGetRequestMessage();
					GetRuntimeEntityDescriptionAsyncResult getRuntimeEntityDescriptionAsyncResult2 = this;
					IteratorAsyncResult<GetRuntimeEntityDescriptionAsyncResult>.BeginCall beginCall1 = (GetRuntimeEntityDescriptionAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.factory.Channel.BeginRequest(thisPtr.request, t, c, s);
					yield return getRuntimeEntityDescriptionAsyncResult2.CallAsync(beginCall1, (GetRuntimeEntityDescriptionAsyncResult thisPtr, IAsyncResult a) => thisPtr.response = thisPtr.factory.Channel.EndRequest(a), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					this.runtimeEntityDescription = GetRuntimeEntityDescriptionAsyncResult.BuildRuntimeEntityDescription(this.response);
					RuntimeEntityDescriptionCache.AddOrUpdate(this.entityAddress, this.runtimeEntityDescription);
				}
				this.clientEntity.RuntimeEntityDescription = this.runtimeEntityDescription;
			}
		}

		private static void OnFinally(AsyncResult asyncResult, Exception exception)
		{
			GetRuntimeEntityDescriptionAsyncResult getRuntimeEntityDescriptionAsyncResult = (GetRuntimeEntityDescriptionAsyncResult)asyncResult;
			if (exception != null)
			{
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteGetRuntimeEntityDescriptionFailed(getRuntimeEntityDescriptionAsyncResult.trackingContext.Activity, getRuntimeEntityDescriptionAsyncResult.trackingContext.TrackingId, getRuntimeEntityDescriptionAsyncResult.trackingContext.SystemTracker, getRuntimeEntityDescriptionAsyncResult.entityAddress, exception.ToString()));
			}
			else
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
			}
			if (!getRuntimeEntityDescriptionAsyncResult.executeOnce)
			{
				getRuntimeEntityDescriptionAsyncResult.ReScheduleIfNeeded(exception);
			}
		}

		private void ReScheduleIfNeeded(Exception exception)
		{
			if (this.ShouldGetEntityInfo(exception))
			{
				IOThreadScheduler.ScheduleCallbackNoFlow((object s) => (new GetRuntimeEntityDescriptionAsyncResult(this.trackingContext, this.clientEntity, this.entityAddress, this.factory, this.messageCreator, false, base.RemainingTime(), null, null)).Start(), null);
			}
		}

		private bool ShouldGetEntityInfo(Exception exception)
		{
			return (this.clientEntity.RuntimeEntityDescription != null || this.factory.Settings.EnableRedirect || !this.factory.IsOpened || !(base.RemainingTime() > TimeSpan.Zero) || exception is MessagingEntityNotFoundException ? false : !(exception is UnauthorizedAccessException));
		}
	}
}