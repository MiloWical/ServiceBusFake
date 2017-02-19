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
	[DataContract(Name="MpnsCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class MpnsCredential : PnsCredential
	{
		internal const string AppPlatformName = "windowsphone";

		internal override string AppPlatform
		{
			get
			{
				return "windowsphone";
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

		public string MpnsCertificate
		{
			get
			{
				return base["MpnsCertificate"];
			}
			set
			{
				base["MpnsCertificate"] = value;
			}
		}

		internal X509Certificate2 NativeCertificate
		{
			get;
			set;
		}

		public MpnsCredential()
		{
		}

		public MpnsCredential(X509Certificate mpnsCertificate, string certificateKey) : this(MpnsCredential.ExportCertificateBytes(mpnsCertificate, certificateKey), certificateKey)
		{
		}

		public MpnsCredential(byte[] certificateBuffer, string certificateKey) : this()
		{
			try
			{
				this.MpnsCertificate = Convert.ToBase64String(certificateBuffer);
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

		public MpnsCredential(string certificatePath, string certificateKey) : this()
		{
			try
			{
				this.MpnsCertificate = this.GetMpnsClientCertificate(certificatePath);
			}
			catch (Exception exception)
			{
				throw new ArgumentException("certificatePath", exception);
			}
			this.CertificateKey = certificateKey;
		}

		public override bool Equals(object other)
		{
			MpnsCredential mpnsCredential = other as MpnsCredential;
			if (mpnsCredential == null)
			{
				return false;
			}
			if (mpnsCredential.CertificateKey != this.CertificateKey)
			{
				return false;
			}
			return mpnsCredential.MpnsCertificate == this.MpnsCertificate;
		}

		private static byte[] ExportCertificateBytes(X509Certificate mpnsCertificate, string certificateKey)
		{
			if (mpnsCertificate == null)
			{
				throw new ArgumentNullException("mpnsCertificate");
			}
			if (string.IsNullOrEmpty(certificateKey))
			{
				throw new ArgumentNullException("certificateKey");
			}
			return mpnsCertificate.Export(X509ContentType.Pfx, certificateKey);
		}

		public override int GetHashCode()
		{
			if (string.IsNullOrWhiteSpace(this.CertificateKey) || string.IsNullOrWhiteSpace(this.MpnsCertificate))
			{
				return base.GetHashCode();
			}
			if (string.IsNullOrWhiteSpace(this.CertificateKey))
			{
				return this.MpnsCertificate.GetHashCode();
			}
			return this.CertificateKey.GetHashCode() ^ this.MpnsCertificate.GetHashCode();
		}

		private string GetMpnsClientCertificate(string certPath)
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

		protected override void OnValidate(bool allowLocalMockPns)
		{
			if (base.Properties != null && base.Properties.Count > 0)
			{
				if (base.Properties.Count > 2)
				{
					throw new InvalidDataContractException(SRClient.MpnsRequiredPropertiesError);
				}
				if (base.Properties.Count < 2 || string.IsNullOrWhiteSpace(this.MpnsCertificate))
				{
					throw new InvalidDataContractException(SRClient.MpnsInvalidPropeties);
				}
				try
				{
					X509Certificate2 x509Certificate2 = null;
					x509Certificate2 = (this.CertificateKey == null ? new X509Certificate2(this.MpnsCertificate) : new X509Certificate2(Convert.FromBase64String(this.MpnsCertificate), this.CertificateKey));
					if (!x509Certificate2.HasPrivateKey)
					{
						throw new InvalidDataContractException(SRClient.MpnsCertificatePrivatekeyMissing);
					}
					if (DateTime.Now > x509Certificate2.NotAfter)
					{
						throw new InvalidDataContractException(SRClient.MpnsCertificateExpired);
					}
					if (DateTime.Now < x509Certificate2.NotBefore)
					{
						throw new InvalidDataContractException(SRClient.InvalidMpnsCertificate);
					}
				}
				catch (CryptographicException cryptographicException)
				{
					throw new InvalidDataContractException(SRClient.MpnsCertificateError(cryptographicException.Message));
				}
				catch (FormatException formatException)
				{
					throw new InvalidDataContractException(SRClient.MpnsCertificateError(formatException.Message));
				}
			}
		}
	}
}