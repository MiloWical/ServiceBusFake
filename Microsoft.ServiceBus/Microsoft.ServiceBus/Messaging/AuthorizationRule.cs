using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(AllowRule))]
	[KnownType(typeof(SharedAccessAuthorizationRule))]
	public abstract class AuthorizationRule
	{
		public const string NameIdentifierClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

		public const string ShortNameIdentifierClaimType = "nameidentifier";

		public const string UpnClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";

		public const string ShortUpnClaimType = "upn";

		public const string RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

		public const string RoleRoleClaimType = "role";

		public const string SharedAccessKeyClaimType = "sharedaccesskey";

		public string ClaimType
		{
			get
			{
				return this.InternalClaimType;
			}
			set
			{
				this.InternalClaimType = value;
			}
		}

		public string ClaimValue
		{
			get
			{
				return this.InternalClaimValue;
			}
			set
			{
				this.InternalClaimValue = value;
			}
		}

		[DataMember(IsRequired=false, Order=1006, EmitDefaultValue=false)]
		public DateTime CreatedTime
		{
			get;
			private set;
		}

		[DataMember(Name="ClaimType", IsRequired=false, Order=1003, EmitDefaultValue=false)]
		internal string InternalClaimType
		{
			get;
			set;
		}

		[DataMember(Name="ClaimValue", IsRequired=true, Order=1004, EmitDefaultValue=false)]
		internal string InternalClaimValue
		{
			get;
			set;
		}

		[DataMember(Name="IssuerName", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		internal string InternalIssuerName
		{
			get;
			set;
		}

		[DataMember(Name="Rights", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		internal IEnumerable<AccessRights> InternalRights
		{
			get;
			set;
		}

		public string IssuerName
		{
			get
			{
				return this.InternalIssuerName;
			}
			set
			{
				this.InternalIssuerName = value;
			}
		}

		public abstract string KeyName
		{
			get;
			set;
		}

		[DataMember(IsRequired=false, Order=1007, EmitDefaultValue=false)]
		public DateTime ModifiedTime
		{
			get;
			private set;
		}

		[DataMember(IsRequired=false, Order=1008, EmitDefaultValue=false)]
		public long Revision
		{
			get;
			set;
		}

		public IEnumerable<AccessRights> Rights
		{
			get
			{
				return this.InternalRights;
			}
			set
			{
				this.ValidateRights(value);
				this.InternalRights = value;
			}
		}

		internal AuthorizationRule()
		{
			this.CreatedTime = DateTime.UtcNow;
			this.ModifiedTime = DateTime.UtcNow;
			this.Revision = (long)0;
		}

		private static bool AreAccessRightsUnique(IEnumerable<AccessRights> rights)
		{
			HashSet<AccessRights> accessRights = new HashSet<AccessRights>(rights);
			return rights.Count<AccessRights>() == accessRights.Count;
		}

		public virtual AuthorizationRule Clone()
		{
			return (AuthorizationRule)this.MemberwiseClone();
		}

		public override bool Equals(object obj)
		{
			if (this.GetType() != obj.GetType())
			{
				return false;
			}
			AuthorizationRule authorizationRule = (AuthorizationRule)obj;
			if (!string.Equals(this.IssuerName, authorizationRule.IssuerName, StringComparison.OrdinalIgnoreCase) || !string.Equals(this.ClaimType, authorizationRule.ClaimType, StringComparison.OrdinalIgnoreCase) || !string.Equals(this.ClaimValue, authorizationRule.ClaimValue, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (this.Rights != null && authorizationRule.Rights == null || this.Rights == null && authorizationRule.Rights != null)
			{
				return false;
			}
			if (this.Rights == null || authorizationRule.Rights == null)
			{
				return true;
			}
			HashSet<AccessRights> accessRights = new HashSet<AccessRights>(this.Rights);
			HashSet<AccessRights> accessRights1 = new HashSet<AccessRights>(authorizationRule.Rights);
			if (accessRights1.Count != accessRights.Count)
			{
				return false;
			}
			return accessRights.All<AccessRights>(new Func<AccessRights, bool>(accessRights1.Contains));
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			string[] issuerName = new string[] { this.IssuerName, this.ClaimValue, this.ClaimType };
			string[] strArrays = issuerName;
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

		internal void MarkModified()
		{
			this.ModifiedTime = DateTime.UtcNow;
			AuthorizationRule revision = this;
			revision.Revision = revision.Revision + (long)1;
		}

		protected virtual void OnValidate()
		{
		}

		internal void Validate()
		{
			if (this.Rights == null || !this.Rights.Any<AccessRights>() || this.Rights.Count<AccessRights>() > 3)
			{
				throw new InvalidDataContractException(SRClient.NullEmptyRights(3));
			}
			if (!AuthorizationRule.AreAccessRightsUnique(this.Rights))
			{
				throw new InvalidDataContractException(SRClient.CannotHaveDuplicateAccessRights);
			}
			this.OnValidate();
		}

		protected virtual void ValidateRights(IEnumerable<AccessRights> value)
		{
			if (value == null || !value.Any<AccessRights>() || value.Count<AccessRights>() > 3)
			{
				throw new ArgumentException(SRClient.NullEmptyRights(3));
			}
			if (!AuthorizationRule.AreAccessRightsUnique(value))
			{
				throw new ArgumentException(SRClient.CannotHaveDuplicateAccessRights);
			}
		}
	}
}