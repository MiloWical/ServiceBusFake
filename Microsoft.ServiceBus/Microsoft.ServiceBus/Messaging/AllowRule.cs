using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="AllowRule", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class AllowRule : AuthorizationRule
	{
		public override string KeyName
		{
			get
			{
				return base.ClaimValue;
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public AllowRule()
		{
		}

		public AllowRule(string issuerName, string claimType, string claimValue, IEnumerable<AccessRights> rights)
		{
			base.IssuerName = issuerName;
			base.ClaimType = claimType;
			base.ClaimValue = claimValue.ToLowerInvariant();
			base.Rights = rights;
		}

		public AllowRule(string issuerName, AllowRuleClaimType claimType, string claimValue, IEnumerable<AccessRights> rights) : this(issuerName, (claimType == AllowRuleClaimType.Upn ? "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn" : "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"), claimValue, rights)
		{
		}
	}
}