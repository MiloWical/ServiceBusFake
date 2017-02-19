using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.ServiceBus.Notifications
{
	public class NotificationHubClient
	{
		private NotificationHubManager manager;

		private string notificationHubPath;

		public bool EnableTestSend
		{
			get;
			private set;
		}

		private NotificationHubClient(string connectionString, string notificationHubPath)
		{
			if (string.IsNullOrWhiteSpace(connectionString))
			{
				throw new ArgumentNullException("connectionString");
			}
			if (string.IsNullOrWhiteSpace(notificationHubPath))
			{
				throw new ArgumentNullException("notificationHubPath");
			}
			this.manager = new NotificationHubManager(connectionString, notificationHubPath);
			this.notificationHubPath = notificationHubPath;
		}

		public Task CancelNotificationAsync(string scheduledNotificationId)
		{
			return this.CancelNotificationAsyncInternal(scheduledNotificationId);
		}

		private Task CancelNotificationAsyncInternal(string scheduledNotificationId)
		{
			Task task;
			if (string.IsNullOrWhiteSpace(scheduledNotificationId))
			{
				throw new ArgumentNullException("scheduledNotificationId");
			}
			char[] chrArray = new char[] { '-' };
			if ((int)scheduledNotificationId.Split(chrArray).Length != 2)
			{
				throw new ArgumentException("scheduledNotificationId");
			}
			try
			{
				task = this.manager.CancelScheduledNotificationAsync(scheduledNotificationId);
			}
			catch (UriFormatException uriFormatException)
			{
				throw new ArgumentException("scheduledNotificationId", uriFormatException);
			}
			return task;
		}

		internal AdmRegistrationDescription CreateAdmNativeRegistration(string admRegistrationId)
		{
			return this.CreateAdmNativeRegistration(admRegistrationId, null);
		}

		internal AdmRegistrationDescription CreateAdmNativeRegistration(string admRegistrationId, IEnumerable<string> tags)
		{
			return this.SyncOp<AdmRegistrationDescription>(() => this.CreateAdmNativeRegistrationAsync(admRegistrationId, tags));
		}

		public Task<AdmRegistrationDescription> CreateAdmNativeRegistrationAsync(string admRegistrationId)
		{
			return this.CreateAdmNativeRegistrationAsync(admRegistrationId, null);
		}

		public Task<AdmRegistrationDescription> CreateAdmNativeRegistrationAsync(string admRegistrationId, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<AdmRegistrationDescription>(new AdmRegistrationDescription(admRegistrationId, tags));
		}

		internal AdmTemplateRegistrationDescription CreateAdmTemplateRegistration(string admRegistrationId, string jsonPayload)
		{
			return this.CreateAdmTemplateRegistration(admRegistrationId, jsonPayload, null);
		}

		internal AdmTemplateRegistrationDescription CreateAdmTemplateRegistration(string admRegistrationId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.SyncOp<AdmTemplateRegistrationDescription>(() => this.CreateAdmTemplateRegistrationAsync(admRegistrationId, jsonPayload, tags));
		}

		public Task<AdmTemplateRegistrationDescription> CreateAdmTemplateRegistrationAsync(string admRegistrationId, string jsonPayload)
		{
			return this.CreateAdmTemplateRegistrationAsync(admRegistrationId, jsonPayload, null);
		}

		public Task<AdmTemplateRegistrationDescription> CreateAdmTemplateRegistrationAsync(string admRegistrationId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<AdmTemplateRegistrationDescription>(new AdmTemplateRegistrationDescription(admRegistrationId, jsonPayload, tags));
		}

		internal AppleRegistrationDescription CreateAppleNativeRegistration(string deviceToken)
		{
			return this.CreateAppleNativeRegistration(deviceToken, null);
		}

		internal AppleRegistrationDescription CreateAppleNativeRegistration(string deviceToken, IEnumerable<string> tags)
		{
			return this.SyncOp<AppleRegistrationDescription>(() => this.CreateAppleNativeRegistrationAsync(deviceToken, tags));
		}

		public Task<AppleRegistrationDescription> CreateAppleNativeRegistrationAsync(string deviceToken)
		{
			return this.CreateAppleNativeRegistrationAsync(deviceToken, null);
		}

		public Task<AppleRegistrationDescription> CreateAppleNativeRegistrationAsync(string deviceToken, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<AppleRegistrationDescription>(new AppleRegistrationDescription(deviceToken, tags));
		}

		internal AppleTemplateRegistrationDescription CreateAppleTemplateRegistration(string deviceToken, string jsonPayload)
		{
			return this.CreateAppleTemplateRegistration(deviceToken, jsonPayload, null);
		}

		internal AppleTemplateRegistrationDescription CreateAppleTemplateRegistration(string deviceToken, string jsonPayload, IEnumerable<string> tags)
		{
			return this.SyncOp<AppleTemplateRegistrationDescription>(() => this.CreateAppleTemplateRegistrationAsync(deviceToken, jsonPayload, tags));
		}

		public Task<AppleTemplateRegistrationDescription> CreateAppleTemplateRegistrationAsync(string deviceToken, string jsonPayload)
		{
			return this.CreateAppleTemplateRegistrationAsync(deviceToken, jsonPayload, null);
		}

		public Task<AppleTemplateRegistrationDescription> CreateAppleTemplateRegistrationAsync(string deviceToken, string jsonPayload, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<AppleTemplateRegistrationDescription>(new AppleTemplateRegistrationDescription(deviceToken, jsonPayload, tags));
		}

		internal BaiduRegistrationDescription CreateBaiduNativeRegistration(string userId, string channelId, IEnumerable<string> tags)
		{
			return this.SyncOp<BaiduRegistrationDescription>(() => this.CreateBaiduNativeRegistrationAsync(userId, channelId, tags));
		}

		internal BaiduRegistrationDescription CreateBaiduNativeRegistration(string userId, string channelId)
		{
			return this.CreateBaiduNativeRegistration(userId, channelId, null);
		}

		public Task<BaiduRegistrationDescription> CreateBaiduNativeRegistrationAsync(string userId, string channelId)
		{
			return this.CreateBaiduNativeRegistrationAsync(userId, channelId, null);
		}

		public Task<BaiduRegistrationDescription> CreateBaiduNativeRegistrationAsync(string userId, string channelId, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<BaiduRegistrationDescription>(new BaiduRegistrationDescription(userId, channelId, tags));
		}

		internal BaiduTemplateRegistrationDescription CreateBaiduTemplateRegistration(string userId, string channelId, string jsonPayload)
		{
			return this.CreateBaiduTemplateRegistration(userId, channelId, jsonPayload, null);
		}

		internal BaiduTemplateRegistrationDescription CreateBaiduTemplateRegistration(string userId, string channelId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.SyncOp<BaiduTemplateRegistrationDescription>(() => this.CreateBaiduTemplateRegistrationAsync(userId, channelId, jsonPayload, tags));
		}

		public Task<BaiduTemplateRegistrationDescription> CreateBaiduTemplateRegistrationAsync(string userId, string channelId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<BaiduTemplateRegistrationDescription>(new BaiduTemplateRegistrationDescription(userId, channelId, jsonPayload, tags));
		}

		public Task<BaiduTemplateRegistrationDescription> CreateBaiduTemplateRegistrationAsync(string userId, string channelId, string jsonPayload)
		{
			return this.CreateBaiduTemplateRegistrationAsync(userId, channelId, jsonPayload, null);
		}

		public static NotificationHubClient CreateClientFromConnectionString(string connectionString, string notificationHubPath)
		{
			return new NotificationHubClient(connectionString, notificationHubPath);
		}

		public static NotificationHubClient CreateClientFromConnectionString(string connectionString, string notificationHubPath, bool enableTestSend)
		{
			return new NotificationHubClient(connectionString, notificationHubPath)
			{
				EnableTestSend = enableTestSend
			};
		}

		internal GcmRegistrationDescription CreateGcmNativeRegistration(string gcmRegistrationId)
		{
			return this.CreateGcmNativeRegistration(gcmRegistrationId, null);
		}

		internal GcmRegistrationDescription CreateGcmNativeRegistration(string gcmRegistrationId, IEnumerable<string> tags)
		{
			return this.SyncOp<GcmRegistrationDescription>(() => this.CreateGcmNativeRegistrationAsync(gcmRegistrationId, tags));
		}

		public Task<GcmRegistrationDescription> CreateGcmNativeRegistrationAsync(string gcmRegistrationId)
		{
			return this.CreateGcmNativeRegistrationAsync(gcmRegistrationId, null);
		}

		public Task<GcmRegistrationDescription> CreateGcmNativeRegistrationAsync(string gcmRegistrationId, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<GcmRegistrationDescription>(new GcmRegistrationDescription(gcmRegistrationId, tags));
		}

		internal GcmTemplateRegistrationDescription CreateGcmTemplateRegistration(string gcmRegistrationId, string jsonPayload)
		{
			return this.CreateGcmTemplateRegistration(gcmRegistrationId, jsonPayload, null);
		}

		internal GcmTemplateRegistrationDescription CreateGcmTemplateRegistration(string gcmRegistrationId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.SyncOp<GcmTemplateRegistrationDescription>(() => this.CreateGcmTemplateRegistrationAsync(gcmRegistrationId, jsonPayload, tags));
		}

		public Task<GcmTemplateRegistrationDescription> CreateGcmTemplateRegistrationAsync(string gcmRegistrationId, string jsonPayload)
		{
			return this.CreateGcmTemplateRegistrationAsync(gcmRegistrationId, jsonPayload, null);
		}

		public Task<GcmTemplateRegistrationDescription> CreateGcmTemplateRegistrationAsync(string gcmRegistrationId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<GcmTemplateRegistrationDescription>(new GcmTemplateRegistrationDescription(gcmRegistrationId, jsonPayload, tags));
		}

		internal MpnsRegistrationDescription CreateMpnsNativeRegistration(string channelUri)
		{
			return this.CreateMpnsNativeRegistration(channelUri, null);
		}

		internal MpnsRegistrationDescription CreateMpnsNativeRegistration(string channelUri, IEnumerable<string> tags)
		{
			return this.SyncOp<MpnsRegistrationDescription>(() => this.CreateMpnsNativeRegistrationAsync(channelUri, tags));
		}

		public Task<MpnsRegistrationDescription> CreateMpnsNativeRegistrationAsync(string channelUri)
		{
			return this.CreateMpnsNativeRegistrationAsync(channelUri, null);
		}

		public Task<MpnsRegistrationDescription> CreateMpnsNativeRegistrationAsync(string channelUri, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<MpnsRegistrationDescription>(new MpnsRegistrationDescription(new Uri(channelUri), tags));
		}

		internal MpnsTemplateRegistrationDescription CreateMpnsTemplateRegistration(string channelUri, string xmlTemplate)
		{
			return this.CreateMpnsTemplateRegistration(channelUri, xmlTemplate, null);
		}

		internal MpnsTemplateRegistrationDescription CreateMpnsTemplateRegistration(string channelUri, string xmlTemplate, IEnumerable<string> tags)
		{
			return this.SyncOp<MpnsTemplateRegistrationDescription>(() => this.CreateMpnsTemplateRegistrationAsync(channelUri, xmlTemplate, tags));
		}

		public Task<MpnsTemplateRegistrationDescription> CreateMpnsTemplateRegistrationAsync(string channelUri, string xmlTemplate)
		{
			return this.CreateMpnsTemplateRegistrationAsync(channelUri, xmlTemplate, null);
		}

		public Task<MpnsTemplateRegistrationDescription> CreateMpnsTemplateRegistrationAsync(string channelUri, string xmlTemplate, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<MpnsTemplateRegistrationDescription>(new MpnsTemplateRegistrationDescription(new Uri(channelUri), xmlTemplate, tags));
		}

		internal NokiaXRegistrationDescription CreateNokiaXNativeRegistration(string nokiaXRegistrationId, IEnumerable<string> tags)
		{
			return this.SyncOp<NokiaXRegistrationDescription>(() => this.CreateNokiaXNativeRegistrationAsync(nokiaXRegistrationId, tags));
		}

		internal NokiaXRegistrationDescription CreateNokiaXNativeRegistration(string nokiaXRegistrationId)
		{
			return this.CreateNokiaXNativeRegistration(nokiaXRegistrationId, null);
		}

		internal Task<NokiaXRegistrationDescription> CreateNokiaXNativeRegistrationAsync(string nokiaXRegistrationId, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<NokiaXRegistrationDescription>(new NokiaXRegistrationDescription(nokiaXRegistrationId, tags));
		}

		internal Task<NokiaXRegistrationDescription> CreateNokiaXNativeRegistrationAsync(string nokiaXRegistrationId)
		{
			return this.CreateNokiaXNativeRegistrationAsync(nokiaXRegistrationId, null);
		}

		internal NokiaXTemplateRegistrationDescription CreateNokiaXTemplateRegistration(string nokiaXRegistrationId, string jsonPayload)
		{
			return this.CreateNokiaXTemplateRegistration(nokiaXRegistrationId, jsonPayload, null);
		}

		internal NokiaXTemplateRegistrationDescription CreateNokiaXTemplateRegistration(string nokiaXRegistrationId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.SyncOp<NokiaXTemplateRegistrationDescription>(() => this.CreateNokiaXTemplateRegistrationAsync(nokiaXRegistrationId, jsonPayload, tags));
		}

		internal Task<NokiaXTemplateRegistrationDescription> CreateNokiaXTemplateRegistrationAsync(string nokiaRegistrationId, string jsonPayload, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<NokiaXTemplateRegistrationDescription>(new NokiaXTemplateRegistrationDescription(nokiaRegistrationId, jsonPayload, tags));
		}

		internal Task<NokiaXTemplateRegistrationDescription> CreateNokiaXTemplateRegistrationAsync(string nokiaXRegistrationId, string jsonPayload)
		{
			return this.CreateNokiaXTemplateRegistrationAsync(nokiaXRegistrationId, jsonPayload, null);
		}

		public Task<T> CreateOrUpdateRegistrationAsync<T>(T registration)
		where T : RegistrationDescription
		{
			if (string.IsNullOrWhiteSpace(registration.RegistrationId))
			{
				throw new ArgumentNullException("RegistrationId");
			}
			return this.manager.CreateOrUpdateRegistrationAsync<T>(registration);
		}

		internal T CreateRegistration<T>(T registration)
		where T : RegistrationDescription
		{
			return this.SyncOp<T>(() => this.CreateRegistrationAsync<T>(registration));
		}

		public Task<T> CreateRegistrationAsync<T>(T registration)
		where T : RegistrationDescription
		{
			if (!string.IsNullOrWhiteSpace(registration.NotificationHubPath) && registration.NotificationHubPath != this.notificationHubPath)
			{
				throw new ArgumentException("NotificationHubPath in RegistrationDescription is not valid.");
			}
			if (!string.IsNullOrWhiteSpace(registration.RegistrationId))
			{
				throw new ArgumentException("RegistrationId should be null or empty");
			}
			return this.manager.CreateRegistrationAsync<T>(registration);
		}

		public Task<string> CreateRegistrationIdAsync()
		{
			return this.manager.CreateRegistrationIdAsync();
		}

		internal WindowsRegistrationDescription CreateWindowsNativeRegistration(string channelUri)
		{
			return this.CreateWindowsNativeRegistration(channelUri, null);
		}

		internal WindowsRegistrationDescription CreateWindowsNativeRegistration(string channelUri, IEnumerable<string> tags)
		{
			return this.SyncOp<WindowsRegistrationDescription>(() => this.CreateWindowsNativeRegistrationAsync(channelUri, tags));
		}

		public Task<WindowsRegistrationDescription> CreateWindowsNativeRegistrationAsync(string channelUri)
		{
			return this.CreateWindowsNativeRegistrationAsync(channelUri, null);
		}

		public Task<WindowsRegistrationDescription> CreateWindowsNativeRegistrationAsync(string channelUri, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<WindowsRegistrationDescription>(new WindowsRegistrationDescription(new Uri(channelUri), tags));
		}

		internal WindowsTemplateRegistrationDescription CreateWindowsTemplateRegistration(string channelUri, string xmlTemplate)
		{
			return this.CreateWindowsTemplateRegistration(channelUri, xmlTemplate, null);
		}

		internal WindowsTemplateRegistrationDescription CreateWindowsTemplateRegistration(string channelUri, string xmlTemplate, IEnumerable<string> tags)
		{
			return this.SyncOp<WindowsTemplateRegistrationDescription>(() => this.CreateWindowsTemplateRegistrationAsync(channelUri, xmlTemplate, tags));
		}

		public Task<WindowsTemplateRegistrationDescription> CreateWindowsTemplateRegistrationAsync(string channelUri, string xmlTemplate)
		{
			return this.CreateWindowsTemplateRegistrationAsync(channelUri, xmlTemplate, null);
		}

		public Task<WindowsTemplateRegistrationDescription> CreateWindowsTemplateRegistrationAsync(string channelUri, string xmlTemplate, IEnumerable<string> tags)
		{
			return this.CreateRegistrationAsync<WindowsTemplateRegistrationDescription>(new WindowsTemplateRegistrationDescription(new Uri(channelUri), xmlTemplate, tags));
		}

		internal void DeleteRegistration(RegistrationDescription registration)
		{
			if (registration == null)
			{
				throw new ArgumentNullException("registration");
			}
			this.DeleteRegistration(registration.RegistrationId, registration.ETag);
		}

		internal void DeleteRegistration(string registrationId)
		{
			this.DeleteRegistration(registrationId, "*");
		}

		internal void DeleteRegistration(string registrationId, string etag)
		{
			this.SyncOp(() => this.DeleteRegistrationAsync(registrationId, etag));
		}

		public Task DeleteRegistrationAsync(RegistrationDescription registration)
		{
			if (registration == null)
			{
				throw new ArgumentNullException("registration");
			}
			return this.DeleteRegistrationAsync(registration.RegistrationId, registration.ETag);
		}

		public Task DeleteRegistrationAsync(string registrationId)
		{
			return this.DeleteRegistrationAsync(registrationId, "*");
		}

		public Task DeleteRegistrationAsync(string registrationId, string etag)
		{
			if (string.IsNullOrWhiteSpace(registrationId))
			{
				throw new ArgumentNullException("registrationId");
			}
			return this.manager.DeleteRegistrationAsync(registrationId, etag);
		}

		internal void DeleteRegistrationsByChannel(string pnsHandle)
		{
			this.SyncOp(() => this.DeleteRegistrationsByChannelAsync(pnsHandle));
		}

		public Task DeleteRegistrationsByChannelAsync(string pnsHandle)
		{
			if (string.IsNullOrWhiteSpace(pnsHandle))
			{
				throw new ArgumentNullException("pnsHandle");
			}
			return this.manager.DeleteRegistrationsByChannelAsync(pnsHandle);
		}

		internal CollectionQueryResult<RegistrationDescription> GetAllRegistrations(int top)
		{
			return this.SyncOp<CollectionQueryResult<RegistrationDescription>>(() => this.manager.GetAllRegistrationsAsync(string.Empty, top));
		}

		internal CollectionQueryResult<RegistrationDescription> GetAllRegistrations(string continuationToken, int top)
		{
			return this.SyncOp<CollectionQueryResult<RegistrationDescription>>(() => this.manager.GetAllRegistrationsAsync(continuationToken, top));
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetAllRegistrationsAsync(int top)
		{
			return this.manager.GetAllRegistrationsAsync(string.Empty, top);
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetAllRegistrationsAsync(string continuationToken, int top)
		{
			return this.manager.GetAllRegistrationsAsync(continuationToken, top);
		}

		public Uri GetBaseUri()
		{
			return this.manager.baseUri;
		}

		internal Task<NotificationDetails> GetNotificationAsync(string notificationId)
		{
			if (string.IsNullOrWhiteSpace(notificationId))
			{
				throw new ArgumentNullException("notificationId");
			}
			return this.manager.GetNotificationAsync(notificationId);
		}

		public Task<NotificationHubJob> GetNotificationHubJobAsync(string jobId)
		{
			return this.manager.GetNotificationHubJobAsync(jobId);
		}

		public Task<IEnumerable<NotificationHubJob>> GetNotificationHubJobsAsync()
		{
			return this.manager.GetNotificationHubJobsAsync();
		}

		internal TRegistrationDescription GetRegistration<TRegistrationDescription>(string registrationId)
		where TRegistrationDescription : RegistrationDescription
		{
			return this.SyncOp<TRegistrationDescription>(() => this.GetRegistrationAsync<TRegistrationDescription>(registrationId));
		}

		public Task<TRegistrationDescription> GetRegistrationAsync<TRegistrationDescription>(string registrationId)
		where TRegistrationDescription : RegistrationDescription
		{
			if (string.IsNullOrWhiteSpace(registrationId))
			{
				throw new ArgumentNullException("registrationId");
			}
			return this.manager.GetRegistrationAsync(registrationId).ContinueWith<TRegistrationDescription>((Task<RegistrationDescription> r) => (TRegistrationDescription)(r.Result as TRegistrationDescription));
		}

		internal Task<RegistrationCounts> GetRegistrationCountsAsync()
		{
			return this.manager.GetRegistrationCountsAsync();
		}

		internal Task<RegistrationCounts> GetRegistrationCountsByTagAsync(string tag)
		{
			if (string.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentNullException("tag");
			}
			return this.manager.GetRegistrationCountsByTagAsync(tag);
		}

		internal CollectionQueryResult<RegistrationDescription> GetRegistrationsByChannel(string pnsHandle, int top)
		{
			return this.GetRegistrationsByChannel(pnsHandle, string.Empty, top);
		}

		internal CollectionQueryResult<RegistrationDescription> GetRegistrationsByChannel(string pnsHandle, string continuationToken, int top)
		{
			return this.SyncOp<CollectionQueryResult<RegistrationDescription>>(() => this.manager.GetRegistrationsByChannelAsync(pnsHandle, continuationToken, top));
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByChannelAsync(string pnsHandle, int top)
		{
			return this.GetRegistrationsByChannelAsync(pnsHandle, string.Empty, top);
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByChannelAsync(string pnsHandle, string continuationToken, int top)
		{
			if (string.IsNullOrWhiteSpace(pnsHandle))
			{
				throw new ArgumentNullException("pnsHandle");
			}
			return this.manager.GetRegistrationsByChannelAsync(pnsHandle, continuationToken, top);
		}

		internal CollectionQueryResult<RegistrationDescription> GetRegistrationsByTag(string tag, int top)
		{
			return this.GetRegistrationsByTag(tag, string.Empty, top);
		}

		internal CollectionQueryResult<RegistrationDescription> GetRegistrationsByTag(string tag, string continuationToken, int top)
		{
			return this.SyncOp<CollectionQueryResult<RegistrationDescription>>(() => this.GetRegistrationsByTagAsync(tag, continuationToken, top));
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByTagAsync(string tag, int top)
		{
			return this.GetRegistrationsByTagAsync(tag, string.Empty, top);
		}

		public Task<CollectionQueryResult<RegistrationDescription>> GetRegistrationsByTagAsync(string tag, string continuationToken, int top)
		{
			if (string.IsNullOrWhiteSpace(tag))
			{
				throw new ArgumentNullException("tag");
			}
			return this.manager.GetRegistrationsByTagAsync(tag, continuationToken, top);
		}

		internal bool RegistrationExists(string registrationId)
		{
			return this.SyncOp<bool>(() => this.RegistrationExistsAsync(registrationId));
		}

		public Task<bool> RegistrationExistsAsync(string registrationId)
		{
			return this.manager.RegistrationExistsAsync(registrationId);
		}

		public Task<ScheduledNotification> ScheduleNotificationAsync(Notification notification, DateTimeOffset scheduledTime)
		{
			return this.ScheduleNotificationAsyncInternal(notification, scheduledTime, string.Empty);
		}

		public Task<ScheduledNotification> ScheduleNotificationAsync(Notification notification, DateTimeOffset scheduledTime, IEnumerable<string> tags)
		{
			if (tags == null)
			{
				throw new ArgumentNullException("tags");
			}
			if (tags.Count<string>() == 0)
			{
				throw new ArgumentException("tags argument should contain atleast one tag");
			}
			string str = string.Join("||", tags);
			return this.ScheduleNotificationAsyncInternal(notification, scheduledTime, str);
		}

		public Task<ScheduledNotification> ScheduleNotificationAsync(Notification notification, DateTimeOffset scheduledTime, string tagExpression)
		{
			return this.ScheduleNotificationAsyncInternal(notification, scheduledTime, tagExpression);
		}

		private Task<ScheduledNotification> ScheduleNotificationAsyncInternal(Notification notification, DateTimeOffset scheduledTime, string tagExpression)
		{
			if (notification == null)
			{
				throw new ArgumentNullException("notification");
			}
			return this.manager.ScheduleNotificationAsync(notification, scheduledTime, tagExpression);
		}

		internal NotificationOutcome SendAdmNativeNotification(string jsonPayload)
		{
			return this.SendAdmNativeNotification(jsonPayload, string.Empty);
		}

		internal NotificationOutcome SendAdmNativeNotification(string jsonPayload, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendAdmNativeNotificationAsync(jsonPayload, tagExpression));
		}

		public Task<NotificationOutcome> SendAdmNativeNotificationAsync(string jsonPayload)
		{
			return this.SendAdmNativeNotificationAsync(jsonPayload, string.Empty);
		}

		public Task<NotificationOutcome> SendAdmNativeNotificationAsync(string jsonPayload, string tagExpression)
		{
			return this.SendNotificationAsync(new AdmNotification(jsonPayload), tagExpression);
		}

		public Task<NotificationOutcome> SendAdmNativeNotificationAsync(string jsonPayload, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new AdmNotification(jsonPayload), tags);
		}

		internal NotificationOutcome SendAppleNativeNotification(string jsonPayload)
		{
			return this.SendAppleNativeNotification(jsonPayload, string.Empty);
		}

		internal NotificationOutcome SendAppleNativeNotification(string jsonPayload, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendAppleNativeNotificationAsync(jsonPayload, tagExpression));
		}

		public Task<NotificationOutcome> SendAppleNativeNotificationAsync(string jsonPayload)
		{
			return this.SendAppleNativeNotificationAsync(jsonPayload, string.Empty);
		}

		public Task<NotificationOutcome> SendAppleNativeNotificationAsync(string jsonPayload, string tagExpression)
		{
			return this.SendNotificationAsync(new AppleNotification(jsonPayload), tagExpression);
		}

		public Task<NotificationOutcome> SendAppleNativeNotificationAsync(string jsonPayload, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new AppleNotification(jsonPayload), tags);
		}

		internal NotificationOutcome SendBaiduNativeNotification(string message)
		{
			return this.SendBaiduNativeNotification(message, string.Empty);
		}

		internal NotificationOutcome SendBaiduNativeNotification(string message, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendBaiduNativeNotificationAsync(message, tagExpression));
		}

		public Task<NotificationOutcome> SendBaiduNativeNotificationAsync(string message)
		{
			return this.SendNotificationAsync(new BaiduNotification(message), string.Empty);
		}

		public Task<NotificationOutcome> SendBaiduNativeNotificationAsync(string message, string tagExpression)
		{
			return this.SendNotificationAsync(new BaiduNotification(message), tagExpression);
		}

		public Task<NotificationOutcome> SendBaiduNativeNotificationAsync(string message, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new BaiduNotification(message), tags);
		}

		internal NotificationOutcome SendGcmNativeNotification(string jsonPayload)
		{
			return this.SendGcmNativeNotification(jsonPayload, string.Empty);
		}

		internal NotificationOutcome SendGcmNativeNotification(string jsonPayload, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendGcmNativeNotificationAsync(jsonPayload, tagExpression));
		}

		public Task<NotificationOutcome> SendGcmNativeNotificationAsync(string jsonPayload)
		{
			return this.SendGcmNativeNotificationAsync(jsonPayload, string.Empty);
		}

		public Task<NotificationOutcome> SendGcmNativeNotificationAsync(string jsonPayload, string tagExpression)
		{
			return this.SendNotificationAsync(new GcmNotification(jsonPayload), tagExpression);
		}

		public Task<NotificationOutcome> SendGcmNativeNotificationAsync(string jsonPayload, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new GcmNotification(jsonPayload), tags);
		}

		internal NotificationOutcome SendMpnsNativeNotification(string nativePayload)
		{
			return this.SendMpnsNativeNotification(nativePayload, string.Empty);
		}

		internal NotificationOutcome SendMpnsNativeNotification(string nativePayload, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendMpnsNativeNotificationAsync(nativePayload, tagExpression));
		}

		public Task<NotificationOutcome> SendMpnsNativeNotificationAsync(string nativePayload)
		{
			return this.SendMpnsNativeNotificationAsync(nativePayload, string.Empty);
		}

		public Task<NotificationOutcome> SendMpnsNativeNotificationAsync(string nativePayload, string tagExpression)
		{
			return this.SendNotificationAsync(new MpnsNotification(nativePayload), tagExpression);
		}

		public Task<NotificationOutcome> SendMpnsNativeNotificationAsync(string nativePayload, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new MpnsNotification(nativePayload), tags);
		}

		internal NotificationOutcome SendNokiaXNativeNotification(string jsonload)
		{
			return this.SendNokiaXNativeNotification(jsonload, string.Empty);
		}

		internal NotificationOutcome SendNokiaXNativeNotification(string jsonPayload, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendNokiaXNativeNotificationAsync(jsonPayload, tagExpression));
		}

		internal Task<NotificationOutcome> SendNokiaXNativeNotificationAsync(string jsonPayload)
		{
			return this.SendNotificationAsync(new NokiaXNotification(jsonPayload), string.Empty);
		}

		internal Task<NotificationOutcome> SendNokiaXNativeNotificationAsync(string jsonPayload, string tagExpression)
		{
			return this.SendNotificationAsync(new NokiaXNotification(jsonPayload), tagExpression);
		}

		internal Task<NotificationOutcome> SendNokiaXNativeNotificationAsync(string jsonPayload, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new NokiaXNotification(jsonPayload), tags);
		}

		internal NotificationOutcome SendNotification(Notification notification)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendNotificationAsync(notification));
		}

		public Task<NotificationOutcome> SendNotificationAsync(Notification notification)
		{
			if (notification == null)
			{
				throw new ArgumentNullException("notification");
			}
			return this.manager.SendNotificationAsync(notification, this.EnableTestSend, notification.tag);
		}

		public Task<NotificationOutcome> SendNotificationAsync(Notification notification, string tagExpression)
		{
			if (notification == null)
			{
				throw new ArgumentNullException("notification");
			}
			if (notification.tag != null)
			{
				throw new ArgumentException("notification.Tag property should be null");
			}
			return this.manager.SendNotificationAsync(notification, this.EnableTestSend, tagExpression);
		}

		public Task<NotificationOutcome> SendNotificationAsync(Notification notification, IEnumerable<string> tags)
		{
			if (notification == null)
			{
				throw new ArgumentNullException("notification");
			}
			if (notification.tag != null)
			{
				throw new ArgumentException("notification.Tag property should be null");
			}
			if (tags == null)
			{
				throw new ArgumentNullException("tags");
			}
			if (tags.Count<string>() == 0)
			{
				throw new ArgumentException("tags argument should contain atleat one tag");
			}
			string str = string.Join("||", tags);
			return this.manager.SendNotificationAsync(notification, this.EnableTestSend, str);
		}

		internal NotificationOutcome SendTemplateNotification(IDictionary<string, string> properties)
		{
			return this.SendTemplateNotification(properties, string.Empty);
		}

		internal NotificationOutcome SendTemplateNotification(IDictionary<string, string> properties, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendTemplateNotificationAsync(properties, tagExpression));
		}

		public Task<NotificationOutcome> SendTemplateNotificationAsync(IDictionary<string, string> properties)
		{
			return this.SendTemplateNotificationAsync(properties, string.Empty);
		}

		public Task<NotificationOutcome> SendTemplateNotificationAsync(IDictionary<string, string> properties, string tagExpression)
		{
			return this.SendNotificationAsync(new TemplateNotification(properties), tagExpression);
		}

		public Task<NotificationOutcome> SendTemplateNotificationAsync(IDictionary<string, string> properties, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new TemplateNotification(properties), tags);
		}

		internal NotificationOutcome SendWindowsNativeNotification(string windowsNativePayload)
		{
			return this.SendWindowsNativeNotification(windowsNativePayload, string.Empty);
		}

		internal NotificationOutcome SendWindowsNativeNotification(string windowsNativePayload, string tagExpression)
		{
			return this.SyncOp<NotificationOutcome>(() => this.SendWindowsNativeNotificationAsync(windowsNativePayload, tagExpression));
		}

		public Task<NotificationOutcome> SendWindowsNativeNotificationAsync(string windowsNativePayload)
		{
			return this.SendWindowsNativeNotificationAsync(windowsNativePayload, string.Empty);
		}

		public Task<NotificationOutcome> SendWindowsNativeNotificationAsync(string windowsNativePayload, string tagExpression)
		{
			return this.SendNotificationAsync(new WindowsNotification(windowsNativePayload), tagExpression);
		}

		public Task<NotificationOutcome> SendWindowsNativeNotificationAsync(string windowsNativePayload, IEnumerable<string> tags)
		{
			return this.SendNotificationAsync(new WindowsNotification(windowsNativePayload), tags);
		}

		public Task<NotificationHubJob> SubmitNotificationHubJobAsync(NotificationHubJob job)
		{
			return this.manager.SubmitNotificationHubJobAsync(job);
		}

		private T SyncOp<T>(Func<Task<T>> func)
		{
			T result;
			try
			{
				result = func().Result;
			}
			catch (AggregateException aggregateException)
			{
				throw aggregateException.Flatten().InnerException;
			}
			return result;
		}

		private void SyncOp(Func<Task> action)
		{
			try
			{
				action().Wait();
			}
			catch (AggregateException aggregateException)
			{
				throw aggregateException.Flatten().InnerException;
			}
		}

		internal T UpdateRegistration<T>(T registration)
		where T : RegistrationDescription
		{
			return this.SyncOp<T>(() => this.UpdateRegistrationAsync<T>(registration));
		}

		public Task<T> UpdateRegistrationAsync<T>(T registration)
		where T : RegistrationDescription
		{
			if (string.IsNullOrWhiteSpace(registration.RegistrationId))
			{
				throw new ArgumentNullException("RegistrationId");
			}
			if (string.IsNullOrWhiteSpace(registration.ETag))
			{
				throw new ArgumentNullException("ETag");
			}
			return this.manager.UpdateRegistrationAsync<T>(registration);
		}

		internal IEnumerable<RegistrationDescription> UpdateRegistrationsWithNewPnsHandle(string oldPnsHandle, string newPnsHandle)
		{
			return this.SyncOp<IEnumerable<RegistrationDescription>>(() => this.UpdateRegistrationsWithNewPnsHandleAsync(oldPnsHandle, newPnsHandle));
		}

		internal Task<IEnumerable<RegistrationDescription>> UpdateRegistrationsWithNewPnsHandleAsync(string oldPnsHandle, string newPnsHandle)
		{
			return this.manager.UpdateRegistrationsWithNewPnsHandleAsync(oldPnsHandle, newPnsHandle);
		}
	}
}