using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Notifications;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class ServiceBusResourceOperations
	{
		internal const string FeedContentType = "application/atom+xml;type=feed;charset=utf-8";

		private static string ConflictOperationInProgressSubCode;

		static ServiceBusResourceOperations()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] str = new object[] { ExceptionErrorCodes.ConflictOperationInProgress.ToString("D") };
			ServiceBusResourceOperations.ConflictOperationInProgressSubCode = string.Format(invariantCulture, "SubCode={0}", str);
		}

		public ServiceBusResourceOperations()
		{
		}

		private static string AddQueryParameter(string currentQuery, string name, string value)
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { name, value };
			string str = string.Format(invariantCulture, "{0}={1}", objArray);
			if (!string.IsNullOrEmpty(currentQuery))
			{
				currentQuery = string.Concat(currentQuery, "&");
			}
			return string.Concat(currentQuery, str);
		}

		public static IAsyncResult BeginCreate<TEntityDescription>(TEntityDescription resourceDescription, string[] resourceNames, NamespaceManager namespaceManager, AsyncCallback callback, object state)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return ServiceBusResourceOperations.BeginCreateOrUpdate<TEntityDescription>(instance, resourceDescription, null, resourceNames, namespaceManager.addresses, namespaceManager.Settings.InternalOperationTimeout, false, false, null, namespaceManager.Settings, callback, state);
		}

		public static IAsyncResult BeginCreate<TEntityDescription>(TEntityDescription resourceDescription, IResourceDescription[] descriptions, string[] resourceNames, NamespaceManager namespaceManager, AsyncCallback callback, object state)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return ServiceBusResourceOperations.BeginCreateOrUpdate<TEntityDescription>(instance, resourceDescription, descriptions, resourceNames, namespaceManager.addresses, namespaceManager.Settings.InternalOperationTimeout, false, false, null, namespaceManager.Settings, callback, state);
		}

		public static IAsyncResult BeginCreateOrUpdate<TEntityDescription>(TrackingContext trackingContext, TEntityDescription resourceDescription, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, TimeSpan timeout, bool isAnonymousAccessible, bool isUpdate, IDictionary<string, string> queryParametersAndValues, NamespaceManagerSettings settings, AsyncCallback callback, object state)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			return new ServiceBusResourceOperations.CreateOrUpdateAsyncResult<TEntityDescription>(trackingContext, resourceDescription, descriptions, resourceNames, addresses, timeout, isAnonymousAccessible, isUpdate, queryParametersAndValues, settings, callback, state);
		}

		public static IAsyncResult BeginCreateRegistrationId(string[] resourceNames, NamespaceManager namespaceManager, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return new ServiceBusResourceOperations.CreateRegistrationIdAsyncResult(instance, null, resourceNames, namespaceManager.addresses, namespaceManager.Settings.InternalOperationTimeout, false, false, null, namespaceManager.Settings, callback, state);
		}

		public static IAsyncResult BeginDelete(string[] resourceNames, NamespaceManager namespaceManager, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return ServiceBusResourceOperations.BeginDelete(instance, null, resourceNames, namespaceManager.addresses, namespaceManager.Settings, namespaceManager.Settings.InternalOperationTimeout, callback, state);
		}

		public static IAsyncResult BeginDelete(TrackingContext trackingContext, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new ServiceBusResourceOperations.DeleteAsyncResult(trackingContext, descriptions, resourceNames, addresses, settings, timeout, callback, state);
		}

		public static IAsyncResult BeginDelete(TrackingContext trackingContext, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, Dictionary<string, string> additionalHeaders, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new ServiceBusResourceOperations.DeleteAsyncResult(trackingContext, descriptions, resourceNames, addresses, additionalHeaders, settings, timeout, callback, state);
		}

		public static IAsyncResult BeginGet<TEntityDescription>(string[] resourceNames, NamespaceManager namespaceManager, AsyncCallback callback, object state)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return ServiceBusResourceOperations.BeginGet<TEntityDescription>(instance, null, resourceNames, namespaceManager.addresses, namespaceManager.Settings, namespaceManager.Settings.InternalOperationTimeout, callback, state);
		}

		public static IAsyncResult BeginGet<TEntityDescription>(TrackingContext trackingContext, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			return new ServiceBusResourceOperations.GetAsyncResult<TEntityDescription>(trackingContext, descriptions, resourceNames, addresses, settings, timeout, callback, state);
		}

		public static IAsyncResult BeginGetAll(IResourceDescription[] resourceDescriptions, NamespaceManager namespaceManager, AsyncCallback callback, object state)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			return ServiceBusResourceOperations.BeginGetAll(instance, resourceDescriptions, null, namespaceManager.addresses, namespaceManager.Settings, callback, state);
		}

		public static IAsyncResult BeginGetAll(TrackingContext trackingContext, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, AsyncCallback callback, object state)
		{
			return new ServiceBusResourceOperations.GetAllAsyncResult(trackingContext, descriptions, resourceNames, addresses, settings, callback, state);
		}

		public static IAsyncResult BeginGetAll(TrackingContext trackingContext, string filter, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, AsyncCallback callback, object state)
		{
			return new ServiceBusResourceOperations.GetAllAsyncResult(trackingContext, filter, descriptions, resourceNames, addresses, settings, callback, state);
		}

		public static IAsyncResult BeginGetAll(TrackingContext trackingContext, string filter, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, int skip, int top, AsyncCallback callback, object state)
		{
			return new ServiceBusResourceOperations.GetAllAsyncResult(trackingContext, filter, descriptions, resourceNames, addresses, settings, skip, top, true, callback, state);
		}

		public static IAsyncResult BeginGetAll(TrackingContext trackingContext, string filter, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, string continuationToken, int top, AsyncCallback callback, object state)
		{
			return new ServiceBusResourceOperations.GetAllAsyncResult(trackingContext, filter, descriptions, resourceNames, addresses, settings, continuationToken, top, true, callback, state);
		}

		public static IAsyncResult BeginGetInformation(TrackingContext trackingContext, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new ServiceBusResourceOperations.GetInformationAsyncResult(trackingContext, addresses, settings, timeout, callback, state);
		}

		public static IAsyncResult BeginUpdate<TEntityDescription>(TEntityDescription resourceDescription, string[] resourceNames, NamespaceManager namespaceManager, AsyncCallback callback, object state)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return ServiceBusResourceOperations.BeginCreateOrUpdate<TEntityDescription>(instance, resourceDescription, null, resourceNames, namespaceManager.addresses, namespaceManager.Settings.InternalOperationTimeout, false, true, null, namespaceManager.Settings, callback, state);
		}

		internal static Exception ConvertIOException(TrackingContext trackingContext, IOException ioException, int timeoutInMilliseconds, bool isRequestAborted)
		{
			if (!isRequestAborted)
			{
				string str = SRClient.TrackableExceptionMessageFormat(ioException.Message, trackingContext.CreateClientTrackingExceptionInfo());
				return new MessagingException(str, ioException);
			}
			string str1 = SRClient.TrackableExceptionMessageFormat(SRClient.OperationRequestTimedOut(timeoutInMilliseconds), trackingContext.CreateClientTrackingExceptionInfo());
			return new TimeoutException(str1, ioException);
		}

		internal static Exception ConvertWebException(TrackingContext trackingContext, WebException webException, int timeoutInMilliseconds, bool isUpdate = false)
		{
			Exception messagingEntityNotFoundException;
			HttpWebResponse response = (HttpWebResponse)webException.Response;
			string message = webException.Message;
			try
			{
				if (response != null)
				{
					ServiceBusErrorData serviceBusErrorData = ServiceBusErrorData.GetServiceBusErrorData(response);
					if (string.IsNullOrEmpty(serviceBusErrorData.Detail) || serviceBusErrorData.Detail.Equals(response.StatusDescription, StringComparison.Ordinal))
					{
						message = SRClient.TrackableExceptionMessageFormat(message, trackingContext.CreateClientTrackingExceptionInfo());
					}
					else
					{
						CultureInfo invariantCulture = CultureInfo.InvariantCulture;
						object[] detail = new object[] { message, serviceBusErrorData.Detail };
						message = string.Format(invariantCulture, "{0} {1}", detail);
					}
					if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
					{
						messagingEntityNotFoundException = new MessagingEntityNotFoundException(message, webException);
						return messagingEntityNotFoundException;
					}
					else if (response.StatusCode == HttpStatusCode.Conflict)
					{
						if (response.Method.Equals("DELETE"))
						{
							messagingEntityNotFoundException = new MessagingException(message, webException);
							return messagingEntityNotFoundException;
						}
						else if (response.Method.Equals("PUT") && isUpdate)
						{
							messagingEntityNotFoundException = new MessagingException(message, webException);
							return messagingEntityNotFoundException;
						}
						else if (!message.Contains(ServiceBusResourceOperations.ConflictOperationInProgressSubCode))
						{
							messagingEntityNotFoundException = new MessagingEntityAlreadyExistsException(message, null, webException);
							return messagingEntityNotFoundException;
						}
						else
						{
							messagingEntityNotFoundException = new MessagingException(message, webException);
							return messagingEntityNotFoundException;
						}
					}
					else if (response.StatusCode == HttpStatusCode.Unauthorized)
					{
						messagingEntityNotFoundException = new UnauthorizedAccessException(message, webException);
						return messagingEntityNotFoundException;
					}
					else if (response.StatusCode == HttpStatusCode.Forbidden)
					{
						messagingEntityNotFoundException = new QuotaExceededException(message, webException);
						return messagingEntityNotFoundException;
					}
					else if (response.StatusCode == HttpStatusCode.BadRequest)
					{
						messagingEntityNotFoundException = new ArgumentException(message, webException);
						return messagingEntityNotFoundException;
					}
					else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
					{
						messagingEntityNotFoundException = new ServerBusyException(message, webException);
						return messagingEntityNotFoundException;
					}
					else if (response.StatusCode == HttpStatusCode.GatewayTimeout)
					{
						messagingEntityNotFoundException = new MessagingCommunicationException(message, webException);
						return messagingEntityNotFoundException;
					}
				}
				else if (webException.Status == WebExceptionStatus.RequestCanceled || webException.Status == WebExceptionStatus.Timeout)
				{
					message = SRClient.TrackableExceptionMessageFormat(SRClient.OperationRequestTimedOut(timeoutInMilliseconds), trackingContext.CreateClientTrackingExceptionInfo());
					messagingEntityNotFoundException = new TimeoutException(message, webException);
					return messagingEntityNotFoundException;
				}
				else if (webException.Status == WebExceptionStatus.ConnectFailure || webException.Status == WebExceptionStatus.NameResolutionFailure)
				{
					message = SRClient.TrackableExceptionMessageFormat(message, trackingContext.CreateClientTrackingExceptionInfo());
					messagingEntityNotFoundException = new MessagingCommunicationException(message, webException);
					return messagingEntityNotFoundException;
				}
				messagingEntityNotFoundException = new MessagingException(message, webException);
			}
			finally
			{
				if (response != null)
				{
					response.Close();
				}
			}
			return messagingEntityNotFoundException;
		}

		public static Task<TEntityDescription> CreateAsync<TEntityDescription>(TEntityDescription resourceDescription, IResourceDescription[] descriptions, string[] resourceNames, NamespaceManager namespaceManager)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return Task.Factory.FromAsync<TEntityDescription>((AsyncCallback callback, object state) => ServiceBusResourceOperations.BeginCreateOrUpdate<TEntityDescription>(instance, resourceDescription, descriptions, resourceNames, namespaceManager.addresses, namespaceManager.Settings.InternalOperationTimeout, false, false, null, namespaceManager.Settings, callback, state), new Func<IAsyncResult, TEntityDescription>(ServiceBusResourceOperations.EndCreate<TEntityDescription>), null);
		}

		public static Task<TEntityDescription> CreateAsync<TEntityDescription>(TEntityDescription resourceDescription, string[] resourceNames, NamespaceManager manager)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			return Task.Factory.FromAsync<TEntityDescription, string[], NamespaceManager, TEntityDescription>(new Func<TEntityDescription, string[], NamespaceManager, AsyncCallback, object, IAsyncResult>(ServiceBusResourceOperations.BeginCreate<TEntityDescription>), new Func<IAsyncResult, TEntityDescription>(ServiceBusResourceOperations.EndCreate<TEntityDescription>), resourceDescription, resourceNames, manager, null);
		}

		private static Uri CreateCollectionUri<T>(Uri baseUri, T[] resourceDescriptions, string[] resourceNames, ContinuationToken continuationToken, int skip, int top, string filter)
		where T : class, IResourceDescription
		{
			if (resourceDescriptions == null || (int)resourceDescriptions.Length == 0)
			{
				throw new ArgumentException(SRClient.NullResourceDescription);
			}
			UriBuilder uriBuilder = new UriBuilder(baseUri);
			if (uriBuilder.Port == -1)
			{
				uriBuilder.Port = RelayEnvironment.RelayHttpsPort;
			}
			uriBuilder.Scheme = Uri.UriSchemeHttps;
			MessagingUtilities.EnsureTrailingSlash(uriBuilder);
			if (resourceNames == null || (int)resourceNames.Length == 0)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] path = new object[] { uriBuilder.Path, "$Resources", resourceDescriptions[0].CollectionName };
				uriBuilder.Path = string.Format(invariantCulture, "{0}{1}/{2}", path);
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder(uriBuilder.Path);
				for (int i = 0; i < (int)resourceDescriptions.Length; i++)
				{
					if (resourceNames != null && i < (int)resourceNames.Length)
					{
						CultureInfo cultureInfo = CultureInfo.InvariantCulture;
						object[] objArray = new object[] { resourceNames[i], resourceDescriptions[i].CollectionName };
						stringBuilder.AppendFormat(cultureInfo, "{0}/{1}/", objArray);
					}
				}
				uriBuilder.Path = stringBuilder.ToString();
			}
			MessagingUtilities.EnsureTrailingSlash(uriBuilder);
			string empty = string.Empty;
			if (continuationToken == null)
			{
				CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
				object[] objArray1 = new object[] { skip, top };
				empty = string.Format(invariantCulture1, "$skip={0}&$top={1}", objArray1);
			}
			else if (string.IsNullOrWhiteSpace(continuationToken.Token))
			{
				CultureInfo cultureInfo1 = CultureInfo.InvariantCulture;
				object[] objArray2 = new object[] { top };
				empty = string.Format(cultureInfo1, "$top={0}", objArray2);
			}
			else
			{
				CultureInfo invariantCulture2 = CultureInfo.InvariantCulture;
				object[] token = new object[] { "continuationtoken", continuationToken.Token, top };
				empty = string.Format(invariantCulture2, "{0}={1}&$top={2}", token);
			}
			if (!string.IsNullOrWhiteSpace(filter))
			{
				empty = ServiceBusResourceOperations.AddQueryParameter(empty, "$filter", HttpUtility.UrlEncode(filter));
			}
			uriBuilder.Query = ServiceBusResourceOperations.AddQueryParameter(empty, "api-version", "2014-09");
			return uriBuilder.Uri;
		}

		private static Uri CreateInformationUri(Uri baseUri)
		{
			UriBuilder uriBuilder = MessagingUtilities.CreateUriBuilderWithHttpsSchemeAndPort(baseUri);
			MessagingUtilities.EnsureTrailingSlash(uriBuilder);
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] path = new object[] { uriBuilder.Path, "$protocol-version" };
			uriBuilder.Path = string.Format(invariantCulture, "{0}{1}/", path);
			return uriBuilder.Uri;
		}

		private static Uri CreateResourceUri(Uri baseUri, IResourceDescription[] resourceDescriptions, string[] resourceNames, IDictionary<string, string> queryParametersAndValues)
		{
			if (resourceNames == null || (int)resourceNames.Length == 0)
			{
				throw new ArgumentException(SRClient.NullResourceName);
			}
			UriBuilder str = MessagingUtilities.CreateUriBuilderWithHttpsSchemeAndPort(baseUri);
			MessagingUtilities.EnsureTrailingSlash(str);
			StringBuilder stringBuilder = new StringBuilder();
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] path = new object[] { str.Path, resourceNames[0] };
			stringBuilder.AppendFormat(invariantCulture, "{0}{1}/", path);
			for (int i = 1; i < (int)resourceNames.Length; i++)
			{
				if (resourceDescriptions != null && i <= (int)resourceDescriptions.Length)
				{
					if (resourceNames[i] == string.Empty)
					{
						CultureInfo cultureInfo = CultureInfo.InvariantCulture;
						object[] collectionName = new object[] { resourceDescriptions[i - 1].CollectionName };
						stringBuilder.AppendFormat(cultureInfo, "{0}/", collectionName);
					}
					else
					{
						CultureInfo invariantCulture1 = CultureInfo.InvariantCulture;
						object[] objArray = new object[] { resourceDescriptions[i - 1].CollectionName, resourceNames[i] };
						stringBuilder.AppendFormat(invariantCulture1, "{0}/{1}/", objArray);
					}
				}
			}
			str.Path = stringBuilder.ToString();
			string str1 = ServiceBusResourceOperations.AddQueryParameter(string.Empty, "api-version", "2014-09");
			if (queryParametersAndValues != null)
			{
				foreach (KeyValuePair<string, string> queryParametersAndValue in queryParametersAndValues)
				{
					str1 = ServiceBusResourceOperations.AddQueryParameter(str1, queryParametersAndValue.Key, queryParametersAndValue.Value);
				}
			}
			str.Query = str1;
			return str.Uri;
		}

		public static Task DeleteAsync(string[] resourceNames, NamespaceManager manager)
		{
			return Task.Factory.FromAsync<string[], NamespaceManager>(new Func<string[], NamespaceManager, AsyncCallback, object, IAsyncResult>(ServiceBusResourceOperations.BeginDelete), new Action<IAsyncResult>(ServiceBusResourceOperations.EndDelete), resourceNames, manager, null);
		}

		public static T EndCreate<T>(IAsyncResult asyncResult)
		where T : EntityDescription, IResourceDescription
		{
			return AsyncResult<ServiceBusResourceOperations.CreateOrUpdateAsyncResult<T>>.End(asyncResult).Result;
		}

		public static string EndCreateRegistrationId(IAsyncResult asyncResult)
		{
			return AsyncResult<ServiceBusResourceOperations.CreateRegistrationIdAsyncResult>.End(asyncResult).Result;
		}

		public static void EndDelete(IAsyncResult asyncResult)
		{
			AsyncResult<ServiceBusResourceOperations.DeleteAsyncResult>.End(asyncResult);
		}

		public static TEntityDescription EndGet<TEntityDescription>(IAsyncResult asyncResult)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			return AsyncResult<ServiceBusResourceOperations.GetAsyncResult<TEntityDescription>>.End(asyncResult).Result;
		}

		public static TEntityDescription EndGet<TEntityDescription>(IAsyncResult asyncResult, out string[] resourceNames)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			ServiceBusResourceOperations.GetAsyncResult<TEntityDescription> getAsyncResult = AsyncResult<ServiceBusResourceOperations.GetAsyncResult<TEntityDescription>>.End(asyncResult);
			resourceNames = getAsyncResult.ResourceNames;
			return getAsyncResult.Result;
		}

		public static SyndicationFeed EndGetAll(IAsyncResult asyncResult)
		{
			return AsyncResult<ServiceBusResourceOperations.GetAllAsyncResult>.End(asyncResult).Feed;
		}

		public static SyndicationFeed EndGetAll(IAsyncResult asyncResult, out string continuationToken)
		{
			ServiceBusResourceOperations.GetAllAsyncResult getAllAsyncResult = AsyncResult<ServiceBusResourceOperations.GetAllAsyncResult>.End(asyncResult);
			continuationToken = getAllAsyncResult.NewContinuationToken;
			return getAllAsyncResult.Feed;
		}

		public static IDictionary<string, string> EndGetInformation(IAsyncResult result)
		{
			return AsyncResult<ServiceBusResourceOperations.GetInformationAsyncResult>.End(result).Information;
		}

		public static T EndUpdate<T>(IAsyncResult asyncResult)
		where T : EntityDescription, IResourceDescription
		{
			return ServiceBusResourceOperations.EndCreate<T>(asyncResult);
		}

		public static Task<SyndicationFeed> GetAllAsync(IResourceDescription[] resourceDescriptions, NamespaceManager manager)
		{
			return Task.Factory.FromAsync<IResourceDescription[], NamespaceManager, SyndicationFeed>(new Func<IResourceDescription[], NamespaceManager, AsyncCallback, object, IAsyncResult>(ServiceBusResourceOperations.BeginGetAll), new Func<IAsyncResult, SyndicationFeed>(ServiceBusResourceOperations.EndGetAll), resourceDescriptions, manager, null);
		}

		public static Task<SyndicationFeed> GetAllAsync(IResourceDescription[] resourceDescriptions, string[] resourceNames, NamespaceManager manager)
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid());
			return Task.Factory.FromAsync<SyndicationFeed>((AsyncCallback callback, object state) => ServiceBusResourceOperations.BeginGetAll(instance, resourceDescriptions, resourceNames, manager.addresses, manager.Settings, callback, state), new Func<IAsyncResult, SyndicationFeed>(ServiceBusResourceOperations.EndGetAll), null);
		}

		public static Task<TEntityDescription> GetAsync<TEntityDescription>(string[] resourceNames, NamespaceManager manager)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			return Task.Factory.FromAsync<string[], NamespaceManager, TEntityDescription>(new Func<string[], NamespaceManager, AsyncCallback, object, IAsyncResult>(ServiceBusResourceOperations.BeginGet<TEntityDescription>), new Func<IAsyncResult, TEntityDescription>(ServiceBusResourceOperations.EndGet<TEntityDescription>), resourceNames, manager, null);
		}

		public static Task<TEntityDescription> GetAsync<TEntityDescription>(IResourceDescription[] descriptions, string[] resourceNames, NamespaceManager manager)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			TrackingContext instance = TrackingContext.GetInstance(Guid.NewGuid(), resourceNames[0]);
			return Task.Factory.FromAsync<TEntityDescription>((AsyncCallback callback, object state) => ServiceBusResourceOperations.BeginGet<TEntityDescription>(instance, descriptions, resourceNames, manager.addresses, manager.Settings, manager.Settings.InternalOperationTimeout, callback, state), new Func<IAsyncResult, TEntityDescription>(ServiceBusResourceOperations.EndGet<TEntityDescription>), null);
		}

		private static bool IsRetriableException(Exception exception)
		{
			if (exception is MessagingCommunicationException)
			{
				return true;
			}
			WebException webException = exception as WebException;
			if (webException == null)
			{
				return false;
			}
			if (webException.Status == WebExceptionStatus.ConnectFailure || webException.Status == WebExceptionStatus.RequestCanceled || webException.Status == WebExceptionStatus.NameResolutionFailure)
			{
				return true;
			}
			return webException.Status == WebExceptionStatus.Timeout;
		}

		public static Task<TEntityDescription> UpdateAsync<TEntityDescription>(TEntityDescription resourceDescription, string[] resourceNames, NamespaceManager manager)
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			return Task.Factory.FromAsync<TEntityDescription, string[], NamespaceManager, TEntityDescription>(new Func<TEntityDescription, string[], NamespaceManager, AsyncCallback, object, IAsyncResult>(ServiceBusResourceOperations.BeginUpdate<TEntityDescription>), new Func<IAsyncResult, TEntityDescription>(ServiceBusResourceOperations.EndUpdate<TEntityDescription>), resourceDescription, resourceNames, manager, null);
		}

		private sealed class CreateOrUpdateAsyncResult<TEntityDescription> : IteratorAsyncResult<ServiceBusResourceOperations.CreateOrUpdateAsyncResult<TEntityDescription>>
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			private readonly TrackingContext trackingContext;

			private readonly EventTraceActivity relatedActivity;

			private readonly SyndicationItem feedItem;

			private readonly NamespaceManagerSettings settings;

			private readonly TokenProvider tokenProvider;

			private readonly bool isAnonymousAccessible;

			private readonly ServiceBusUriManager uriManager;

			private readonly EntityDescription entityDescription;

			private readonly IDictionary<string, string> queryParametersAndValues;

			private HttpWebRequest request;

			private Uri currentResourceUri;

			private Stream requestStream;

			private HttpWebResponse response;

			private IResourceDescription[] collectionDescriptions;

			private string[] resourceNames;

			private bool isUpdate;

			private IOThreadTimer requestCancelTimer;

			private volatile bool isRequestAborted;

			public TEntityDescription Result
			{
				get;
				private set;
			}

			public CreateOrUpdateAsyncResult(TrackingContext trackingContext, TEntityDescription resourceDescription, IResourceDescription[] collectionDescriptions, string[] resourceNames, IEnumerable<Uri> baseAddresses, TimeSpan timeout, bool isAnonymousAccessible, bool isUpdate, IDictionary<string, string> queryParametersAndValues, NamespaceManagerSettings settings, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw new ArgumentNullException("trackingContext");
				}
				this.trackingContext = trackingContext;
				this.settings = settings;
				this.tokenProvider = settings.TokenProvider;
				this.collectionDescriptions = collectionDescriptions;
				this.isAnonymousAccessible = isAnonymousAccessible;
				this.resourceNames = resourceNames;
				this.feedItem = new SyndicationItem()
				{
					Content = new XmlSyndicationContent("application/atom+xml;type=entry;charset=utf-8", new SyndicationElementExtension((object)resourceDescription))
				};
				this.uriManager = new ServiceBusUriManager(baseAddresses.ToList<Uri>(), false);
				this.isUpdate = isUpdate;
				this.entityDescription = resourceDescription;
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				this.queryParametersAndValues = queryParametersAndValues;
				base.Start();
			}

			private void CancelRequest(object state)
			{
				this.isRequestAborted = true;
				((HttpWebRequest)state).Abort();
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusResourceOperations.CreateOrUpdateAsyncResult<TEntityDescription>>.AsyncStep> GetAsyncSteps()
			{
				// 
				// Current member / type: System.Collections.Generic.IEnumerator`1<Microsoft.ServiceBus.Messaging.IteratorAsyncResult`1/AsyncStep<Microsoft.ServiceBus.Messaging.ServiceBusResourceOperations/CreateOrUpdateAsyncResult`1<TEntityDescription>>> Microsoft.ServiceBus.Messaging.ServiceBusResourceOperations/CreateOrUpdateAsyncResult`1::GetAsyncSteps()
				// File path: C:\Users\Milo.Wical\Desktop\Microsoft.ServiceBus.dll
				// 
				// Product version: 2016.3.1003.0
				// Exception in: System.Collections.Generic.IEnumerator<Microsoft.ServiceBus.Messaging.IteratorAsyncResult<TIteratorAsyncResult>/AsyncStep<Microsoft.ServiceBus.Messaging.ServiceBusResourceOperations/CreateOrUpdateAsyncResult<TEntityDescription>>> GetAsyncSteps()
				// 
				// Invalid state value
				//    at ..( , Queue`1 ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\LogicFlow\YieldGuardedBlocksBuilder.cs:line 203
				//    at ..( ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\LogicFlow\YieldGuardedBlocksBuilder.cs:line 187
				//    at ..( ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\LogicFlow\YieldGuardedBlocksBuilder.cs:line 129
				//    at ..( ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\LogicFlow\YieldGuardedBlocksBuilder.cs:line 76
				//    at ..() in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\LogicFlow\LogicalFlowBuilderStep.cs:line 126
				//    at ..( ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\LogicFlow\LogicalFlowBuilderStep.cs:line 51
				//    at ..(MethodBody ,  , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 88
				//    at ..(MethodBody , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 70
				//    at ..(MethodBody ,  ,  , Func`2 , & ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 104
				//    at ..(MethodBody ,  , & ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 139
				//    at ..() in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Steps\RebuildYieldStatementsStep.cs:line 134
				//    at ..Match( ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Steps\RebuildYieldStatementsStep.cs:line 49
				//    at ..( ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Steps\RebuildYieldStatementsStep.cs:line 20
				//    at ..(MethodBody ,  , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 88
				//    at ..(MethodBody , ILanguage ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\DecompilationPipeline.cs:line 70
				//    at ..( , ILanguage , MethodBody , & ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 95
				//    at ..(MethodBody , ILanguage , & ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\Extensions.cs:line 58
				//    at ..(ILanguage , MethodDefinition ,  ) in c:\Builds\556\Behemoth\ReleaseBranch Production Build NT\Sources\OpenSource\Cecil.Decompiler\Decompiler\WriterContextServices\BaseWriterContextService.cs:line 117
				// 
				// mailto: JustDecompilePublicFeedback@telerik.com

			}

			private string getMethod()
			{
				if (typeof(TEntityDescription).FullName.Contains("RegistrationDescription"))
				{
					if (!this.isUpdate)
					{
						return "POST";
					}
					return "PUT";
				}
				if (typeof(TEntityDescription).FullName.Contains("NotificationHubJob"))
				{
					return "POST";
				}
				return "PUT";
			}
		}

		private sealed class CreateRegistrationIdAsyncResult : RetryAsyncResult<ServiceBusResourceOperations.CreateRegistrationIdAsyncResult>
		{
			private static UriTemplate NotificationHubRegistrationIdsUriTemplate;

			private readonly TrackingContext trackingContext;

			private readonly EventTraceActivity relatedActivity;

			private readonly NamespaceManagerSettings settings;

			private readonly TokenProvider tokenProvider;

			private readonly bool isAnonymousAccessible;

			private readonly ServiceBusUriManager uriManager;

			private readonly IDictionary<string, string> queryParametersAndValues;

			private HttpWebRequest request;

			private Uri currentResourceUri;

			private HttpWebResponse response;

			private IResourceDescription[] collectionDescriptions;

			private string[] resourceNames;

			private IOThreadTimer requestCancelTimer;

			private volatile bool isRequestAborted;

			public string Result
			{
				get;
				private set;
			}

			static CreateRegistrationIdAsyncResult()
			{
				ServiceBusResourceOperations.CreateRegistrationIdAsyncResult.NotificationHubRegistrationIdsUriTemplate = new UriTemplate("registrationids/{registration}", true);
			}

			public CreateRegistrationIdAsyncResult(TrackingContext trackingContext, IResourceDescription[] collectionDescriptions, string[] resourceNames, IEnumerable<Uri> baseAddresses, TimeSpan timeout, bool isAnonymousAccessible, bool isUpdate, IDictionary<string, string> queryParametersAndValues, NamespaceManagerSettings settings, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw new ArgumentNullException("trackingContext");
				}
				this.trackingContext = trackingContext;
				this.settings = settings;
				this.tokenProvider = settings.TokenProvider;
				this.collectionDescriptions = collectionDescriptions;
				this.isAnonymousAccessible = isAnonymousAccessible;
				this.resourceNames = resourceNames;
				this.uriManager = new ServiceBusUriManager(baseAddresses.ToList<Uri>(), false);
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				this.queryParametersAndValues = queryParametersAndValues;
				base.Start();
			}

			private void CancelRequest(object state)
			{
				this.isRequestAborted = true;
				((HttpWebRequest)state).Abort();
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusResourceOperations.CreateRegistrationIdAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				while (true)
				{
				}
			}
		}

		private sealed class DeleteAsyncResult : RetryAsyncResult<ServiceBusResourceOperations.DeleteAsyncResult>
		{
			private readonly TrackingContext trackingContext;

			private readonly EventTraceActivity relatedActivity;

			private readonly NamespaceManagerSettings settings;

			private readonly TokenProvider tokenProvider;

			private readonly ServiceBusUriManager uriManager;

			private readonly IResourceDescription[] collectionDescriptions;

			private readonly string[] collectionResourceNames;

			private readonly RetryPolicy retryPolicy;

			private Uri resourceUri;

			private HttpWebResponse response;

			private HttpWebRequest request;

			private IOThreadTimer requestCancelTimer;

			private volatile bool isRequestAborted;

			private Dictionary<string, string> additionalHeaders;

			public DeleteAsyncResult(TrackingContext trackingContext, IResourceDescription[] collectionDescriptions, string[] collectionResourceNames, IEnumerable<Uri> baseAddresses, Dictionary<string, string> additionalHeaders, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw new ArgumentNullException("trackingContext");
				}
				this.trackingContext = trackingContext;
				this.settings = settings;
				this.tokenProvider = settings.TokenProvider;
				this.retryPolicy = settings.RetryPolicy;
				this.collectionDescriptions = collectionDescriptions;
				this.collectionResourceNames = collectionResourceNames;
				this.uriManager = new ServiceBusUriManager(baseAddresses.ToList<Uri>(), true);
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				this.additionalHeaders = additionalHeaders;
				base.Start();
			}

			public DeleteAsyncResult(TrackingContext trackingContext, IResourceDescription[] collectionDescriptions, string[] collectionResourceNames, IEnumerable<Uri> baseAddresses, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state) : this(trackingContext, collectionDescriptions, collectionResourceNames, baseAddresses, null, settings, timeout, callback, state)
			{
			}

			private void CancelRequest(object state)
			{
				this.isRequestAborted = true;
				((HttpWebRequest)state).Abort();
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusResourceOperations.DeleteAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				int num;
				bool flag;
				bool flag1;
				bool flag2;
				int num1 = 0;
				timeSpan = (this.retryPolicy.IsServerBusy ? RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (!this.retryPolicy.IsServerBusy || !(RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag2 = false;
						if (timeSpan1 != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan1);
						}
						base.LastAsyncStepException = null;
						this.resourceUri = ServiceBusResourceOperations.CreateResourceUri(this.uriManager.Current, this.collectionDescriptions, this.collectionResourceNames, null);
						this.request = (HttpWebRequest)WebRequest.Create(this.resourceUri);
						this.request.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
						this.request.ContentType = "application/atom+xml;type=entry;charset=utf-8";
						this.request.Method = "DELETE";
						HttpWebRequest httpWebRequest = this.request;
						num = (base.OriginalTimeout.TotalMilliseconds > 2147483647 ? 2147483647 : (int)base.OriginalTimeout.TotalMilliseconds);
						httpWebRequest.Timeout = num;
						this.request.SetUserAgentHeader();
						this.request.AddXProcessAtHeader();
						this.request.AddAuthorizationHeader(this.tokenProvider, this.uriManager.Current, "Manage");
						this.request.AddTrackingIdHeader(this.trackingContext);
						this.request.AddCorrelationHeader(this.relatedActivity);
						this.request.AddFaultInjectionHeader(this.settings.FaultInjectionInfo);
						this.requestCancelTimer = new IOThreadTimer(new Action<object>(this.CancelRequest), this.request, true);
						if (this.additionalHeaders != null)
						{
							foreach (KeyValuePair<string, string> additionalHeader in this.additionalHeaders)
							{
								this.request.Headers.Add(additionalHeader.Key, additionalHeader.Value);
							}
						}
						try
						{
							TimeSpan timeSpan2 = base.RemainingTime();
							if (timeSpan2 > TimeSpan.Zero)
							{
								ServiceBusResourceOperations.DeleteAsyncResult deleteAsyncResult = this;
								IteratorAsyncResult<ServiceBusResourceOperations.DeleteAsyncResult>.BeginCall beginCall = (ServiceBusResourceOperations.DeleteAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
									IAsyncResult asyncResult = thisPtr.request.BeginGetResponse(c, s);
									thisPtr.requestCancelTimer.SetIfValid(timeSpan2);
									return asyncResult;
								};
								yield return deleteAsyncResult.CallAsync(beginCall, (ServiceBusResourceOperations.DeleteAsyncResult thisPtr, IAsyncResult r) => thisPtr.response = (HttpWebResponse)thisPtr.request.EndGetResponse(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException == null)
								{
									try
									{
										using (this.response)
										{
											if (this.response.StatusCode != HttpStatusCode.OK)
											{
												base.LastAsyncStepException = new MessagingException(this.response.StatusDescription, new WebException(this.response.StatusDescription, null, WebExceptionStatus.ProtocolError, this.response));
											}
										}
									}
									catch (WebException webException1)
									{
										WebException webException = webException1;
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, webException, this.request.Timeout, false);
									}
									catch (IOException oException1)
									{
										IOException oException = oException1;
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, oException, this.request.Timeout, this.isRequestAborted);
									}
									if (base.LastAsyncStepException == null)
									{
										this.retryPolicy.ResetServerBusy();
									}
									else
									{
										flag = (base.TransactionExists ? false : this.retryPolicy.ShouldRetry(base.RemainingTime(), num1, base.LastAsyncStepException, out timeSpan1));
										flag2 = flag;
										if (!flag2)
										{
											continue;
										}
										string str = string.Format("Delete:{0}", this.resourceUri.AbsoluteUri);
										MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.retryPolicy.GetType().Name, str, num1, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
										num1++;
									}
								}
								else
								{
									WebException lastAsyncStepException = base.LastAsyncStepException as WebException;
									IOException lastAsyncStepException1 = base.LastAsyncStepException as IOException;
									if (lastAsyncStepException != null)
									{
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, lastAsyncStepException, this.request.Timeout, false);
									}
									if (lastAsyncStepException1 != null)
									{
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, lastAsyncStepException1, this.request.Timeout, this.isRequestAborted);
									}
									if (!(base.LastAsyncStepException is MessagingEntityNotFoundException))
									{
										flag1 = (base.TransactionExists ? false : this.retryPolicy.ShouldRetry(base.RemainingTime(), num1, base.LastAsyncStepException, out timeSpan1));
										flag2 = flag1;
										if (!flag2)
										{
											continue;
										}
										string str1 = string.Format("Delete:{0}", this.resourceUri.AbsoluteUri);
										MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.retryPolicy.GetType().Name, str1, num1, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
										num1++;
									}
									else
									{
										base.LastAsyncStepException = null;
										this.retryPolicy.ResetServerBusy();
										goto Label0;
									}
								}
							}
							else
							{
								base.Complete(new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout)));
								goto Label0;
							}
						}
						finally
						{
							this.requestCancelTimer.Cancel();
						}
					}
					while (this.uriManager.MoveNextUri() && flag2);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str2 = this.retryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str2));
				}
			Label0:
				yield break;
			}
		}

		private sealed class GetAllAsyncResult : RetryAsyncResult<ServiceBusResourceOperations.GetAllAsyncResult>
		{
			private readonly static Func<SyndicationLink, bool> nextLinkPredicate;

			private readonly TrackingContext trackingContext;

			private readonly EventTraceActivity relatedActivity;

			private readonly IResourceDescription[] descriptions;

			private readonly TokenProvider tokenProvider;

			private readonly int timeoutInMilliseconds;

			private readonly string[] resourceNames;

			private readonly NamespaceManagerSettings settings;

			private readonly ServiceBusUriManager uriManager;

			private readonly string filter;

			private readonly int skip;

			private readonly int top;

			private readonly ContinuationToken continuationToken;

			private readonly bool stopAfterOnePage;

			private HttpWebRequest request;

			private HttpWebResponse response;

			private IOThreadTimer requestCancelTimer;

			private TimeoutHelper operationTimer;

			private volatile bool isRequestAborted;

			public SyndicationFeed Feed
			{
				get;
				private set;
			}

			public string NewContinuationToken
			{
				get;
				private set;
			}

			static GetAllAsyncResult()
			{
				ServiceBusResourceOperations.GetAllAsyncResult.nextLinkPredicate = (SyndicationLink link) => string.Equals(link.RelationshipType, "next", StringComparison.Ordinal);
			}

			public GetAllAsyncResult(TrackingContext trackingContext, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, AsyncCallback callback, object state) : this(trackingContext, string.Empty, descriptions, resourceNames, addresses, settings, -1, -1, false, callback, state)
			{
			}

			public GetAllAsyncResult(TrackingContext trackingContext, string filter, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, AsyncCallback callback, object state) : this(trackingContext, filter, descriptions, resourceNames, addresses, settings, -1, -1, false, callback, state)
			{
			}

			public GetAllAsyncResult(TrackingContext trackingContext, string filter, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, int skip, int top, bool stopAfterTopEntities, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				if (trackingContext == null)
				{
					throw new ArgumentNullException("trackingContext");
				}
				this.trackingContext = trackingContext;
				this.filter = filter;
				this.tokenProvider = settings.TokenProvider;
				this.descriptions = descriptions;
				this.skip = (skip >= 0 ? skip : 0);
				this.top = (top > 0 ? top : settings.GetEntitiesPageSize);
				TimeSpan internalOperationTimeout = settings.InternalOperationTimeout;
				this.timeoutInMilliseconds = (internalOperationTimeout.TotalMilliseconds > 2147483647 ? 2147483647 : (int)internalOperationTimeout.TotalMilliseconds);
				this.uriManager = new ServiceBusUriManager(addresses.ToList<Uri>(), true);
				this.resourceNames = resourceNames;
				this.settings = settings;
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				this.stopAfterOnePage = stopAfterTopEntities;
				base.Start();
			}

			public GetAllAsyncResult(TrackingContext trackingContext, string filter, IResourceDescription[] descriptions, string[] resourceNames, IEnumerable<Uri> addresses, NamespaceManagerSettings settings, string continuationTokenString, int top, bool stopAfterTopEntities, AsyncCallback callback, object state) : base(TimeSpan.MaxValue, callback, state)
			{
				if (trackingContext == null)
				{
					throw new ArgumentNullException("trackingContext");
				}
				this.trackingContext = trackingContext;
				this.filter = filter;
				this.tokenProvider = settings.TokenProvider;
				this.descriptions = descriptions;
				this.skip = (this.skip >= 0 ? this.skip : 0);
				this.top = (top > 0 ? top : settings.GetEntitiesPageSize);
				TimeSpan internalOperationTimeout = settings.InternalOperationTimeout;
				this.timeoutInMilliseconds = (internalOperationTimeout.TotalMilliseconds > 2147483647 ? 2147483647 : (int)internalOperationTimeout.TotalMilliseconds);
				this.uriManager = new ServiceBusUriManager(addresses.ToList<Uri>(), true);
				this.resourceNames = resourceNames;
				this.settings = settings;
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				this.stopAfterOnePage = stopAfterTopEntities;
				this.continuationToken = new ContinuationToken(continuationTokenString);
				base.Start();
			}

			private void CancelRequest(object state)
			{
				this.isRequestAborted = true;
				((HttpWebRequest)state).Abort();
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusResourceOperations.GetAllAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				bool flag;
				bool flag1;
				bool flag2;
				int num = 0;
				timeSpan = (this.settings.RetryPolicy.IsServerBusy ? RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (this.continuationToken == null || this.continuationToken.IsValid)
				{
					Uri uri = ServiceBusResourceOperations.CreateCollectionUri<IResourceDescription>(this.uriManager.Current, this.descriptions, this.resourceNames, this.continuationToken, this.skip, this.top, this.filter);
					if (!this.settings.RetryPolicy.IsServerBusy || !(RetryPolicy.ServerBusyBaseSleepTime >= this.settings.InternalOperationTimeout))
					{
						this.operationTimer = new TimeoutHelper(this.settings.InternalOperationTimeout, true);
						do
						{
							flag2 = false;
							if (timeSpan1 != TimeSpan.Zero)
							{
								yield return base.CallAsyncSleep(timeSpan1);
							}
							base.LastAsyncStepException = null;
							this.InitializeRequest(uri);
							this.requestCancelTimer = new IOThreadTimer(new Action<object>(this.CancelRequest), this.request, true);
							try
							{
								TimeSpan timeSpan2 = this.operationTimer.RemainingTime();
								if (timeSpan2 > TimeSpan.Zero)
								{
									ServiceBusResourceOperations.GetAllAsyncResult getAllAsyncResult = this;
									IteratorAsyncResult<ServiceBusResourceOperations.GetAllAsyncResult>.BeginCall beginCall = (ServiceBusResourceOperations.GetAllAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
										IAsyncResult asyncResult = thisPtr.request.BeginGetResponse(c, s);
										thisPtr.requestCancelTimer.SetIfValid(timeSpan2);
										return asyncResult;
									};
									yield return getAllAsyncResult.CallAsync(beginCall, (ServiceBusResourceOperations.GetAllAsyncResult thisPtr, IAsyncResult r) => thisPtr.response = (HttpWebResponse)thisPtr.request.EndGetResponse(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
									if (base.LastAsyncStepException == null)
									{
										Uri uri1 = null;
										try
										{
											using (this.response)
											{
												if (this.response.StatusCode == HttpStatusCode.OK)
												{
													this.ProcessResponse(out uri1);
												}
												else
												{
													base.LastAsyncStepException = new MessagingException(this.response.StatusDescription, false, new WebException(this.response.StatusDescription, null, WebExceptionStatus.ProtocolError, this.response));
												}
											}
											this.response = null;
										}
										catch (XmlException xmlException)
										{
											base.LastAsyncStepException = new MessagingException(SRClient.InvalidXmlFormat, false, xmlException);
										}
										catch (WebException webException1)
										{
											WebException webException = webException1;
											base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, webException, this.request.Timeout, false);
										}
										catch (IOException oException1)
										{
											IOException oException = oException1;
											base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, oException, this.request.Timeout, this.isRequestAborted);
										}
										if (base.LastAsyncStepException == null)
										{
											this.requestCancelTimer.Cancel();
											this.settings.RetryPolicy.ResetServerBusy();
											if (this.stopAfterOnePage)
											{
												break;
											}
											if (uri1 == null)
											{
												goto Label1;
											}
											this.operationTimer = new TimeoutHelper(this.settings.InternalOperationTimeout, true);
											uri = uri1;
											flag2 = true;
											goto Label2;
										}
										else
										{
											TimeSpan timeSpan3 = this.operationTimer.RemainingTime();
											flag = (base.TransactionExists ? false : this.settings.RetryPolicy.ShouldRetry(timeSpan3, num, base.LastAsyncStepException, out timeSpan1));
											flag2 = flag;
											if (!flag2)
											{
												goto Label4;
											}
											string str = string.Format("GetAll:{0}", uri.AbsoluteUri);
											MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.settings.RetryPolicy.GetType().Name, str, num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
											num++;
										}
									}
									else
									{
										WebException lastAsyncStepException = base.LastAsyncStepException as WebException;
										IOException lastAsyncStepException1 = base.LastAsyncStepException as IOException;
										if (lastAsyncStepException != null)
										{
											base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, lastAsyncStepException, this.request.Timeout, false);
										}
										if (lastAsyncStepException1 != null)
										{
											base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, lastAsyncStepException1, this.request.Timeout, this.isRequestAborted);
										}
										TimeSpan timeSpan4 = this.operationTimer.RemainingTime();
										flag1 = (base.TransactionExists ? false : this.settings.RetryPolicy.ShouldRetry(timeSpan4, num, base.LastAsyncStepException, out timeSpan1));
										flag2 = flag1;
										if (!flag2)
										{
											goto Label3;
										}
										string str1 = string.Format("GetAll:{0}", uri.AbsoluteUri);
										MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.settings.RetryPolicy.GetType().Name, str1, num, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
										num++;
										goto Label3;
									}
								}
								else
								{
									base.Complete(new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout)));
									goto Label0;
								}
							}
							finally
							{
								this.requestCancelTimer.Cancel();
							}
						Label4:
						Label6:
						}
						while (this.uriManager.MoveNextUri() && flag2);
					Label5:
						base.Complete(base.LastAsyncStepException);
					}
					else
					{
						string str2 = this.settings.RetryPolicy.ServerBusyExceptionMessage;
						yield return base.CallAsyncSleep(base.RemainingTime());
						base.Complete(new ServerBusyException(str2));
					}
				}
				else
				{
					base.Complete(new ArgumentException("continuationToken"));
				}
			Label0:
				yield break;
			Label1:
				goto Label5;
			Label2:
				goto Label6;
			Label3:
				goto Label6;
			}

			private void InitializeRequest(Uri resourceUri)
			{
				this.request = (HttpWebRequest)WebRequest.Create(resourceUri);
				this.request.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
				this.request.Method = "GET";
				this.request.ContentType = "application/atom+xml;type=entry;charset=utf-8";
				this.request.Timeout = this.timeoutInMilliseconds;
				this.request.SetUserAgentHeader();
				this.request.AddXProcessAtHeader();
				this.request.AddAuthorizationHeader(this.tokenProvider, this.uriManager.Current, "Manage");
				this.request.AddTrackingIdHeader(this.trackingContext);
				this.request.AddCorrelationHeader(this.relatedActivity);
				this.request.AddFaultInjectionHeader(this.settings.FaultInjectionInfo);
			}

			private void ProcessResponse(out Uri nextPageLink)
			{
				Uri uri;
				nextPageLink = null;
				Stream responseStream = this.response.GetResponseStream();
				XmlReaderSettings xmlReaderSetting = new XmlReaderSettings()
				{
					CloseInput = true
				};
				using (XmlReader xmlReader = XmlReader.Create(responseStream, xmlReaderSetting))
				{
					SyndicationFeed syndicationFeed = SyndicationFeed.Load<SyndicationFeed>(xmlReader);
					if (syndicationFeed != null && syndicationFeed.Links != null)
					{
						SyndicationLink syndicationLink = syndicationFeed.Links.FirstOrDefault<SyndicationLink>(ServiceBusResourceOperations.GetAllAsyncResult.nextLinkPredicate);
						if (syndicationLink == null)
						{
							uri = null;
						}
						else
						{
							uri = syndicationLink.Uri;
						}
						nextPageLink = uri;
					}
					if (this.Feed != null)
					{
						SyndicationFeed feed = this.Feed;
						if (syndicationFeed != null && syndicationFeed.Items != null)
						{
							this.Feed = new SyndicationFeed(feed.Items.Union<SyndicationItem>(syndicationFeed.Items));
						}
					}
					else
					{
						this.Feed = syndicationFeed;
					}
				}
				this.NewContinuationToken = this.response.Headers["x-ms-continuationtoken"];
			}
		}

		private sealed class GetAsyncResult<TEntityDescription> : RetryAsyncResult<ServiceBusResourceOperations.GetAsyncResult<TEntityDescription>>
		where TEntityDescription : EntityDescription, IResourceDescription
		{
			private readonly TrackingContext trackingContext;

			private readonly EventTraceActivity relatedActivity;

			private readonly NamespaceManagerSettings settings;

			private readonly TokenProvider tokenProvider;

			private readonly MessagingDescriptionSerializer<TEntityDescription> serializer;

			private readonly IResourceDescription[] collectionDescriptions;

			private readonly string[] collectionResourceNames;

			private readonly ServiceBusUriManager uriManager;

			private readonly RetryPolicy retryPolicy;

			private HttpWebRequest request;

			private HttpWebResponse response;

			private IOThreadTimer requestCancelTimer;

			private volatile bool isRequestAborted;

			public string[] ResourceNames
			{
				get;
				private set;
			}

			public TEntityDescription Result
			{
				get;
				private set;
			}

			public GetAsyncResult(TrackingContext trackingContext, IResourceDescription[] collectionDescriptions, string[] collectionResourceNames, IEnumerable<Uri> managementAddresses, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw new ArgumentNullException("trackingContext");
				}
				if (collectionResourceNames == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("collectionResourceNames");
				}
				if (settings == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("settings");
				}
				this.trackingContext = trackingContext;
				this.ResourceNames = collectionResourceNames;
				this.settings = settings;
				this.tokenProvider = settings.TokenProvider;
				this.retryPolicy = settings.RetryPolicy;
				this.collectionDescriptions = collectionDescriptions;
				this.collectionResourceNames = collectionResourceNames;
				this.uriManager = new ServiceBusUriManager(managementAddresses.ToList<Uri>(), true);
				this.serializer = new MessagingDescriptionSerializer<TEntityDescription>();
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				base.Start();
			}

			private void CancelRequest(object state)
			{
				this.isRequestAborted = true;
				((HttpWebRequest)state).Abort();
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusResourceOperations.GetAsyncResult<TEntityDescription>>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				int num;
				string str;
				bool flag;
				bool flag1;
				bool flag2;
				int num1 = 0;
				timeSpan = (this.retryPolicy.IsServerBusy ? RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (!this.retryPolicy.IsServerBusy || !(RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag2 = false;
						if (timeSpan1 != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan1);
						}
						base.LastAsyncStepException = null;
						Uri uri = ServiceBusResourceOperations.CreateResourceUri(this.uriManager.Current, this.collectionDescriptions, this.collectionResourceNames, null);
						this.request = (HttpWebRequest)WebRequest.Create(uri);
						this.request.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
						this.request.Method = "GET";
						this.request.ContentType = "application/atom+xml;type=entry;charset=utf-8";
						HttpWebRequest httpWebRequest = this.request;
						num = (base.OriginalTimeout.TotalMilliseconds > 2147483647 ? 2147483647 : (int)base.OriginalTimeout.TotalMilliseconds);
						httpWebRequest.Timeout = num;
						this.request.SetUserAgentHeader();
						this.request.AddXProcessAtHeader();
						this.request.AddAuthorizationHeader(this.tokenProvider, this.uriManager.Current, "Manage");
						this.request.AddTrackingIdHeader(this.trackingContext);
						this.request.AddCorrelationHeader(this.relatedActivity);
						this.request.AddFaultInjectionHeader(this.settings.FaultInjectionInfo);
						this.requestCancelTimer = new IOThreadTimer(new Action<object>(this.CancelRequest), this.request, true);
						try
						{
							TimeSpan timeSpan2 = base.RemainingTime();
							if (timeSpan2 > TimeSpan.Zero)
							{
								ServiceBusResourceOperations.GetAsyncResult<TEntityDescription> getAsyncResult = this;
								IteratorAsyncResult<ServiceBusResourceOperations.GetAsyncResult<TEntityDescription>>.BeginCall beginCall = (ServiceBusResourceOperations.GetAsyncResult<TEntityDescription> thisPtr, TimeSpan t, AsyncCallback c, object s) => {
									IAsyncResult asyncResult = thisPtr.request.BeginGetResponse(c, s);
									thisPtr.requestCancelTimer.SetIfValid(timeSpan2);
									return asyncResult;
								};
								yield return getAsyncResult.CallAsync(beginCall, (ServiceBusResourceOperations.GetAsyncResult<TEntityDescription> thisPtr, IAsyncResult r) => thisPtr.response = (HttpWebResponse)thisPtr.request.EndGetResponse(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException == null)
								{
									try
									{
										using (this.response)
										{
											bool flag3 = this.response.ContentType.Replace(" ", "").Equals("application/atom+xml;type=feed;charset=utf-8", StringComparison.OrdinalIgnoreCase);
											str = ((int)this.ResourceNames.Length == 0 || string.IsNullOrEmpty(this.ResourceNames[0]) ? "Null" : this.ResourceNames[0]);
											string str1 = str;
											if (this.response.StatusCode != HttpStatusCode.OK)
											{
												base.LastAsyncStepException = new MessagingException(this.response.StatusDescription, false, new WebException(this.response.StatusDescription, null, WebExceptionStatus.ProtocolError, this.response));
											}
											else if (!flag3 || !typeof(TEntityDescription).FullName.Contains("TopicDescription") && !typeof(TEntityDescription).FullName.Contains("QueueDescription") && !typeof(TEntityDescription).FullName.Contains("SubscriptionDescription") && !typeof(TEntityDescription).FullName.Contains("RelayDescription") && !typeof(TEntityDescription).FullName.Contains("NotificationHubDescription") && !typeof(TEntityDescription).FullName.Contains("ConsumerGroupDescription") && !typeof(TEntityDescription).FullName.Contains("PartitionDescription") && !typeof(TEntityDescription).FullName.Contains("EventHubDescription"))
											{
												using (Stream responseStream = this.response.GetResponseStream())
												{
													try
													{
														this.Result = this.serializer.DeserializeFromAtomFeed(responseStream);
													}
													catch (InvalidCastException invalidCastException1)
													{
														InvalidCastException invalidCastException = invalidCastException1;
														base.LastAsyncStepException = new MessagingException(SRClient.InvalidManagementEntityType(str1, typeof(TEntityDescription).Name), false, invalidCastException);
													}
													catch (SerializationException serializationException1)
													{
														SerializationException serializationException = serializationException1;
														base.LastAsyncStepException = new MessagingException(SRClient.InvalidManagementEntityType(str1, typeof(TEntityDescription).Name), false, serializationException);
													}
												}
											}
											else
											{
												base.LastAsyncStepException = new MessagingEntityNotFoundException(str1);
											}
										}
									}
									catch (XmlException xmlException)
									{
										base.LastAsyncStepException = new MessagingException(SRClient.InvalidXmlFormat, false, xmlException);
									}
									catch (WebException webException1)
									{
										WebException webException = webException1;
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, webException, this.request.Timeout, false);
									}
									catch (IOException oException1)
									{
										IOException oException = oException1;
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, oException, this.request.Timeout, this.isRequestAborted);
									}
									if (base.LastAsyncStepException == null)
									{
										this.retryPolicy.ResetServerBusy();
									}
									else
									{
										TimeSpan timeSpan3 = base.RemainingTime();
										flag = (base.TransactionExists ? false : this.retryPolicy.ShouldRetry(timeSpan3, num1, base.LastAsyncStepException, out timeSpan1));
										flag2 = flag;
										if (!flag2)
										{
											continue;
										}
										string str2 = string.Format("Get:{0}", uri.AbsoluteUri);
										MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.retryPolicy.GetType().Name, str2, num1, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
										num1++;
									}
								}
								else
								{
									WebException lastAsyncStepException = base.LastAsyncStepException as WebException;
									IOException lastAsyncStepException1 = base.LastAsyncStepException as IOException;
									if (lastAsyncStepException != null)
									{
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, lastAsyncStepException, this.request.Timeout, false);
									}
									if (lastAsyncStepException1 != null)
									{
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, lastAsyncStepException1, this.request.Timeout, this.isRequestAborted);
									}
									TimeSpan timeSpan4 = base.RemainingTime();
									flag1 = (base.TransactionExists ? false : this.retryPolicy.ShouldRetry(timeSpan4, num1, base.LastAsyncStepException, out timeSpan1));
									flag2 = flag1;
									if (!flag2)
									{
										continue;
									}
									string str3 = string.Format("Get:{0}", uri.AbsoluteUri);
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.retryPolicy.GetType().Name, str3, num1, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
									num1++;
								}
							}
							else
							{
								base.Complete(new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout)));
								goto Label0;
							}
						}
						finally
						{
							this.requestCancelTimer.Cancel();
						}
					}
					while (this.uriManager.MoveNextUri() && flag2);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str4 = this.retryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str4));
				}
			Label0:
				yield break;
			}
		}

		private sealed class GetInformationAsyncResult : RetryAsyncResult<ServiceBusResourceOperations.GetInformationAsyncResult>
		{
			private readonly EventTraceActivity relatedActivity;

			private readonly NamespaceManagerSettings settings;

			private readonly TokenProvider tokenProvider;

			private readonly ServiceBusUriManager uriManager;

			private readonly RetryPolicy retryPolicy;

			private readonly TrackingContext trackingContext;

			private HttpWebRequest request;

			private HttpWebResponse response;

			private IOThreadTimer requestCancelTimer;

			private volatile bool isRequestAborted;

			public IDictionary<string, string> Information
			{
				get;
				private set;
			}

			public GetInformationAsyncResult(TrackingContext trackingContext, IEnumerable<Uri> managementAddresses, NamespaceManagerSettings settings, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				if (trackingContext == null)
				{
					throw new ArgumentNullException("trackingContext");
				}
				if (settings == null)
				{
					throw Microsoft.ServiceBus.Messaging.FxTrace.Exception.ArgumentNull("settings");
				}
				this.trackingContext = trackingContext;
				this.settings = settings;
				this.tokenProvider = settings.TokenProvider;
				this.retryPolicy = settings.RetryPolicy;
				this.Information = new ConcurrentDictionary<string, string>();
				this.uriManager = new ServiceBusUriManager(managementAddresses.ToList<Uri>(), true);
				this.relatedActivity = EventTraceActivity.CreateFromThread();
				base.Start();
			}

			private void CancelRequest(object state)
			{
				this.isRequestAborted = true;
				((HttpWebRequest)state).Abort();
			}

			protected override IEnumerator<IteratorAsyncResult<ServiceBusResourceOperations.GetInformationAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				TimeSpan timeSpan;
				int num;
				bool flag;
				bool flag1;
				bool flag2;
				int num1 = 0;
				timeSpan = (this.retryPolicy.IsServerBusy ? RetryPolicy.ServerBusyBaseSleepTime : TimeSpan.Zero);
				TimeSpan timeSpan1 = timeSpan;
				if (!this.retryPolicy.IsServerBusy || !(RetryPolicy.ServerBusyBaseSleepTime >= base.OriginalTimeout))
				{
					do
					{
						flag2 = false;
						if (timeSpan1 != TimeSpan.Zero)
						{
							yield return base.CallAsyncSleep(timeSpan1);
						}
						base.LastAsyncStepException = null;
						Uri uri = ServiceBusResourceOperations.CreateInformationUri(this.uriManager.Current);
						this.request = (HttpWebRequest)WebRequest.Create(uri);
						this.request.ServicePoint.MaxIdleTime = Constants.ServicePointMaxIdleTimeMilliSeconds;
						this.request.Method = "GET";
						this.request.ContentType = "application/atom+xml;type=entry;charset=utf-8";
						HttpWebRequest httpWebRequest = this.request;
						num = (base.OriginalTimeout.TotalMilliseconds > 2147483647 ? 2147483647 : (int)base.OriginalTimeout.TotalMilliseconds);
						httpWebRequest.Timeout = num;
						this.request.SetUserAgentHeader();
						this.request.AddAuthorizationHeader(this.tokenProvider, this.uriManager.Current, "Manage");
						this.request.AddTrackingIdHeader(this.trackingContext);
						this.request.AddCorrelationHeader(this.relatedActivity);
						this.request.AddXProcessAtHeader();
						this.request.AddFaultInjectionHeader(this.settings.FaultInjectionInfo);
						this.requestCancelTimer = new IOThreadTimer(new Action<object>(this.CancelRequest), this.request, true);
						try
						{
							TimeSpan timeSpan2 = base.RemainingTime();
							if (timeSpan2 > TimeSpan.Zero)
							{
								ServiceBusResourceOperations.GetInformationAsyncResult getInformationAsyncResult = this;
								IteratorAsyncResult<ServiceBusResourceOperations.GetInformationAsyncResult>.BeginCall beginCall = (ServiceBusResourceOperations.GetInformationAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => {
									IAsyncResult asyncResult = thisPtr.request.BeginGetResponse(c, s);
									thisPtr.requestCancelTimer.SetIfValid(timeSpan2);
									return asyncResult;
								};
								yield return getInformationAsyncResult.CallAsync(beginCall, (ServiceBusResourceOperations.GetInformationAsyncResult thisPtr, IAsyncResult r) => thisPtr.response = (HttpWebResponse)thisPtr.request.EndGetResponse(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
								if (base.LastAsyncStepException == null)
								{
									try
									{
										using (this.response)
										{
											if (this.response.StatusCode == HttpStatusCode.OK)
											{
												string[] allKeys = this.response.Headers.AllKeys;
												for (int i = 0; i < (int)allKeys.Length; i++)
												{
													string item = allKeys[i];
													this.Information[item] = this.response.Headers[item];
												}
											}
											else
											{
												base.LastAsyncStepException = new MessagingException(this.response.StatusDescription, new WebException(this.response.StatusDescription, null, WebExceptionStatus.ProtocolError, this.response));
											}
										}
									}
									catch (XmlException xmlException)
									{
										base.LastAsyncStepException = new MessagingException(SRClient.InvalidXmlFormat, xmlException);
									}
									catch (WebException webException1)
									{
										WebException webException = webException1;
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, webException, this.request.Timeout, false);
									}
									catch (IOException oException1)
									{
										IOException oException = oException1;
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, oException, this.request.Timeout, this.isRequestAborted);
									}
									if (base.LastAsyncStepException == null)
									{
										this.retryPolicy.ResetServerBusy();
									}
									else
									{
										flag = (base.TransactionExists ? false : this.retryPolicy.ShouldRetry(base.RemainingTime(), num1, base.LastAsyncStepException, out timeSpan1));
										flag2 = flag;
										if (!flag2)
										{
											continue;
										}
										MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.retryPolicy.GetType().Name, "GetInfo", num1, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
										num1++;
									}
								}
								else
								{
									WebException lastAsyncStepException = base.LastAsyncStepException as WebException;
									IOException lastAsyncStepException1 = base.LastAsyncStepException as IOException;
									if (lastAsyncStepException != null)
									{
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertWebException(this.trackingContext, lastAsyncStepException, this.request.Timeout, false);
									}
									if (lastAsyncStepException1 != null)
									{
										base.LastAsyncStepException = ServiceBusResourceOperations.ConvertIOException(this.trackingContext, lastAsyncStepException1, this.request.Timeout, this.isRequestAborted);
									}
									flag1 = (base.TransactionExists ? false : this.retryPolicy.ShouldRetry(base.RemainingTime(), num1, base.LastAsyncStepException, out timeSpan1));
									flag2 = flag1;
									if (!flag2)
									{
										continue;
									}
									MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteRetryPolicyIteration(null, string.Empty, this.retryPolicy.GetType().Name, "GetInfo", num1, timeSpan1.ToString(), this.LastAsyncStepException.GetType().FullName, this.LastAsyncStepException.Message));
									num1++;
								}
							}
							else
							{
								base.Complete(new TimeoutException(SRClient.OperationRequestTimedOut(base.OriginalTimeout)));
								goto Label0;
							}
						}
						finally
						{
							this.requestCancelTimer.Cancel();
						}
					}
					while (this.uriManager.MoveNextUri() && flag2);
					base.Complete(base.LastAsyncStepException);
				}
				else
				{
					string str = this.retryPolicy.ServerBusyExceptionMessage;
					yield return base.CallAsyncSleep(base.RemainingTime());
					base.Complete(new ServerBusyException(str));
				}
			Label0:
				yield break;
			}
		}
	}
}