using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus
{
	internal static class RelayedOnewayManager
	{
		private readonly static Microsoft.ServiceBus.Channels.UriPrefixTable<IRelayedOnewayListener> listenerTable;

		static RelayedOnewayManager()
		{
			RelayedOnewayManager.listenerTable = new Microsoft.ServiceBus.Channels.UriPrefixTable<IRelayedOnewayListener>();
		}

		private static Uri AppendSlash(Uri uri)
		{
			if (uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
			{
				return uri;
			}
			return new Uri(string.Concat(uri.AbsoluteUri, "/"));
		}

		public static IRelayedOnewaySender CreateConnection(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, Uri uri)
		{
			ConnectivitySettings connectivitySetting = context.BindingParameters.Find<ConnectivitySettings>();
			NameSettings nameSetting = context.BindingParameters.Find<NameSettings>();
			ConnectivityMode connectivityMode = (connectivitySetting != null ? connectivitySetting.Mode : ConnectivityModeHelper.SystemConnectivityMode);
			if (connectivityMode == ConnectivityMode.AutoDetect)
			{
				connectivityMode = NetworkDetector.DetectConnectivityModeForAutoDetect(uri);
			}
			if (connectivityMode != ConnectivityMode.Tcp)
			{
				return new RelayedOnewayHttpSender(context, uri, nameSetting.ServiceSettings.TransportProtection == RelayTransportProtectionMode.EndToEnd);
			}
			return new RelayedOnewayTcpSender(context, transportBindingElement, uri, nameSetting.ServiceSettings.TransportProtection == RelayTransportProtectionMode.EndToEnd, new EventTraceActivity());
		}

		private static void OnListenerClosing(object sender, EventArgs args)
		{
			IRelayedOnewayListener relayedOnewayListener;
			IRelayedOnewayListener relayedOnewayListener1 = (IRelayedOnewayListener)sender;
			lock (RelayedOnewayManager.listenerTable)
			{
				if (RelayedOnewayManager.listenerTable.TryLookupUri(relayedOnewayListener1.Uri, HostNameComparisonMode.Exact, out relayedOnewayListener) && relayedOnewayListener == relayedOnewayListener1)
				{
					RelayedOnewayManager.listenerTable.UnregisterUri(relayedOnewayListener1.Uri, HostNameComparisonMode.Exact);
				}
			}
		}

		public static IRelayedOnewayListener RegisterListener(BindingContext context, RelayedOnewayTransportBindingElement transportBindingElement, RelayedOnewayChannelListener channelListener)
		{
			IRelayedOnewayListener relayedOnewayListener;
			lock (RelayedOnewayManager.listenerTable)
			{
				if (!RelayedOnewayManager.listenerTable.TryLookupUri(channelListener.BaseAddress, HostNameComparisonMode.Exact, out relayedOnewayListener) || !(RelayedOnewayManager.AppendSlash(relayedOnewayListener.Uri).ToString() == RelayedOnewayManager.AppendSlash(channelListener.BaseAddress).ToString()))
				{
					relayedOnewayListener = new RelayedOnewayListener(context, transportBindingElement, channelListener.BaseAddress, new EventTraceActivity());
					relayedOnewayListener.Closing += new EventHandler(RelayedOnewayManager.OnListenerClosing);
					RelayedOnewayManager.listenerTable.RegisterUri(channelListener.BaseAddress, HostNameComparisonMode.Exact, relayedOnewayListener);
				}
				else if (!relayedOnewayListener.NameSettings.IsCompatible(channelListener.NameSettings))
				{
					throw Fx.Exception.AsError(new AddressAlreadyInUseException(SRClient.IncompatibleChannelListener), null);
				}
				relayedOnewayListener.Register(channelListener);
			}
			return relayedOnewayListener;
		}
	}
}