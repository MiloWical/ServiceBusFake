using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	[DataContract(Name="PnsCredential", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(AdmCredential))]
	[KnownType(typeof(ApnsCredential))]
	[KnownType(typeof(BaiduCredential))]
	[KnownType(typeof(GcmCredential))]
	[KnownType(typeof(MpnsCredential))]
	[KnownType(typeof(NokiaXCredential))]
	[KnownType(typeof(SmtpCredential))]
	[KnownType(typeof(WnsCredential))]
	public abstract class PnsCredential : EntityDescription
	{
		internal abstract string AppPlatform
		{
			get;
		}

		[DataMember(Name="BlockedOn", IsRequired=false, EmitDefaultValue=false)]
		public DateTime? BlockedOn
		{
			get;
			set;
		}

		protected string this[string name]
		{
			get
			{
				if (!this.Properties.ContainsKey(name))
				{
					return null;
				}
				return this.Properties[name];
			}
			set
			{
				if (this.Properties.ContainsKey(name))
				{
					this.Properties[name] = value;
					return;
				}
				this.Properties.Add(name, value);
			}
		}

		[DataMember(IsRequired=true)]
		public PnsCredentialProperties Properties
		{
			get;
			set;
		}

		internal PnsCredential()
		{
			this.Properties = new PnsCredentialProperties();
		}

		public static bool IsEqual(PnsCredential cred1, PnsCredential cred2)
		{
			if (cred1 != null && cred2 != null)
			{
				return cred1.Equals(cred2);
			}
			return cred1 == cred2;
		}

		protected virtual void OnValidate(bool allowLocalMockPns)
		{
		}

		internal void Validate(bool allowLocalMockPns)
		{
			this.OnValidate(allowLocalMockPns);
		}
	}
}