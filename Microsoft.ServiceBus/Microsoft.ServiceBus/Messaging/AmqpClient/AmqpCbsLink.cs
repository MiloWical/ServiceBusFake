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
	internal sealed class AmqpCbsLink : ICloseable
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

		public AmqpCbsLink(AmqpConnection connection)
		{
			this.connection = connection;
			this.FaultTolerantLink = new FaultTolerantObject<RequestResponseAmqpLink>(this, new Action<RequestResponseAmqpLink>(this.CloseLink), new Func<TimeSpan, AsyncCallback, object, IAsyncResult>(this.BeginCreateCbsLink), new Func<IAsyncResult, RequestResponseAmqpLink>(this.EndCreateCbsLink));
			this.connection.Extensions.Add(this);
		}

		private IAsyncResult BeginCreateCbsLink(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult(this.connection, timeout, callback, state);
		}

		public IAsyncResult BeginSendToken(TokenProvider tokenProvider, Uri namespaceAddress, string audience, string resource, string[] requiredClaims, TimeSpan timeout, AsyncCallback callback, object state)
		{
			return new AmqpCbsLink.SendTokenAsyncResult(this, tokenProvider, namespaceAddress, audience, resource, requiredClaims, timeout, callback, state);
		}

		private void CloseLink(RequestResponseAmqpLink link)
		{
			link.Session.SafeClose();
		}

		private RequestResponseAmqpLink EndCreateCbsLink(IAsyncResult result)
		{
			return AsyncResult<AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult>.End(result).Link;
		}

		public DateTime EndSendToken(IAsyncResult result)
		{
			return AsyncResult<AmqpCbsLink.SendTokenAsyncResult>.End(result).ValidTo;
		}

		private sealed class OpenCbsRequestResponseLinkAsyncResult : IteratorAsyncResult<AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult>, ILinkFactory
		{
			private readonly AmqpConnection connection;

			private AmqpSession session;

			public RequestResponseAmqpLink Link
			{
				get;
				private set;
			}

			public OpenCbsRequestResponseLinkAsyncResult(AmqpConnection connection, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.connection = connection;
				base.Start();
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				string str = "$cbs";
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
						base.Complete(new MessagingException(invalidOperationException.Message, false, invalidOperationException));
						goto Label0;
					}
					AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult openCbsRequestResponseLinkAsyncResult = this;
					IteratorAsyncResult<AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult>.BeginCall beginCall = (AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.session.BeginOpen(t, c, s);
					yield return openCbsRequestResponseLinkAsyncResult.CallAsync(beginCall, (AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult thisPtr, IAsyncResult r) => thisPtr.session.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
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
						AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult openCbsRequestResponseLinkAsyncResult1 = this;
						IteratorAsyncResult<AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult>.BeginCall beginCall1 = (AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.Link.BeginOpen(t, c, s);
						yield return openCbsRequestResponseLinkAsyncResult1.CallAsync(beginCall1, (AmqpCbsLink.OpenCbsRequestResponseLinkAsyncResult thisPtr, IAsyncResult r) => thisPtr.Link.EndOpen(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
						lastAsyncStepException = base.LastAsyncStepException;
						if (lastAsyncStepException == null)
						{
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntitySucceeded(this.connection, this.Link, this.Link.Name, str));
						}
						else
						{
							this.Link = null;
							MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntityFailed(this.connection, this.Link, this.Link.Name, str, lastAsyncStepException.Message));
							this.session.SafeClose();
							base.Complete(Microsoft.ServiceBus.Messaging.Amqp.ExceptionHelper.ToMessagingContract(lastAsyncStepException, this.connection.RemoteEndpoint.ToString()));
						}
					}
					else
					{
						MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpOpenEntityFailed(this.connection, this.session, string.Empty, str, lastAsyncStepException.Message));
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
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWriteAmqpLogOperation(this, TraceOperation.Create, sendingAmqpLink));
				return sendingAmqpLink;
			}

			void Microsoft.ServiceBus.Messaging.Amqp.ILinkFactory.EndOpenLink(IAsyncResult result)
			{
				CompletedAsyncResult.End(result);
			}
		}

		private sealed class SendTokenAsyncResult : IteratorAsyncResult<AmqpCbsLink.SendTokenAsyncResult>
		{
			private readonly TokenProvider tokenProvider;

			private readonly AmqpCbsLink cbsLink;

			private readonly string[] requiredClaims;

			private readonly Uri namespaceAddress;

			private readonly string audience;

			private readonly string resource;

			private SimpleWebSecurityToken swt;

			private RequestResponseAmqpLink requestResponseLink;

			public DateTime ValidTo
			{
				get;
				private set;
			}

			public SendTokenAsyncResult(AmqpCbsLink cbsLink, TokenProvider tokenProvider, Uri namespaceAddress, string audience, string resource, string[] requiredClaims, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.cbsLink = cbsLink;
				this.namespaceAddress = namespaceAddress;
				this.audience = audience;
				this.resource = resource;
				this.requiredClaims = requiredClaims;
				this.tokenProvider = tokenProvider;
				base.Start();
			}

			private static Exception ConvertToException(int statusCode, string statusDescription)
			{
				Exception messagingException;
				if (!Enum.IsDefined(typeof(HttpStatusCode), statusCode))
				{
					messagingException = new MessagingException(SRAmqp.AmqpPutTokenFailed(statusCode, statusDescription));
				}
				else
				{
					HttpStatusCode httpStatusCode = (HttpStatusCode)statusCode;
					if (httpStatusCode == HttpStatusCode.BadRequest)
					{
						messagingException = new ArgumentException(SRAmqp.AmqpPutTokenFailed(statusCode, statusDescription));
					}
					else if (httpStatusCode == HttpStatusCode.NotFound)
					{
						messagingException = new MessagingEntityNotFoundException(SRAmqp.AmqpPutTokenFailed(statusCode, statusDescription));
					}
					else
					{
						messagingException = new MessagingException(SRAmqp.AmqpPutTokenFailed(statusCode, statusDescription));
					}
				}
				return messagingException;
			}

			protected override IEnumerator<IteratorAsyncResult<AmqpCbsLink.SendTokenAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				string str;
				AmqpCbsLink.SendTokenAsyncResult sendTokenAsyncResult = this;
				IteratorAsyncResult<AmqpCbsLink.SendTokenAsyncResult>.BeginCall beginCall = (AmqpCbsLink.SendTokenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.tokenProvider.BeginGetMessagingToken(this.namespaceAddress, thisPtr.resource, thisPtr.requiredClaims[0], false, t, c, s);
				yield return sendTokenAsyncResult.CallAsync(beginCall, (AmqpCbsLink.SendTokenAsyncResult thisPtr, IAsyncResult r) => thisPtr.swt = (SimpleWebSecurityToken)thisPtr.tokenProvider.EndGetMessagingToken(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				if (this.swt is SharedAccessSignatureToken)
				{
					str = "servicebus.windows.net:sastoken";
				}
				else if (this.swt != null)
				{
					str = "amqp:swt";
				}
				else
				{
					str = null;
				}
				string str1 = str;
				if (str1 != null)
				{
					if (!this.cbsLink.FaultTolerantLink.TryGetOpenedObject(out this.requestResponseLink))
					{
						AmqpCbsLink.SendTokenAsyncResult sendTokenAsyncResult1 = this;
						IteratorAsyncResult<AmqpCbsLink.SendTokenAsyncResult>.BeginCall beginCall1 = (AmqpCbsLink.SendTokenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => thisPtr.cbsLink.FaultTolerantLink.BeginGetInstance(t, c, s);
						yield return sendTokenAsyncResult1.CallAsync(beginCall1, (AmqpCbsLink.SendTokenAsyncResult thisPtr, IAsyncResult r) => thisPtr.requestResponseLink = thisPtr.cbsLink.FaultTolerantLink.EndGetInstance(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					}
					AmqpValue amqpValue = new AmqpValue()
					{
						Value = this.swt.Token
					};
					AmqpMessage applicationProperty = AmqpMessage.Create(amqpValue);
					applicationProperty.ApplicationProperties = new ApplicationProperties();
					applicationProperty.ApplicationProperties.Map["operation"] = "put-token";
					applicationProperty.ApplicationProperties.Map["type"] = str1;
					applicationProperty.ApplicationProperties.Map["name"] = this.audience;
					applicationProperty.ApplicationProperties.Map["expiration"] = this.swt.ExpiresOn;
					AmqpMessage amqpMessage = null;
					yield return base.CallAsync((AmqpCbsLink.SendTokenAsyncResult thisPtr, TimeSpan t, AsyncCallback c, object s) => this.requestResponseLink.BeginRequest(applicationProperty, t, c, s), (AmqpCbsLink.SendTokenAsyncResult thisPtr, IAsyncResult r) => amqpMessage = this.requestResponseLink.EndRequest(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					int item = (int)amqpMessage.ApplicationProperties.Map["status-code"];
					string item1 = (string)amqpMessage.ApplicationProperties.Map["status-description"];
					if (item != 202)
					{
						base.Complete(AmqpCbsLink.SendTokenAsyncResult.ConvertToException(item, item1));
					}
					else
					{
						this.ValidTo = this.swt.ValidTo;
					}
				}
				else
				{
					base.Complete(new InvalidOperationException(SRAmqp.AmqpUnssuportedTokenType));
				}
			}
		}
	}
}