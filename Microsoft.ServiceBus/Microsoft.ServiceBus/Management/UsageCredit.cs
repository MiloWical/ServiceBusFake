using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Microsoft.ServiceBus.Management
{
	[DataContract(Name="UsageCredit", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class UsageCredit : IExtensibleDataObject, IEquatable<UsageCredit>
	{
		private const char ConnectChar = '\u005F';

		public readonly static DataContractSerializer Serializer;

		public ExtensionDataObject ExtensionData
		{
			get;
			set;
		}

		internal bool Hidden
		{
			get;
			set;
		}

		internal long Id
		{
			get;
			set;
		}

		[DataMember(Name="Identifier", IsRequired=true, Order=1003, EmitDefaultValue=false)]
		public string Identifier
		{
			get;
			set;
		}

		[DataMember(Name="KeyName", IsRequired=false, Order=1001, EmitDefaultValue=false)]
		public string KeyName
		{
			get
			{
				return string.Concat(this.RequestorService, '\u005F', this.Identifier);
			}
			set
			{
			}
		}

		internal string NamespaceName
		{
			get;
			set;
		}

		[DataMember(Name="NHBasicUnit", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		public int? NHBasicUnit
		{
			get;
			set;
		}

		[DataMember(Name="NHStandardUnit", IsRequired=false, Order=1005, EmitDefaultValue=false)]
		public int? NHStandardUnit
		{
			get;
			set;
		}

		[DataMember(Name="RequestorService", IsRequired=true, Order=1002, EmitDefaultValue=false)]
		public string RequestorService
		{
			get;
			set;
		}

		[DataMember(Name="Revision", IsRequired=false, Order=1006, EmitDefaultValue=true)]
		public long Revision
		{
			get;
			set;
		}

		internal string SubscriptionId
		{
			get;
			set;
		}

		[DataMember(Name="UpdatedAt", IsRequired=false, Order=1007, EmitDefaultValue=false)]
		public DateTime UpdatedAt
		{
			get;
			set;
		}

		static UsageCredit()
		{
			UsageCredit.Serializer = new DataContractSerializer(typeof(UsageCredit));
		}

		public UsageCredit()
		{
		}

		public bool Equals(UsageCredit other)
		{
			if (this.Id != (long)0 && other.Id != (long)0)
			{
				return this.Id == other.Id;
			}
			if (!this.SubscriptionId.Equals(other.SubscriptionId, StringComparison.OrdinalIgnoreCase) || !this.NamespaceName.Equals(other.NamespaceName, StringComparison.OrdinalIgnoreCase) || !this.RequestorService.Equals(other.RequestorService, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return this.Identifier.Equals(other.Identifier, StringComparison.OrdinalIgnoreCase);
		}

		public override string ToString()
		{
			string str;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				XmlWriterSettings xmlWriterSetting = new XmlWriterSettings()
				{
					Encoding = Encoding.UTF8
				};
				using (XmlWriter xmlWriter = XmlWriter.Create(memoryStream, xmlWriterSetting))
				{
					UsageCredit.Serializer.WriteObject(memoryStream, this);
					memoryStream.Flush();
					str = Encoding.UTF8.GetString(memoryStream.ToArray());
				}
			}
			return str;
		}

		internal static bool TryParseKey(string key, out string identifier, out string requestorService)
		{
			string[] strArrays = key.Split(new char[] { '\u005F' });
			if ((int)strArrays.Length != 2)
			{
				identifier = null;
				requestorService = null;
				return false;
			}
			requestorService = strArrays[0];
			identifier = strArrays[1];
			return true;
		}
	}
}