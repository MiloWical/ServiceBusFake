using Microsoft.ServiceBus.Channels;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Claims;
using System.IO;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text.RegularExpressions;

namespace Microsoft.ServiceBus
{
	internal static class SecureSocketUtil
	{
		public static IAsyncResult BeginInitiateSecureClientUpgradeIfNeeded(IConnection connection, IDefaultCommunicationTimeouts defaultTimeouts, SocketSecurityRole socketSecurityMode, string targetHost, TimeSpan timeout, AsyncCallback callback, object state)
		{
			SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult initiateSecureClientUpgradeClientSideAsyncResult = new SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult(connection, defaultTimeouts, socketSecurityMode, targetHost, timeout, callback, state);
			return initiateSecureClientUpgradeClientSideAsyncResult.Start();
		}

		public static bool CertificateCheckSubjectAlternativeNames(X509Extension extensions, string hostName)
		{
			if (extensions != null)
			{
				string[] strArrays = Regex.Split(extensions.Format(true), Environment.NewLine);
				string[] strArrays1 = strArrays;
				for (int i = 0; i < (int)strArrays1.Length; i++)
				{
					string str = strArrays1[i];
					if (!string.IsNullOrEmpty(str))
					{
						string[] strArrays2 = str.Trim().Split(new char[] { '=' });
						if (strArrays2[0].Trim().Equals("DNS Name") && LenientDnsIdentityVerifier.CheckTopLevelDomainCompatibleness(strArrays2[1].Trim(), hostName))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool CustomizedCertificateValidator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, string hostName)
		{
			bool flag;
			if (sslPolicyErrors == SslPolicyErrors.None)
			{
				return true;
			}
			if (sslPolicyErrors != SslPolicyErrors.RemoteCertificateNameMismatch)
			{
				return false;
			}
			X509Certificate2 x509Certificate2 = certificate as X509Certificate2;
			Fx.AssertAndThrow(x509Certificate2 != null, "CustomizedCertificateValidator received an invalid certificate");
			try
			{
				foreach (Claim claim in new X509CertificateClaimSet(x509Certificate2))
				{
					if (!(claim.ClaimType == ClaimTypes.Dns) || !LenientDnsIdentityVerifier.CheckTopLevelDomainCompatibleness(claim.Resource.ToString(), hostName))
					{
						continue;
					}
					flag = true;
					return flag;
				}
				flag = SecureSocketUtil.CertificateCheckSubjectAlternativeNames(x509Certificate2.Extensions["2.5.29.17"], hostName);
			}
			catch (Exception exception)
			{
				if (Fx.IsFatal(exception))
				{
					throw;
				}
				return false;
			}
			return flag;
		}

		public static IConnection EndInitiateSecureClientUpgradeIfNeeded(IAsyncResult result)
		{
			return AsyncResult<SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult>.End(result).ResultConnection;
		}

		public static IConnection InitiateSecureClientUpgradeIfNeeded(IConnection connection, IDefaultCommunicationTimeouts defaultTimeouts, SocketSecurityRole socketSecurityMode, string targetHost, TimeSpan timeout)
		{
			SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult initiateSecureClientUpgradeClientSideAsyncResult = new SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult(connection, defaultTimeouts, socketSecurityMode, targetHost, timeout, null, null);
			initiateSecureClientUpgradeClientSideAsyncResult.RunSynchronously();
			return initiateSecureClientUpgradeClientSideAsyncResult.ResultConnection;
		}

		private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			if (sslPolicyErrors == SslPolicyErrors.None)
			{
				return true;
			}
			return false;
		}

		private class InitiateSecureClientUpgradeClientSideAsyncResult : IteratorAsyncResult<SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult>
		{
			private readonly IDefaultCommunicationTimeouts defaultTimeouts;

			private readonly SocketSecurityRole socketSecurityMode;

			private readonly string targetHost;

			private readonly IConnection connection;

			private SslStream sslStream;

			public IConnection ResultConnection
			{
				get;
				private set;
			}

			public InitiateSecureClientUpgradeClientSideAsyncResult(IConnection connection, IDefaultCommunicationTimeouts defaultTimeouts, SocketSecurityRole socketSecurityMode, string targetHost, TimeSpan timeout, AsyncCallback callback, object state) : base(timeout, callback, state)
			{
				this.connection = connection;
				this.defaultTimeouts = defaultTimeouts;
				this.socketSecurityMode = socketSecurityMode;
				this.targetHost = targetHost;
			}

			protected override IEnumerator<IteratorAsyncResult<SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult>.AsyncStep> GetAsyncSteps()
			{
				ConnectionStream connectionStream1;
				if (this.socketSecurityMode != SocketSecurityRole.SslClient)
				{
					this.ResultConnection = this.connection;
					SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult initiateSecureClientUpgradeClientSideAsyncResult = this;
					IteratorAsyncResult<SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult>.BeginCall beginCall = (SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.ResultConnection.BeginWrite(ConnectConstants.NoSsl, 0, 1, true, t, c, s);
					yield return initiateSecureClientUpgradeClientSideAsyncResult.CallAsync(beginCall, (SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult thisRef, IAsyncResult r) => thisRef.ResultConnection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
				}
				else
				{
					connectionStream1 = (this.defaultTimeouts == null ? new ConnectionStream(this.connection) : new ConnectionStream(this.connection, this.defaultTimeouts));
					ConnectionStream connectionStream = connectionStream1;
					SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult initiateSecureClientUpgradeClientSideAsyncResult1 = this;
					IteratorAsyncResult<SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult>.BeginCall beginCall1 = (SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.connection.BeginWrite(ConnectConstants.UseSsl, 0, 1, true, t, c, s);
					yield return initiateSecureClientUpgradeClientSideAsyncResult1.CallAsync(beginCall1, (SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult thisRef, IAsyncResult r) => thisRef.connection.EndWrite(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Transfer);
					this.sslStream = new SslStream(connectionStream, false, new RemoteCertificateValidationCallback(SecureSocketUtil.ValidateServerCertificate), null);
					SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult initiateSecureClientUpgradeClientSideAsyncResult2 = this;
					IteratorAsyncResult<SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult>.BeginCall beginCall2 = (SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult thisRef, TimeSpan t, AsyncCallback c, object s) => thisRef.sslStream.BeginAuthenticateAsClient(this.targetHost, c, s);
					yield return initiateSecureClientUpgradeClientSideAsyncResult2.CallAsync(beginCall2, (SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult thisRef, IAsyncResult r) => thisRef.sslStream.EndAuthenticateAsClient(r), IteratorAsyncResult<TIteratorAsyncResult>.ExceptionPolicy.Continue);
					if (base.LastAsyncStepException == null)
					{
						this.ResultConnection = new StreamConnection(this.sslStream, connectionStream);
					}
					else
					{
						try
						{
							this.sslStream.Close();
						}
						catch (Exception exception1)
						{
							Exception exception = exception1;
							if (Fx.IsFatal(exception))
							{
								throw;
							}
							Fx.Exception.TraceHandled(exception, "SecureSocketUtil.InitiateSecureClientUpgradeClientSideAsyncResult", null);
						}
						base.Complete(base.LastAsyncStepException);
					}
				}
			}
		}
	}
}