using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="SharedAccessAuthorizationRule", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class SharedAccessAuthorizationRule : AuthorizationRule
	{
		private const int SupportedSASKeyLength = 44;

		private const string FixedClaimType = "SharedAccessKey";

		private const string FixedClaimValue = "None";

		public readonly static DataContractSerializer Serializer;

		[DataMember(Name="KeyName", IsRequired=true, Order=1001, EmitDefaultValue=false)]
		internal string InternalKeyName
		{
			get;
			set;
		}

		[DataMember(Name="PrimaryKey", IsRequired=true, Order=1002, EmitDefaultValue=false)]
		internal string InternalPrimaryKey
		{
			get;
			set;
		}

		[DataMember(Name="SecondaryKey", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		internal string InternalSecondaryKey
		{
			get;
			set;
		}

		public sealed override string KeyName
		{
			get
			{
				return this.InternalKeyName;
			}
			set
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new ArgumentNullException("KeyName");
				}
				if (!string.Equals(this.InternalKeyName, HttpUtility.UrlEncode(this.InternalKeyName)))
				{
					throw new ArgumentException(SRCore.SharedAccessAuthorizationRuleKeyContainsInvalidCharacters);
				}
				if (value.Length > 256)
				{
					throw new ArgumentOutOfRangeException("KeyName", SRCore.ArgumentStringTooBig("KeyName", 256));
				}
				this.InternalKeyName = value;
			}
		}

		public string PrimaryKey
		{
			get
			{
				return this.InternalPrimaryKey;
			}
			set
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					throw new ArgumentNullException("PrimaryKey");
				}
				if (value.Length > 256)
				{
					throw new ArgumentOutOfRangeException("PrimaryKey", SRCore.ArgumentStringTooBig("PrimaryKey", 256));
				}
				this.InternalPrimaryKey = value;
			}
		}

		public string SecondaryKey
		{
			get
			{
				return this.InternalSecondaryKey;
			}
			set
			{
				if (string.IsNullOrWhiteSpace(value))
				{
					this.InternalSecondaryKey = value;
				}
				else if (value.Length > 256)
				{
					throw new ArgumentOutOfRangeException("SecondaryKey", SRCore.ArgumentStringTooBig("SecondaryKey", 256));
				}
				this.InternalSecondaryKey = value;
			}
		}

		static SharedAccessAuthorizationRule()
		{
			SharedAccessAuthorizationRule.Serializer = new DataContractSerializer(typeof(SharedAccessAuthorizationRule));
		}

		private SharedAccessAuthorizationRule()
		{
		}

		public SharedAccessAuthorizationRule(string keyName, IEnumerable<AccessRights> rights) : this(keyName, SharedAccessAuthorizationRule.GenerateRandomKey(), SharedAccessAuthorizationRule.GenerateRandomKey(), rights)
		{
		}

		public SharedAccessAuthorizationRule(string keyName, string primaryKey, IEnumerable<AccessRights> rights) : this(keyName, primaryKey, SharedAccessAuthorizationRule.GenerateRandomKey(), rights)
		{
		}

		public SharedAccessAuthorizationRule(string keyName, string primaryKey, string secondaryKey, IEnumerable<AccessRights> rights)
		{
			base.ClaimType = "SharedAccessKey";
			base.ClaimValue = "None";
			this.PrimaryKey = primaryKey;
			this.SecondaryKey = secondaryKey;
			base.Rights = rights;
			this.KeyName = keyName;
		}

		private static bool CheckBase64(string base64EncodedString)
		{
			bool flag;
			try
			{
				Convert.FromBase64String(base64EncodedString);
				flag = true;
			}
			catch (Exception exception)
			{
				flag = false;
			}
			return flag;
		}

		public override bool Equals(object obj)
		{
			if (!base.Equals(obj))
			{
				return false;
			}
			SharedAccessAuthorizationRule sharedAccessAuthorizationRule = (SharedAccessAuthorizationRule)obj;
			if (!string.Equals(this.KeyName, sharedAccessAuthorizationRule.KeyName, StringComparison.OrdinalIgnoreCase) || !string.Equals(this.PrimaryKey, sharedAccessAuthorizationRule.PrimaryKey, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return string.Equals(this.SecondaryKey, sharedAccessAuthorizationRule.SecondaryKey, StringComparison.OrdinalIgnoreCase);
		}

		public static string GenerateRandomKey()
		{
			byte[] numArray = new byte[32];
			using (RNGCryptoServiceProvider rNGCryptoServiceProvider = new RNGCryptoServiceProvider())
			{
				rNGCryptoServiceProvider.GetBytes(numArray);
			}
			return Convert.ToBase64String(numArray);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			string[] keyName = new string[] { this.KeyName, this.PrimaryKey, this.SecondaryKey };
			string[] strArrays = keyName;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str = strArrays[i];
				if (!string.IsNullOrEmpty(str))
				{
					hashCode = hashCode + str.GetHashCode();
				}
			}
			return hashCode;
		}

		private static bool IsValidCombinationOfRights(IEnumerable<AccessRights> rights)
		{
			if (!rights.Contains<AccessRights>(AccessRights.Manage))
			{
				return true;
			}
			return rights.Count<AccessRights>() == 3;
		}

		protected override void OnValidate()
		{
			if (string.IsNullOrEmpty(this.InternalKeyName) || !string.Equals(this.InternalKeyName, HttpUtility.UrlEncode(this.InternalKeyName)))
			{
				throw new InvalidDataContractException(SRCore.SharedAccessAuthorizationRuleKeyContainsInvalidCharacters);
			}
			if (this.InternalKeyName.Length > 256)
			{
				throw new InvalidDataContractException(SRCore.SharedAccessAuthorizationRuleKeyNameTooBig(256));
			}
			if (string.IsNullOrEmpty(this.InternalPrimaryKey))
			{
				throw new InvalidDataContractException(SRCore.SharedAccessAuthorizationRuleRequiresPrimaryKey);
			}
			if (Encoding.ASCII.GetByteCount(this.InternalPrimaryKey) != 44)
			{
				throw new InvalidDataContractException(SRCore.SharedAccessRuleAllowsFixedLengthKeys(44));
			}
			if (!SharedAccessAuthorizationRule.CheckBase64(this.InternalPrimaryKey))
			{
				throw new InvalidDataContractException(SRCore.SharedAccessKeyShouldbeBase64);
			}
			if (!string.IsNullOrEmpty(this.InternalSecondaryKey))
			{
				if (Encoding.ASCII.GetByteCount(this.InternalSecondaryKey) != 44)
				{
					throw new InvalidDataContractException(SRCore.SharedAccessRuleAllowsFixedLengthKeys(44));
				}
				if (!SharedAccessAuthorizationRule.CheckBase64(this.InternalSecondaryKey))
				{
					throw new InvalidDataContractException(SRCore.SharedAccessKeyShouldbeBase64);
				}
			}
			if (!SharedAccessAuthorizationRule.IsValidCombinationOfRights(base.Rights))
			{
				throw new InvalidDataContractException(SRClient.InvalidCombinationOfManageRight);
			}
		}

		protected override void ValidateRights(IEnumerable<AccessRights> value)
		{
			base.ValidateRights(value);
			if (!SharedAccessAuthorizationRule.IsValidCombinationOfRights(value))
			{
				throw new ArgumentException(SRClient.InvalidCombinationOfManageRight);
			}
		}
	}
}