using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslSwtHandler : SaslHandler
	{
		public readonly static string Name;

		private TokenProvider tokenProvider;

		private string action;

		private string appliesTo;

		private Func<string, string[]> tokenAuthenticator;

		static SaslSwtHandler()
		{
			SaslSwtHandler.Name = "SWT";
		}

		private SaslSwtHandler()
		{
			base.Mechanism = SaslSwtHandler.Name;
		}

		public SaslSwtHandler(TokenProvider tokenProvider, string action, string appliesTo) : this()
		{
			this.action = action;
			this.tokenProvider = tokenProvider;
			this.appliesTo = appliesTo;
		}

		public SaslSwtHandler(Func<string, string[]> tokenAuthenticator) : this()
		{
			this.tokenAuthenticator = tokenAuthenticator;
		}

		public override SaslHandler Clone()
		{
			return new SaslSwtHandler(this.tokenProvider, this.action, this.appliesTo);
		}

		private string GetClientMessage()
		{
			SimpleWebSecurityToken token = (SimpleWebSecurityToken)this.tokenProvider.GetToken(this.appliesTo, this.action, false, TimeSpan.FromSeconds(5));
			return token.Token;
		}

		public override void OnChallenge(SaslChallenge challenge)
		{
			throw new NotImplementedException();
		}

		private void OnInit(SaslInit init)
		{
			SaslCode saslCode = SaslCode.Ok;
			if (init.InitialResponse.Count > 0)
			{
				System.Text.Encoding uTF8 = System.Text.Encoding.UTF8;
				byte[] array = init.InitialResponse.Array;
				int offset = init.InitialResponse.Offset;
				ArraySegment<byte> initialResponse = init.InitialResponse;
				string str = uTF8.GetString(array, offset, initialResponse.Count);
				MessagingClientEtwProvider.TraceClient(() => {
				});
				if (this.tokenAuthenticator != null)
				{
					try
					{
						string[] strArrays = this.tokenAuthenticator(str);
						base.Principal = new GenericPrincipal(new GenericIdentity("acs-client", SaslSwtHandler.Name), strArrays);
					}
					catch (Exception exception)
					{
						if (Fx.IsFatal(exception))
						{
							throw;
						}
						saslCode = SaslCode.Auth;
					}
				}
			}
			base.Negotiator.CompleteNegotiation(saslCode, null);
		}

		public override void OnResponse(SaslResponse response)
		{
			throw new NotImplementedException();
		}

		protected override void OnStart(SaslInit init, bool isClient)
		{
			if (!isClient)
			{
				this.OnInit(init);
				return;
			}
			string clientMessage = this.GetClientMessage();
			MessagingClientEtwProvider.TraceClient(() => {
			});
			init.InitialResponse = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(clientMessage));
			base.Negotiator.WriteFrame(init, true);
		}
	}
}