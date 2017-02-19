using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	public sealed class SendAvailabilityPairedNamespaceOptions : PairedNamespaceOptions
	{
		private const int DefaultBacklogQueueCount = 10;

		private readonly static TimeSpan MaxPingPrimaryInterval;

		private readonly static TimeSpan MinPingPrimaryInterval;

		private readonly static TimeSpan DefaultPingPrimaryInterval;

		private readonly static TimeSpan MaxSyphonRetryInterval;

		private readonly static TimeSpan MinSyphonRetryInterval;

		private readonly static TimeSpan DefaultSyphonRetryInterval;

		private readonly static TimeSpan PingTimeout;

		private int backlogQueueCount;

		private TimeSpan pingPrimaryInterval;

		private TimeSpan syphonRetryInterval;

		private bool enableSyphon;

		private long closed;

		private SendAvailabilityMessagePump pump;

		private readonly List<string> availableBacklogQueues;

		private readonly ConcurrentDictionary<string, Exception> unavailableEntities = new ConcurrentDictionary<string, Exception>(StringComparer.OrdinalIgnoreCase);

		private readonly ConcurrentDictionary<string, bool> availableEntities = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		private readonly ConcurrentDictionary<string, IOThreadTimer> availabilityTimers = new ConcurrentDictionary<string, IOThreadTimer>(StringComparer.OrdinalIgnoreCase);

		private readonly ConcurrentDictionary<string, IOThreadTimer> primaryFailureTimers = new ConcurrentDictionary<string, IOThreadTimer>(StringComparer.OrdinalIgnoreCase);

		public int BacklogQueueCount
		{
			get
			{
				return this.backlogQueueCount;
			}
			private set
			{
				this.backlogQueueCount = value;
			}
		}

		private bool Closed
		{
			get
			{
				return Interlocked.Read(ref this.closed) != (long)0;
			}
			set
			{
				object obj;
				if (value)
				{
					obj = 1;
				}
				else
				{
					obj = null;
				}
				long num = (long)obj;
				Interlocked.Exchange(ref this.closed, num);
			}
		}

		public bool EnableSyphon
		{
			get
			{
				return this.enableSyphon;
			}
			private set
			{
				this.enableSyphon = value;
			}
		}

		internal ISendAvailabilityExceptionInspector ExceptionInspector
		{
			get;
			private set;
		}

		public TimeSpan PingPrimaryInterval
		{
			get
			{
				return this.pingPrimaryInterval;
			}
			set
			{
				if (value < SendAvailabilityPairedNamespaceOptions.MinPingPrimaryInterval || value > SendAvailabilityPairedNamespaceOptions.MaxPingPrimaryInterval)
				{
					throw Fx.Exception.AsError(new ArgumentOutOfRangeException("value", (object)value, SRClient.PairedNamespaceValidTimespanRange), null);
				}
				this.pingPrimaryInterval = value;
			}
		}

		internal string SbNamespace
		{
			get
			{
				string empty = string.Empty;
				string host = base.PrimaryMessagingFactory.Address.Host;
				char[] chrArray = new char[] { '.' };
				string[] strArrays = host.Split(chrArray, StringSplitOptions.RemoveEmptyEntries);
				if ((int)strArrays.Length > 0)
				{
					empty = strArrays[0];
				}
				return empty;
			}
		}

		internal TimeSpan SyphonRetryInterval
		{
			get
			{
				return this.syphonRetryInterval;
			}
			set
			{
				if (value < SendAvailabilityPairedNamespaceOptions.MinSyphonRetryInterval || value > SendAvailabilityPairedNamespaceOptions.MaxSyphonRetryInterval)
				{
					throw Fx.Exception.AsError(new ArgumentOutOfRangeException("value", (object)value, SRClient.PairedNamespaceValidTimespanRange), null);
				}
				this.syphonRetryInterval = value;
			}
		}

		static SendAvailabilityPairedNamespaceOptions()
		{
			SendAvailabilityPairedNamespaceOptions.MaxPingPrimaryInterval = TimeSpan.FromMinutes(30);
			SendAvailabilityPairedNamespaceOptions.MinPingPrimaryInterval = TimeSpan.FromSeconds(1);
			SendAvailabilityPairedNamespaceOptions.DefaultPingPrimaryInterval = TimeSpan.FromMinutes(1);
			SendAvailabilityPairedNamespaceOptions.MaxSyphonRetryInterval = TimeSpan.FromMinutes(2);
			SendAvailabilityPairedNamespaceOptions.MinSyphonRetryInterval = TimeSpan.FromSeconds(1);
			SendAvailabilityPairedNamespaceOptions.DefaultSyphonRetryInterval = TimeSpan.FromSeconds(1);
			SendAvailabilityPairedNamespaceOptions.PingTimeout = TimeSpan.FromMinutes(2);
		}

		public SendAvailabilityPairedNamespaceOptions(NamespaceManager secondaryNamespaceManager, MessagingFactory messagingFactory) : this(secondaryNamespaceManager, messagingFactory, Constants.DefaultPrimaryFailoverInterval, 10, false, null)
		{
		}

		public SendAvailabilityPairedNamespaceOptions(NamespaceManager secondaryNamespaceManager, MessagingFactory messagingFactory, int backlogQueueCount) : this(secondaryNamespaceManager, messagingFactory, Constants.DefaultPrimaryFailoverInterval, backlogQueueCount, false, null)
		{
		}

		public SendAvailabilityPairedNamespaceOptions(NamespaceManager secondaryNamespaceManager, MessagingFactory messagingFactory, int backlogQueueCount, TimeSpan failoverInterval, bool enableSyphon) : this(secondaryNamespaceManager, messagingFactory, failoverInterval, backlogQueueCount, enableSyphon, null)
		{
		}

		internal SendAvailabilityPairedNamespaceOptions(NamespaceManager secondaryNamespaceManager, MessagingFactory messagingFactory, TimeSpan failoverInterval, int backlogQueueCount, bool enableSyphon, ISendAvailabilityExceptionInspector inspector) : base(secondaryNamespaceManager, messagingFactory, failoverInterval)
		{
			if (backlogQueueCount <= 0)
			{
				throw Fx.Exception.AsError(new ArgumentException(SRClient.PairedNamespaceInvalidBacklogQueueCount, "backlogQueueCount"), null);
			}
			this.pingPrimaryInterval = SendAvailabilityPairedNamespaceOptions.DefaultPingPrimaryInterval;
			this.syphonRetryInterval = SendAvailabilityPairedNamespaceOptions.DefaultSyphonRetryInterval;
			if (inspector == null)
			{
				inspector = new SendAvailabilityExceptionInspector();
			}
			this.ExceptionInspector = inspector;
			this.BacklogQueueCount = backlogQueueCount;
			this.EnableSyphon = enableSyphon;
			this.availableBacklogQueues = new List<string>(this.BacklogQueueCount);
		}

		private void AvailabilityCallback(object state)
		{
			string str = (string)state;
			SendAvailabilityPairedNamespaceOptions.PingAsyncResult pingAsyncResult = new SendAvailabilityPairedNamespaceOptions.PingAsyncResult(this, str, SendAvailabilityPairedNamespaceOptions.PingTimeout, new AsyncCallback(this.PingCompleted), state);
			pingAsyncResult.Start();
		}

		protected internal override void ClearPairing()
		{
			this.availableBacklogQueues.Clear();
			this.availableEntities.Clear();
			foreach (IOThreadTimer value in this.availabilityTimers.Values)
			{
				value.Cancel();
			}
			this.availabilityTimers.Clear();
			this.unavailableEntities.Clear();
			this.Closed = false;
			base.ClearPairing();
		}

		internal string CreateBacklogQueueName(int index)
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] sbNamespace = new object[] { this.SbNamespace, index };
			return string.Format(invariantCulture, "{0}/x-servicebus-transfer/{1}", sbNamespace);
		}

		internal override IPairedNamespaceFactory CreatePairedNamespaceFactory()
		{
			return new SendAvailabilityPairedNamespaceOptions.SendAvailabilityPairedNamespaceFactory(this);
		}

		internal string FetchBacklogQueueName(int index)
		{
			return this.availableBacklogQueues[index];
		}

		internal bool IsPathAvailable(string path)
		{
			return !this.availabilityTimers.ContainsKey(path);
		}

		internal bool IsUnavailable(string path)
		{
			return this.unavailableEntities.ContainsKey(path);
		}

		public void MarkPathHealthy(string path)
		{
			IOThreadTimer oThreadTimer;
			if (this.availabilityTimers.TryRemove(path, out oThreadTimer))
			{
				oThreadTimer.Cancel();
			}
			this.availableEntities.TryUpdate(path, true, false);
			this.OnNotifyPrimarySendResult(path, true);
		}

		protected override void OnNotifyPrimarySendResult(string path, bool success)
		{
			IOThreadTimer oThreadTimer;
			if (success)
			{
				if (this.primaryFailureTimers.TryRemove(path, out oThreadTimer))
				{
					oThreadTimer.Cancel();
					return;
				}
			}
			else if (!this.primaryFailureTimers.ContainsKey(path))
			{
				Tuple<SendAvailabilityPairedNamespaceOptions, string> tuple = new Tuple<SendAvailabilityPairedNamespaceOptions, string>(this, path);
				if (base.FailoverInterval == TimeSpan.Zero)
				{
					SendAvailabilityPairedNamespaceOptions.OnPrimaryFailedIntervalExpired(tuple);
					return;
				}
				IOThreadTimer oThreadTimer1 = new IOThreadTimer(new Action<object>(SendAvailabilityPairedNamespaceOptions.OnPrimaryFailedIntervalExpired), tuple, false);
				if (this.primaryFailureTimers.TryAdd(path, oThreadTimer1))
				{
					oThreadTimer1.Set(base.FailoverInterval);
				}
			}
		}

		private static void OnPrimaryFailedIntervalExpired(object state)
		{
			Tuple<SendAvailabilityPairedNamespaceOptions, string> tuple = (Tuple<SendAvailabilityPairedNamespaceOptions, string>)state;
			tuple.Item1.StartCheckForAvailability(tuple.Item2);
		}

		private void PingCompleted(IAsyncResult result)
		{
			IOThreadTimer oThreadTimer;
			string asyncState = (string)result.AsyncState;
			try
			{
				AsyncResult<SendAvailabilityPairedNamespaceOptions.PingAsyncResult>.End(result);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespacePingException(exception));
			}
			if (this.availabilityTimers.TryGetValue(asyncState, out oThreadTimer) && !this.Closed)
			{
				oThreadTimer.Set(this.PingPrimaryInterval);
			}
		}

		internal Exception RetrieveNontransientException(string path)
		{
			Exception exception;
			if (!this.unavailableEntities.TryGetValue(path, out exception))
			{
				exception = null;
			}
			return exception;
		}

		internal void StartCheckForAvailability(string path)
		{
			bool flag = false;
			if (this.availableEntities.ContainsKey(path))
			{
				if (this.availableEntities.TryUpdate(path, false, true))
				{
					flag = true;
				}
			}
			else if (this.availableEntities.TryAdd(path, false))
			{
				flag = true;
			}
			if (flag && this.availabilityTimers.TryAdd(path, null))
			{
				IOThreadTimer oThreadTimer = new IOThreadTimer(new Action<object>(this.AvailabilityCallback), path, false);
				if (this.availabilityTimers.TryUpdate(path, oThreadTimer, null) && !this.Closed)
				{
					oThreadTimer.Set(this.PingPrimaryInterval);
					return;
				}
				oThreadTimer.Cancel();
			}
		}

		internal void UpdateBacklogQueueCount(int count)
		{
			this.backlogQueueCount = count;
		}

		private class CloseAsyncResult : IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.CloseAsyncResult>
		{
			private readonly SendAvailabilityPairedNamespaceOptions options;

			public CloseAsyncResult(SendAvailabilityPairedNamespaceOptions options, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.options = options;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.CloseAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (this.options.availabilityTimers.Count > 0)
				{
					foreach (IOThreadTimer value in this.options.availabilityTimers.Values)
					{
						value.Cancel();
					}
				}
				if (this.options.EnableSyphon)
				{
					SendAvailabilityPairedNamespaceOptions.CloseAsyncResult closeAsyncResult = this;
					IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.CloseAsyncResult>.BeginCall beginCall = (SendAvailabilityPairedNamespaceOptions.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.options.pump.BeginClose(t, c, s);
					yield return closeAsyncResult.CallAsync(beginCall, (SendAvailabilityPairedNamespaceOptions.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.options.pump.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					SendAvailabilityPairedNamespaceOptions.CloseAsyncResult closeAsyncResult1 = this;
					IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.CloseAsyncResult>.BeginCall beginCall1 = (SendAvailabilityPairedNamespaceOptions.CloseAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.options.SecondaryMessagingFactory.BeginClose(t, c, s);
					yield return closeAsyncResult1.CallAsync(beginCall1, (SendAvailabilityPairedNamespaceOptions.CloseAsyncResult thisPtr, IAsyncResult r) => thisPtr.options.SecondaryMessagingFactory.EndClose(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null)
					{
						this.options.SecondaryMessagingFactory.Abort();
					}
				}
			}
		}

		private class PingAsyncResult : IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.PingAsyncResult>
		{
			private readonly static TimeSpan PingMessageTimeToLive;

			private readonly SendAvailabilityPairedNamespaceOptions options;

			private readonly string path;

			static PingAsyncResult()
			{
				SendAvailabilityPairedNamespaceOptions.PingAsyncResult.PingMessageTimeToLive = TimeSpan.FromSeconds(1);
			}

			public PingAsyncResult(SendAvailabilityPairedNamespaceOptions options, string path, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.options = options;
				this.path = path;
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.PingAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				Exception exception;
				MessageSender messageSender = null;
				SendAvailabilityPairedNamespaceOptions.PingAsyncResult pingAsyncResult = this;
				yield return pingAsyncResult.CallAsync((SendAvailabilityPairedNamespaceOptions.PingAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.options.PrimaryMessagingFactory.BeginCreateMessageSender(null, thisPtr.path, false, t, c, s), (SendAvailabilityPairedNamespaceOptions.PingAsyncResult thisPtr, IAsyncResult r) => messageSender = thisPtr.options.PrimaryMessagingFactory.EndCreateMessageSender(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
				if (base.LastAsyncStepException == null)
				{
					List<BrokeredMessage> brokeredMessages = new List<BrokeredMessage>();
					List<BrokeredMessage> brokeredMessages1 = brokeredMessages;
					BrokeredMessage brokeredMessage = new BrokeredMessage((object)base.GetType().Name)
					{
						ContentType = "application/vnd.ms-servicebus-ping",
						TimeToLive = SendAvailabilityPairedNamespaceOptions.PingAsyncResult.PingMessageTimeToLive,
						SessionId = Guid.NewGuid().ToString("N")
					};
					brokeredMessages1.Add(brokeredMessage);
					List<BrokeredMessage> brokeredMessages2 = brokeredMessages;
					yield return base.CallAsync((SendAvailabilityPairedNamespaceOptions.PingAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => messageSender.BeginSend(brokeredMessages2, t, c, s), (SendAvailabilityPairedNamespaceOptions.PingAsyncResult thisPtr, IAsyncResult r) => messageSender.EndSend(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException != null)
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespacePingException(base.LastAsyncStepException));
						MessagingException lastAsyncStepException = base.LastAsyncStepException as MessagingException;
						if (lastAsyncStepException != null && !lastAsyncStepException.IsTransient)
						{
							base.Complete(base.LastAsyncStepException);
							this.options.unavailableEntities.TryAdd(messageSender.Path, base.LastAsyncStepException);
						}
					}
					else
					{
						this.options.unavailableEntities.TryRemove(this.path, out exception);
						this.options.MarkPathHealthy(this.path);
					}
				}
				else
				{
					MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceCouldNotCreateMessageSender(this.options.PrimaryMessagingFactory.Address.ToString(), this.path, base.LastAsyncStepException));
				}
			}
		}

		private class SendAvailabilityPairedNamespaceFactory : IPairedNamespaceFactory
		{
			private SendAvailabilityPairedNamespaceOptions options;

			public SendAvailabilityPairedNamespaceFactory(SendAvailabilityPairedNamespaceOptions options)
			{
				this.options = options;
			}

			IAsyncResult Microsoft.ServiceBus.Messaging.IPairedNamespaceFactory.BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
			{
				this.options.Closed = true;
				return new SendAvailabilityPairedNamespaceOptions.CloseAsyncResult(this.options, timeout, callback, state);
			}

			IAsyncResult Microsoft.ServiceBus.Messaging.IPairedNamespaceFactory.BeginStart(MessagingFactory primary, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new SendAvailabilityPairedNamespaceOptions.StartAsyncResult(this.options, timeout, callback, state);
			}

			MessageSender Microsoft.ServiceBus.Messaging.IPairedNamespaceFactory.CreateMessageSender(MessageSender primary)
			{
				return new SendAvailabilityPairedNamespaceMessageSender(primary);
			}

			void Microsoft.ServiceBus.Messaging.IPairedNamespaceFactory.EndClose(IAsyncResult result)
			{
				AsyncResult<SendAvailabilityPairedNamespaceOptions.CloseAsyncResult>.End(result);
			}

			void Microsoft.ServiceBus.Messaging.IPairedNamespaceFactory.EndStart(IAsyncResult result)
			{
				AsyncResult<SendAvailabilityPairedNamespaceOptions.StartAsyncResult>.End(result);
			}
		}

		private class StartAsyncResult : IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.StartAsyncResult>
		{
			private readonly static TimeSpan CreateQueuePauseTime;

			private readonly static TimeSpan GetQueuesTime;

			private readonly static TimeSpan LocalMaxLockDuration;

			private readonly SendAvailabilityPairedNamespaceOptions options;

			static StartAsyncResult()
			{
				SendAvailabilityPairedNamespaceOptions.StartAsyncResult.CreateQueuePauseTime = TimeSpan.FromSeconds(30);
				SendAvailabilityPairedNamespaceOptions.StartAsyncResult.GetQueuesTime = TimeSpan.FromMinutes(2);
				SendAvailabilityPairedNamespaceOptions.StartAsyncResult.LocalMaxLockDuration = TimeSpan.FromMinutes(1);
			}

			public StartAsyncResult(SendAvailabilityPairedNamespaceOptions options, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.options = options;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.StartAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				MessagingException lastAsyncStepException;
				IEnumerable<QueueDescription> queueDescriptions = null;
				Stopwatch stopwatch = Stopwatch.StartNew();
				do
				{
					if (stopwatch.Elapsed >= SendAvailabilityPairedNamespaceOptions.StartAsyncResult.GetQueuesTime)
					{
						break;
					}
					CultureInfo invariantCulture = CultureInfo.InvariantCulture;
					object[] sbNamespace = new object[] { this.options.SbNamespace };
					string str = string.Format(invariantCulture, "startswith(path, '{0}/x-servicebus-transfer/') eq true", sbNamespace);
					SendAvailabilityPairedNamespaceOptions.StartAsyncResult startAsyncResult = this;
					IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.StartAsyncResult>.BeginCall beginCall = (SendAvailabilityPairedNamespaceOptions.StartAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.options.SecondaryNamespaceManager.BeginGetQueues(str, c, s);
					yield return startAsyncResult.CallAsync(beginCall, (SendAvailabilityPairedNamespaceOptions.StartAsyncResult thisPtr, IAsyncResult r) => queueDescriptions = thisPtr.options.SecondaryNamespaceManager.EndGetQueues(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null && queueDescriptions != null)
					{
						break;
					}
					lastAsyncStepException = base.LastAsyncStepException as MessagingException;
				}
				while (lastAsyncStepException == null || lastAsyncStepException.IsTransient);
				if (queueDescriptions != null || base.LastAsyncStepException == null)
				{
					HashSet<string> strs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (QueueDescription queueDescription in queueDescriptions)
					{
						strs.Add(queueDescription.Path);
					}
					for (int num = 0; num < this.options.BacklogQueueCount; num++)
					{
						string str1 = this.options.CreateBacklogQueueName(num);
						if (!strs.Contains(str1))
						{
							this.options.availableBacklogQueues.Add(string.Empty);
						}
						else
						{
							this.options.availableBacklogQueues.Add(str1);
						}
					}
					for (int j1 = 0; j1 < this.options.BacklogQueueCount; j1++)
					{
						string str2 = this.options.CreateBacklogQueueName(j1);
						if (string.IsNullOrWhiteSpace(this.options.availableBacklogQueues[j1]))
						{
							int num1 = 0;
							while (num1 < 3)
							{
								if (base.LastAsyncStepException == null)
								{
									QueueDescription queueDescription1 = new QueueDescription(str2)
									{
										MaxSizeInMegabytes = (long)5120,
										MaxDeliveryCount = 2147483647,
										DefaultMessageTimeToLive = TimeSpan.MaxValue,
										AutoDeleteOnIdle = TimeSpan.MaxValue
									};
									QueueDescription queueDescription2 = queueDescription1;
									timeSpan = (Constants.MaximumLockDuration < SendAvailabilityPairedNamespaceOptions.StartAsyncResult.LocalMaxLockDuration ? Constants.MaximumLockDuration : SendAvailabilityPairedNamespaceOptions.StartAsyncResult.LocalMaxLockDuration);
									queueDescription2.LockDuration = timeSpan;
									queueDescription1.EnableDeadLetteringOnMessageExpiration = true;
									queueDescription1.EnableBatchedOperations = true;
									SendAvailabilityPairedNamespaceOptions.StartAsyncResult startAsyncResult1 = this;
									IteratorAsyncResult<SendAvailabilityPairedNamespaceOptions.StartAsyncResult>.BeginCall beginCall1 = (SendAvailabilityPairedNamespaceOptions.StartAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.options.SecondaryNamespaceManager.BeginCreateQueue(queueDescription1, c, s);
									yield return startAsyncResult1.CallAsync(beginCall1, (SendAvailabilityPairedNamespaceOptions.StartAsyncResult thisPtr, IAsyncResult r) => thisPtr.options.SecondaryNamespaceManager.EndCreateQueue(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								}
								if (base.LastAsyncStepException != null)
								{
									if (base.LastAsyncStepException is MessagingEntityAlreadyExistsException)
									{
										QueueDescription queueDescription3 = null;
										SendAvailabilityPairedNamespaceOptions.StartAsyncResult startAsyncResult2 = this;
										yield return startAsyncResult2.CallAsync((SendAvailabilityPairedNamespaceOptions.StartAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.options.SecondaryNamespaceManager.BeginGetQueue(str2, c, s), (SendAvailabilityPairedNamespaceOptions.StartAsyncResult thisPtr, IAsyncResult r) => queueDescription3 = thisPtr.options.SecondaryNamespaceManager.EndGetQueue(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
										if (base.LastAsyncStepException == null && queueDescription3 != null && queueDescription3.Path == str2)
										{
											this.options.availableBacklogQueues[j1] = str2;
											break;
										}
									}
									if (!(base.LastAsyncStepException is UnauthorizedAccessException))
									{
										MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePairedNamespaceTransferQueueCreateError(str2, this.options.SecondaryNamespaceManager.Address.ToString(), this.LastAsyncStepException.ToString()));
										yield return base.CallAsyncSleep(SendAvailabilityPairedNamespaceOptions.StartAsyncResult.CreateQueuePauseTime);
										num1++;
									}
									else
									{
										base.Complete(base.LastAsyncStepException);
										goto Label0;
									}
								}
								else
								{
									this.options.availableBacklogQueues[j1] = str2;
									break;
								}
							}
						}
					}
					for (int k = this.options.availableBacklogQueues.Count - 1; k >= 0; k--)
					{
						if (string.IsNullOrWhiteSpace(this.options.availableBacklogQueues[k]))
						{
							this.options.availableBacklogQueues.RemoveAt(k);
						}
					}
					this.options.UpdateBacklogQueueCount(this.options.availableBacklogQueues.Count);
					if (this.options.BacklogQueueCount == 0)
					{
						throw Fx.Exception.AsError(new SendAvailabilityBacklogException(SRClient.SendAvailabilityNoTransferQueuesCreated), null);
					}
					if (this.options.EnableSyphon)
					{
						this.options.pump = new SendAvailabilityMessagePump(this.options);
						this.options.pump.Start();
					}
				}
				else
				{
					base.Complete(base.LastAsyncStepException);
				}
			Label0:
				yield break;
			}
		}
	}
}