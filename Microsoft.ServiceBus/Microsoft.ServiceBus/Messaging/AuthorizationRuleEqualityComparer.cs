using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class AuthorizationRuleEqualityComparer : EqualityComparer<AuthorizationRule>
	{
		public AuthorizationRuleEqualityComparer()
		{
		}

		public override bool Equals(AuthorizationRule x, AuthorizationRule y)
		{
			if (x == null || y == null)
			{
				return false;
			}
			return x.Equals(y);
		}

		public override int GetHashCode(AuthorizationRule obj)
		{
			if (obj == null)
			{
				throw new ArgumentNullException("obj");
			}
			return obj.GetHashCode();
		}
	}
}