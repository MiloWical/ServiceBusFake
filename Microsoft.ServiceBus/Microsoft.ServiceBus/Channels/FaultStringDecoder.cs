using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal class FaultStringDecoder : StringDecoder
	{
		internal const int FaultSizeQuota = 256;

		public FaultStringDecoder() : base(256)
		{
		}

		public static Exception GetFaultException(string faultString, string via, string contentType)
		{
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/EndpointNotFound")
			{
				string endpointNotFound = Resources.EndpointNotFound;
				object[] objArray = new object[] { via };
				return new EndpointNotFoundException(Microsoft.ServiceBus.SR.GetString(endpointNotFound, objArray));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/ContentTypeInvalid")
			{
				string framingContentTypeMismatch = Resources.FramingContentTypeMismatch;
				object[] objArray1 = new object[] { contentType, via };
				return new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingContentTypeMismatch, objArray1));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/ServiceActivationFailed")
			{
				string hostingServiceActivationFailed = Resources.Hosting_ServiceActivationFailed;
				object[] objArray2 = new object[] { via };
				return new ServiceActivationException(Microsoft.ServiceBus.SR.GetString(hostingServiceActivationFailed, objArray2));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/ConnectionDispatchFailed")
			{
				string sharingConnectionDispatchFailed = Resources.Sharing_ConnectionDispatchFailed;
				object[] objArray3 = new object[] { via };
				return new CommunicationException(Microsoft.ServiceBus.SR.GetString(sharingConnectionDispatchFailed, objArray3));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/EndpointUnavailable")
			{
				string sharingEndpointUnavailable = Resources.Sharing_EndpointUnavailable;
				object[] objArray4 = new object[] { via };
				return new EndpointNotFoundException(Microsoft.ServiceBus.SR.GetString(sharingEndpointUnavailable, objArray4));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/MaxMessageSizeExceededFault")
			{
				Exception quotaExceededException = new QuotaExceededException(Microsoft.ServiceBus.SR.GetString(Resources.FramingMaxMessageSizeExceeded, new object[0]));
				return new CommunicationException(quotaExceededException.Message, quotaExceededException);
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/UnsupportedMode")
			{
				string framingModeNotSupportedFault = Resources.FramingModeNotSupportedFault;
				object[] objArray5 = new object[] { via };
				return new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingModeNotSupportedFault, objArray5));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/UnsupportedVersion")
			{
				string framingVersionNotSupportedFault = Resources.FramingVersionNotSupportedFault;
				object[] objArray6 = new object[] { via };
				return new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingVersionNotSupportedFault, objArray6));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/ContentTypeTooLong")
			{
				string framingContentTypeTooLongFault = Resources.FramingContentTypeTooLongFault;
				object[] objArray7 = new object[] { contentType };
				Exception exception = new QuotaExceededException(Microsoft.ServiceBus.SR.GetString(framingContentTypeTooLongFault, objArray7));
				return new CommunicationException(exception.Message, exception);
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/ViaTooLong")
			{
				string framingViaTooLongFault = Resources.FramingViaTooLongFault;
				object[] objArray8 = new object[] { via };
				Exception quotaExceededException1 = new QuotaExceededException(Microsoft.ServiceBus.SR.GetString(framingViaTooLongFault, objArray8));
				return new CommunicationException(quotaExceededException1.Message, quotaExceededException1);
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/ServerTooBusy")
			{
				string serverTooBusy = Resources.ServerTooBusy;
				object[] objArray9 = new object[] { via };
				return new ServerTooBusyException(Microsoft.ServiceBus.SR.GetString(serverTooBusy, objArray9));
			}
			if (faultString == "http://schemas.microsoft.com/ws/2006/05/framing/faults/UpgradeInvalid")
			{
				string framingUpgradeInvalid = Resources.FramingUpgradeInvalid;
				object[] objArray10 = new object[] { via };
				return new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingUpgradeInvalid, objArray10));
			}
			string framingFaultUnrecognized = Resources.FramingFaultUnrecognized;
			object[] objArray11 = new object[] { faultString };
			return new ProtocolException(Microsoft.ServiceBus.SR.GetString(framingFaultUnrecognized, objArray11));
		}

		protected override Exception OnSizeQuotaExceeded(int size)
		{
			string framingFaultTooLong = Resources.FramingFaultTooLong;
			object[] objArray = new object[] { size };
			return new InvalidDataException(Microsoft.ServiceBus.SR.GetString(framingFaultTooLong, objArray));
		}
	}
}