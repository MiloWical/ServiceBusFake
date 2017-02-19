using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class HttpWebRequestExtensions
	{
		private const string userAgentTemplate = "SERVICEBUS/2014-09(api-origin=DotNetSdk;os={0};os-version={1})";

		public static void AddAuthorizationHeader(this HttpWebRequest request, TokenProvider tokenProvider, Uri namespaceAddress, string action)
		{
			if (tokenProvider != null)
			{
				string messagingWebToken = tokenProvider.GetMessagingWebToken(namespaceAddress, request.RequestUri.AbsoluteUri, action, false, Constants.TokenRequestOperationTimeout);
				request.Headers[HttpRequestHeader.Authorization] = messagingWebToken;
			}
		}

		public static void AddCorrelationHeader(this HttpWebRequest request, EventTraceActivity activity)
		{
			if (activity != null && activity != EventTraceActivity.Empty)
			{
				request.Headers[EventTraceActivity.Name] = Convert.ToBase64String(activity.ActivityId.ToByteArray());
			}
		}

		public static void AddFaultInjectionHeader(this HttpWebRequest request, FaultInjectionInfo faultInjectionInfo)
		{
			if (faultInjectionInfo != null)
			{
				faultInjectionInfo.AddToHeader(request);
			}
		}

		public static void AddServiceBusDlqSupplementaryAuthorizationHeader(this HttpWebRequest request, TokenProvider tokenProvider, Uri namespaceAddress, Uri appliesToUri, string action)
		{
			request.InternalAddServiceBusSupplementaryAuthorizationHeader("ServiceBusDlqSupplementaryAuthorization", tokenProvider, namespaceAddress, appliesToUri, action);
		}

		public static void AddServiceBusSupplementaryAuthorizationHeader(this HttpWebRequest request, TokenProvider tokenProvider, Uri namespaceAddress, Uri appliesToUri, string action)
		{
			request.InternalAddServiceBusSupplementaryAuthorizationHeader("ServiceBusSupplementaryAuthorization", tokenProvider, namespaceAddress, appliesToUri, action);
		}

		public static void AddTrackingIdHeader(this HttpWebRequest request, TrackingContext trackingContext)
		{
			if (trackingContext != null)
			{
				request.Headers["TrackingId"] = trackingContext.TrackingId;
			}
		}

		public static void AddXProcessAtHeader(this HttpWebRequest request)
		{
			request.Headers.Add("X-PROCESS-AT", "ServiceBus");
		}

		private static void InternalAddServiceBusSupplementaryAuthorizationHeader(this HttpWebRequest request, string authorizationHeaderName, TokenProvider tokenProvider, Uri namespaceAddress, Uri appliesToUri, string action)
		{
			if (tokenProvider != null)
			{
				string messagingWebToken = tokenProvider.GetMessagingWebToken(namespaceAddress, appliesToUri.AbsoluteUri, action, false, Constants.TokenRequestOperationTimeout);
				request.Headers[authorizationHeaderName] = messagingWebToken;
			}
		}

		public static void SetUserAgentHeader(this HttpWebRequest request)
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] platform = new object[] { Environment.OSVersion.Platform, Environment.OSVersion.Version };
			request.UserAgent = string.Format(invariantCulture, "SERVICEBUS/2014-09(api-origin=DotNetSdk;os={0};os-version={1})", platform);
		}
	}
}