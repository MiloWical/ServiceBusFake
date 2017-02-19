using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;

namespace Microsoft.ServiceBus.Channels
{
	internal static class DecoderHelper
	{
		public static void ValidateSize(int size)
		{
			if (size <= 0)
			{
				throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("size", (object)size, Microsoft.ServiceBus.SR.GetString(Resources.ValueMustBePositive, new object[0])));
			}
		}
	}
}