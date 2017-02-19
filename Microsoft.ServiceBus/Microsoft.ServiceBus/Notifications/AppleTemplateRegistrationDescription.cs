using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=3L)]
	[DataContract(Name="AppleTemplateRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class AppleTemplateRegistrationDescription : AppleRegistrationDescription
	{
		internal override string AppPlatForm
		{
			get
			{
				return "apple";
			}
		}

		[AmqpMember(Mandatory=true, Order=4)]
		[DataMember(Name="BodyTemplate", IsRequired=true, Order=3001)]
		public CDataMember BodyTemplate
		{
			get;
			set;
		}

		[AmqpMember(Mandatory=false, Order=5)]
		[DataMember(Name="Expiry", IsRequired=false, Order=3002)]
		public string Expiry
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "appletemplate";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "template";
			}
		}

		[AmqpMember(Mandatory=false, Order=6)]
		[DataMember(Name="TemplateName", IsRequired=false, Order=3003)]
		public string TemplateName
		{
			get;
			set;
		}

		public AppleTemplateRegistrationDescription(AppleTemplateRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.BodyTemplate = sourceRegistration.BodyTemplate;
			this.Expiry = sourceRegistration.Expiry;
			this.TemplateName = sourceRegistration.TemplateName;
		}

		public AppleTemplateRegistrationDescription(string deviceToken) : base(deviceToken)
		{
		}

		public AppleTemplateRegistrationDescription(string deviceToken, string jsonPayload) : this(string.Empty, deviceToken, jsonPayload, null)
		{
		}

		public AppleTemplateRegistrationDescription(string deviceToken, string jsonPayload, IEnumerable<string> tags) : this(string.Empty, deviceToken, jsonPayload, tags)
		{
		}

		internal AppleTemplateRegistrationDescription(string notificationHubPath, string deviceToken, string jsonPayload, IEnumerable<string> tags) : base(notificationHubPath, deviceToken, tags)
		{
			if (string.IsNullOrWhiteSpace(jsonPayload))
			{
				throw new ArgumentNullException("jsonPayload");
			}
			this.BodyTemplate = new CDataMember(jsonPayload);
		}

		internal override RegistrationDescription Clone()
		{
			return new AppleTemplateRegistrationDescription(this);
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			DateTime dateTime;
			base.OnValidate(allowLocalMockPns, version);
			if (this.Expiry != null)
			{
				if (this.Expiry == string.Empty)
				{
					throw new InvalidDataContractException(SRClient.EmptyExpiryValue);
				}
				if (ExpressionEvaluator.Validate(this.Expiry, version) == ExpressionEvaluator.ExpressionType.Literal && !DateTime.TryParse(this.Expiry, out dateTime) && !string.Equals("0", this.Expiry, StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidDataContractException(SRClient.ExpiryDeserializationError);
				}
			}
			try
			{
				using (XmlReader xmlReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(this.BodyTemplate), new XmlDictionaryReaderQuotas()))
				{
					foreach (XElement xElement in XDocument.Load(xmlReader).Root.DescendantsAndSelf())
					{
						foreach (XAttribute xAttribute in xElement.Attributes())
						{
							ExpressionEvaluator.Validate(xAttribute.Value, version);
						}
						if (xElement.HasElements || string.IsNullOrEmpty(xElement.Value))
						{
							continue;
						}
						ExpressionEvaluator.Validate(xElement.Value, version);
					}
				}
			}
			catch (InvalidOperationException invalidOperationException)
			{
				throw new XmlException(SRClient.FailedToDeserializeBodyTemplate);
			}
			this.ValidateTemplateName();
		}

		private void ValidateTemplateName()
		{
			if (this.TemplateName != null && this.TemplateName.Length > 200)
			{
				throw new InvalidDataContractException(SRClient.TemplateNameLengthExceedsLimit(200));
			}
		}
	}
}