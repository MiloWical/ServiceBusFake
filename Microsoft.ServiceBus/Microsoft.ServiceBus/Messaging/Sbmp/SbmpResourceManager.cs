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
using System.Threading;
using System.Transactions;

namespace Microsoft.ServiceBus.Messaging.Sbmp
{
	internal class SbmpResourceManager
	{
		private readonly static SbmpResourceManager singletonInstance;

		private readonly object syncRoot;

		private Dictionary<string, SbmpResourceManager.TransactionEnlistment> enlistmentMap;

		public static SbmpResourceManager Instance
		{
			get
			{
				return SbmpResourceManager.singletonInstance;
			}
		}

		static SbmpResourceManager()
		{
			SbmpResourceManager.singletonInstance = new SbmpResourceManager();
		}

		private SbmpResourceManager()
		{
			this.syncRoot = new object();
			this.enlistmentMap = new Dictionary<string, SbmpResourceManager.TransactionEnlistment>(StringComparer.Ordinal);
		}

		public IAsyncResult BeginEnlist(Transaction transaction, IRequestSessionChannel channel, SbmpMessageCreator messageCreator, Action<RequestInfo> partitionInfoSetter, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new SbmpResourceManager.EnlistAsyncResult(this, transaction, channel, messageCreator, partitionInfoSetter, timeout, callback, state);
		}

		public int EndEnlist(IAsyncResult asyncResult)
		{
			return SbmpResourceManager.EnlistAsyncResult.End(asyncResult);
		}

		private class EnlistAsyncResult : AsyncResult
		{
			private readonly static AsyncResult.AsyncCompletion getInstanceComplete;

			private readonly SbmpResourceManager.TransactionEnlistment transactionEnlistment;

			private int sequenceNumber;

			static EnlistAsyncResult()
			{
				SbmpResourceManager.EnlistAsyncResult.getInstanceComplete = new AsyncResult.AsyncCompletion(SbmpResourceManager.EnlistAsyncResult.GetInstanceComplete);
			}

