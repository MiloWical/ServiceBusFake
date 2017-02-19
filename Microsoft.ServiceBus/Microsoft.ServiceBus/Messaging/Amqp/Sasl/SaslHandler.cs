using Microsoft.ServiceBus.Common;
using System;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal abstract class SaslHandler
	{
		private SaslNegotiator saslNegotiator;

		public string Mechanism
		{
			get;
			protected set;
		}

		protected SaslNegotiator Negotiator
		{
			get
			{
				return this.saslNegotiator;
			}
		}

		public IPrincipal Principal
		{
			get;
			protected set;
		}

		protected SaslHandler()
		{
		}

		public abstract SaslHandler Clone();

		public abstract void OnChallenge(SaslChallenge challenge);

		public abstract void OnResponse(SaslResponse response);

		protected abstract void OnStart(SaslInit init, bool isClient);

		public void Start(SaslNegotiator saslNegotiator, SaslInit init, bool isClient)
		{
			this.saslNegotiator = saslNegotiator;
			try
			{
				this.OnStart(init, isClient);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				this.saslNegotiator.CompleteNegotiation(SaslCode.Sys, exception);
			}
		}

		public override string ToString()
		{
			return this.Mechanism;
		}
	}
}