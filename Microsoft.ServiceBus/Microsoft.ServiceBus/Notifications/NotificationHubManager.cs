using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Parallel;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Notifications
{
	internal class NotificationHubManager
	{
		internal const string TagHeaderName = "ServiceBusNotification-Tags";

		internal const string ScheduleTimeHeaderName = "ServiceBusNotification-ScheduleTime";

		internal string notificationHubPath;

		internal TokenProvider tokenProvider;

		internal NamespaceManager namespaceManager;

		internal Uri baseUri;

		public NotificationHubManager(string connectionString, string notificationHubPath)
		{
			this.notificationHubPath = notificationHubPath;
			KeyValueConfigurationManager keyValueConfigurationManager = new KeyValueConfigurationManager(connectionString);
			this.namespaceManager = keyValueConfigurationManager.CreateNamespaceManager();
			this.GetTokenProvider(keyValueConfigurationManager);
			this.GetBaseUri(keyValueConfigurationManager);
		}

		private IAsyncResult BeginCancelScheduledNotification(string scheduledNotificationId, AsyncCallback callback, object state)
		{
			CancelScheduledNotificationAsyncResult cancelScheduledNotificationAsyncResult = new CancelScheduledNotificationAsyncResult(this, scheduledNotificationId, callback, state);
			cancelScheduledNotificationAsyncResult.Start();
			return cancelScheduledNotificationAsyncResult;
		}

		private IAsyncResult BeginScheduleNotification(Notification notification, DateTimeOffset scheduledTime, string tagExpression, AsyncCallback callback, object state)
		{
			notification.ValidateAndPopulateHeaders();
			ScheduleNotificationAsyncResult scheduleNotificationAsyncResult = new ScheduleNotificationAsyncResult(this, notification, scheduledTime, tagExpression, callback, state);
			scheduleNotificationAsyncResult.Start();
			return scheduleNotificationAsyncResult;
		}

		private IAsyncResult BeginSendNotification(Notification notification, bool testSend, string tagExpression, AsyncCallback callback, object state)
		{
			notification.ValidateAndPopulateHeaders();
			SendNotificationAsyncResult sendNotificationAsyncResult = new SendNotificationAsyncResult(this, notification, testSend, tagExpression, callback, state);
			sendNotificationAsyncResult.Start();
			return sendNotificationAsyncResult;
		}

		private IAsyncResult BeginUpdateRegistrationsWithNewPnsHandle(string oldPnsHandle, string newPnsHandle, AsyncCallback callback, object state)
		{
			if (string.IsNullOrEmpty(oldPnsHandle))
			{
				throw new ArgumentNullException("oldPnsHandle");
			}
			if (string.IsNullOrEmpty(newPnsHandle))
			{
				throw new ArgumentNullException("newPnsHandle");
			}
			UpdatePnsHandleAsyncResult updatePnsHandleAsyncResult = new UpdatePnsHandleAsyncResult(this, oldPnsHandle, newPnsHandle, callback, state);
			updatePnsHandleAsyncResult.Start();
			return updatePnsHandleAsyncResult;
		}

		public Task CancelScheduledNotificationAsync(string scheduledNotificationId)
		{
			return TaskHelpers.CreateTask((AsyncCallback c, object s) => this.BeginCancelScheduledNotification(scheduledNotificationId, c, s), new Action<IAsyncResult>(this.EndCancelScheduledNotification));
		}

		public Task<T> CreateOrUpdateRegistrationAsync<T>(T registration)
		where T : RegistrationDescription
		{
			registration = (T)registration.Clone();
			registration.NotificationHubPath = this.notificationHubPath;
			registration.ETag = null;
			registration.ExpirationTime = null;
			return this.namespaceManager.UpdateRegistrationAsync<T>(registration);
		}

		public Task<T> CreateRegistrationAsync<T>(T registration)
		where T : RegistrationDescription
		{
			registration = (T)registration.Clone();
			registration.NotificationHubPath = this.notificationHubPath;
			registration.ExpirationTime = null;
			registration.ETag = null;
			registration.RegistrationId = null;
			return this.namespaceManager.CreateRegistrationAsync<T>(registration);
		}

		public Task<string> CreateRegistrationIdAsync()
		{
			return this.namespaceManager.CreateRegistrationIdAsync(this.notificationHubPath);
		}

		public Task DeleteRegistrationAsync(string registrationId, string etag)
		{
			return this.namespaceManager.DeleteRegistrationAsync(this.notificationHubPath, registrationId, etag);
		}

		public Task DeleteRegistrationsByChannelAsync(string pnsHandle)
		{
			TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();
			this.DeleteRegistrationsByChannelAsyncInternal(pnsHandle, taskCompletionSource);
			return taskCompletionSource.Task;
		}

		private void DeleteRegistrationsByChannelAsyncInternal(string pnsHandle, TaskCompletionSource<object> taskSource)
		{
			this.namespaceManager.GetRegistrationsByChannelAsync(pnsHandle, this.notificationHubPath, null, -1).ContinueWith((Task<CollectionQueryResult<RegistrationDescription>> t) => {
				Func<Task, bool> isFaulted = null;
				Func<Task, bool> func = null;
				Func<Task, Exception> innerException = null;
				if (t.IsFaulted)
				{
					taskSource.SetException(t.Exception.InnerException);
					return;
				}
				List<Task> list = (
					from item in t.Result
					select this.namespaceManager.DeleteRegistrationAsync(this.notificationHubPath, item.RegistrationId, "*")).ToList<Task>();
				if (!list.Any<Task>())
				{
					taskSource.SetResult(null);
					return;
				}
				Task.Factory.ContinueWhenAll(list.ToArray(), (Task[] deleteTasks) => {
					Task[] taskArray = deleteTasks;
					if (isFaulted == null)
					{
						isFaulted = (Task deleteTask) => deleteTask.IsFaulted;
					}
					if (!((IEnumerable<Task>)taskArray).Any<Task>(isFaulted))
					{
						if (t.Result.ContinuationToken == null)
						{
							taskSource.SetResult(null);
							return;
						}
						this.DeleteRegistrationsByChannelAsyncInternal(pnsHandle, taskSource);
						return;
					}
					TaskCompletionSource<object> cSu0024u003cu003e8_locals1e = taskSource;
					Task[] taskArray1 = deleteTasks;
					if (func == null)
					{
						func = (Task deleteTask) => deleteTask.IsFaulted;
					}
					IEnumerable<Task> tasks = ((IEnumerable<Task>)taskArray1).Where<Task>(func);
					if (innerException == null)
					{
						innerException = (Task deleteTask) => deleteTask.Exception.InnerException;
					}
					cSu0024u003cu003e8_locals1e.SetException(tasks.Select<Task, Exception>(innerException));
				});
			});
		}

		private void EndCancelScheduledNotification(IAsyncResult result)
		{
			AsyncResult<CancelScheduledNotificationAsyncResult>.End(result);
		}

		private ScheduledNotification EndScheduleNotification(IAsyncResult result)
		{
			return AsyncResult<ScheduleNotificationAsyncResult>.End(result).Result;
		}

		private NotificationOutcome EndSendNotification(IAsyncResult result)
		{
			return AsyncResult<SendNotificationAsyncResult>.End(result).Result;
		}

		private IEnumerable<RegistrationDescription> EndUpdateRegistrationsWithNewPnsHandle(IAsyncResult result)
		{
			return AsyncResult<UpdatePnsHandleAsyncResult>.End(result).UpdatedRegistrations;
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetAllRegistrationsAsync(string continuationToken, int top)
		{
			return this.namespaceManager.GetAllRegistrationsAsync(this.notificationHubPath, continuationToken, top);
		}

		private void GetBaseUri(KeyValueConfigurationManager manager)
		{
			string item = manager.connectionProperties["Endpoint"];
			string str = manager.connectionProperties["ManagementPort"];
			using (IEnumerator<Uri> enumerator = KeyValueConfigurationManager.GetEndpointAddresses(item, str).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					this.baseUri = enumerator.Current;
				}
			}
		}

		internal Task<NotificationDetails> GetNotificationAsync(string notificationId)
		{
			return TaskHelpers.CreateTask<NotificationDetails>((AsyncCallback c, object s) => new GetNotificationAsyncResult(this, notificationId, c, s), (IAsyncResult r) => AsyncResult<GetNotificationAsyncResult>.End(r).Result);
		}

		public Task<NotificationHubJob> GetNotificationHubJobAsync(string jobId)
		{
			return this.namespaceManager.GetNotificationHubJobAsync(jobId, this.notificationHubPath);
		}

		public Task<IEnumerable<NotificationHubJob>> GetNotificationHubJobsAsync()
		{
			return this.namespaceManager.GetNotificationHubJobsAsync(this.notificationHubPath);
		}

		public Task<RegistrationDescription> GetRegistrationAsync(string registrationId)
		{
			return this.namespaceManager.GetRegistrationAsync<RegistrationDescription>(registrationId, this.notificationHubPath).ContinueWith<RegistrationDescription>((Task<RegistrationDescription> r) => {
				if (!r.IsFaulted)
				{
					return r.Result;
				}
				if (!(r.Exception.Flatten().InnerException is MessagingEntityNotFoundException))
				{
					throw r.Exception;
				}
				return null;
			});
		}

		public Task<RegistrationCounts> GetRegistrationCountsAsync()
		{
			return TaskHelpers.CreateTask<RegistrationCounts>((AsyncCallback c, object s) => new GetRegistrationCountsAsyncResult(this, null, c, s), (IAsyncResult r) => AsyncResult<GetRegistrationCountsAsyncResult>.End(r).Result);
		}

		public Task<RegistrationCounts> GetRegistrationCountsByTagAsync(string tag)
		{
			return TaskHelpers.CreateTask<RegistrationCounts>((AsyncCallback c, object s) => new GetRegistrationCountsAsyncResult(this, tag, c, s), (IAsyncResult r) => AsyncResult<GetRegistrationCountsAsyncResult>.End(r).Result);
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByChannelAsync(string pnsHandle, string continuationToken, int top)
		{
			return this.namespaceManager.GetRegistrationsByChannelAsync(pnsHandle, this.notificationHubPath, continuationToken, top);
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByTagAsync(string tag, string continuationToken, int top)
		{
			return this.namespaceManager.GetRegistrationsByTagAsync(this.notificationHubPath, tag, continuationToken, top);
		}

		private void GetTokenProvider(KeyValueConfigurationManager manager)
		{
			IEnumerable<Uri> endpointAddresses = KeyValueConfigurationManager.GetEndpointAddresses(manager.connectionProperties["StsEndpoint"], null);
			string item = manager.connectionProperties["SharedSecretIssuer"];
			string str = manager.connectionProperties["SharedSecretValue"];
			string item1 = manager.connectionProperties["SharedAccessKeyName"];
			string str1 = manager.connectionProperties["SharedAccessKey"];
			if (string.IsNullOrEmpty(str))
			{
				if (string.IsNullOrEmpty(item1))
				{
					throw new ArgumentException("connectionString");
				}
				this.tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(item1, str1);
				return;
			}
			if (endpointAddresses == null || !endpointAddresses.Any<Uri>())
			{
				this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(item, str);
				return;
			}
			this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(item, str, endpointAddresses.First<Uri>());
		}

		public Task<bool> RegistrationExistsAsync(string registrationId)
		{
			return this.GetRegistrationAsync(registrationId).ContinueWith<bool>((Task<RegistrationDescription> r) => r.Result != null);
		}

		public Task<ScheduledNotification> ScheduleNotificationAsync(Notification notification, DateTimeOffset scheduledTime, string tagExpression)
		{
			return TaskHelpers.CreateTask<ScheduledNotification>((AsyncCallback c, object s) => this.BeginScheduleNotification(notification, scheduledTime, tagExpression, c, s), new Func<IAsyncResult, ScheduledNotification>(this.EndScheduleNotification));
		}

		public Task<NotificationOutcome> SendNotificationAsync(Notification notification, bool testSend, string tagExpression)
		{
			return TaskHelpers.CreateTask<NotificationOutcome>((AsyncCallback c, object s) => this.BeginSendNotification(notification, testSend, tagExpression, c, s), new Func<IAsyncResult, NotificationOutcome>(this.EndSendNotification));
		}

		public Task<NotificationHubJob> SubmitNotificationHubJobAsync(NotificationHubJob job)
		{
			return this.namespaceManager.SubmitNotificationHubJobAsync(job, this.notificationHubPath);
		}

		public Task<T> UpdateRegistrationAsync<T>(T registration)
		where T : RegistrationDescription
		{
			registration = (T)registration.Clone();
			registration.NotificationHubPath = this.notificationHubPath;
			registration.ExpirationTime = null;
			return this.namespaceManager.UpdateRegistrationAsync<T>(registration);
		}

		public Task<IEnumerable<RegistrationDescription>> UpdateRegistrationsWithNewPnsHandleAsync(string oldPnsHandle, string newPnsHandle)
		{
			return TaskHelpers.CreateTask<IEnumerable<RegistrationDescription>>((AsyncCallback c, object s) => this.BeginUpdateRegistrationsWithNewPnsHandle(oldPnsHandle, newPnsHandle, c, s), new Func<IAsyncResult, IEnumerable<RegistrationDescription>>(this.EndUpdateRegistrationsWithNewPnsHandle));
		}
	}
}