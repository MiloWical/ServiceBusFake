using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp;
using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
using Microsoft.ServiceBus.Messaging.Amqp.Framing;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging.AmqpClient
{
	internal sealed class AmqpManagementLink : ICloseable
	{
		private readonly AmqpConnection connection;

		private FaultTolerantObject<RequestResponseAmqpLink> FaultTolerantLink
		{
			get;
			set;
		}

		bool Microsoft.ServiceBus.Messaging.ICloseable.IsClosedOrClosing
		{
			get
			{
				return this.connection.IsClosing();
			}
		}

		public AmqpManagementLink(AmqpConnection connection)
		{
			this.connection = connection;
			this.FaultTolerantLink = new FaultTolerantObject<RequestResponseAmqpLink>(this, new Action<RequestResponseAmqpLink>(this.CloseLink), new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginCreateManagementLink), new Func<IAsyncResult, RequestResponseAmqpLink>(this.EndCreateManagementLink));
			this.connection.Extensions.Add(this);
		}

		private IAsyncResult BeginCreateManagementLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult(this.connection, timeout, callback, state);
		}

		public IAsyncResult BeginGetEventHubRuntimeInfo(string eventHubPath, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult(this, eventHubPath, timeout, callback, state);
		}

		private void CloseLink(RequestResponseAmqpLink link)
		{
			link.Session.SafeClose();
		}

		private RequestResponseAmqpLink EndCreateManagementLink(IAsyncResult result)
		{
			return AsyncResult<AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult>.End(result).Link;
		}

		public EventHubRuntimeInformation EndGetEventHubRuntimeInfo(IAsyncResult result)
		{
			return AsyncResult<AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult>.End(result).Result;
		}

		private sealed class GetEventHubRuntimeInfoAsyncResult : IteratorAsyncResult<AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult>
		{
			private readonly AmqpManagementLink managementLink;

			private readonly string eventHubPath;

			private AmqpMessage getRuntimeInfoResponse;

			private RequestResponseAmqpLink requestResponseLink;

			public EventHubRuntimeInformation Result
			{
				get;
				private set;
			}

			public GetEventHubRuntimeInfoAsyncResult(AmqpManagementLink managementLink, string eventHubPath, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.managementLink = managementLink;
				this.eventHubPath = eventHubPath;
				base.Start();
			}

			private static Exception ConvertToException(int statusCode, string statusDescription)
			{
				Exception messagingException;
				if (!Enum.IsDefined(typeof(HttpStatusCode), statusCode))
				{
					messagingException = new MessagingException(SRAmqp.AmqpManagementOperationFailed(statusCode, statusDescription));
				}
				else
				{
					HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
					if (httpStatusCode == HttpStatusCode.BadRequest)
					{
						messagingException = new ArgumentException(SRAmqp.AmqpManagementOperationFailed(statusCode, statusDescription));
					}
					else if (httpStatusCode == HttpStatusCode.NotFound)
					{
						messagingException = new MessagingEntityNotFoundException(SRAmqp.AmqpManagementOperationFailed(statusCode, statusDescription));
					}
					else
					{
						messagingException = new MessagingException(SRAmqp.AmqpManagementOperationFailed(statusCode, statusDescription));
					}
				}
				return messagingException;
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				if (!this.managementLink.FaultTolerantLink.TryGetOpenedObject(out this.requestResponseLink))
				{
					AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult getEventHubRuntimeInfoAsyncResult = this;
					IteratorAsyncResult<AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult>.BeginCall beginCall = (AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.managementLink.FaultTolerantLink.BeginGetInstance(t, c, s);
					yield return getEventHubRuntimeInfoAsyncResult.CallAsync(beginCall, (AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult thisPtr, IAsyncResult r) => thisPtr.requestResponseLink = thisPtr.managementLink.FaultTolerantLink.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				AmqpMessage applicationProperty = AmqpMessage.Create();
				applicationProperty.ApplicationProperties = new ApplicationProperties();
				applicationProperty.ApplicationProperties.Map["name"] = this.eventHubPath;
				applicationProperty.ApplicationProperties.Map["operation"] = "READ";
				applicationProperty.ApplicationProperties.Map["type"] = "com.microsoft:eventhub";
				AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult getEventHubRuntimeInfoAsyncResult1 = this;
				IteratorAsyncResult<AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult>.BeginCall beginCall1 = (AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.requestResponseLink.BeginRequest(applicationProperty, t, c, s);
				yield return getEventHubRuntimeInfoAsyncResult1.CallAsync(beginCall1, (AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult thisPtr, IAsyncResult r) => thisPtr.getRuntimeInfoResponse = thisPtr.requestResponseLink.EndRequest(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				int item = (int)this.getRuntimeInfoResponse.ApplicationProperties.Map["status-code"];
				string str = (string)this.getRuntimeInfoResponse.ApplicationProperties.Map["status-description"];
				if (item != 200)
				{
					base.Complete(AmqpManagementLink.GetEventHubRuntimeInfoAsyncResult.ConvertToException(item, str));
				}
				else
				{
					this.Result = MessageConverter.GetEventHubRuntimeInfo(this.getRuntimeInfoResponse);
				}
			}
		}

		private sealed class OpenManagementRequestResponseLinkAsyncResult : IteratorAsyncResult<AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult>, ILinkFactory
		{
			private readonly AmqpConnection connection;

			private AmqpSession session;

			public RequestResponseAmqpLink Link
			{
				get;
				private set;
			}

			public OpenManagementRequestResponseLinkAsyncResult(AmqpConnection connection, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.connection = connection;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				string str = "$management";
				if (base.RemainingTime() > TimeSpan.Zero)
				{
					try
					{
						AmqpSessionSettings amqpSessionSetting = new AmqpSessionSettings()
						{
							Properties = new Fields()
						};
						this.session = new AmqpSession(this.connection, amqpSessionSetting, this);
						this.connection.AddSession(this.session, null);
					}
					catch (InvalidOperationException invalidOperationException1)
					{
						InvalidOperationException invalidOperationException = invalidOperationException1;
						base.Complete(new MessagingException(invalidOperationException.Message, true, invalidOperationException));
						goto Label0;
					}
					AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult openManagementRequestResponseLinkAsyncResult = this;
					IteratorAsyncResult<AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult>.BeginCall beginCall = (AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.BeginOpen(t, c, s);
					yield return openManagementRequestResponseLinkAsyncResult.CallAsync(beginCall, (AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult thisPtr, IAsyncResult r) => thisPtr.session.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					Exception lastAsyncStepException = base.LastAsyncStepException;
					if (lastAsyncStepException == null)
					{
						AmqpLinkSettings amqpLinkSetting = new AmqpLinkSettings();
						AmqpLinkSettings amqpLinkSetting1 = amqpLinkSetting;
						AmqpSymbol timeoutName = ClientConstants.TimeoutName;
						TimeSpan timeSpan = base.RemainingTime();
						amqpLinkSetting1.AddProperty(timeoutName, (uint)timeSpan.TotalMilliseconds);
						amqpLinkSetting.Target = new Target()
						{
							Address = str
						};
						amqpLinkSetting.Source = new Source()
						{
							Address = str
						};
						amqpLinkSetting.InitialDeliveryCount = new uint?(0);
						amqpLinkSetting.TotalLinkCredit = 50;
						amqpLinkSetting.AutoSendFlow = true;
						amqpLinkSetting.SettleType = SettleMode.SettleOnSend;
						this.Link = new RequestResponseAmqpLink(this.session, amqpLinkSetting);
						AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult openManagementRequestResponseLinkAsyncResult1 = this;
						IteratorAsyncResult<AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult>.BeginCall beginCall1 = (AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Link.BeginOpen(t, c, s);
						yield return openManagementRequestResponseLinkAsyncResult1.CallAsync(beginCall1, (AmqpManagementLink.OpenManagementRequestResponseLinkAsyncResult thisPtr, IAsyncResult r) => thisPtr.Link.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						lastAsyncStepException = base.LastAsyncStepException;
						if (lastAsyncStepException == null)
						{
							MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntitySucceeded(this.connection, this.Link, this.Link.Name, str);
						}
						else
						{
							this.Link = null;
							MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntityFailed(this.connection, this.Link, this.Link.Name, str, lastAsyncStepException.Message);
							this.session.SafeClose();
							base.Complete(Microsoft.ServiceBus.Messaging.Amqp.ExceptionHelper.ToMessagingContract(lastAsyncStepException, this.connection.RemoteEndpoint.ToString()));
						}
					}
					else
					{
						MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntityFailed(this.connection, this.session, string.Empty, str, lastAsyncStepException.Message);
						this.session.Abort();
						base.Complete(Microsoft.ServiceBus.Messaging.Amqp.ExceptionHelper.ToMessagingContract(lastAsyncStepException, this.connection.RemoteEndpoint.ToString()));
					}
				}
				else
				{
					if (this.session != null)
					{
						this.session.SafeClose();
					}
					base.Complete(new TimeoutException(SRAmqp.AmqpTimeout(base.OriginalTimeout, str)));
				}
			Label0:
				yield break;
			}

			IAsyncResult Microsoft.ServiceBus.Messaging.Amqp.ILinkFactory.BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
			{
				return new CompletedAsyncResult(callback, state);
			}

			AmqpLink Microsoft.ServiceBus.Messaging.Amqp.ILinkFactory.CreateLink(AmqpSession session, AmqpLinkSettings settings)
			{
				AmqpLink sendingAmqpLink;
				if (!settings.IsReceiver())
				{
					sendingAmqpLink = new SendingAmqpLink(session, settings);
				}
				else
				{
					sendingAmqpLink = new ReceivingAmqpLink(session, settings);
				}
				MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, sendingAmqpLink);
				return sendingAmqpLink;
			}

			void Microsoft.ServiceBus.Messaging.Amqp.ILinkFactory.EndOpenLink(IAsyncResult result)
			{
				CompletedAsyncResult.End(result);
			}
		}
	}
}