using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal static class WSMessageEncodingHelper
	{
		internal static bool IsDefined(WSMessageEncoding value)
		{
			if (value == WSMessageEncoding.Text)
			{
				return true;
			}
			return value == WSMessageEncoding.Mtom;
		}

		internal static void SyncUpEncodingBindingElementProperties(TextMessageEncodingBindingElement textEncoding, MtomMessageEncodingBindingElement mtomEncoding)
		{
			textEncoding.ReaderQuotas.CopyTo(mtomEncoding.ReaderQuotas);
			mtomEncoding.WriteEncoding = textEncoding.WriteEncoding;
		}
	}
}