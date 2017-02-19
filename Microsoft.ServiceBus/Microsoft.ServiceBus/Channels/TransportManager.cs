using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging.Channels;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceModel.Channels;

namespace Microsoft.ServiceBus.Channels
{
	internal abstract class TransportManager
	{
		private ServiceModelActivity activity;

		private int openCount;

		private object thisLock = new object();

		protected ServiceModelActivity Activity
		{
			get
			{
				return this.activity;
			}
		}

		internal abstract string Scheme
		{
			get;
		}

		internal object ThisLock
		{
			get
			{
				return this.thisLock;
			}
		}

		protected TransportManager()
		{
		}

		internal void Close(TimeSpan timeout, Microsoft.ServiceBus.Channels.TransportChannelListener channelListener)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			using (Microsoft.ServiceBus.Diagnostics.Activity activity = ServiceModelActivity.BoundOperation(this.Activity))
			{
				this.Unregister(timeoutHelper.RemainingTime(), channelListener);
			}
			lock (this.ThisLock)
			{
				if (this.openCount <= 0)
				{
					throw Fx.AssertAndThrow("Invalid Open/Close state machine.");
				}
				Microsoft.ServiceBus.Channels.TransportManager transportManager = this;
				transportManager.openCount = transportManager.openCount - 1;
				if (this.openCount == 0)
				{
					using (Microsoft.ServiceBus.Diagnostics.Activity activity1 = ServiceModelActivity.BoundOperation(this.Activity, true))
					{
						this.OnClose(timeoutHelper.RemainingTime());
					}
					if (this.Activity != null)
					{
						this.Activity.Dispose();
					}
				}
			}
		}

		internal static void EnsureRegistered<TChannelListener>(Microsoft.ServiceBus.Channels.UriPrefixTable<TChannelListener> addressTable, TChannelListener channelListener)
		where TChannelListener : Microsoft.ServiceBus.Channels.TransportChannelListener
		{
			TChannelListener tChannelListener;
			if (!addressTable.TryLookupUri(channelListener.Uri, channelListener.HostNameComparisonModeInternal, out tChannelListener) || (object)tChannelListener != (object)channelListener)
			{
				ExceptionUtility exceptionUtility = DiagnosticUtility.ExceptionUtility;
				string listenerFactoryNotRegistered = Resources.ListenerFactoryNotRegistered;
				object[] uri = new object[] { channelListener.Uri };
				throw exceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(listenerFactoryNotRegistered, uri)));
			}
		}

		protected void Fault<TChannelListener>(Microsoft.ServiceBus.Channels.UriPrefixTable<TChannelListener> addressTable, Exception exception)
		where TChannelListener : ChannelListenerBase, ICommunicationObjectInternals
		{
			foreach (KeyValuePair<Microsoft.ServiceBus.Channels.BaseUriWithWildcard, TChannelListener> all in addressTable.GetAll())
			{
				TChannelListener value = all.Value;
				value.Fault(exception);
				value.Abort();
			}
		}

		internal abstract void OnClose(TimeSpan timeout);

		internal abstract void OnOpen(TimeSpan timeout);

		internal void Open(TimeSpan timeout, Microsoft.ServiceBus.Channels.TransportChannelListener channelListener)
		{
			TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
			if (DiagnosticUtility.ShouldUseActivity)
			{
				if (this.activity == null)
				{
					this.activity = ServiceModelActivity.CreateActivity(true);
				}
				channelListener.ServiceModelActivity = this.Activity;
			}
			using (Microsoft.ServiceBus.Diagnostics.Activity activity = ServiceModelActivity.BoundOperation(this.Activity))
			{
				if (DiagnosticUtility.ShouldTraceInformation)
				{
					DiagnosticTrace diagnosticTrace = DiagnosticUtility.DiagnosticTrace;
					string traceCodeTransportListen = Resources.TraceCodeTransportListen;
					object[] absoluteUri = new object[] { channelListener.Uri.AbsoluteUri };
					diagnosticTrace.TraceEvent(TraceEventType.Information, TraceCode.TransportListen, Microsoft.ServiceBus.SR.GetString(traceCodeTransportListen, absoluteUri), null, null, this);
				}
				this.Register(timeoutHelper.RemainingTime(), channelListener);
				try
				{
					lock (this.ThisLock)
					{
						if (this.openCount == 0)
						{
							this.OnOpen(timeoutHelper.RemainingTime());
						}
						Microsoft.ServiceBus.Channels.TransportManager transportManager = this;
						transportManager.openCount = transportManager.openCount + 1;
					}
				}
				catch
				{
					this.Unregister(timeoutHelper.RemainingTime(), channelListener);
					throw;
				}
			}
		}

		internal abstract void Register(TimeSpan timeout, Microsoft.ServiceBus.Channels.TransportChannelListener channelListener);

		internal abstract void Unregister(TimeSpan timeout, Microsoft.ServiceBus.Channels.TransportChannelListener channelListener);
	}
}