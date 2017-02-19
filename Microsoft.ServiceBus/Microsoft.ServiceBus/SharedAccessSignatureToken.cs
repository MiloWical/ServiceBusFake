using System;
using System.Collections.Generic;
using System.Web;

namespace Microsoft.ServiceBus
{
	public class SharedAccessSignatureToken : SimpleWebSecurityToken
	{
		public const int MaxKeyNameLength = 256;

		public const int MaxKeyLength = 256;

		public const string SharedAccessSignature = "SharedAccessSignature";

		public const string SignedResource = "sr";

		public const string Signature = "sig";

		public const string SignedKeyName = "skn";

		public const string SignedExpiry = "se";

		public const string SignedResourceFullFieldName = "SharedAccessSignature sr";

		public const string SasKeyValueSeparator = "=";

		public const string SasPairSeparator = "&";

		protected override string AudienceFieldName
		{
			get
			{
				return "SharedAccessSignature sr";
			}
		}

		protected override string ExpiresOnFieldName
		{
			get
			{
				return "se";
			}
		}

		protected override string KeyValueSeparator
		{
			get
			{
				return "=";
			}
		}

		protected override string PairSeparator
		{
			get
			{
				return "&";
			}
		}

		public SharedAccessSignatureToken(string tokenString, DateTime expiry, string audience) : base(tokenString, expiry, audience)
		{
		}

		public SharedAccessSignatureToken(string tokenString, DateTime expiry) : base(tokenString, expiry)
		{
		}

		public SharedAccessSignatureToken(string tokenString) : this(string.Concat("uuid:", Guid.NewGuid().ToString()), tokenString)
		{
		}

		public SharedAccessSignatureToken(string id, string tokenString) : base(id, tokenString)
		{
		}

		private static IDictionary<string, string> ExtractFieldValues(string sharedAccessSignature)
		{
			string[] strArrays = sharedAccessSignature.Split(new char[0]);
			if (!string.Equals(strArrays[0].Trim(), "SharedAccessSignature", StringComparison.OrdinalIgnoreCase) || (int)strArrays.Length != 2)
			{
				throw new ArgumentNullException("sharedAccessSignature");
			}
			IDictionary<string, string> strs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			string str = strArrays[1].Trim();
			string[] strArrays1 = new string[] { "&" };
			string[] strArrays2 = str.Split(strArrays1, StringSplitOptions.None);
			for (int i = 0; i < (int)strArrays2.Length; i++)
			{
				string str1 = strArrays2[i];
				if (str1 != string.Empty)
				{
					string[] strArrays3 = new string[] { "=" };
					string[] strArrays4 = str1.Split(strArrays3, StringSplitOptions.None);
					if (!string.Equals(strArrays4[0], "sr", StringComparison.OrdinalIgnoreCase))
					{
						strs.Add(strArrays4[0], HttpUtility.UrlDecode(strArrays4[1]));
					}
					else
					{
						strs.Add(strArrays4[0], strArrays4[1]);
					}
				}
			}
			return strs;
		}

		internal static void Validate(string sharedAccessSignature)
		{
			string str;
			string str1;
			string str2;
			string str3;
			if (string.IsNullOrEmpty(sharedAccessSignature))
			{
				throw new ArgumentNullException("sharedAccessSignature");
			}
			IDictionary<string, string> strs = SharedAccessSignatureToken.ExtractFieldValues(sharedAccessSignature);
			if (!strs.TryGetValue("sig", out str))
			{
				throw new ArgumentNullException("sig");
			}
			if (!strs.TryGetValue("se", out str1))
			{
				throw new ArgumentNullException("se");
			}
			if (!strs.TryGetValue("skn", out str2))
			{
				throw new ArgumentNullException("skn");
			}
			if (!strs.TryGetValue("sr", out str3))
			{
				throw new ArgumentNullException("sr");
			}
		}
	}
}