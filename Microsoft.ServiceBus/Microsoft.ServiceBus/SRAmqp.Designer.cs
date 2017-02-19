using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Resources;

namespace Microsoft.ServiceBus
{
	internal class SRAmqp
	{
		private static System.Resources.ResourceManager resourceManager;

		private static CultureInfo resourceCulture;

		internal static string AmqpBufferAlreadyReclaimed
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpBufferAlreadyReclaimed", SRAmqp.Culture);
			}
		}

		internal static string AmqpCannotCloneSentMessage
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpCannotCloneSentMessage", SRAmqp.Culture);
			}
		}

		internal static string AmqpConnectionInactive
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpConnectionInactive", SRAmqp.Culture);
			}
		}

		internal static string AmqpDynamicTerminusNotSupported
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpDynamicTerminusNotSupported", SRAmqp.Culture);
			}
		}

		internal static string AmqpFieldSessionId
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpFieldSessionId", SRAmqp.Culture);
			}
		}

		internal static string AmqpFramingError
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpFramingError", SRAmqp.Culture);
			}
		}

		internal static string AmqpInvalidCommand
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpInvalidCommand", SRAmqp.Culture);
			}
		}

		internal static string AmqpInvalidMessageBodyType
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpInvalidMessageBodyType", SRAmqp.Culture);
			}
		}

		internal static string AmqpInvalidRemoteIp
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpInvalidRemoteIp", SRAmqp.Culture);
			}
		}

		internal static string AmqpNotSupportMechanism
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpNotSupportMechanism", SRAmqp.Culture);
			}
		}

		internal static string AmqpUnopenObject
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpUnopenObject", SRAmqp.Culture);
			}
		}

		internal static string AmqpUnssuportedTokenType
		{
			get
			{
				return SRAmqp.ResourceManager.GetString("AmqpUnssuportedTokenType", SRAmqp.Culture);
			}
		}

		[GeneratedCode("StrictResXFileCodeGenerator", "4.0.0.0")]
		internal static CultureInfo Culture
		{
			get
			{
				return SRAmqp.resourceCulture;
			}
			set
			{
				SRAmqp.resourceCulture = value;
			}
		}

		internal static System.Resources.ResourceManager ResourceManager
		{
			get
			{
				if (object.ReferenceEquals(SRAmqp.resourceManager, null))
				{
					SRAmqp.resourceManager = new System.Resources.ResourceManager("Microsoft.ServiceBus.SRAmqp", typeof(SRAmqp).Assembly);
				}
				return SRAmqp.resourceManager;
			}
		}

		private SRAmqp()
		{
		}

		internal static string AmqpApplicationProperties(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpApplicationProperties", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpCbsLinkAlreadyOpen(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpCbsLinkAlreadyOpen", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpChannelNotFound(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpChannelNotFound", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpDeliveryIDInUse(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpDeliveryIDInUse", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpDuplicateMemberOrder(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpDuplicateMemberOrder", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpEncodingTypeMismatch(object param0, object param1, object param2, object param3)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpEncodingTypeMismatch", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1, param2, param3 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpErrorOccurred(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpErrorOccurred", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpGlobalOpaqueAddressesNotSupported(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpGlobalOpaqueAddressesNotSupported", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpHandleExceeded(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpHandleExceeded", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpHandleInUse(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpHandleInUse", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpHandleNotFound(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpHandleNotFound", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpIdleTimeoutNotSupported(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpIdleTimeoutNotSupported", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpIllegalOperationState(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpIllegalOperationState", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInsufficientBufferSize(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInsufficientBufferSize", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidFormatCode(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidFormatCode", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidLinkAttachAddress(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidLinkAttachAddress", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidLinkAttachScheme(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidLinkAttachScheme", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidMessageSectionCode(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidMessageSectionCode", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidPerformativeCode(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidPerformativeCode", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidPropertyType(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidPropertyType", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidPropertyValue(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidPropertyValue", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidReOpenOperation(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidReOpenOperation", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidSequenceNumberComparison(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidSequenceNumberComparison", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpInvalidType(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpInvalidType", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpManagementLinkAlreadyOpen(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpManagementLinkAlreadyOpen", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpManagementOperationFailed(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpManagementOperationFailed", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpMessageSizeExceeded(object param0, object param1, object param2)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpMessageSizeExceeded", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1, param2 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpMissingOrInvalidProperty(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpMissingOrInvalidProperty", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpMissingProperty(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpMissingProperty", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpNoValidAddressForHost(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpNoValidAddressForHost", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpObjectAborted(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpObjectAborted", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpOperationNotSupported(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpOperationNotSupported", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpProtocolVersionNotSet(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpProtocolVersionNotSet", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpProtocolVersionNotSupported(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpProtocolVersionNotSupported", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpPutTokenAudienceMismatch(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpPutTokenAudienceMismatch", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpPutTokenFailed(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpPutTokenFailed", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpRequiredFieldNotSet(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpRequiredFieldNotSet", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpTimeout(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpTimeout", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpTransferLimitExceeded(object param0)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpTransferLimitExceeded", SRAmqp.Culture);
			object[] objArray = new object[] { param0 };
			return string.Format(culture, str, objArray);
		}

		internal static string AmqpUnknownDescriptor(object param0, object param1)
		{
			CultureInfo culture = SRAmqp.Culture;
			string str = SRAmqp.ResourceManager.GetString("AmqpUnknownDescriptor", SRAmqp.Culture);
			object[] objArray = new object[] { param0, param1 };
			return string.Format(culture, str, objArray);
		}
	}
}