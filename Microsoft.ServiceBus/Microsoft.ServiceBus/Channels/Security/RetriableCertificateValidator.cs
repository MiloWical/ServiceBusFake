using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Configuration;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Diagnostics;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Microsoft.ServiceBus.Channels.Security
{
	internal class RetriableCertificateValidator : X509CertificateValidator
	{
		private const int maxNumberofRetries = 3;

		private const int retryIntervalInSeconds = 5;

		private static RetriableCertificateValidator instance;

		private readonly X509CertificateValidator peerOrChainTrustOnline;

		private readonly X509CertificateValidator peerOrChainTrustNoCheck;

		private readonly X509CertificateValidator peerOrChainTrustOffline;

		private readonly X509RevocationMode revocationMode;

		internal static RetriableCertificateValidator Instance
		{
			get
			{
				if (RetriableCertificateValidator.instance == null)
				{
					RetriableCertificateValidator.instance = new RetriableCertificateValidator();
				}
				return RetriableCertificateValidator.instance;
			}
		}

		private RetriableCertificateValidator()
		{
			X509ChainPolicy x509ChainPolicy = new X509ChainPolicy()
			{
				RevocationMode = X509RevocationMode.Online
			};
			this.peerOrChainTrustOnline = X509CertificateValidator.CreatePeerOrChainTrustValidator(true, x509ChainPolicy);
			X509ChainPolicy x509ChainPolicy1 = new X509ChainPolicy()
			{
				RevocationMode = X509RevocationMode.NoCheck
			};
			this.peerOrChainTrustNoCheck = X509CertificateValidator.CreatePeerOrChainTrustValidator(true, x509ChainPolicy1);
			X509ChainPolicy x509ChainPolicy2 = new X509ChainPolicy()
			{
				RevocationMode = X509RevocationMode.Offline
			};
			this.peerOrChainTrustOffline = X509CertificateValidator.CreatePeerOrChainTrustValidator(true, x509ChainPolicy2);
			this.revocationMode = ConfigurationHelpers.GetCertificateRevocationMode();
		}

		private void PeerOrChainTrustValidateByValidator(X509CertificateValidator validator, X509Certificate2 certificate)
		{
			int num = 0;
			Exception exception = null;
			while (num < 3)
			{
				try
				{
					validator.Validate(certificate);
					exception = null;
					break;
				}
				catch (SecurityTokenValidationException securityTokenValidationException)
				{
					exception = securityTokenValidationException;
					num++;
					Thread.Sleep(5000);
				}
			}
			if (exception != null)
			{
				throw FxTrace.Exception.AsError(exception, null);
			}
		}

		private void PeerOrChainTrustValidateInOnlineMode(X509Certificate2 certificate)
		{
			int num = 0;
			Exception exception = null;
			while (num < 3)
			{
				try
				{
					this.peerOrChainTrustOnline.Validate(certificate);
					exception = null;
					break;
				}
				catch (SecurityTokenValidationException securityTokenValidationException1)
				{
					exception = securityTokenValidationException1;
					if (num == 0)
					{
						try
						{
							this.peerOrChainTrustNoCheck.Validate(certificate);
							if (DiagnosticUtility.ShouldTraceError)
							{
								DiagnosticUtility.DiagnosticTrace.TraceEvent(TraceEventType.Error, TraceCode.HttpsClientCertificateInvalid, SRClient.X509CRLCheckFailed(certificate.SubjectName.Name));
							}
						}
						catch (SecurityTokenValidationException securityTokenValidationException)
						{
							break;
						}
					}
					num++;
					Thread.Sleep(5000);
				}
			}
			if (exception != null)
			{
				throw FxTrace.Exception.AsError(exception, null);
			}
		}

		public override void Validate(X509Certificate2 certificate)
		{
			if (certificate == null)
			{
				throw FxTrace.Exception.AsError(new ArgumentNullException("certificate"), null);
			}
			DateTime now = DateTime.Now;
			if (now > certificate.NotAfter || now < certificate.NotBefore)
			{
				throw FxTrace.Exception.AsError(new SecurityTokenValidationException(SRClient.X509InvalidUsageTime(certificate.SubjectName.Name)), null);
			}
			this.ValidateUnTrustedCertificate(certificate);
			switch (this.revocationMode)
			{
				case X509RevocationMode.NoCheck:
				{
					this.PeerOrChainTrustValidateByValidator(this.peerOrChainTrustNoCheck, certificate);
					return;
				}
				case X509RevocationMode.Online:
				{
					this.PeerOrChainTrustValidateInOnlineMode(certificate);
					return;
				}
				case X509RevocationMode.Offline:
				{
					this.PeerOrChainTrustValidateByValidator(this.peerOrChainTrustOffline, certificate);
					return;
				}
				default:
				{
					return;
				}
			}
		}

		private void ValidateUnTrustedCertificate(X509Certificate2 certificate)
		{
			bool count = false;
			X509Store x509Store = new X509Store(StoreName.Disallowed, StoreLocation.CurrentUser);
			try
			{
				x509Store.Open(OpenFlags.ReadOnly);
				X509Certificate2Collection x509Certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
				count = x509Certificate2Collection.Count > 0;
			}
			finally
			{
				x509Store.Close();
			}
			if (count)
			{
				throw FxTrace.Exception.AsError(new SecurityTokenValidationException(SRClient.X509InUnTrustedStore(certificate.SubjectName.Name)), null);
			}
		}
	}
}