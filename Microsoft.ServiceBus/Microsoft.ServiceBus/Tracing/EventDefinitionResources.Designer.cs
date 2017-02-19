using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Resources;

namespace Microsoft.ServiceBus.Tracing
{
	internal class EventDefinitionResources
	{
		private static System.Resources.ResourceManager resourceManager;

		private static CultureInfo resourceCulture;

		[GeneratedCode("StrictResXFileCodeGenerator", "4.0.0.0")]
		internal static CultureInfo Culture
		{
			get
			{
				return EventDefinitionResources.resourceCulture;
			}
			set
			{
				EventDefinitionResources.resourceCulture = value;
			}
		}

		internal static string event_GetStateTransfer
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_GetStateTransfer", EventDefinitionResources.Culture);
			}
		}

		internal static string event_HybridConnectionManagerConfigSettingsChanged
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionManagerConfigSettingsChanged", EventDefinitionResources.Culture);
			}
		}

		internal static string event_HybridConnectionManagerStarting
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionManagerStarting", EventDefinitionResources.Culture);
			}
		}

		internal static string event_HybridConnectionManagerStopping
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionManagerStopping", EventDefinitionResources.Culture);
			}
		}

		internal static string event_MessagePeekTransfer
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_MessagePeekTransfer", EventDefinitionResources.Culture);
			}
		}

		internal static string event_MessageReceiveTransfer
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_MessageReceiveTransfer", EventDefinitionResources.Culture);
			}
		}

		internal static string event_MessageSendingTransfer
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_MessageSendingTransfer", EventDefinitionResources.Culture);
			}
		}

		internal static string event_RelayChannelConnectionTransfer
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_RelayChannelConnectionTransfer", EventDefinitionResources.Culture);
			}
		}

		internal static string event_RenewSessionLockTransfer
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_RenewSessionLockTransfer", EventDefinitionResources.Culture);
			}
		}

		internal static string event_SetStateTransfer
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("event_SetStateTransfer", EventDefinitionResources.Culture);
			}
		}

		internal static string keyword_Client
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("keyword_Client", EventDefinitionResources.Culture);
			}
		}

		internal static string keyword_Gateway
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("keyword_Gateway", EventDefinitionResources.Culture);
			}
		}

		internal static string keyword_Host
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("keyword_Host", EventDefinitionResources.Culture);
			}
		}

		internal static string keyword_Powershell
		{
			get
			{
				return EventDefinitionResources.ResourceManager.GetString("keyword_Powershell", EventDefinitionResources.Culture);
			}
		}

		internal static System.Resources.ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(EventDefinitionResources.resourceManager, null))
				{
					EventDefinitionResources.resourceManager = new System.Resources.ResourceManager("Microsoft.ServiceBus.Tracing.EventDefinitionResources", typeof(EventDefinitionResources).Assembly);
				}
				return EventDefinitionResources.resourceManager;
			}
		}

		private EventDefinitionResources()
		{
		}

		internal static string ArgumentOutOfRange_MaxArgExceeded(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("ArgumentOutOfRange_MaxArgExceeded", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ArgumentOutOfRange_MaxStringsExceeded(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("ArgumentOutOfRange_MaxStringsExceeded", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AcceptSessionRequestBegin(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AcceptSessionRequestBegin", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AcceptSessionRequestEnd(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AcceptSessionRequestEnd", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AcceptSessionRequestFailed(object param0, object param1, object param2, object param3, object param4)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AcceptSessionRequestFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpAddSession(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpAddSession", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpAttachLink(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpAttachLink", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpDeliveryNotFound(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpDeliveryNotFound", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpDispose(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpDispose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpDynamicReadBufferSizeChange(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpDynamicReadBufferSizeChange", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpInputSessionChannelMessageReceived(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpInputSessionChannelMessageReceived", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpInsecureTransport(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpInsecureTransport", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpListenSocketAcceptError(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpListenSocketAcceptError", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpLogError(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpLogError", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpLogOperation(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpLogOperation", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpLogOperationVerbose(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpLogOperationVerbose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpManageLink(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpManageLink", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpOpenEntityFailed(object param0, object param1, object param2, object param3, object param4)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpOpenEntityFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpOpenEntitySucceeded(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpOpenEntitySucceeded", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpReceiveMessage(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpReceiveMessage", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpRemoveLink(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpRemoveLink", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpRemoveSession(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpRemoveSession", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpSettle(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpSettle", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpStateTransition(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpStateTransition", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AmqpUpgradeTransport(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AmqpUpgradeTransport", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_AppDomainUnload(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_AppDomainUnload", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_BatchManagerException(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_BatchManagerException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_BatchManagerExecutingBatchedObject(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_BatchManagerExecutingBatchedObject", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_BatchManagerTransactionInDoubt(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_BatchManagerTransactionInDoubt", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ChannelFaulted(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ChannelFaulted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ChannelReceiveContextAbandon(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ChannelReceiveContextAbandon", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ChannelReceiveContextComplete(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ChannelReceiveContextComplete", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ChannelReceivedMessage(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ChannelReceivedMessage", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ChannelSendingMessage(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ChannelSendingMessage", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_DetectConnectivityModeFailed(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_DetectConnectivityModeFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_DetectConnectivityModeSucceeded(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_DetectConnectivityModeSucceeded", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ExceptionAsInformation(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ExceptionAsInformation", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ExceptionAsWarning(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ExceptionAsWarning", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_FailedToCancelNotification(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_FailedToCancelNotification", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_FailFastOccurred(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_FailFastOccurred", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_FramingOuputPumpPingException(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_FramingOuputPumpPingException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_FramingOuputPumpRunException(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_FramingOuputPumpRunException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_GetRuntimeEntityDescriptionCompleted(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_GetRuntimeEntityDescriptionCompleted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_GetRuntimeEntityDescriptionFailed(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_GetRuntimeEntityDescriptionFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_GetRuntimeEntityDescriptionStarted(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_GetRuntimeEntityDescriptionStarted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HandledExceptionWarning(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HandledExceptionWarning", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HandledExceptionWithEntityName(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HandledExceptionWithEntityName", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HandledExceptionWithFunctionName(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HandledExceptionWithFunctionName", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionFailedToReadResourceDescriptionMetaData(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionFailedToReadResourceDescriptionMetaData", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionFailedToStart(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionFailedToStart", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionFailedToStop(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionFailedToStop", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionInvalidConnectionString(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionInvalidConnectionString", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionManagerConfigurationFileError(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionManagerConfigurationFileError", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionManagerManagementServerError(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionManagerManagementServerError", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionManagerManagementServiceStarting(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionManagerManagementServiceStarting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionManagerManagementServiceStopping(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionManagerManagementServiceStopping", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionSecurityException(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionSecurityException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionStarted(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionStarted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_HybridConnectionStopped(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_HybridConnectionStopped", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_LogAsWarning(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_LogAsWarning", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_LogOperation(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_LogOperation", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_LogOperationWarning(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_LogOperationWarning", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageAbandon(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageAbandon", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageCanceling(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageCanceling", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageComplete(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageComplete", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageDefer(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageDefer", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceived(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceived", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceivePumpBackoff(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceivePumpBackoff", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceivePumpFailedToAbandon(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceivePumpFailedToAbandon", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceivePumpFailedToComplete(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceivePumpFailedToComplete", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceivePumpReceiveException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceivePumpReceiveException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceivePumpStopped(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceivePumpStopped", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceivePumpUnexpectedException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceivePumpUnexpectedException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageReceivePumpUserCallbackException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageReceivePumpUserCallbackException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageRenew(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageRenew", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageSending(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageSending", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_MessageSuspend(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_MessageSuspend", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_NonSerializableException(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_NonSerializableException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_NullReferenceErrorOccurred(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_NullReferenceErrorOccurred", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceCouldNotCreateMessageSender(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceCouldNotCreateMessageSender", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceDeadletterException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceDeadletterException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceDestinationSendException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceDestinationSendException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceMessageNoPathInBacklog(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceMessageNoPathInBacklog", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceMessagePumpProcessCloseSenderFailed(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceMessagePumpProcessCloseSenderFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceMessagePumpProcessQueueFailed(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceMessagePumpProcessQueueFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceMessagePumpReceiveFailed(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceMessagePumpReceiveFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespacePingException(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespacePingException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceReceiveMessageFromSecondary(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceReceiveMessageFromSecondary", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceSendingMessage(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceSendingMessage", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceSendMessageFailure(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceSendMessageFailure", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceSendMessageSuccess(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceSendMessageSuccess", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceSendToBacklogFailed(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceSendToBacklogFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceStartSyphon(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceStartSyphon", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceStopSyphon(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceStopSyphon", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceTransferQueueCreateError(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceTransferQueueCreateError", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PairedNamespaceTransferQueueCreateFailure(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PairedNamespaceTransferQueueCreateFailure", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PerformanceCounterCreationFailed(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PerformanceCounterCreationFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PerformanceCounterInstanceCreated(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PerformanceCounterInstanceCreated", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_PerformanceCounterInstanceRemoved(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_PerformanceCounterInstanceRemoved", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayChannelAborting(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayChannelAborting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayChannelClosing(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayChannelClosing", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayChannelFaulting(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayChannelFaulting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayChannelOpening(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayChannelOpening", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientConnected(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientConnected", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientConnectFailed(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientConnectFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientConnecting(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientConnecting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientConnectivityModeDetected(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientConnectivityModeDetected", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientConnectRedirected(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientConnectRedirected", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientDisconnected(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientDisconnected", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientFailedToAcquireToken(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientFailedToAcquireToken", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientGoingOnline(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientGoingOnline", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientPingFailed(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientPingFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientReconnecting(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientReconnecting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayClientStopConnecting(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayClientStopConnecting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayListenerClientAccepted(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayListenerClientAccepted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayListenerClientAcceptFailed(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayListenerClientAcceptFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayListenerFailedToDispatchMessage(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayListenerFailedToDispatchMessage", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayListenerRelayedConnectReceived(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayListenerRelayedConnectReceived", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RelayLogVerbose(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RelayLogVerbose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RetryOperation(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RetryOperation", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RetryPolicyIteration(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RetryPolicyIteration", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4, param5, param6 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RetryPolicyStreamNotClonable(object param0, object param1, object param2, object param3, object param4)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RetryPolicyStreamNotClonable", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RetryPolicyStreamNotSeekable(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RetryPolicyStreamNotSeekable", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RuntimeChannelAborting(object param0, object param1, object param2, object param3, object param4)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RuntimeChannelAborting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RuntimeChannelCreated(object param0, object param1, object param2, object param3, object param4)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RuntimeChannelCreated", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RuntimeChannelFaulting(object param0, object param1, object param2, object param3, object param4, object param5)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RuntimeChannelFaulting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4, param5 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RuntimeChannelPingFailed(object param0, object param1, object param2, object param3, object param4, object param5)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RuntimeChannelPingFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4, param5 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RuntimeChannelPingIncorrectState(object param0, object param1, object param2, object param3, object param4, object param5)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RuntimeChannelPingIncorrectState", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4, param5 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_RuntimeChannelStopPingWithIncorrectState(object param0, object param1, object param2, object param3, object param4, object param5, object param6)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_RuntimeChannelStopPingWithIncorrectState", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4, param5, param6 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ShipAssertExceptionMessage(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ShipAssertExceptionMessage", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_SingletonManagerLoadSucceeded(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_SingletonManagerLoadSucceeded", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ThreadNeutralSemaphoreEnterFailed(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ThreadNeutralSemaphoreEnterFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ThrowingExceptionError(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ThrowingExceptionError", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ThrowingExceptionInformational(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ThrowingExceptionInformational", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ThrowingExceptionVerbose(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ThrowingExceptionVerbose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ThrowingExceptionWarning(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ThrowingExceptionWarning", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_ThrowingExceptionWithEntityName(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_ThrowingExceptionWithEntityName", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_TraceCodeEventLogCritical(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_TraceCodeEventLogCritical", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_TraceCodeEventLogError(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_TraceCodeEventLogError", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_TraceCodeEventLogInformational(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_TraceCodeEventLogInformational", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_TraceCodeEventLogVerbose(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_TraceCodeEventLogVerbose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_TraceCodeEventLogWarning(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_TraceCodeEventLogWarning", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_UnexpectedExceptionTelemetry(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_UnexpectedExceptionTelemetry", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_UnexpectedScheduledNotificationIdFormat(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_UnexpectedScheduledNotificationIdFormat", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_UnhandledException(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_UnhandledException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketConnectionAbort(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketConnectionAbort", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketConnectionClose(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketConnectionClose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketConnectionEstablished(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketConnectionEstablished", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketConnectionShutdown(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketConnectionShutdown", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketTransportAborted(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketTransportAborted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketTransportClosed(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketTransportClosed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketTransportEstablished(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketTransportEstablished", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebSocketTransportShutdown(object param0)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebSocketTransportShutdown", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamAbort(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamAbort", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamClose(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamClose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamConnectCompleted(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamConnectCompleted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamConnectFailed(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamConnectFailed", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamConnecting(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamConnecting", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamConnectionAbort(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamConnectionAbort", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamConnectionClose(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamConnectionClose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamConnectionShutdown(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamConnectionShutdown", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamDispose(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamDispose", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamFramingInputPumpSlowRead(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamFramingInputPumpSlowRead", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamFramingInputPumpSlowReadWithException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamFramingInputPumpSlowReadWithException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamFramingOuputPumpPingSlow(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamFramingOuputPumpPingSlow", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamFramingOuputPumpPingSlowException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamFramingOuputPumpPingSlowException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamFramingOuputPumpSlow(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamFramingOuputPumpSlow", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamFramingOuputPumpSlowException(object param0, object param1, object param2)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamFramingOuputPumpSlowException", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamReadStreamCompleted(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamReadStreamCompleted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamReset(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamReset", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamReturningZero(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamReturningZero", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamShutdown(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamShutdown", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string event_WebStreamWriteStreamCompleted(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("event_WebStreamWriteStreamCompleted", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string EventSource_UndefinedChannel(object param0, object param1)
		{
			CultureInfo culture = EventDefinitionResources.Culture;
			string str = EventDefinitionResources.ResourceManager.GetString("EventSource_UndefinedChannel", EventDefinitionResources.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}
	}
}