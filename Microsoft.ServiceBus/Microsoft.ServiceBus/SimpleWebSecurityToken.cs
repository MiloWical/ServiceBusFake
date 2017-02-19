using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Web;

namespace Microsoft.ServiceBus
{
	public class SimpleWebSecurityToken : SecurityToken
	{
		private const string InternalExpiresOnFieldName = "ExpiresOn";

		private const string InternalAudienceFieldName = "Audience";

		private const string InternalKeyValueSeparator = "=";

		private const string InternalPairSeparator = "&";

		private readonly static Func<string, string> Decoder;

		private readonly static DateTime EpochTime;

		private readonly string id;

		private readonly string token;

		private readonly DateTime validFrom;

		private readonly DateTime validTo;

		private readonly string audience;

		public string Audience
		{
			get
			{
				return this.audience;
			}
		}

		protected virtual string AudienceFieldName
		{
			get
			{
				return "Audience";
			}
		}

		public DateTime ExpiresOn
		{
			get
			{
				return this.validTo;
			}
		}

		protected virtual string ExpiresOnFieldName
		{
			get
			{
				return "ExpiresOn";
			}
		}

		public override string Id
		{
			get
			{
				return this.id;
			}
		}

		protected virtual string KeyValueSeparator
		{
			get
			{
				return "=";
			}
		}

		protected virtual string PairSeparator
		{
			get
			{
				return "&";
			}
		}

		public override ReadOnlyCollection<SecurityKey> SecurityKeys
		{
			get
			{
				return new ReadOnlyCollection<SecurityKey>(new List<SecurityKey>());
			}
		}

		public string Token
		{
			get
			{
				return this.token;
			}
		}

		public override DateTime ValidFrom
		{
			get
			{
				return this.validFrom;
			}
		}

		public override DateTime ValidTo
		{
			get
			{
				return this.validTo;
			}
		}

		static SimpleWebSecurityToken()
		{
			SimpleWebSecurityToken.Decoder = new Func<string, string>(HttpUtility.UrlDecode);
			SimpleWebSecurityToken.EpochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		}

		public SimpleWebSecurityToken(string tokenString, DateTime expiry, string audience)
		{
			if (tokenString == null)
			{
				throw new NullReferenceException("tokenString");
			}
			if (audience == null)
			{
				throw new NullReferenceException("audience");
			}
			Guid guid = Guid.NewGuid();
			this.id = string.Concat("uuid:", guid.ToString());
			this.token = tokenString;
			this.validFrom = DateTime.MinValue;
			this.validTo = expiry;
			this.audience = audience;
		}

		public SimpleWebSecurityToken(string tokenString, DateTime expiry)
		{
			if (tokenString == null)
			{
				throw new NullReferenceException("tokenString");
			}
			Guid guid = Guid.NewGuid();
			this.id = string.Concat("uuid:", guid.ToString());
			this.token = tokenString;
			this.validFrom = DateTime.MinValue;
			this.validTo = expiry;
			this.audience = this.GetAudienceFromToken(tokenString);
		}

		public SimpleWebSecurityToken(string tokenString) : this(string.Concat("uuid:", Guid.NewGuid().ToString()), tokenString)
		{
		}

		public SimpleWebSecurityToken(string id, string tokenString)
		{
			if (id == null)
			{
				throw new NullReferenceException("id");
			}
			if (tokenString == null)
			{
				throw new NullReferenceException("tokenString");
			}
			this.id = id;
			this.token = tokenString;
			this.GetExpirationDateAndAudienceFromToken(tokenString, out this.validTo, out this.audience);
		}

		private static IDictionary<string, string> Decode(string encodedString, Func<string, string> keyDecoder, Func<string, string> valueDecoder, string keyValueSeparator, string pairSeparator)
		{
			IDictionary<string, string> strs = new Dictionary<string, string>();
			foreach (string str in encodedString.Split(new string[] { pairSeparator }, StringSplitOptions.None))
			{
				string[] strArrays = new string[] { keyValueSeparator };
				string[] strArrays1 = str.Split(strArrays, StringSplitOptions.None);
				if ((int)strArrays1.Length != 2)
				{
					throw new FormatException(SRClient.InvalidEncoding);
				}
				strs.Add(keyDecoder(strArrays1[0]), valueDecoder(strArrays1[1]));
			}
			return strs;
		}

		private string GetAudienceFromToken(string token)
		{
			string str;
			if (!SimpleWebSecurityToken.Decode(token, SimpleWebSecurityToken.Decoder, SimpleWebSecurityToken.Decoder, this.KeyValueSeparator, this.PairSeparator).TryGetValue(this.AudienceFieldName, out str))
			{
				throw new FormatException(SRClient.TokenAudience);
			}
			return str;
		}

		private void GetExpirationDateAndAudienceFromToken(string token, out DateTime expiresOn, out string audience)
		{
			string str;
			IDictionary<string, string> strs = SimpleWebSecurityToken.Decode(token, SimpleWebSecurityToken.Decoder, SimpleWebSecurityToken.Decoder, this.KeyValueSeparator, this.PairSeparator);
			if (!strs.TryGetValue(this.ExpiresOnFieldName, out str))
			{
				throw new FormatException(SRClient.TokenExpiresOn);
			}
			if (!strs.TryGetValue(this.AudienceFieldName, out audience))
			{
				throw new FormatException(SRClient.TokenAudience);
			}
			expiresOn = SimpleWebSecurityToken.EpochTime + TimeSpan.FromSeconds(double.Parse(str, CultureInfo.InvariantCulture));
		}
	}
}