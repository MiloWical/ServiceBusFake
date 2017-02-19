using System;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal static class BasicHttpRelayBindingDefaults
	{
		internal const WSMessageEncoding MessageEncoding = WSMessageEncoding.Text;

		internal const System.ServiceModel.TransferMode TransferMode = System.ServiceModel.TransferMode.Buffered;
	}
}