using System;

namespace Microsoft.ServiceBus.Management
{
	public enum CertificateType
	{
		GeneratedFarmRootCertificate,
		GeneratedFarmCertificate,
		CustomFarmCertificate,
		GeneratedCertificateRevocationList
	}
}