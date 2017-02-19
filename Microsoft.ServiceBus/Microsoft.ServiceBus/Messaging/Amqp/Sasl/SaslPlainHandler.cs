using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
	internal sealed class SaslPlainHandler : SaslHandler
	{
		public readonly static string Name;

		private readonly static string InvalidCredential;

		private ISaslPlainAuthenticator authenticator;

		public string AuthenticationIdentity
		{
			get;
			set;
		}

		public string AuthorizationIdentity
		{
			get;
			set;
		}

		public string Password
		{
			get;
			set;
		}

		static SaslPlainHandler()
		{
			SaslPlainHandler.Name = "PLAIN";
			SaslPlainHandler.InvalidCredential = "Invalid user name or password.";
		}

		public SaslPlainHandler()
		{
			base.Mechanism = SaslPlainHandler.Name;
		}

		public SaslPlainHandler(ISaslPlainAuthenticator authenticator) : this()
		{
			this.authenticator = authenticator;
		}

		public override SaslHandler Clone()
		{
			SaslPlainHandler saslPlainHandler = new SaslPlainHandler(this.authenticator)
			{
				AuthorizationIdentity = this.AuthorizationIdentity,
				AuthenticationIdentity = this.AuthenticationIdentity,
				Password = this.Password
			};
			return saslPlainHandler;
		}

		private string GetClientMessage()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] authorizationIdentity = new object[] { this.AuthorizationIdentity, this.AuthenticationIdentity, this.Password };
			return string.Format(invariantCulture, "{0}\0{1}\0{2}", authorizationIdentity);
		}

		public override void OnChallenge(SaslChallenge challenge)
		{
			throw new NotImplementedException();
		}

		private void OnInit(SaslInit init)
		{
			string str = null;
			if (init.InitialResponse.Count > 0)
			{
				System.Text.Encoding uTF8 = System.Text.Encoding.UTF8;
				byte[] array = init.InitialResponse.Array;
				int offset = init.InitialResponse.Offset;
				ArraySegment<byte> initialResponse = init.InitialResponse;
				string str1 = uTF8.GetString(array, offset, initialResponse.Count);
				string[] strArrays = str1.Split(new char[1]);
				if ((int)strArrays.Length != 3)
				{
					throw new UnauthorizedAccessException(SaslPlainHandler.InvalidCredential);
				}
				this.AuthorizationIdentity = strArrays[0];
				this.AuthenticationIdentity = strArrays[1];
				str = strArrays[2];
			}
			if (string.IsNullOrEmpty(this.AuthenticationIdentity))
			{
				throw new UnauthorizedAccessException(SaslPlainHandler.InvalidCredential);
			}
			if (this.authenticator != null)
			{
				base.Principal = this.authenticator.Authenticate(this.AuthenticationIdentity, str);
			}
			base.Negotiator.CompleteNegotiation(SaslCode.Ok, null);
		}

		public override void OnResponse(SaslResponse response)
		{
			throw new NotImplementedException();
		}

		protected override void OnStart(SaslInit init, bool isClient)
		{
			if (!isClient)
			{
				this.OnInit(init);
				return;
			}
			string clientMessage = this.GetClientMessage();
			init.InitialResponse = new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(clientMessage));
			base.Negotiator.WriteFrame(init, true);
		}
	}
}