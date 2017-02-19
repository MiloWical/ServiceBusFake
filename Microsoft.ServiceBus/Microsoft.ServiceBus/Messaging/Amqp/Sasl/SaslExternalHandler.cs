using System;
using System.Security.Principal;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslExternalHandler : SaslHandler
	{
		public readonly static string Name;

		static SaslExternalHandler()
		{
			SaslExternalHandler.Name = "EXTERNAL";
		}

		public SaslExternalHandler()
		{
			base.Mechanism = SaslExternalHandler.Name;
		}

		public override SaslHandler Clone()
		{
			return new SaslExternalHandler();
		}

		public override void OnChallenge(SaslChallenge challenge)
		{
			throw new NotImplementedException();
		}

		public override void OnResponse(SaslResponse response)
		{
			throw new NotImplementedException();
		}

		protected override void OnStart(SaslInit init, bool isClient)
		{
			if (isClient)
			{
				base.Negotiator.WriteFrame(init, true);
				return;
			}
			base.Principal = new GenericPrincipal(new GenericIdentity("dummy-identity", "dummy-identity"), null);
			base.Negotiator.CompleteNegotiation(SaslCode.Ok, null);
		}
	}
}