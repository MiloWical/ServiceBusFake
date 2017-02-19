using Microsoft.ServiceBus.Channels.Security;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.AmqpClient;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.Amqp
{
	internal sealed class ActiveClientLinkManager
	{
		private readonly static TimeSpan SendTokenTimeout;

		private readonly static TimeSpan TokenRefreshBuffer;

		private readonly static Action<object> onLinkExpiration;

		private readonly IOThreadTimer validityTimer;

		private readonly AmqpMessagingFactory factory;

		private readonly object syncRoot;

		private ActiveClientLink activeClientLink;

		static ActiveClientLinkManager()
		{
			ActiveClientLinkManager.SendTokenTimeout = TimeSpan.FromMinutes(1);
			ActiveClientLinkManager.TokenRefreshBuffer = TimeSpan.FromSeconds(10);
			ActiveClientLinkManager.onLinkExpiration = new Action<object>(ActiveClientLinkManager.OnLinkExpiration);
		}

		public ActiveClientLinkManager(AmqpMessagingFactory factory)
		{
			this.factory = factory;
			this.validityTimer = new IOThreadTimer(ActiveClientLinkManager.onLinkExpiration, this, false);
			this.syncRoot = new object();
		}

		private void CancelValidityTimer()
		{
			this.validityTimer.Cancel();
		}

		public void Close()
		{
			this.CancelValidityTimer();
		}

		private void OnLinkClosed(object sender, EventArgs e)
		{
			this.Close();
		}

		private static void OnLinkExpiration(object state)
		{
			ActiveClientLinkManager activeClientLinkManager = (ActiveClientLinkManager)state;
			try
			{
				MessagingClientEtwProvider.TraceClient(() => {
				});
				AmqpCbsLink amqpCbsLink = activeClientLinkManager.activeClientLink.Link.Session.Connection.Extensions.Find<AmqpCbsLink>() ?? new AmqpCbsLink(activeClientLinkManager.activeClientLink.Link.Session.Connection);
				amqpCbsLink.BeginSendToken(activeClientLinkManager.factory.ServiceBusSecuritySettings.TokenProvider, activeClientLinkManager.factory.Address, activeClientLinkManager.activeClientLink.Audience, activeClientLinkManager.activeClientLink.EndpointUri, activeClientLinkManager.activeClientLink.RequiredClaims, ActiveClientLinkManager.SendTokenTimeout, new AsyncCallback(ActiveClientLinkManager.OnSendTokenComplete), activeClientLinkManager);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(activeClientLinkManager.activeClientLink.Link, "BeginSendToken", exception.Message));
			}
		}

		private static void OnSendTokenComplete(IAsyncResult result)
		{
			ActiveClientLinkManager asyncState = (ActiveClientLinkManager)result.AsyncState;
			try
			{
				AmqpCbsLink amqpCbsLink = asyncState.activeClientLink.Link.Session.Connection.Extensions.Find<AmqpCbsLink>() ?? new AmqpCbsLink(asyncState.activeClientLink.Link.Session.Connection);
				DateTime dateTime = amqpCbsLink.EndSendToken(result);
				MessagingClientEtwProvider.TraceClient(() => {
				});
				lock (asyncState.syncRoot)
				{
					asyncState.activeClientLink.AuthorizationValidToUtc = dateTime;
					asyncState.ScheduleValidityTimer();
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogError(asyncState.activeClientLink.Link, "EndSendToken", exception.Message));
				asyncState.CancelValidityTimer();
			}
		}

		private void ScheduleValidityTimer()
		{
			if (this.activeClientLink.AuthorizationValidToUtc < DateTime.UtcNow)
			{
				return;
			}
			TimeSpan tokenRefreshBuffer = this.activeClientLink.AuthorizationValidToUtc.Subtract(DateTime.UtcNow);
			tokenRefreshBuffer = tokenRefreshBuffer + ActiveClientLinkManager.TokenRefreshBuffer;
			tokenRefreshBuffer = (tokenRefreshBuffer < ClientConstants.ClientMinimumTokenRefreshInterval ? ClientConstants.ClientMinimumTokenRefreshInterval : tokenRefreshBuffer);
			this.validityTimer.Set(tokenRefreshBuffer);
			MessagingClientEtwProvider.TraceClient(() => {
			});
		}

		public void SetActiveLink(ActiveClientLink activeClientLink)
		{
			lock (this.syncRoot)
			{
				this.activeClientLink = activeClientLink;
				this.activeClientLink.Link.Closed += new EventHandler(this.OnLinkClosed);
				if (this.activeClientLink.Link.State == AmqpObjectState.Opened && this.activeClientLink.IsClientToken)
				{
					this.ScheduleValidityTimer();
				}
			}
		}
	}
}