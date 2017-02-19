using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Resources;

namespace Microsoft.ServiceBus
{
	internal class SRClient
	{
		private static System.Resources.ResourceManager resourceManager;

		private static CultureInfo resourceCulture;

		internal static string ActionMustBeProcessed
		{
			get
			{
				return SRClient.ResourceManager.GetString("ActionMustBeProcessed", SRClient.Culture);
			}
		}

		internal static string AdmRegistrationIdInvalid
		{
			get
			{
				return SRClient.ResourceManager.GetString("AdmRegistrationIdInvalid", SRClient.Culture);
			}
		}

		internal static string AlreadyRunning
		{
			get
			{
				return SRClient.ResourceManager.GetString("AlreadyRunning", SRClient.Culture);
			}
		}

		internal static string ApnsCertificateExpired
		{
			get
			{
				return SRClient.ResourceManager.GetString("ApnsCertificateExpired", SRClient.Culture);
			}
		}

		internal static string ApnsCertificateNotValid
		{
			get
			{
				return SRClient.ResourceManager.GetString("ApnsCertificateNotValid", SRClient.Culture);
			}
		}

		internal static string ApnsCertificatePrivatekeyMissing
		{
			get
			{
				return SRClient.ResourceManager.GetString("ApnsCertificatePrivatekeyMissing", SRClient.Culture);
			}
		}

		internal static string ApnsEndpointNotAllowed
		{
			get
			{
				return SRClient.ResourceManager.GetString("ApnsEndpointNotAllowed", SRClient.Culture);
			}
		}

		internal static string ApnsPropertiesNotSpecified
		{
			get
			{
				return SRClient.ResourceManager.GetString("ApnsPropertiesNotSpecified", SRClient.Culture);
			}
		}

		internal static string ApnsRequiredPropertiesError
		{
			get
			{
				return SRClient.ResourceManager.GetString("ApnsRequiredPropertiesError", SRClient.Culture);
			}
		}

		internal static string ArgumentOutOfRangeLessThanOne
		{
			get
			{
				return SRClient.ResourceManager.GetString("ArgumentOutOfRangeLessThanOne", SRClient.Culture);
			}
		}

		internal static string AsyncResultDifferent
		{
			get
			{
				return SRClient.ResourceManager.GetString("AsyncResultDifferent", SRClient.Culture);
			}
		}

		internal static string AsyncResultInUse
		{
			get
			{
				return SRClient.ResourceManager.GetString("AsyncResultInUse", SRClient.Culture);
			}
		}

		internal static string AsyncResultNotInUse
		{
			get
			{
				return SRClient.ResourceManager.GetString("AsyncResultNotInUse", SRClient.Culture);
			}
		}

		internal static string BacklogDeadletterDescriptionNoQueuePath
		{
			get
			{
				return SRClient.ResourceManager.GetString("BacklogDeadletterDescriptionNoQueuePath", SRClient.Culture);
			}
		}

		internal static string BacklogDeadletterReasonNoQueuePath
		{
			get
			{
				return SRClient.ResourceManager.GetString("BacklogDeadletterReasonNoQueuePath", SRClient.Culture);
			}
		}

		internal static string BacklogDeadletterReasonNotRetryable
		{
			get
			{
				return SRClient.ResourceManager.GetString("BacklogDeadletterReasonNotRetryable", SRClient.Culture);
			}
		}

		internal static string BaiduApiKeyNotSpecified
		{
			get
			{
				return SRClient.ResourceManager.GetString("BaiduApiKeyNotSpecified", SRClient.Culture);
			}
		}

		internal static string BaiduEndpointNotSpecified
		{
			get
			{
				return SRClient.ResourceManager.GetString("BaiduEndpointNotSpecified", SRClient.Culture);
			}
		}

		internal static string BaiduRegistrationInvalidId
		{
			get
			{
				return SRClient.ResourceManager.GetString("BaiduRegistrationInvalidId", SRClient.Culture);
			}
		}

		internal static string BaiduRequiredProperties
		{
			get
			{
				return SRClient.ResourceManager.GetString("BaiduRequiredProperties", SRClient.Culture);
			}
		}

		internal static string BatchManagerAborted
		{
			get
			{
				return SRClient.ResourceManager.GetString("BatchManagerAborted", SRClient.Culture);
			}
		}

		internal static string BeginGetWebTokenNotSupported
		{
			get
			{
				return SRClient.ResourceManager.GetString("BeginGetWebTokenNotSupported", SRClient.Culture);
			}
		}

		internal static string BodyIsNotSupportedExpression
		{
			get
			{
				return SRClient.ResourceManager.GetString("BodyIsNotSupportedExpression", SRClient.Culture);
			}
		}

		internal static string BufferAlreadyReclaimed
		{
			get
			{
				return SRClient.ResourceManager.GetString("BufferAlreadyReclaimed", SRClient.Culture);
			}
		}

