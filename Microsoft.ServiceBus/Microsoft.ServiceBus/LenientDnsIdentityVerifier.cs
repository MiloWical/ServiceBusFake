using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Security;

namespace Microsoft.ServiceBus
{
	internal class LenientDnsIdentityVerifier : IdentityVerifier
	{
		private readonly DnsEndpointIdentity expectedIdentity;

		public LenientDnsIdentityVerifier()
		{
		}

		public LenientDnsIdentityVerifier(DnsEndpointIdentity expectedIdentity)
		{
			this.expectedIdentity = expectedIdentity;
		}

		public LenientDnsIdentityVerifier(string dnsName) : this(new DnsEndpointIdentity(dnsName))
		{
		}

		public override bool CheckAccess(EndpointIdentity identity, AuthorizationContext authContext)
		{
			List<Claim> claims = new List<Claim>();
			X509Extension item = null;
			foreach (ClaimSet claimSet in authContext.ClaimSets)
			{
				if (item == null)
				{
					X509CertificateClaimSet x509CertificateClaimSet = claimSet as X509CertificateClaimSet;
					if (x509CertificateClaimSet != null && x509CertificateClaimSet.X509Certificate != null)
					{
						item = x509CertificateClaimSet.X509Certificate.Extensions["2.5.29.17"];
					}
				}
				foreach (Claim claim in claimSet)
				{
					if (ClaimTypes.Dns != claim.ClaimType)
					{
						continue;
					}
					claims.Add(claim);
				}
			}
			if (1 != claims.Count)
			{
				throw new InvalidOperationException(SRClient.InvalidDNSClaims(claims.Count));
			}
			if (LenientDnsIdentityVerifier.CheckTopLevelDomainCompatibleness(claims[0].Resource.ToString(), identity.IdentityClaim.Resource.ToString()))
			{
				return true;
			}
			return SecureSocketUtil.CertificateCheckSubjectAlternativeNames(item, identity.IdentityClaim.Resource.ToString());
		}

		public static bool CheckTopLevelDomainCompatibleness(string actualIdentity, string expectedIdentity)
		{
			if (string.IsNullOrEmpty(actualIdentity) || string.IsNullOrEmpty(expectedIdentity))
			{
				return false;
			}
			if (actualIdentity.StartsWith("*.", StringComparison.Ordinal))
			{
				actualIdentity = actualIdentity.Substring(2);
			}
			char[] chrArray = new char[] { '.' };
			if ((int)actualIdentity.Split(chrArray).Length < 2 && expectedIdentity.IndexOf(".", StringComparison.Ordinal) != -1)
			{
				return false;
			}
			if (expectedIdentity.EndsWith(actualIdentity, StringComparison.OrdinalIgnoreCase))
			{
				string str = expectedIdentity.Substring(0, expectedIdentity.Length - actualIdentity.Length);
				if (str.IndexOf(".", StringComparison.Ordinal) != str.Length - 1)
				{
					return false;
				}
				return true;
			}
			string ascii = (new IdnMapping()).GetAscii(expectedIdentity);
			if (!ascii.EndsWith(actualIdentity, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string str1 = ascii.Substring(0, ascii.Length - actualIdentity.Length);
			if (str1.IndexOf(".", StringComparison.Ordinal) != str1.Length - 1)
			{
				return false;
			}
			return true;
		}

		private static EndpointIdentity TryCreateDnsIdentity(EndpointAddress reference)
		{
			Uri uri = reference.Uri;
			if (!uri.IsAbsoluteUri)
			{
				return null;
			}
			return EndpointIdentity.CreateDnsIdentity(uri.DnsSafeHost);
		}

		public override bool TryGetIdentity(EndpointAddress reference, out EndpointIdentity identity)
		{
			if (this.expectedIdentity != null)
			{
				identity = this.expectedIdentity;
				return true;
			}
			if (reference == null)
			{
				throw new ArgumentNullException("reference");
			}
			identity = reference.Identity;
			if (identity == null)
			{
				identity = LenientDnsIdentityVerifier.TryCreateDnsIdentity(reference);
			}
			if (identity == null)
			{
				return false;
			}
			return true;
		}
	}
}