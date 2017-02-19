using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="PropertyReference", IsReference=true, Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class PropertyReference
	{
		[DataMember(Name="PropertyName", Order=65537, IsRequired=false, EmitDefaultValue=false)]
		public QualifiedPropertyName PropertyName
		{
			get;
			private set;
		}

		[DataMember(Name="Value", Order=65538, IsRequired=false, EmitDefaultValue=false)]
		public object Value
		{
			get;
			set;
		}

		public PropertyReference(string propertyName)
		{
			this.PropertyName = new QualifiedPropertyName(PropertyScope.User, propertyName);
		}

		public PropertyReference(PropertyScope scope, string propertyName)
		{
			this.PropertyName = new QualifiedPropertyName(scope, propertyName);
		}

		public PropertyReference(QualifiedPropertyName propertyName)
		{
			this.PropertyName = propertyName;
		}

		public static T GetValue<T>(object propertyReferenceOrValue)
		{
			T t;
			PropertyReference propertyReference = propertyReferenceOrValue as PropertyReference;
			t = (propertyReference == null ? (T)propertyReferenceOrValue : (T)propertyReference.Value);
			return t;
		}

		public override string ToString()
		{
			return this.PropertyName.ToString();
		}
	}
}