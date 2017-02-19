using Microsoft.ServiceBus.Diagnostics;
using System;
using System.ComponentModel;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal static class TransferModeHelper
	{
		public static bool IsDefined(TransferMode v)
		{
			if (v == TransferMode.Buffered || v == TransferMode.Streamed || v == TransferMode.StreamedRequest)
			{
				return true;
			}
			return v == TransferMode.StreamedResponse;
		}

		public static bool IsRequestStreamed(TransferMode v)
		{
			if (v == TransferMode.StreamedRequest)
			{
				return true;
			}
			return v == TransferMode.Streamed;
		}

		public static bool IsResponseStreamed(TransferMode v)
		{
			if (v == TransferMode.StreamedResponse)
			{
				return true;
			}
			return v == TransferMode.Streamed;
		}

		public static void Validate(TransferMode value)
		{
			if (!Microsoft.ServiceBus.Channels.TransferModeHelper.IsDefined(value))
			{
				throw Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidEnumArgumentException("value", (int)value, typeof(TransferMode)));
			}
		}
	}
}