		internal static string CannotCheckpointWithCurrentConsumerGroup
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotCheckpointWithCurrentConsumerGroup", SRClient.Culture);
			}
		}

		internal static string CannotCreateClientOnSubQueue
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotCreateClientOnSubQueue", SRClient.Culture);
			}
		}

		internal static string CannotCreateMessageSessionForSubQueue
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotCreateMessageSessionForSubQueue", SRClient.Culture);
			}
		}

		internal static string CannotCreateReceiverWithDispatcher
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotCreateReceiverWithDispatcher", SRClient.Culture);
			}
		}

		internal static string CannotHaveDuplicateAccessRights
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotHaveDuplicateAccessRights", SRClient.Culture);
			}
		}

		internal static string CannotHaveDuplicateSAARule
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotHaveDuplicateSAARule", SRClient.Culture);
			}
		}

		internal static string CannotSendReceivedMessage
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotSendReceivedMessage", SRClient.Culture);
			}
		}

		internal static string CannotSerializeMessageWithPartiallyConsumedBodyStream
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotSerializeMessageWithPartiallyConsumedBodyStream", SRClient.Culture);
			}
		}

		internal static string CannotSerializeSessionStateWithPartiallyConsumedStream
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotSerializeSessionStateWithPartiallyConsumedStream", SRClient.Culture);
			}
		}

		internal static string CannotSpecifyExpirationTime
		{
			get
			{
				return SRClient.ResourceManager.GetString("CannotSpecifyExpirationTime", SRClient.Culture);
			}
		}

		internal static string ChannelUriNullOrEmpty
		{
			get
			{
				return SRClient.ResourceManager.GetString("ChannelUriNullOrEmpty", SRClient.Culture);
			}
		}

		internal static string ClientTargetHostAlreadySet
		{
			get
			{
				return SRClient.ResourceManager.GetString("ClientTargetHostAlreadySet", SRClient.Culture);
			}
		}

		internal static string ClientTargetHostServerCertificateNotSet
		{
			get
			{
				return SRClient.ResourceManager.GetString("ClientTargetHostServerCertificateNotSet", SRClient.Culture);
			}
		}

		internal static string ConnectFailed
		{
			get
			{
				return SRClient.ResourceManager.GetString("ConnectFailed", SRClient.Culture);
			}
		}

		internal static string ConnectFailedCommunicationException
		{
			get
			{
				return SRClient.ResourceManager.GetString("ConnectFailedCommunicationException", SRClient.Culture);
			}
		}

		internal static string ConnectionStatusBehavior
		{
			get
			{
				return SRClient.ResourceManager.GetString("ConnectionStatusBehavior", SRClient.Culture);
			}
		}

		internal static string ConnectionStringWithInvalidScheme
		{
			get
			{
				return SRClient.ResourceManager.GetString("ConnectionStringWithInvalidScheme", SRClient.Culture);
			}
		}

		internal static string ConnectionTermination
		{
			get
			{
				return SRClient.ResourceManager.GetString("ConnectionTermination", SRClient.Culture);
			}
		}

		internal static string CreateSessionOnClosingConnection
		{
			get
			{
				return SRClient.ResourceManager.GetString("CreateSessionOnClosingConnection", SRClient.Culture);
			}
		}

		internal static string CreditListenerAlreadyRegistered
		{
			get
			{
				return SRClient.ResourceManager.GetString("CreditListenerAlreadyRegistered", SRClient.Culture);
			}
		}

		[GeneratedCode("StrictResXFileCodeGenerator", "4.0.0.0")]
		internal static CultureInfo Culture
		{
			get
			{
				return SRClient.resourceCulture;
			}
			set
			{
				SRClient.resourceCulture = value;
			}
		}

		internal static string DeviceTokenHexaDecimalDigitError
		{
			get
			{
				return SRClient.ResourceManager.GetString("DeviceTokenHexaDecimalDigitError", SRClient.Culture);
			}
		}

		internal static string DeviceTokenIsEmpty
		{
			get
			{
				return SRClient.ResourceManager.GetString("DeviceTokenIsEmpty", SRClient.Culture);
			}
		}

		internal static string DispositionListenerAlreadyRegistered
		{
			get
			{
				return SRClient.ResourceManager.GetString("DispositionListenerAlreadyRegistered", SRClient.Culture);
			}
		}

		internal static string DispositionListenerSetNotSupported
		{
			get
			{
				return SRClient.ResourceManager.GetString("DispositionListenerSetNotSupported", SRClient.Culture);
			}
		}

		internal static string DownstreamConnection
		{
			get
			{
				return SRClient.ResourceManager.GetString("DownstreamConnection", SRClient.Culture);
			}
		}

		internal static string DuplicateConnectionID
		{
			get
			{
				return SRClient.ResourceManager.GetString("DuplicateConnectionID", SRClient.Culture);
			}
		}

		internal static string DuplicateConnectionIDFault
		{
			get
			{
				return SRClient.ResourceManager.GetString("DuplicateConnectionIDFault", SRClient.Culture);
			}
		}

		internal static string EmptyExpiryValue
		{
			get
			{
				return SRClient.ResourceManager.GetString("EmptyExpiryValue", SRClient.Culture);
			}
		}

		internal static string EmptyPropertyInCorrelationFilter
		{
			get
			{
				return SRClient.ResourceManager.GetString("EmptyPropertyInCorrelationFilter", SRClient.Culture);
			}
		}

		internal static string EnabledAutoFlowCreditIssuing
		{
			get
			{
				return SRClient.ResourceManager.GetString("EnabledAutoFlowCreditIssuing", SRClient.Culture);
			}
		}

		internal static string EndpointNotFound
		{
			get
			{
				return SRClient.ResourceManager.GetString("EndpointNotFound", SRClient.Culture);
			}
		}

		internal static string EndpointNotFoundFault
		{
			get
			{
				return SRClient.ResourceManager.GetString("EndpointNotFoundFault", SRClient.Culture);
			}
		}

		internal static string EntityClosedOrAborted
		{
			get
			{
				return SRClient.ResourceManager.GetString("EntityClosedOrAborted", SRClient.Culture);
			}
		}

		internal static string ErroConvertingToChar
		{
			get
			{
				return SRClient.ResourceManager.GetString("ErroConvertingToChar", SRClient.Culture);
			}
		}

		internal static string ErrorNoCotent
		{
			get
			{
				return SRClient.ResourceManager.GetString("ErrorNoCotent", SRClient.Culture);
			}
		}

		internal static string EventDataDisposed
		{
			get
			{
				return SRClient.ResourceManager.GetString("EventDataDisposed", SRClient.Culture);
			}
		}

		internal static string EventDataListIsNullOrEmpty
		{
			get
			{
				return SRClient.ResourceManager.GetString("EventDataListIsNullOrEmpty", SRClient.Culture);
			}
		}

		internal static string EventHubPathMismatch
		{
			get
			{
				return SRClient.ResourceManager.GetString("EventHubPathMismatch", SRClient.Culture);
			}
		}

		internal static string ExpectedBytesNotRead
		{
			get
			{
				return SRClient.ResourceManager.GetString("ExpectedBytesNotRead", SRClient.Culture);
			}
		}

		internal static string ExpiryDeserializationError
		{
			get
			{
				return SRClient.ResourceManager.GetString("ExpiryDeserializationError", SRClient.Culture);
			}
		}

		internal static string FactoryEndpoint
		{
			get
			{
				return SRClient.ResourceManager.GetString("FactoryEndpoint", SRClient.Culture);
			}
		}

		internal static string FailedToDeserializeBodyTemplate
		{
			get
			{
				return SRClient.ResourceManager.GetString("FailedToDeserializeBodyTemplate", SRClient.Culture);
			}
		}

		internal static string FailedToDeSerializeEntireBodyStream
		{
			get
			{
				return SRClient.ResourceManager.GetString("FailedToDeSerializeEntireBodyStream", SRClient.Culture);
			}
		}

		internal static string FailedToDeSerializeEntireSessionStateStream
		{
			get
			{
				return SRClient.ResourceManager.GetString("FailedToDeSerializeEntireSessionStateStream", SRClient.Culture);
			}
		}

		internal static string FailedToSerializeEntireBodyStream
		{
			get
			{
				return SRClient.ResourceManager.GetString("FailedToSerializeEntireBodyStream", SRClient.Culture);
			}
		}

		internal static string FailedToSerializeEntireSessionStateStream
		{
			get
			{
				return SRClient.ResourceManager.GetString("FailedToSerializeEntireSessionStateStream", SRClient.Culture);
			}
		}

		internal static string FaultyEndpointResponse
		{
			get
			{
				return SRClient.ResourceManager.GetString("FaultyEndpointResponse", SRClient.Culture);
			}
		}

		internal static string FilterExpressionTooComplex
		{
			get
			{
				return SRClient.ResourceManager.GetString("FilterExpressionTooComplex", SRClient.Culture);
			}
		}

		internal static string FilterMustBeProcessed
		{
			get
			{
				return SRClient.ResourceManager.GetString("FilterMustBeProcessed", SRClient.Culture);
			}
		}

		internal static string GcmEndpointNotSpecified
		{
			get
			{
				return SRClient.ResourceManager.GetString("GcmEndpointNotSpecified", SRClient.Culture);
			}
		}

		internal static string GCMRegistrationInvalidId
		{
			get
			{
				return SRClient.ResourceManager.GetString("GCMRegistrationInvalidId", SRClient.Culture);
			}
		}

		internal static string GcmRequiredProperties
		{
			get
			{
				return SRClient.ResourceManager.GetString("GcmRequiredProperties", SRClient.Culture);
			}
		}

		internal static string GoogleApiKeyNotSpecified
		{
			get
			{
				return SRClient.ResourceManager.GetString("GoogleApiKeyNotSpecified", SRClient.Culture);
			}
		}

		internal static string HTTPAuthTokenNotSupportedException
		{
			get
			{
				return SRClient.ResourceManager.GetString("HTTPAuthTokenNotSupportedException", SRClient.Culture);
			}
		}

		internal static string HTTPConnectivityMode
		{
			get
			{
				return SRClient.ResourceManager.GetString("HTTPConnectivityMode", SRClient.Culture);
			}
		}

		internal static string IncompatibleChannelListener
		{
			get
			{
				return SRClient.ResourceManager.GetString("IncompatibleChannelListener", SRClient.Culture);
			}
		}

		internal static string InternalServerError
		{
			get
			{
				return SRClient.ResourceManager.GetString("InternalServerError", SRClient.Culture);
			}
		}

		internal static string InvalidAdmAuthTokenUrl
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidAdmAuthTokenUrl", SRClient.Culture);
			}
		}

		internal static string InvalidAdmSendUrlTemplate
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidAdmSendUrlTemplate", SRClient.Culture);
			}
		}

		internal static string InvalidBaiduEndpoint
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidBaiduEndpoint", SRClient.Culture);
			}
		}

		internal static string InvalidBatchFlushInterval
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidBatchFlushInterval", SRClient.Culture);
			}
		}

		internal static string InvalidBufferSize
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidBufferSize", SRClient.Culture);
			}
		}

		internal static string InvalidCallFaultException
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidCallFaultException", SRClient.Culture);
			}
		}

		internal static string InvalidChannelType
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidChannelType", SRClient.Culture);
			}
		}

		internal static string InvalidCombinationOfManageRight
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidCombinationOfManageRight", SRClient.Culture);
			}
		}

		internal static string InvalidConfiguration
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidConfiguration", SRClient.Culture);
			}
		}

		internal static string InvalidEncoding
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidEncoding", SRClient.Culture);
			}
		}

		internal static string InvalidGcmEndpoint
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidGcmEndpoint", SRClient.Culture);
			}
		}

		internal static string InvalidID
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidID", SRClient.Culture);
			}
		}

		internal static string InvalidIssuerSecret
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidIssuerSecret", SRClient.Culture);
			}
		}

		internal static string InvalidLengthofReceivedContent
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidLengthofReceivedContent", SRClient.Culture);
			}
		}

		internal static string InvalidMpnsCertificate
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidMpnsCertificate", SRClient.Culture);
			}
		}

		internal static string InvalidNokiaXEndpoint
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidNokiaXEndpoint", SRClient.Culture);
			}
		}

		internal static string InvalidOperationOnSessionBrowser
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidOperationOnSessionBrowser", SRClient.Culture);
			}
		}

		internal static string InvalidPayLoadFormat
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidPayLoadFormat", SRClient.Culture);
			}
		}

		internal static string InvalidReceivedContent
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidReceivedContent", SRClient.Culture);
			}
		}

		internal static string InvalidReceivedSessionId
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidReceivedSessionId", SRClient.Culture);
			}
		}

		internal static string InvalidRefcountedCommunicationObject
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidRefcountedCommunicationObject", SRClient.Culture);
			}
		}

		internal static string InvalidStateMachineRefcountedCommunicationObject
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidStateMachineRefcountedCommunicationObject", SRClient.Culture);
			}
		}

		internal static string InvalidWindowsLiveEndpoint
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidWindowsLiveEndpoint", SRClient.Culture);
			}
		}

		internal static string InvalidXmlFormat
		{
			get
			{
				return SRClient.ResourceManager.GetString("InvalidXmlFormat", SRClient.Culture);
			}
		}

		internal static string IOThreadTimerCannotAcceptMaxTimeSpan
		{
			get
			{
				return SRClient.ResourceManager.GetString("IOThreadTimerCannotAcceptMaxTimeSpan", SRClient.Culture);
			}
		}

		internal static string IsolationLevelNotSupported
		{
			get
			{
				return SRClient.ResourceManager.GetString("IsolationLevelNotSupported", SRClient.Culture);
			}
		}

		internal static string ITokenProviderType
		{
			get
			{
				return SRClient.ResourceManager.GetString("ITokenProviderType", SRClient.Culture);
			}
		}

		internal static string ListenerLengthArgumentOutOfRange
		{
			get
			{
				return SRClient.ResourceManager.GetString("ListenerLengthArgumentOutOfRange", SRClient.Culture);
			}
		}

		internal static string LockedMessageInfo
		{
			get
			{
				return SRClient.ResourceManager.GetString("LockedMessageInfo", SRClient.Culture);
			}
		}

		internal static string MaximumAttemptsExceeded
		{
			get
			{
				return SRClient.ResourceManager.GetString("MaximumAttemptsExceeded", SRClient.Culture);
			}
		}

		internal static string MessageBodyConsumed
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessageBodyConsumed", SRClient.Culture);
			}
		}

		internal static string MessageBodyNull
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessageBodyNull", SRClient.Culture);
			}
		}

		internal static string MessageEntityDisposed
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessageEntityDisposed", SRClient.Culture);
			}
		}

		internal static string MessageEntityNotOpened
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessageEntityNotOpened", SRClient.Culture);
			}
		}

		internal static string MessageListenerAlreadyRegistered
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessageListenerAlreadyRegistered", SRClient.Culture);
			}
		}

		internal static string MessageLockLost
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessageLockLost", SRClient.Culture);
			}
		}

		internal static string MessageSizeExceeded
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessageSizeExceeded", SRClient.Culture);
			}
		}

		internal static string MessagingCommunicationError
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessagingCommunicationError", SRClient.Culture);
			}
		}

		internal static string MessagingPartitioningInvalidOperation
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessagingPartitioningInvalidOperation", SRClient.Culture);
			}
		}

		internal static string MessagingPartitioningUnsupportedBatchingLockTockens
		{
			get
			{
				return SRClient.ResourceManager.GetString("MessagingPartitioningUnsupportedBatchingLockTockens", SRClient.Culture);
			}
		}

		internal static string MismatchedListSizeEncodedValueLength
		{
			get
			{
				return SRClient.ResourceManager.GetString("MismatchedListSizeEncodedValueLength", SRClient.Culture);
			}
		}

		internal static string MoreThanOneAddressCandidate
		{
			get
			{
				return SRClient.ResourceManager.GetString("MoreThanOneAddressCandidate", SRClient.Culture);
			}
		}

		internal static string MoreThanOneIPEndPoint
		{
			get
			{
				return SRClient.ResourceManager.GetString("MoreThanOneIPEndPoint", SRClient.Culture);
			}
		}

		internal static string MpnsCertificateExpired
		{
			get
			{
				return SRClient.ResourceManager.GetString("MpnsCertificateExpired", SRClient.Culture);
			}
		}

		internal static string MpnsCertificatePrivatekeyMissing
		{
			get
			{
				return SRClient.ResourceManager.GetString("MpnsCertificatePrivatekeyMissing", SRClient.Culture);
			}
		}

		internal static string MpnsInvalidPropeties
		{
			get
			{
				return SRClient.ResourceManager.GetString("MpnsInvalidPropeties", SRClient.Culture);
			}
		}

		internal static string MpnsRequiredPropertiesError
		{
			get
			{
				return SRClient.ResourceManager.GetString("MpnsRequiredPropertiesError", SRClient.Culture);
			}
		}

		internal static string MultipleConnectionModeAssertions
		{
			get
			{
				return SRClient.ResourceManager.GetString("MultipleConnectionModeAssertions", SRClient.Culture);
			}
		}

		internal static string MultipleResourceManagersNotSupported
		{
			get
			{
				return SRClient.ResourceManager.GetString("MultipleResourceManagersNotSupported", SRClient.Culture);
			}
		}

		internal static string NokiaXAuthorizationKeyNotSpecified
		{
			get
			{
				return SRClient.ResourceManager.GetString("NokiaXAuthorizationKeyNotSpecified", SRClient.Culture);
			}
		}

		internal static string NokiaXEndpointNotSpecified
		{
			get
			{
				return SRClient.ResourceManager.GetString("NokiaXEndpointNotSpecified", SRClient.Culture);
			}
		}

		internal static string NokiaXRegistrationInvalidId
		{
			get
			{
				return SRClient.ResourceManager.GetString("NokiaXRegistrationInvalidId", SRClient.Culture);
			}
		}

		internal static string NokiaXRequiredProperties
		{
			get
			{
				return SRClient.ResourceManager.GetString("NokiaXRequiredProperties", SRClient.Culture);
			}
		}

		internal static string NotificationHubOperationNotAllowedForSKU
		{
			get
			{
				return SRClient.ResourceManager.GetString("NotificationHubOperationNotAllowedForSKU", SRClient.Culture);
			}
		}

		internal static string NotSupportedTypeofChannel
		{
			get
			{
				return SRClient.ResourceManager.GetString("NotSupportedTypeofChannel", SRClient.Culture);
			}
		}

		internal static string NotSupportedXMLFormatAsBodyTemplate
		{
			get
			{
				return SRClient.ResourceManager.GetString("NotSupportedXMLFormatAsBodyTemplate", SRClient.Culture);
			}
		}

		internal static string NotSupportedXMLFormatAsBodyTemplateForMpns
		{
			get
			{
				return SRClient.ResourceManager.GetString("NotSupportedXMLFormatAsBodyTemplateForMpns", SRClient.Culture);
			}
		}

		internal static string NotSupportedXMLFormatAsPayload
		{
			get
			{
				return SRClient.ResourceManager.GetString("NotSupportedXMLFormatAsPayload", SRClient.Culture);
			}
		}

		internal static string NotSupportedXMLFormatAsPayloadForMpns
		{
			get
			{
				return SRClient.ResourceManager.GetString("NotSupportedXMLFormatAsPayloadForMpns", SRClient.Culture);
			}
		}

		internal static string NoValidHostAddress
		{
			get
			{
				return SRClient.ResourceManager.GetString("NoValidHostAddress", SRClient.Culture);
			}
		}

		internal static string NullAppliesTo
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullAppliesTo", SRClient.Culture);
			}
		}

		internal static string NullAsString
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullAsString", SRClient.Culture);
			}
		}

		internal static string NullHostname
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullHostname", SRClient.Culture);
			}
		}

		internal static string NullIssuerName
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullIssuerName", SRClient.Culture);
			}
		}

		internal static string NullIssuerSecret
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullIssuerSecret", SRClient.Culture);
			}
		}

		internal static string NullRawDataInToken
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullRawDataInToken", SRClient.Culture);
			}
		}

		internal static string NullResourceDescription
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullResourceDescription", SRClient.Culture);
			}
		}

		internal static string NullResourceName
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullResourceName", SRClient.Culture);
			}
		}

		internal static string NullRoot
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullRoot", SRClient.Culture);
			}
		}

		internal static string NullSAMLs
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullSAMLs", SRClient.Culture);
			}
		}

		internal static string NullServiceNameSpace
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullServiceNameSpace", SRClient.Culture);
			}
		}

		internal static string NullSimpleWebToken
		{
			get
			{
				return SRClient.ResourceManager.GetString("NullSimpleWebToken", SRClient.Culture);
			}
		}

		internal static string ObjectIsReadOnly
		{
			get
			{
				return SRClient.ResourceManager.GetString("ObjectIsReadOnly", SRClient.Culture);
			}
		}

		internal static string OnMessageAlreadyCalled
		{
			get
			{
				return SRClient.ResourceManager.GetString("OnMessageAlreadyCalled", SRClient.Culture);
			}
		}

		internal static string PackageSidAndSecretKeyAreRequired
		{
			get
			{
				return SRClient.ResourceManager.GetString("PackageSidAndSecretKeyAreRequired", SRClient.Culture);
			}
		}

		internal static string PackageSidOrSecretKeyInvalid
		{
			get
			{
				return SRClient.ResourceManager.GetString("PackageSidOrSecretKeyInvalid", SRClient.Culture);
			}
		}

		internal static string PairedNamespaceInvalidBacklogQueueCount
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespaceInvalidBacklogQueueCount", SRClient.Culture);
			}
		}

		internal static string PairedNamespaceMessagingFactoryAlreadyPaired
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespaceMessagingFactoryAlreadyPaired", SRClient.Culture);
			}
		}

		internal static string PairedNamespaceMessagingFactoryInOptionsAlreadyPaired
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespaceMessagingFactoryInOptionsAlreadyPaired", SRClient.Culture);
			}
		}

		internal static string PairedNamespaceMessagingFactoyCannotBeChanged
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespaceMessagingFactoyCannotBeChanged", SRClient.Culture);
			}
		}

		internal static string PairedNamespaceOnlyCallOnce
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespaceOnlyCallOnce", SRClient.Culture);
			}
		}

		internal static string PairedNamespacePrimaryAndSecondaryEqual
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespacePrimaryAndSecondaryEqual", SRClient.Culture);
			}
		}

		internal static string PairedNamespacePrimaryEntityUnreachable
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespacePrimaryEntityUnreachable", SRClient.Culture);
			}
		}

		internal static string PairedNamespacePropertyExtractionDlqReason
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespacePropertyExtractionDlqReason", SRClient.Culture);
			}
		}

		internal static string PairedNamespaceValidTimespanRange
		{
			get
			{
				return SRClient.ResourceManager.GetString("PairedNamespaceValidTimespanRange", SRClient.Culture);
			}
		}

		internal static string PartitionedEntityViaSenderNeedsViaPatitionKey
		{
			get
			{
				return SRClient.ResourceManager.GetString("PartitionedEntityViaSenderNeedsViaPatitionKey", SRClient.Culture);
			}
		}

		internal static string PartitionKeyMustBeEqualsToNonNullPublisher
		{
			get
			{
				return SRClient.ResourceManager.GetString("PartitionKeyMustBeEqualsToNonNullPublisher", SRClient.Culture);
			}
		}

		internal static string PartitionKeyMustBeEqualsToNonNullSessionId
		{
			get
			{
				return SRClient.ResourceManager.GetString("PartitionKeyMustBeEqualsToNonNullSessionId", SRClient.Culture);
			}
		}

		internal static string PathSegmentASCIICharacters
		{
			get
			{
				return SRClient.ResourceManager.GetString("PathSegmentASCIICharacters", SRClient.Culture);
			}
		}

		internal static string PeekLockModeRequired
		{
			get
			{
				return SRClient.ResourceManager.GetString("PeekLockModeRequired", SRClient.Culture);
			}
		}

		internal static string PropTokenNotAllowedInCompositeExpr
		{
			get
			{
				return SRClient.ResourceManager.GetString("PropTokenNotAllowedInCompositeExpr", SRClient.Culture);
			}
		}

		internal static string PublisherMustBeEqualsToNonNullSessionId
		{
			get
			{
				return SRClient.ResourceManager.GetString("PublisherMustBeEqualsToNonNullSessionId", SRClient.Culture);
			}
		}

		internal static string ReadNotSupported
		{
			get
			{
				return SRClient.ResourceManager.GetString("ReadNotSupported", SRClient.Culture);
			}
		}

		internal static string ReadOnlyPolicy
		{
			get
			{
				return SRClient.ResourceManager.GetString("ReadOnlyPolicy", SRClient.Culture);
			}
		}

		internal static string ReceiveContextNull
		{
			get
			{
				return SRClient.ResourceManager.GetString("ReceiveContextNull", SRClient.Culture);
			}
		}

		internal static string RelayCertificateNotFound
		{
			get
			{
				return SRClient.ResourceManager.GetString("RelayCertificateNotFound", SRClient.Culture);
			}
		}

		internal static System.Resources.ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(SRClient.resourceManager, null))
				{
					SRClient.resourceManager = new System.Resources.ResourceManager("Microsoft.ServiceBus.SRClient", typeof(SRClient).Assembly);
				}
				return SRClient.resourceManager;
			}
		}

		internal static string RuleCreationActionRequiresFilterTemplate
		{
			get
			{
				return SRClient.ResourceManager.GetString("RuleCreationActionRequiresFilterTemplate", SRClient.Culture);
			}
		}

		internal static string SbmpTransport
		{
			get
			{
				return SRClient.ResourceManager.GetString("SbmpTransport", SRClient.Culture);
			}
		}

		internal static string SeekNotSupported
		{
			get
			{
				return SRClient.ResourceManager.GetString("SeekNotSupported", SRClient.Culture);
			}
		}

		internal static string SendAvailabilityNoTransferQueuesCreated
		{
			get
			{
				return SRClient.ResourceManager.GetString("SendAvailabilityNoTransferQueuesCreated", SRClient.Culture);
			}
		}

		internal static string ServerCertificateAlreadySet
		{
			get
			{
				return SRClient.ResourceManager.GetString("ServerCertificateAlreadySet", SRClient.Culture);
			}
		}

		internal static string ServerCertificateNotSet
		{
			get
			{
				return SRClient.ResourceManager.GetString("ServerCertificateNotSet", SRClient.Culture);
			}
		}

		internal static string ServerDidNotReply
		{
			get
			{
				return SRClient.ResourceManager.GetString("ServerDidNotReply", SRClient.Culture);
			}
		}

		internal static string SessionHandlerAlreadyRegistered
		{
			get
			{
				return SRClient.ResourceManager.GetString("SessionHandlerAlreadyRegistered", SRClient.Culture);
			}
		}

		internal static string SessionLockExpiredOnMessageSession
		{
			get
			{
				return SRClient.ResourceManager.GetString("SessionLockExpiredOnMessageSession", SRClient.Culture);
			}
		}

		internal static string SetTokenScopeNotSupported
		{
			get
			{
				return SRClient.ResourceManager.GetString("SetTokenScopeNotSupported", SRClient.Culture);
			}
		}

		internal static string StreamClosed
		{
			get
			{
				return SRClient.ResourceManager.GetString("StreamClosed", SRClient.Culture);
			}
		}

		internal static string STSURIFormat
		{
			get
			{
				return SRClient.ResourceManager.GetString("STSURIFormat", SRClient.Culture);
			}
		}

		internal static string SystemTrackerHeaderMissing
		{
			get
			{
				return SRClient.ResourceManager.GetString("SystemTrackerHeaderMissing", SRClient.Culture);
			}
		}

		internal static string SystemTrackerPropertyMissing
		{
			get
			{
				return SRClient.ResourceManager.GetString("SystemTrackerPropertyMissing", SRClient.Culture);
			}
		}

		internal static string TargetHostNotSet
		{
			get
			{
				return SRClient.ResourceManager.GetString("TargetHostNotSet", SRClient.Culture);
			}
		}

		internal static string TimeoutExceeded
		{
			get
			{
				return SRClient.ResourceManager.GetString("TimeoutExceeded", SRClient.Culture);
			}
		}

		internal static string TokenAudience
		{
			get
			{
				return SRClient.ResourceManager.GetString("TokenAudience", SRClient.Culture);
			}
		}

		internal static string TokenExpiresOn
		{
			get
			{
				return SRClient.ResourceManager.GetString("TokenExpiresOn", SRClient.Culture);
			}
		}

		internal static string TrackingIDHeaderMissing
		{
			get
			{
				return SRClient.ResourceManager.GetString("TrackingIDHeaderMissing", SRClient.Culture);
			}
		}

		internal static string TrackingIDPropertyMissing
		{
			get
			{
				return SRClient.ResourceManager.GetString("TrackingIDPropertyMissing", SRClient.Culture);
			}
		}

		internal static string TransactionPartitionKeyMissing
		{
			get
			{
				return SRClient.ResourceManager.GetString("TransactionPartitionKeyMissing", SRClient.Culture);
			}
		}

		internal static string TransportSecurity
		{
			get
			{
				return SRClient.ResourceManager.GetString("TransportSecurity", SRClient.Culture);
			}
		}

		internal static string UnexpectedFormat
		{
			get
			{
				return SRClient.ResourceManager.GetString("UnexpectedFormat", SRClient.Culture);
			}
		}

		internal static string UnrecognizedCredentialType
		{
			get
			{
				return SRClient.ResourceManager.GetString("UnrecognizedCredentialType", SRClient.Culture);
			}
		}

		internal static string UnsupportedBatchingDistinctPartitionKey
		{
			get
			{
				return SRClient.ResourceManager.GetString("UnsupportedBatchingDistinctPartitionKey", SRClient.Culture);
			}
		}

		internal static string UnsupportedBatchingSequenceNumbersForDistinctPartitions
		{
			get
			{
				return SRClient.ResourceManager.GetString("UnsupportedBatchingSequenceNumbersForDistinctPartitions", SRClient.Culture);
			}
		}

		internal static string UnsupportedDeDupBatchingDistinctPartitionKey
		{
			get
			{
				return SRClient.ResourceManager.GetString("UnsupportedDeDupBatchingDistinctPartitionKey", SRClient.Culture);
			}
		}

		internal static string UnsupportedEncodingType
		{
			get
			{
				return SRClient.ResourceManager.GetString("UnsupportedEncodingType", SRClient.Culture);
			}
		}

		internal static string UpstreamConnection
		{
			get
			{
				return SRClient.ResourceManager.GetString("UpstreamConnection", SRClient.Culture);
			}
		}

		internal static string URIEndpoint
		{
			get
			{
				return SRClient.ResourceManager.GetString("URIEndpoint", SRClient.Culture);
			}
		}

		internal static string UseOverloadWithBaseAddress
		{
			get
			{
				return SRClient.ResourceManager.GetString("UseOverloadWithBaseAddress", SRClient.Culture);
			}
		}

		internal static string ValueMustBeNonNegative
		{
			get
			{
				return SRClient.ResourceManager.GetString("ValueMustBeNonNegative", SRClient.Culture);
			}
		}

		internal static string ValueVisibility
		{
			get
			{
				return SRClient.ResourceManager.GetString("ValueVisibility", SRClient.Culture);
			}
		}

		internal static string WebStreamShutdown
		{
			get
			{
				return SRClient.ResourceManager.GetString("WebStreamShutdown", SRClient.Culture);
			}
		}

		private SRClient()
		{
		}

		internal static string ApnsCertificateNotUsable(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ApnsCertificateNotUsable", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AppSettingsConfigDuplicateSetting(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("AppSettingsConfigDuplicateSetting", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AppSettingsConfigIncompleteSettingCombination(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("AppSettingsConfigIncompleteSettingCombination", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AppSettingsConfigMissingSetting(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("AppSettingsConfigMissingSetting", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AppSettingsConfigSettingInvalidKey(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("AppSettingsConfigSettingInvalidKey", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AppSettingsConfigSettingInvalidValue(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("AppSettingsConfigSettingInvalidValue", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AppSettingsCreateFactoryWithInvalidConnectionString(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("AppSettingsCreateFactoryWithInvalidConnectionString", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AppSettingsCreateManagerWithInvalidConnectionString(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("AppSettingsCreateManagerWithInvalidConnectionString", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ArgumentInvalidCombination(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ArgumentInvalidCombination", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ArgumentOutOfRange(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ArgumentOutOfRange", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string BacklogDeadletterDescriptionNotRetryable(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("BacklogDeadletterDescriptionNotRetryable", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string BadUriFormat(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("BadUriFormat", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string BaseAddressScheme(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("BaseAddressScheme", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string BrokeredMessageApplicationProperties(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("BrokeredMessageApplicationProperties", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string BrokeredMessageStreamNotCloneable(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("BrokeredMessageStreamNotCloneable", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string BufferedOutputStreamQuotaExceeded(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("BufferedOutputStreamQuotaExceeded", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string CannotConvertFilterAction(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("CannotConvertFilterAction", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string CannotConvertFilterExpression(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("CannotConvertFilterExpression", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string CannotFindTransactionResult(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("CannotFindTransactionResult", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string CannotSendAnEmptyEvent(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("CannotSendAnEmptyEvent", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string CannotUseSameMessageInstanceInMultipleOperations(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("CannotUseSameMessageInstanceInMultipleOperations", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ChannelTypeNotSupported(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ChannelTypeNotSupported", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string CommunicationObjectFaulted(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("CommunicationObjectFaulted", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ConfigInvalidBindingConfigurationName(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ConfigInvalidBindingConfigurationName", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string DelimitedIdentifierNotTerminated(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("DelimitedIdentifierNotTerminated", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string DominatingPropertyMustBeEqualsToNonNullDormantProperty(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("DominatingPropertyMustBeEqualsToNonNullDormantProperty", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string DuplicateHistoryExpiryTimeExceedsMaximumAllowed(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("DuplicateHistoryExpiryTimeExceedsMaximumAllowed", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string EntityNameLengthExceedsLimit(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("EntityNameLengthExceedsLimit", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string EntityNameNotFound(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("EntityNameNotFound", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string EventHubSendBatchMismatchPartitionKey(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("EventHubSendBatchMismatchPartitionKey", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string EventHubSendBatchMismatchPublisher(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("EventHubSendBatchMismatchPublisher", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string EventHubUnsupportedOperation(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("EventHubUnsupportedOperation", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string EventHubUnsupportedTransport(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("EventHubUnsupportedTransport", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ExceededMessagePropertySizeLimit(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ExceededMessagePropertySizeLimit", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string ExpectedTypeInvalidCastException(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ExpectedTypeInvalidCastException", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string ExtraParameterSpecifiedForSqlExpression(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ExtraParameterSpecifiedForSqlExpression", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string FailedToDeserializeUnsupportedProperty(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FailedToDeserializeUnsupportedProperty", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string FailedToSerializeUnsupportedType(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FailedToSerializeUnsupportedType", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string FaultingPairedMessagingFactory(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FaultingPairedMessagingFactory", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string FeatureNotSupported(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FeatureNotSupported", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string FilterActionTooManyStatements(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FilterActionTooManyStatements", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string FilterFunctionIncorrectNumberOfArguments(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FilterFunctionIncorrectNumberOfArguments", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string FilterScopeNotSupported(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FilterScopeNotSupported", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string FilterUnknownFunctionName(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("FilterUnknownFunctionName", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string HttpServerAlreadyRunning(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("HttpServerAlreadyRunning", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string IncompatibleQueueExport(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("IncompatibleQueueExport", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string IncompatibleTopicExport(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("IncompatibleTopicExport", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string IncorrectContentTypeFault(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("IncorrectContentTypeFault", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InitialOffsetProviderReturnTypeNotSupported(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InitialOffsetProviderReturnTypeNotSupported", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InputURIPath(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InputURIPath", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidAddressPath(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidAddressPath", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidCharacterInEntityName(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidCharacterInEntityName", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidCharactersInEntityName(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidCharactersInEntityName", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidDNSClaims(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidDNSClaims", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidElement(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidElement", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidEntityNameFormatWithSlash(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidEntityNameFormatWithSlash", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidEventHubCheckpointSettings(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidEventHubCheckpointSettings", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidFrameSize(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidFrameSize", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidManagementEntityType(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidManagementEntityType", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidMethodWhilePeeking(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidMethodWhilePeeking", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidSchemeValue(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidSchemeValue", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidServiceNameSpace(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidServiceNameSpace", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidSubQueueNameString(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidSubQueueNameString", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidToken(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidToken", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string InvalidUriScheme(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("InvalidUriScheme", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string LitteralMissing(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("LitteralMissing", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string LockTimeExceedsMaximumAllowed(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("LockTimeExceedsMaximumAllowed", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MaxConcurrentCallsMustBeGreaterThanZero(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MaxConcurrentCallsMustBeGreaterThanZero", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MaxRedirectsExceeded(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MaxRedirectsExceeded", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessageAttributeGetMethodNotAccessible(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessageAttributeGetMethodNotAccessible", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessageAttributeSetMethodNotAccessible(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessageAttributeSetMethodNotAccessible", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessageGetPropertyNotFound(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessageGetPropertyNotFound", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessageHeaderRetrieval(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessageHeaderRetrieval", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessageIdIsNullOrEmptyOrOverMaxValue(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessageIdIsNullOrEmptyOrOverMaxValue", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessageSetPropertyNotFound(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessageSetPropertyNotFound", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEndpointCommunicationError(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEndpointCommunicationError", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityAlreadyExists(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityAlreadyExists", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityCouldNotBeFound(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityCouldNotBeFound", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityGone(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityGone", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityIsDisabledException(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityIsDisabledException", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityIsDisabledForReceiveException(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityIsDisabledForReceiveException", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityIsDisabledForSendException(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityIsDisabledForSendException", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityMoved(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityMoved", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityRequestConflict(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityRequestConflict", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MessagingEntityUpdateConflict(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MessagingEntityUpdateConflict", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MismatchServiceBusDomain(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MismatchServiceBusDomain", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string MissingMpnsHeader(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MissingMpnsHeader", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MissingWNSHeader(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MissingWNSHeader", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MpnsCertificateError(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MpnsCertificateError", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string MpnsHeaderIsNullOrEmpty(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("MpnsHeaderIsNullOrEmpty", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string NoAddressesFound(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("NoAddressesFound", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string NoCorrelationForChannelMessageId(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("NoCorrelationForChannelMessageId", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string NoCorrelationResponseForChannelMessageId(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("NoCorrelationResponseForChannelMessageId", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string NotSupportedCompatibilityLevel(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("NotSupportedCompatibilityLevel", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string NotSupportedPropertyType(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("NotSupportedPropertyType", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string NotSupportFrameCode(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("NotSupportFrameCode", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string NullEmptyRights(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("NullEmptyRights", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string OnlyNPropertiesRequired(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("OnlyNPropertiesRequired", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string OpenChannelFailed(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("OpenChannelFailed", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string OperationRequestTimedOut(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("OperationRequestTimedOut", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string OverflowWhenAddingException(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("OverflowWhenAddingException", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string PairedNamespacePropertyExtractionDlqDescription(object param0, object param1, object param2, object param3, object param4)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PairedNamespacePropertyExtractionDlqDescription", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3, param4 };
			return string.Format(culture, str, objArray);
		}

		internal static string ParameterNotSpecifiedForSqlExpression(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ParameterNotSpecifiedForSqlExpression", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string PartitionInvalidPartitionKey(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PartitionInvalidPartitionKey", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string PropertyInvalidCombination(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PropertyInvalidCombination", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string PropertyIsNullOrEmpty(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PropertyIsNullOrEmpty", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string PropertyLengthError(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PropertyLengthError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string PropertyMustBeEqualOrLessThanOtherProperty(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PropertyMustBeEqualOrLessThanOtherProperty", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string PropertyNameError(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PropertyNameError", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string PropertyOverMaxValue(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PropertyOverMaxValue", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string PropertyReferenceUsedWithoutInitializes(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("PropertyReferenceUsedWithoutInitializes", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string QueueProvisioningError(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("QueueProvisioningError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string QueueUnProvisioningError(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("QueueUnProvisioningError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string ReceivedCorrelationMessage(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ReceivedCorrelationMessage", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string RequiredPropertiesNotSpecified(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("RequiredPropertiesNotSpecified", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string RequiredPropertyNotSpecified(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("RequiredPropertyNotSpecified", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ResponseHeaderRetrieval(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ResponseHeaderRetrieval", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string RetryPolicyInvalidBackoffPeriod(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("RetryPolicyInvalidBackoffPeriod", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string SentCorrelationMessage(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SentCorrelationMessage", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string SessionHandlerDoesNotHaveDefaultConstructor(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SessionHandlerDoesNotHaveDefaultConstructor", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SessionHandlerMissingInterfaces(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SessionHandlerMissingInterfaces", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string SessionIdIsOverMaxValue(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SessionIdIsOverMaxValue", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SqlFilterActionCannotRemoveSystemProperty(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SqlFilterActionCannotRemoveSystemProperty", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SqlFilterActionStatmentTooLong(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SqlFilterActionStatmentTooLong", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string SqlFilterReservedKeyword(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SqlFilterReservedKeyword", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SqlFilterStatmentTooLong(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SqlFilterStatmentTooLong", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string SqlSettingNotFound(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SqlSettingNotFound", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SQLSyntaxError(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SQLSyntaxError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string SQLSyntaxErrorDetailed(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SQLSyntaxErrorDetailed", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string StringIsTooLong(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("StringIsTooLong", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string StringLiteralNotTerminated(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("StringLiteralNotTerminated", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string SubscriptionProvisioningError(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("SubscriptionProvisioningError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string TemplateNameLengthExceedsLimit(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("TemplateNameLengthExceedsLimit", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string TokenBeginError(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("TokenBeginError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string TooManyMessageProperties(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("TooManyMessageProperties", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string TopicProvisioningError(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("TopicProvisioningError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string TopicUnProvisioningError(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("TopicUnProvisioningError", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string TrackableExceptionMessageFormat(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("TrackableExceptionMessageFormat", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string TrackingIdAndTimestampFormat(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("TrackingIdAndTimestampFormat", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnableToReach(object param0, object param1, object param2)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnableToReach", SRClient.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnexpectedSSL(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnexpectedSSL", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnexpedtedURIHostName(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnexpedtedURIHostName", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnknownApiVersion(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnknownApiVersion", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedAction(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedAction", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedChannelType(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedChannelType", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedChannelUri(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedChannelUri", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedConnectivityMode(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedConnectivityMode", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedExpression(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedExpression", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedGetClaim(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedGetClaim", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedRight(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedRight", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string UnsupportedServiceBusDomainPrefix(object param0, object param1)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("UnsupportedServiceBusDomainPrefix", SRClient.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string URIServiceNameSpace(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("URIServiceNameSpace", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string ValueMustBePositive(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("ValueMustBePositive", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string WNSHeaderNullOrEmpty(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("WNSHeaderNullOrEmpty", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string X509CRLCheckFailed(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("X509CRLCheckFailed", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string X509InUnTrustedStore(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("X509InUnTrustedStore", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string X509InvalidUsageTime(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("X509InvalidUsageTime", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string XMLContentReadFault(object param0)
		{
			CultureInfo culture = SRClient.Culture;
			string str = SRClient.ResourceManager.GetString("XMLContentReadFault", SRClient.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}
	}
}