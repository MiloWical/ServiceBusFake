using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="ApnsCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class ApnsCredential : PnsCredential
	{
		internal const string AppPlatformName = "apple";

		internal const string ApnsGatewayEndpoint = "gateway.push.apple.com";

		internal const string ApnsGatewaySandboxEndpoint = "gateway.sandbox.push.apple.com";

		internal const string ApnsFeedbackEndpoint = "feedback.push.apple.com";

		internal const string ApnsFeedbackSandboxEndpoint = "feedback.sandbox.push.apple.com";

		private static HashSet<string> validApnsEndpoints;

		private static HashSet<string> validLocalApnsEndpoints;

		public string ApnsCertificate
		{
			get
			{
				return base["ApnsCertificate"];
			}
			set
			{
				base["ApnsCertificate"] = value;
			}
		}

		internal override string AppPlatform
		{
			get
			{
				return "apple";
			}
		}

		public string CertificateKey
		{
			get
			{
				return base["CertificateKey"];
			}
			set
			{
				base["CertificateKey"] = value;
			}
		}

		public string Endpoint
		{
			get
			{
				return base["Endpoint"];
			}
			set
			{
				base["Endpoint"] = value;
			}
		}

		internal X509Certificate2 NativeCertificate
		{
			get;
			set;
		}

		static ApnsCredential()
		{
			HashSet<string> strs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			strs.Add("gateway.push.apple.com");
			strs.Add("gateway.sandbox.push.apple.com");
			strs.Add("pushtestservice.cloudapp.net");
			strs.Add("pushtestservice2.cloudapp.net");
			strs.Add("pushperfnotificationserver.cloudapp.net");
			strs.Add("pushstressnotificationserver.cloudapp.net");
			strs.Add("pushnotificationserver.cloudapp.net");
			ApnsCredential.validApnsEndpoints = strs;
			HashSet<string> strs1 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			strs1.Add("localhost");
			strs1.Add("127.0.0.1");
			ApnsCredential.validLocalApnsEndpoints = strs1;
		}

		public ApnsCredential()
		{
			this.Endpoint = "gateway.push.apple.com";
		}

		public ApnsCredential(byte[] certificateBuffer, string certificateKey) : this()
		{
			try
			{
				this.ApnsCertificate = Convert.ToBase64String(certificateBuffer);
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new ArgumentException("certificateBuffer", exception);
				}
				throw;
			}
			this.CertificateKey = certificateKey;
		}

		public ApnsCredential(string certificatePath, string certificateKey) : this()
		{
			try
			{
				this.ApnsCertificate = this.GetApnsClientCertificate(certificatePath);
			}
			catch (Exception exception)
			{
				throw new ArgumentException("certificatePath", exception);
			}
			this.CertificateKey = certificateKey;
		}

		public override bool Equals(object other)
		{
			ApnsCredential apnsCredential = other as ApnsCredential;
			if (apnsCredential == null)
			{
				return false;
			}
			if (apnsCredential.CertificateKey != this.CertificateKey)
			{
				return false;
			}
			return apnsCredential.ApnsCertificate == this.ApnsCertificate;
		}

		private string GetApnsClientCertificate(string certPath)
		{
			byte[] array;
			string base64String;
			using (FileStream fileStream = File.OpenRead(certPath))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					fileStream.CopyTo(memoryStream);
					array = memoryStream.ToArray();
				}
				base64String = Convert.ToBase64String(array);
			}
			return base64String;
		}

		public override int GetHashCode()
		{
			if (string.IsNullOrWhiteSpace(this.CertificateKey) || string.IsNullOrWhiteSpace(this.ApnsCertificate))
			{
				return base.GetHashCode();
			}
			if (string.IsNullOrWhiteSpace(this.CertificateKey))
			{
				return this.ApnsCertificate.GetHashCode();
			}
			return this.CertificateKey.GetHashCode() ^ this.ApnsCertificate.GetHashCode();
		}

		internal static bool IsMockApns(string endpoint)
		{
			return endpoint.ToUpperInvariant().Contains("CLOUDAPP.NET");
		}

		internal static bool IsValidApnsEndpoint(string endpoint)
		{
			return ApnsCredential.validApnsEndpoints.Contains(endpoint);
		}

		protected override void OnValidate(bool allowLocalMockPns)
		{
			if (base.Properties == null || base.Properties.Count > 3)
			{
				throw new InvalidDataContractException(SRClient.ApnsRequiredPropertiesError);
			}
			if (string.IsNullOrWhiteSpace(this.ApnsCertificate) || string.IsNullOrWhiteSpace(this.Endpoint))
			{
				throw new InvalidDataContractException(SRClient.ApnsPropertiesNotSpecified);
			}
			if (!ApnsCredential.validApnsEndpoints.Contains(this.Endpoint) && (!allowLocalMockPns || !ApnsCredential.validLocalApnsEndpoints.Contains(this.Endpoint)))
			{
				throw new InvalidDataContractException(SRClient.ApnsEndpointNotAllowed);
			}
			try
			{
				if (this.CertificateKey == null)
				{
					this.NativeCertificate = new X509Certificate2(this.ApnsCertificate);
				}
				else
				{
					this.NativeCertificate = new X509Certificate2(Convert.FromBase64String(this.ApnsCertificate), this.CertificateKey);
				}
				if (!this.NativeCertificate.HasPrivateKey)
				{
					throw new InvalidDataContractException(SRClient.ApnsCertificatePrivatekeyMissing);
				}
				if (DateTime.UtcNow > this.NativeCertificate.NotAfter)
				{
					throw new InvalidDataContractException(SRClient.ApnsCertificateExpired);
				}
				if (DateTime.UtcNow < this.NativeCertificate.NotBefore)
				{
					throw new InvalidDataContractException(SRClient.ApnsCertificateNotValid);
				}
			}
			catch (CryptographicException cryptographicException)
			{
				throw new InvalidDataContractException(SRClient.ApnsCertificateNotUsable(cryptographicException.Message));
			}
			catch (FormatException formatException)
			{
				throw new InvalidDataContractException(SRClient.ApnsCertificateNotUsable(formatException.Message));
			}
		}
	}
}