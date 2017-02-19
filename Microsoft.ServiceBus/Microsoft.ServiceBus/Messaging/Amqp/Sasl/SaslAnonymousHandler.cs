using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslAnonymousHandler : SaslHandler
	{
		public readonly static string Name;

		public string Identity
		{
			get;
			set;
		}

		static SaslAnonymousHandler()
		{
			SaslAnonymousHandler.Name = "ANONYMOUS";
		}

		public SaslAnonymousHandler()
		{
			base.Mechanism = SaslAnonymousHandler.Name;
		}

		public override SaslHandler Clone()
		{
			return new SaslAnonymousHandler();
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
			if (!isClient)
			{
				base.Negotiator.CompleteNegotiation(SaslCode.Ok, null);
				return;
			}
			if (this.Identity != null)
			{
				init.InitialResponse = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(this.Identity));
			}
			base.Negotiator.WriteFrame(init, true);
		}
	}
}