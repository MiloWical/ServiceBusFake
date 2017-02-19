using System;

namespace Microsoft.ServiceBus
{
	public sealed class NonDualMessageSecurityOverRelayHttp : MessageSecurityOverRelayHttp
	{
		internal const bool DefaultEstablishSecurityContext = true;

		private bool establishSecurityContext;

		public bool EstablishSecurityContext
		{
			get
			{
				return this.establishSecurityContext;
			}
			set
			{
				this.establishSecurityContext = value;
			}
		}

		internal NonDualMessageSecurityOverRelayHttp()
		{
			this.establishSecurityContext = true;
		}

		protected override bool IsSecureConversationEnabled()
		{
			return this.establishSecurityContext;
		}
	}
}