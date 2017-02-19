using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging.Amqp.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.ServiceBus.Notifications
{
	[AmqpContract(Code=8L)]
	[DataContract(Name="MpnsTemplateRegistrationDescription", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class MpnsTemplateRegistrationDescription : MpnsRegistrationDescription
	{
		internal override string AppPlatForm
		{
			get
			{
				return "windowsphone";
			}
		}

		[AmqpMember(Mandatory=true, Order=5)]
		[DataMember(Name="BodyTemplate", IsRequired=true, Order=3001)]
		public CDataMember BodyTemplate
		{
			get;
			set;
		}

		[AmqpMember(Mandatory=false, Order=7)]
		internal List<int> ExpressionLengths
		{
			get;
			set;
		}

		[AmqpMember(Mandatory=false, Order=8)]
		internal List<string> Expressions
		{
			get;
			set;
		}

		[AmqpMember(Mandatory=false, Order=6)]
		internal List<int> ExpressionStartIndices
		{
			get;
			set;
		}

		[AmqpMember(Mandatory=true, Order=4)]
		[DataMember(Name="MpnsHeaders", IsRequired=true, Order=3002)]
		public MpnsHeaderCollection MpnsHeaders
		{
			get;
			set;
		}

		internal override string PlatformType
		{
			get
			{
				return "windowsphonetemplate";
			}
		}

		internal override string RegistrationType
		{
			get
			{
				return "template";
			}
		}

		[AmqpMember(Mandatory=false, Order=9)]
		[DataMember(Name="TemplateName", IsRequired=false, Order=3003)]
		public string TemplateName
		{
			get;
			set;
		}

		public MpnsTemplateRegistrationDescription(MpnsTemplateRegistrationDescription sourceRegistration) : base(sourceRegistration)
		{
			this.MpnsHeaders = new MpnsHeaderCollection();
			if (sourceRegistration.MpnsHeaders != null)
			{
				foreach (KeyValuePair<string, string> mpnsHeader in sourceRegistration.MpnsHeaders)
				{
					this.MpnsHeaders.Add(mpnsHeader.Key, mpnsHeader.Value);
				}
			}
			this.BodyTemplate = sourceRegistration.BodyTemplate;
			this.TemplateName = sourceRegistration.TemplateName;
		}

		public MpnsTemplateRegistrationDescription(Uri channelUri) : base(channelUri)
		{
		}

		public MpnsTemplateRegistrationDescription(Uri channelUri, string templatePayload) : this(string.Empty, channelUri, templatePayload, null, null)
		{
		}

		public MpnsTemplateRegistrationDescription(Uri channelUri, XmlDocument xmlTemplate) : this(string.Empty, channelUri, xmlTemplate.InnerXml, null, null)
		{
		}

		public MpnsTemplateRegistrationDescription(Uri channelUri, string templatePayload, IDictionary<string, string> mpnsHeaders) : this(string.Empty, channelUri, templatePayload, mpnsHeaders, null)
		{
		}

		public MpnsTemplateRegistrationDescription(Uri channelUri, string templatePayload, IEnumerable<string> tags) : this(string.Empty, channelUri, templatePayload, null, tags)
		{
		}

		public MpnsTemplateRegistrationDescription(Uri channelUri, string templatePayload, IDictionary<string, string> mpnsHeaders, IEnumerable<string> tags) : this(string.Empty, channelUri, templatePayload, mpnsHeaders, tags)
		{
		}

		public MpnsTemplateRegistrationDescription(string channelUri) : base(new Uri(channelUri))
		{
		}

		public MpnsTemplateRegistrationDescription(string channelUri, string templatePayload) : this(string.Empty, new Uri(channelUri), templatePayload, null, null)
		{
		}

		public MpnsTemplateRegistrationDescription(string channelUri, XmlDocument xmlTemplate) : this(string.Empty, new Uri(channelUri), xmlTemplate.InnerXml, null, null)
		{
		}

		public MpnsTemplateRegistrationDescription(string channelUri, string templatePayload, IDictionary<string, string> mpnsHeaders) : this(string.Empty, new Uri(channelUri), templatePayload, mpnsHeaders, null)
		{
		}

		public MpnsTemplateRegistrationDescription(string channelUri, string templatePayload, IEnumerable<string> tags) : this(string.Empty, new Uri(channelUri), templatePayload, null, tags)
		{
		}

		public MpnsTemplateRegistrationDescription(string channelUri, string templatePayload, IDictionary<string, string> mpnsHeaders, IEnumerable<string> tags) : this(string.Empty, new Uri(channelUri), templatePayload, mpnsHeaders, tags)
		{
		}

		internal MpnsTemplateRegistrationDescription(string notificationHubPath, string channelUri, string templatePayload, IDictionary<string, string> mpnsHeaders, IEnumerable<string> tags) : this(notificationHubPath, new Uri(channelUri), templatePayload, mpnsHeaders, tags)
		{
		}

		internal MpnsTemplateRegistrationDescription(string notificationHubPath, Uri channelUri, string templatePayload, IDictionary<string, string> mpnsHeaders, IEnumerable<string> tags) : base(notificationHubPath, channelUri, tags)
		{
			if (string.IsNullOrWhiteSpace(templatePayload))
			{
				throw new ArgumentNullException("templatePayload");
			}
			this.BodyTemplate = new CDataMember(templatePayload);
			this.MpnsHeaders = new MpnsHeaderCollection();
			if (mpnsHeaders != null)
			{
				foreach (KeyValuePair<string, string> mpnsHeader in mpnsHeaders)
				{
					this.MpnsHeaders.Add(mpnsHeader.Key, mpnsHeader.Value);
				}
			}
		}

		private void AddExpression(string expression, string escapedExpression, IDictionary<string, int> expressionToIndexMap)
		{
			int num;
			if (!expressionToIndexMap.TryGetValue(expression, out num))
			{
				num = -1;
			}
			int num1 = this.BodyTemplate.Value.IndexOf(escapedExpression, num + 1, StringComparison.OrdinalIgnoreCase);
			if (num1 == -1)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] objArray = new object[] { expression };
				throw new InvalidDataContractException(string.Format(invariantCulture, "Unsupported expression: {0}", objArray));
			}
			this.ExpressionStartIndices.Add(num1);
			this.ExpressionLengths.Add(escapedExpression.Length);
			this.Expressions.Add(expression);
			expressionToIndexMap[expression] = num1;
		}

		internal override RegistrationDescription Clone()
		{
			return new MpnsTemplateRegistrationDescription(this);
		}

		internal bool IsJsonObjectPayLoad()
		{
			string str = this.BodyTemplate.Value.Trim();
			if (!str.StartsWith("{", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return str.EndsWith("}", StringComparison.OrdinalIgnoreCase);
		}

		internal bool IsXmlPayLoad()
		{
			string str = this.BodyTemplate.Value.Trim();
			return str.StartsWith("<", StringComparison.OrdinalIgnoreCase);
		}

		internal override void OnValidate(bool allowLocalMockPns, ApiVersion version)
		{
			base.OnValidate(allowLocalMockPns, version);
			this.ValidateMpnsHeaders(version);
			if (!this.IsXmlPayLoad())
			{
				if (!this.IsJsonObjectPayLoad())
				{
					throw new InvalidDataContractException(SRClient.InvalidPayLoadFormat);
				}
				this.ValidateJsonPayLoad(version);
			}
			else
			{
				this.ValidateXmlPayLoad(version);
			}
			this.ValidateTemplateName();
		}

		private void ValidateJsonPayLoad(ApiVersion version)
		{
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
		}

		private void ValidateMpnsHeaders(ApiVersion version)
		{
			if (this.MpnsHeaders == null || !this.MpnsHeaders.ContainsKey("X-NotificationClass") || string.IsNullOrWhiteSpace(this.MpnsHeaders["X-NotificationClass"]))
			{
				throw new InvalidDataContractException(SRClient.MissingMpnsHeader("X-NotificationClass"));
			}
			foreach (string key in this.MpnsHeaders.Keys)
			{
				if (string.IsNullOrWhiteSpace(this.MpnsHeaders[key]))
				{
					throw new InvalidDataContractException(SRClient.MpnsHeaderIsNullOrEmpty(key));
				}
				ExpressionEvaluator.Validate(this.MpnsHeaders[key], version);
			}
		}

		private void ValidateTemplateName()
		{
			if (this.TemplateName != null && this.TemplateName.Length > 200)
			{
				throw new InvalidDataContractException(SRClient.TemplateNameLengthExceedsLimit(200));
			}
		}

		private void ValidateXmlPayLoad(ApiVersion version)
		{
			XDocument xDocument = XDocument.Parse(this.BodyTemplate);
			this.ExpressionStartIndices = new List<int>();
			this.ExpressionLengths = new List<int>();
			this.Expressions = new List<string>();
			IDictionary<string, int> strs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (XElement xElement in xDocument.Root.DescendantsAndSelf())
			{
				foreach (XAttribute xAttribute in xElement.Attributes())
				{
					if (ExpressionEvaluator.Validate(xAttribute.Value, version) == ExpressionEvaluator.ExpressionType.Literal)
					{
						continue;
					}
					string str = xAttribute.ToString();
					string str1 = str.Substring(str.IndexOf('=') + 1);
					string str2 = str1.Substring(1, str1.Length - 2);
					this.AddExpression(xAttribute.Value, str2, strs);
				}
				if (xElement.HasElements || string.IsNullOrEmpty(xElement.Value) || ExpressionEvaluator.Validate(xElement.Value, version) == ExpressionEvaluator.ExpressionType.Literal)
				{
					continue;
				}
				using (XmlReader xmlReader = xElement.CreateReader())
				{
					xmlReader.MoveToContent();
					string str3 = xmlReader.ReadInnerXml();
					this.AddExpression(xElement.Value, str3, strs);
				}
			}
		}

		private enum MpnsTemplateRegistrationType
		{
			Toast,
			Tile
		}
	}
}