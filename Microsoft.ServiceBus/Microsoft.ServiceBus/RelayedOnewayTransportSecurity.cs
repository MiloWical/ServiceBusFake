using Microsoft.ServiceBus.Diagnostics;
using System;
using System.Net.Security;

namespace Microsoft.ServiceBus
{
	public sealed class RelayedOnewayTransportSecurity
	{
		internal const System.Net.Security.ProtectionLevel DefaultProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;

		private System.Net.Security.ProtectionLevel protectionLevel;

		public System.Net.Security.ProtectionLevel ProtectionLevel
		{
			get
			{
				return this.protectionLevel;
			}
			set
			{
				if (!ProtectionLevelHelper.IsDefined(value))
				{
					throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
				}
				this.protectionLevel = value;
			}
		}

		internal RelayedOnewayTransportSecurity()
		{
			this.protectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
		}
	}
}