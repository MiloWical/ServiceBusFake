using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.IO;

namespace Microsoft.ServiceBus.Channels
{
	internal class ViaStringDecoder : StringDecoder
	{
		private Uri via;

		public Uri ValueAsUri
		{
			get
			{
				if (!base.IsValueDecoded)
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(Resources.FramingValueNotAvailable, new object[0])));
				}
				return this.via;
			}
		}

		public ViaStringDecoder(int sizeQuota) : base(sizeQuota)
		{
		}

		protected override void OnComplete(string value)
		{
			try
			{
				this.via = new Uri(value);
				base.OnComplete(value);
			}
			catch (UriFormatException uriFormatException1)
			{
				UriFormatException uriFormatException = uriFormatException1;
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string framingViaNotUri = Resources.FramingViaNotUri;
				object[] objArray = new object[] { value };
				throw exceptionUtility.ThrowHelperError(new InvalidDataException(Microsoft.ServiceBus.SR.GetString(framingViaNotUri, objArray), uriFormatException));
			}
		}

		protected override Exception OnSizeQuotaExceeded(int size)
		{
			string framingViaTooLong = Resources.FramingViaTooLong;
			object[] objArray = new object[] { size };
			Exception invalidDataException = new InvalidDataException(Microsoft.ServiceBus.SR.GetString(framingViaTooLong, objArray));
			FramingEncodingString.AddFaultString(invalidDataException, "http://schemas.microsoft.com/ws/2006/05/framing/faults/ViaTooLong");
			return invalidDataException;
		}
	}
}