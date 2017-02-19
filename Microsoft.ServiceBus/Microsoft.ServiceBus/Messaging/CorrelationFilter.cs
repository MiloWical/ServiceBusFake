using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="CorrelationFilter", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(DateTimeOffset))]
	public sealed class CorrelationFilter : Filter
	{
		[DataMember(Name="Properties", Order=131080, EmitDefaultValue=false, IsRequired=false)]
		private PropertyDictionary properties;

		[DataMember(Name="ContentType", Order=131079, EmitDefaultValue=false, IsRequired=false)]
		public string ContentType
		{
			get;
			set;
		}

		[DataMember(Name="CorrelationId", Order=65537, EmitDefaultValue=false, IsRequired=false)]
		public string CorrelationId
		{
			get;
			set;
		}

		internal override Microsoft.ServiceBus.Messaging.FilterType FilterType
		{
			get
			{
				return Microsoft.ServiceBus.Messaging.FilterType.CorrelationFilter;
			}
		}

		[DataMember(Name="Label", Order=131076, EmitDefaultValue=false, IsRequired=false)]
		public string Label
		{
			get;
			set;
		}

		[DataMember(Name="MessageId", Order=131073, EmitDefaultValue=false, IsRequired=false)]
		public string MessageId
		{
			get;
			set;
		}

		public IDictionary<string, object> Properties
		{
			get
			{
				if (this.properties == null)
				{
					this.properties = new PropertyDictionary();
				}
				return this.properties;
			}
		}

		[DataMember(Name="ReplyTo", Order=131075, EmitDefaultValue=false, IsRequired=false)]
		public string ReplyTo
		{
			get;
			set;
		}

		[DataMember(Name="ReplyToSessionId", Order=131078, EmitDefaultValue=false, IsRequired=false)]
		public string ReplyToSessionId
		{
			get;
			set;
		}

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		[DataMember(Name="SessionId", Order=131077, EmitDefaultValue=false, IsRequired=false)]
		public string SessionId
		{
			get;
			set;
		}

		[DataMember(Name="To", Order=131074, EmitDefaultValue=false, IsRequired=false)]
		public string To
		{
			get;
			set;
		}

		public CorrelationFilter()
		{
		}

		public CorrelationFilter(string correlationId) : this()
		{
			if (string.IsNullOrWhiteSpace(correlationId))
			{
				throw Fx.Exception.ArgumentNullOrWhiteSpace("correlationId");
			}
			this.CorrelationId = correlationId;
		}

		private void AppendPropertyExpression(ref bool firstExpression, StringBuilder builder, string propertyName, object value)
		{
			if (value != null)
			{
				if (!firstExpression)
				{
					builder.Append(" And ");
				}
				else
				{
					firstExpression = false;
				}
				builder.AppendFormat("{0} = '{1}'", propertyName, value);
			}
		}

		internal IEnumerable<KeyValuePair<QualifiedPropertyName, object>> EnumerateQualifiedProperties()
		{
			if (this.CorrelationId != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "CorrelationId"), this.CorrelationId);
			}
			if (this.MessageId != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "MessageId"), this.MessageId);
			}
			if (this.To != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "To"), this.To);
			}
			if (this.ReplyTo != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "ReplyTo"), this.ReplyTo);
			}
			if (this.Label != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "Label"), this.Label);
			}
			if (this.SessionId != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "SessionId"), this.SessionId);
			}
			if (this.ReplyToSessionId != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "ReplyToSessionId"), this.ReplyToSessionId);
			}
			if (this.ContentType != null)
			{
				yield return new KeyValuePair<QualifiedPropertyName, object>(new QualifiedPropertyName(PropertyScope.System, "ContentType"), this.ContentType);
			}
			if (this.properties != null && this.properties.Count > 0)
			{
				foreach (KeyValuePair<string, object> property in this.properties)
				{
					QualifiedPropertyName qualifiedPropertyName = new QualifiedPropertyName(PropertyScope.User, property.Key);
					yield return new KeyValuePair<QualifiedPropertyName, object>(qualifiedPropertyName, property.Value);
				}
			}
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Two && (this.CorrelationId == null || this.MessageId != null || this.To != null || this.ReplyTo != null || this.Label != null || this.SessionId != null || this.ReplyToSessionId != null || this.ContentType != null || this.properties != null))
			{
				return false;
			}
			return true;
		}

		public override bool Match(BrokeredMessage message)
		{
			object obj;
			bool flag;
			if (message == null)
			{
				throw new ArgumentNullException("message");
			}
			if (this.CorrelationId != null && !this.CorrelationId.Equals(message.CorrelationId))
			{
				return false;
			}
			if (this.MessageId != null && !this.MessageId.Equals(message.MessageId))
			{
				return false;
			}
			if (this.To != null && !this.To.Equals(message.To))
			{
				return false;
			}
			if (this.ReplyTo != null && !this.ReplyTo.Equals(message.ReplyTo))
			{
				return false;
			}
			if (this.Label != null && !this.Label.Equals(message.Label))
			{
				return false;
			}
			if (this.SessionId != null && !this.SessionId.Equals(message.SessionId))
			{
				return false;
			}
			if (this.ReplyToSessionId != null && !this.ReplyToSessionId.Equals(message.ReplyToSessionId))
			{
				return false;
			}
			if (this.ContentType != null && !this.ContentType.Equals(message.ContentType))
			{
				return false;
			}
			if (this.Properties.Count > 0)
			{
				using (IEnumerator<KeyValuePair<string, object>> enumerator = this.Properties.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<string, object> current = enumerator.Current;
						if (message.Properties.TryGetValue(current.Key, out obj))
						{
							if (current.Value.Equals(obj))
							{
								continue;
							}
							flag = false;
							return flag;
						}
						else
						{
							flag = false;
							return flag;
						}
					}
					return true;
				}
				return flag;
			}
			return true;
		}

		public override Filter Preprocess()
		{
			return this;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("CorrelationFilter: ");
			bool flag = true;
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.CorrelationId", this.CorrelationId);
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.MessageId", this.MessageId);
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.To", this.To);
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.ReplyTo", this.ReplyTo);
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.Label", this.Label);
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.SessionId", this.SessionId);
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.ReplyToSessionId", this.ReplyToSessionId);
			this.AppendPropertyExpression(ref flag, stringBuilder, "sys.ContentType", this.ContentType);
			foreach (KeyValuePair<string, object> property in this.Properties)
			{
				string key = property.Key;
				this.AppendPropertyExpression(ref flag, stringBuilder, key, property.Value);
			}
			return stringBuilder.ToString();
		}

		internal override void UpdateForVersion(ApiVersion version, Filter existingFilter = null)
		{
			string messageId;
			string to;
			string replyTo;
			string label;
			string sessionId;
			string replyToSessionId;
			string contentType;
			PropertyDictionary propertyDictionaries;
			CorrelationFilter correlationFilter = existingFilter as CorrelationFilter;
			base.UpdateForVersion(version, existingFilter);
			if (version < ApiVersion.Two)
			{
				if (correlationFilter == null)
				{
					messageId = null;
				}
				else
				{
					messageId = correlationFilter.MessageId;
				}
				this.MessageId = messageId;
				if (correlationFilter == null)
				{
					to = null;
				}
				else
				{
					to = correlationFilter.To;
				}
				this.To = to;
				if (correlationFilter == null)
				{
					replyTo = null;
				}
				else
				{
					replyTo = correlationFilter.ReplyTo;
				}
				this.ReplyTo = replyTo;
				if (correlationFilter == null)
				{
					label = null;
				}
				else
				{
					label = correlationFilter.Label;
				}
				this.Label = label;
				if (correlationFilter == null)
				{
					sessionId = null;
				}
				else
				{
					sessionId = correlationFilter.SessionId;
				}
				this.SessionId = sessionId;
				if (correlationFilter == null)
				{
					replyToSessionId = null;
				}
				else
				{
					replyToSessionId = correlationFilter.ReplyToSessionId;
				}
				this.ReplyToSessionId = replyToSessionId;
				if (correlationFilter == null)
				{
					contentType = null;
				}
				else
				{
					contentType = correlationFilter.ContentType;
				}
				this.ContentType = contentType;
				if (correlationFilter == null)
				{
					propertyDictionaries = null;
				}
				else
				{
					propertyDictionaries = correlationFilter.properties;
				}
				this.properties = propertyDictionaries;
			}
		}

		public override void Validate()
		{
			if (!this.EnumerateQualifiedProperties().Any<KeyValuePair<QualifiedPropertyName, object>>())
			{
				throw new FilterException(SRClient.EmptyPropertyInCorrelationFilter);
			}
		}
	}
}