using Microsoft.ServiceBus.Common;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="QualifiedPropertyName", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal struct QualifiedPropertyName : IEquatable<QualifiedPropertyName>, IComparable<QualifiedPropertyName>
	{
		[DataMember(Name="Scope", EmitDefaultValue=true, IsRequired=true, Order=65537)]
		private readonly PropertyScope scope;

		[DataMember(Name="Name", EmitDefaultValue=true, IsRequired=true, Order=65538)]
		private readonly string name;

		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public PropertyScope Scope
		{
			get
			{
				return this.scope;
			}
		}

		public QualifiedPropertyName(PropertyScope scope, string name)
		{
			this.scope = scope;
			this.name = name;
		}

		public int CompareTo(QualifiedPropertyName other)
		{
			int num = this.scope.CompareTo((int)other.scope);
			if (num == 0)
			{
				num = StringComparer.OrdinalIgnoreCase.Compare(this.name, other.name);
			}
			return num;
		}

		public bool Equals(QualifiedPropertyName other)
		{
			if (this.scope != other.scope)
			{
				return false;
			}
			return StringComparer.OrdinalIgnoreCase.Equals(this.name, other.name);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is QualifiedPropertyName))
			{
				return false;
			}
			return this.Equals((QualifiedPropertyName)obj);
		}

		public override int GetHashCode()
		{
			int hashCode = this.scope.GetHashCode();
			int num = StringComparer.OrdinalIgnoreCase.GetHashCode(this.name);
			return HashCode.CombineHashCodes(hashCode, num);
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] objArray = new object[] { this.scope, this.name };
			return string.Format(invariantCulture, "{0}.{1}", objArray);
		}
	}
}