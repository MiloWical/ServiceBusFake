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
	[AmqpContract(Code=14L)]
	[DataContract(Name="NokiaXTemplateRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	internal class NokiaXTemplateRegistrationDescription : NokiaXRegistrationDescription
	{
		internal override string AppPlatForm
		{
			get
			{
				return "nokiax";
			}
		}

		[AmqpMember(Mandatory=true, Order=4)]
		[DataMember(Name="BodyTemplate", IsRequired=true, Order=3001)]
		public CDataMember BodyTemplate
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "nokiaxtemplate";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "template";
			}
		}

		[AmqpMember(Mandatory=false, Order=5)]
		[DataMember(Name="TemplateName", IsRequired=false, Order=3002)]
		public string TemplateName
		{
			get;
			set;
		}

		public NokiaXTemplateRegistrationDescription(NokiaXTemplateRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.BodyTemplate = sourceRegistration.BodyTemplate;
			this.TemplateName = sourceRegistration.TemplateName;
		}

		public NokiaXTemplateRegistrationDescription(string nokiaXRegistrationId) : base(nokiaXRegistrationId)
		{
		}

		public NokiaXTemplateRegistrationDescription(string nokiaXRegistrationId, string jsonPayload) : this(string.Empty, nokiaXRegistrationId, jsonPayload, null)
		{
		}

		public NokiaXTemplateRegistrationDescription(string nokiaXRegistrationId, string jsonPayload, IEnumerable<string> tags) : this(string.Empty, nokiaXRegistrationId, jsonPayload, tags)
		{
		}

		internal NokiaXTemplateRegistrationDescription(string notificationHubPath, string nokiaXRegistrationId, string jsonPayload, IEnumerable<string> tags) : base(notificationHubPath, nokiaXRegistrationId, tags)
		{
			if (string.IsNullOrWhiteSpace(jsonPayload))
			{
				throw new ArgumentNullException("jsonPayload");
			}
			this.BodyTemplate = new CDataMember(jsonPayload);
		}

		internal override RegistrationDescription Clone()
		{
			return new NokiaXTemplateRegistrationDescription(this);
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			base.OnValidate(allowLocalMockPns, version);
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