			public EnlistAsyncResult(SbmpResourceManager resourceManager, Transaction transaction, IRequestSessionChannel channel, SbmpMessageCreator messageCreator, Action<RequestInfo> partitionInfoSetter, TimeSpan timeout, AsyncCallback callback, object state) : base(callback, state)
			{
				if (transaction.IsolationLevel != IsolationLevel.Serializable)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(SRClient.IsolationLevelNotSupported), null);
				}
				string localIdentifier = transaction.TransactionInformation.LocalIdentifier;
				lock (resourceManager.syncRoot)
				{
					if (!resourceManager.enlistmentMap.TryGetValue(localIdentifier, out this.transactionEnlistment))
					{
						this.transactionEnlistment = new SbmpResourceManager.TransactionEnlistment(transaction, resourceManager, channel, messageCreator, partitionInfoSetter);
						resourceManager.enlistmentMap.Add(localIdentifier, this.transactionEnlistment);
						if (!transaction.EnlistPromotableSinglePhase(this.transactionEnlistment))
						{
							resourceManager.enlistmentMap.Remove(localIdentifier);
							throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new InvalidOperationException(SRClient.MultipleResourceManagersNotSupported), null);
						}
					}
				}
				this.sequenceNumber = this.transactionEnlistment.GetNextSequenceNumber();
				if (base.SyncContinue(this.transactionEnlistment.BeginGetInstance(timeout, base.PrepareAsyncCompletion(SbmpResourceManager.EnlistAsyncResult.getInstanceComplete), this)))
				{
					base.Complete(true);
				}
			}

			public static new int End(IAsyncResult asyncResult)
			{
				return AsyncResult.End<SbmpResourceManager.EnlistAsyncResult>(asyncResult).sequenceNumber;
			}

			private static bool GetInstanceComplete(IAsyncResult asyncResult)
			{
				((SbmpResourceManager.EnlistAsyncResult)asyncResult.AsyncState).transactionEnlistment.EndGetInstance(asyncResult);
				return true;
			}
		}

		public class TransactionEnlistment : SingletonManager<IRequestSessionChannel>, IPromotableSinglePhaseNotification, ITransactionPromoter
		{
			private readonly static TimeSpan rollbackTimeout;

			private readonly Transaction transaction;

			private readonly string transactionId;

			private readonly SbmpResourceManager resourceManager;

			private readonly IRequestSessionChannel channel;

			private readonly SbmpMessageCreator messageCreator;

			private int sequenceNumber;

			public RequestInfo RequestInfo
			{
				get;
				private set;
			}

			static TransactionEnlistment()
			{
				SbmpResourceManager.TransactionEnlistment.rollbackTimeout = TimeSpan.FromMinutes(1);
			}

			public TransactionEnlistment(Transaction transaction, SbmpResourceManager resourceManager, IRequestSessionChannel channel, SbmpMessageCreator messageCreator, Action<RequestInfo> partitionInfoSetter) : base(new object())
			{
				this.transaction = transaction;
				this.transactionId = this.transaction.TransactionInformation.LocalIdentifier;
				this.resourceManager = resourceManager;
				this.channel = channel;
				this.messageCreator = messageCreator;
				this.sequenceNumber = -1;
				this.RequestInfo = new RequestInfo();
				partitionInfoSetter(this.RequestInfo);
			}

			public int GetNextSequenceNumber()
			{
				return Interlocked.Increment(ref this.sequenceNumber);
			}

			protected override IAsyncResult OnBeginCreateInstance(TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult(this, timeout, callback, state);
			}

			protected override IRequestSessionChannel OnEndCreateInstance(IAsyncResult asyncResult)
			{
				return SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult.End(asyncResult);
			}

			void System.Transactions.IPromotableSinglePhaseNotification.Initialize()
			{
			}

			void System.Transactions.IPromotableSinglePhaseNotification.Rollback(SinglePhaseEnlistment singlePhaseEnlistment)
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
				(new SbmpResourceManager.TransactionEnlistment.CompleteTransactionHelper(this, singlePhaseEnlistment, false)).Start();
			}

			void System.Transactions.IPromotableSinglePhaseNotification.SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
				(new SbmpResourceManager.TransactionEnlistment.CompleteTransactionHelper(this, singlePhaseEnlistment, true)).Start();
			}

			byte[] System.Transactions.ITransactionPromoter.Promote()
			{
				throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.AsError(new TransactionPromotionException(SRClient.MultipleResourceManagersNotSupported), null);
			}

			private class CompleteTransactionHelper
			{
				private readonly static AsyncCallback operationCallback;

				private readonly SbmpResourceManager.TransactionEnlistment transactionEnlistment;

				private readonly SinglePhaseEnlistment singlePhaseEnlistment;

				private readonly bool commit;

				static CompleteTransactionHelper()
				{
					SbmpResourceManager.TransactionEnlistment.CompleteTransactionHelper.operationCallback = new AsyncCallback(SbmpResourceManager.TransactionEnlistment.CompleteTransactionHelper.OperationCallback);
				}

				public CompleteTransactionHelper(SbmpResourceManager.TransactionEnlistment transactionEnlistment, SinglePhaseEnlistment singlePhaseEnlistment, bool commit)
				{
					this.transactionEnlistment = transactionEnlistment;
					this.singlePhaseEnlistment = singlePhaseEnlistment;
					this.commit = commit;
				}

				private void OnError(Exception exception)
				{
					CommunicationException communicationException = exception as CommunicationException;
					if (communicationException != null)
					{
						exception = MessagingExceptionHelper.Unwrap(communicationException, false);
					}
					if (this.commit)
					{
						this.singlePhaseEnlistment.InDoubt(exception);
						return;
					}
					this.singlePhaseEnlistment.Aborted(exception);
				}

				private static void OperationCallback(IAsyncResult result)
				{
					if (result.CompletedSynchronously)
					{
						return;
					}
					SbmpResourceManager.TransactionEnlistment.CompleteTransactionHelper asyncState = (SbmpResourceManager.TransactionEnlistment.CompleteTransactionHelper)result.AsyncState;
					try
					{
						asyncState.OperationComplete(result);
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						asyncState.OnError(exception);
					}
				}

				private void OperationComplete(IAsyncResult result)
				{
					CompleteTransactionResponseCommand body = this.transactionEnlistment.channel.EndRequest(result).GetBody<CompleteTransactionResponseCommand>();
					if (!this.commit)
					{
						switch (body.TransactionStatus)
						{
							case TransactionStatus.Active:
							case TransactionStatus.InDoubt:
							{
								this.singlePhaseEnlistment.InDoubt(new MessagingException(body.Details));
								return;
							}
							case TransactionStatus.Committed:
							{
								throw Fx.AssertAndThrow("The server Committed the Transaction even though the client requested it Abort!");
							}
							case TransactionStatus.Aborted:
							{
								this.singlePhaseEnlistment.Aborted();
								return;
							}
							default:
							{
								this.singlePhaseEnlistment.InDoubt(new MessagingException(body.Details));
								return;
							}
						}
					}
					switch (body.TransactionStatus)
					{
						case TransactionStatus.Active:
						case TransactionStatus.InDoubt:
						{
							this.singlePhaseEnlistment.InDoubt(new MessagingException(body.Details));
							return;
						}
						case TransactionStatus.Committed:
						{
							this.singlePhaseEnlistment.Committed();
							return;
						}
						case TransactionStatus.Aborted:
						{
							this.singlePhaseEnlistment.Aborted(new MessagingException(body.Details));
							return;
						}
						default:
						{
							this.singlePhaseEnlistment.InDoubt(new MessagingException(body.Details));
							return;
						}
					}
				}

				public void Start()
				{
					lock (this.transactionEnlistment.resourceManager.syncRoot)
					{
						this.transactionEnlistment.resourceManager.enlistmentMap.Remove(this.transactionEnlistment.transactionId);
					}
					try
					{
						CompleteTransactionCommand completeTransactionCommand = new CompleteTransactionCommand()
						{
							TransactionId = this.transactionEnlistment.transactionId,
							Timeout = SbmpResourceManager.TransactionEnlistment.rollbackTimeout,
							Commit = this.commit
						};
						RequestInfo nullable = this.transactionEnlistment.RequestInfo.Clone();
						nullable.ServerTimeout = new TimeSpan?(completeTransactionCommand.Timeout);
						nullable.TransactionId = completeTransactionCommand.TransactionId;
						Message message = this.transactionEnlistment.messageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CompleteTransaction", completeTransactionCommand, nullable);
						WorkUnitInfo.AddTo(message.Headers, "TxnWorkUnit", completeTransactionCommand.TransactionId, this.transactionEnlistment.GetNextSequenceNumber());
						IAsyncResult asyncResult = this.transactionEnlistment.channel.BeginRequest(message, SbmpProtocolDefaults.BufferTimeout(SbmpResourceManager.TransactionEnlistment.rollbackTimeout, this.transactionEnlistment.messageCreator.DisableClientOperationTimeBuffer), SbmpResourceManager.TransactionEnlistment.CompleteTransactionHelper.operationCallback, this);
						if (asyncResult.CompletedSynchronously)
						{
							this.OperationComplete(asyncResult);
						}
					}
					catch (Exception exception1)
					{
						Exception exception = exception1;
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						this.OnError(exception);
					}
				}
			}

			private sealed class CreateInstanceAsyncResult : IteratorAsyncResult<SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult>
			{
				private readonly static Action<AsyncResult, Exception> Finally;

				private readonly SbmpResourceManager.TransactionEnlistment owner;

				private Message request;

				private Message response;

				static CreateInstanceAsyncResult()
				{
					SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult.Finally = new Action<AsyncResult, Exception>(SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult.OnFinally);
				}

				public CreateInstanceAsyncResult(SbmpResourceManager.TransactionEnlistment owner, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
				{
					this.owner = owner;
					SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult createInstanceAsyncResult = this;
					createInstanceAsyncResult.OnCompleting = (Action<AsyncResult, Exception>)Delegate.Combine(createInstanceAsyncResult.OnCompleting, SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult.Finally);
					base.Start();
				}

				public static new IRequestSessionChannel End(IAsyncResult result)
				{
					return AsyncResult<SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult>.End(result).owner.channel;
				}

				protected override IEnumerator<IteratorAsyncResult<SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult>.AsyncStep> GetAsyncSteps()
				{
					CreateTransactionCommand createTransactionCommand = new CreateTransactionCommand()
					{
						TransactionId = this.owner.transactionId,
						Timeout = base.RemainingTime()
					};
					RequestInfo nullable = this.owner.RequestInfo.Clone();
					nullable.ServerTimeout = new TimeSpan?(createTransactionCommand.Timeout);
					nullable.TransactionId = createTransactionCommand.TransactionId;
					this.request = this.owner.messageCreator.CreateWcfMessage("http://schemas.microsoft.com/netservices/2011/06/servicebus/SbmpConnection/CreateTransaction", createTransactionCommand, nullable);
					SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult createInstanceAsyncResult = this;
					IteratorAsyncResult<SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult>.BeginCall beginCall = (SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.owner.channel.BeginRequest(thisPtr.request, SbmpProtocolDefaults.BufferTimeout(t, thisPtr.owner.messageCreator.DisableClientOperationTimeBuffer), c, s);
					yield return createInstanceAsyncResult.CallAsync(beginCall, (SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult thisPtr, IAsyncResult r) => thisPtr.response = thisPtr.owner.channel.EndRequest(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					if (this.response.IsFault)
					{
						base.Complete(new MessagingException(this.response.ToString()));
					}
				}

				private static void OnFinally(AsyncResult result, Exception exception)
				{
					if (exception != null)
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteLogOperationWarning(exception.ToString(), "SbmpResourceManager.TransactionEnlistment.CreateInstanceAsyncResult.OnFinally"));
					}
				}
			}
		}
	}
}