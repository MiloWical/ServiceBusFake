using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=0L)]
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(AdmRegistrationDescription))]
	[KnownType(typeof(AdmTemplateRegistrationDescription))]
	[KnownType(typeof(AppleRegistrationDescription))]
	[KnownType(typeof(AppleTemplateRegistrationDescription))]
	[KnownType(typeof(BaiduRegistrationDescription))]
	[KnownType(typeof(BaiduTemplateRegistrationDescription))]
	[KnownType(typeof(EmailRegistrationDescription))]
	[KnownType(typeof(GcmRegistrationDescription))]
	[KnownType(typeof(GcmTemplateRegistrationDescription))]
	[KnownType(typeof(MpnsRegistrationDescription))]
	[KnownType(typeof(MpnsTemplateRegistrationDescription))]
	[KnownType(typeof(NokiaXRegistrationDescription))]
	[KnownType(typeof(NokiaXTemplateRegistrationDescription))]
	[KnownType(typeof(WindowsRegistrationDescription))]
	[KnownType(typeof(WindowsTemplateRegistrationDescription))]
	public abstract class RegistrationDescription : EntityDescription, IResourceDescription
	{
		internal const string TemplateRegistrationType = "template";

		internal static Regex SingleTagRegex;

		internal static Regex TagRegex;

		internal static string[] RegistrationRange;

		private string channelHash;

		internal abstract string AppPlatForm
		{
			get;
		}

		internal long DatabaseId
		{
			get;
			set;
		}

		internal int DbVersion
		{
			get;
			set;
		}

		[DataMember(Name="ETag", IsRequired=false, Order=1001, EmitDefaultValue=false)]
		public string ETag
		{
			get;
			internal set;
		}

		[AmqpMember(Order=2, Mandatory=false)]
		[DataMember(Name="ExpirationTime", IsRequired=false, Order=1002, EmitDefaultValue=false)]
		public DateTime? ExpirationTime
		{
			get;
			internal set;
		}

		internal string FormattedETag
		{
			get
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] eTag = new object[] { this.ETag };
				return string.Format(invariantCulture, "W/\"{0}\"", eTag);
			}
		}

		internal bool InvalidTags
		{
			get;
			private set;
		}

		string Microsoft.ServiceBus.Messaging.IResourceDescription.CollectionName
		{
			get
			{
				return "Registrations";
			}
		}

		internal string Namespace
		{
			get;
			set;
		}

		internal long NotificationHubId
		{
			get;
			set;
		}

		internal string NotificationHubPath
		{
			get;
			set;
		}

		internal string NotificationHubRuntimeUrl
		{
			get;
			set;
		}

		internal abstract string PlatformType
		{
			get;
		}

		[AmqpMember(Order=0, Mandatory=false)]
		[DataMember(Name="RegistrationId", Order=1003, IsRequired=false)]
		public string RegistrationId
		{
			get;
			set;
		}

		internal abstract string RegistrationType
		{
			get;
		}

		public ISet<string> Tags
		{
			get;
			set;
		}

		[AmqpMember(Order=1, Mandatory=false)]
		[DataMember(Name="Tags", IsRequired=false, Order=1004, EmitDefaultValue=false)]
		internal string TagsString
		{
			get
			{
				if (this.Tags == null || this.Tags.Count <= 0)
				{
					return null;
				}
				StringBuilder stringBuilder = new StringBuilder();
				string[] array = this.Tags.ToArray<string>();
				for (int i = 0; i < (int)array.Length; i++)
				{
					stringBuilder.Append(array[i]);
					if (i < (int)array.Length - 1)
					{
						stringBuilder.Append(",");
					}
				}
				return stringBuilder.ToString();
			}
			set
			{
				ISet<string> strs;
				this.InvalidTags = (string.IsNullOrEmpty(value) ? false : !RegistrationDescription.ValidateTags(value));
				if (string.IsNullOrEmpty(value) || this.InvalidTags)
				{
					strs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				}
				else
				{
					char[] chrArray = new char[] { ',' };
					strs = new HashSet<string>(value.Split(chrArray), StringComparer.OrdinalIgnoreCase);
				}
				this.Tags = strs;
				if (!this.InvalidTags)
				{
					foreach (string tag in this.Tags)
					{
						if (tag.Length <= Microsoft.ServiceBus.Messaging.Constants.MaximumTagSize)
						{
							continue;
						}
						this.InvalidTags = true;
						break;
					}
				}
			}
		}

		static RegistrationDescription()
		{
			RegistrationDescription.SingleTagRegex = new Regex("^[a-zA-Z0-9-_@#.:]+$");
			RegistrationDescription.TagRegex = new Regex("^([a-zA-Z0-9-_@#.:]+)(,[a-zA-Z0-9-_@#.:]+)*$");
			string[] strArrays = new string[] { "_", "1", "2", "3", "4", "5", "6", "7", "9", "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "Y", "ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ" };
			RegistrationDescription.RegistrationRange = strArrays;
		}

		public RegistrationDescription(RegistrationDescription registration)
		{
			this.NotificationHubPath = registration.NotificationHubPath;
			this.RegistrationId = registration.RegistrationId;
			this.Tags = registration.Tags;
			this.ETag = registration.ETag;
		}

		internal RegistrationDescription(string notificationHubPath)
		{
			this.NotificationHubPath = notificationHubPath;
		}

		internal RegistrationDescription(string notificationHubPath, string registrationId)
		{
			this.NotificationHubPath = notificationHubPath;
			this.RegistrationId = registrationId;
		}

		internal abstract RegistrationDescription Clone();

		internal static string ComputeChannelHash(string pnsHandle)
		{
			ASCIIEncoding aSCIIEncoding = new ASCIIEncoding();
			SHA1 sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
			return RegistrationDescription.GenerateUrlSafeBase64(sHA1CryptoServiceProvider.ComputeHash(aSCIIEncoding.GetBytes(pnsHandle)));
		}

		public static RegistrationDescription Deserialize(string descriptionString)
		{
			RegistrationDescription registrationDescription;
			DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(RegistrationDescription));
			using (XmlReader xmlReader = XmlReader.Create(new StringReader(descriptionString)))
			{
				registrationDescription = (RegistrationDescription)dataContractSerializer.ReadObject(xmlReader);
			}
			return registrationDescription;
		}

		protected static string GenerateUrlSafeBase64(byte[] hash)
		{
			string base64String = Convert.ToBase64String(hash);
			char[] chrArray = new char[] { '=' };
			return base64String.TrimEnd(chrArray).Replace('+', '-').Replace('/', '\u005F');
		}

		internal string GetChannelHash()
		{
			if (string.IsNullOrEmpty(this.channelHash))
			{
				this.channelHash = RegistrationDescription.ComputeChannelHash(this.GetPnsHandle());
			}
			return this.channelHash;
		}

		internal abstract string GetPnsHandle();

		internal virtual void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
		}

		public string Serialize()
		{
			this.Validate(true, ApiVersion.Four, false);
			return (new MessagingDescriptionSerializer<RegistrationDescription>()).Serialize(this);
		}

		internal abstract void SetPnsHandle(string pnsHandle);

		public static int TagCount(string tags)
		{
			char[] chrArray = new char[] { ',' };
			return (int)tags.Split(chrArray).Length;
		}

		internal void Validate(bool allowLocalMockPns, ApiVersion version, bool checkExpirationTime = true)
		{
			if (checkExpirationTime && this.ExpirationTime.HasValue)
			{
				throw new InvalidDataContractException(SRClient.CannotSpecifyExpirationTime);
			}
			this.OnValidate(allowLocalMockPns, version);
		}

		public static bool ValidateTags(string tags)
		{
			return RegistrationDescription.TagRegex.IsMatch(tags);
		}
	}
}