using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class TemplateNotification : Notification
	{
		private IDictionary<string, string> templateProperties;

		protected override string PlatformType
		{
			get
			{
				return "template";
			}
		}

		public TemplateNotification(IDictionary<string, string> templateProperties) : base(null, null)
		{
			if (templateProperties == null)
			{
				throw new ArgumentNullException("properties");
			}
			this.templateProperties = templateProperties;
		}

		[Obsolete("This method is obsolete.")]
		public TemplateNotification(IDictionary<string, string> templateProperties, string tag) : base(null, tag)
		{
			if (templateProperties == null)
			{
				throw new ArgumentNullException("properties");
			}
			this.templateProperties = templateProperties;
		}

		protected override void OnValidateAndPopulateHeaders()
		{
			base.Body = (new JavaScriptSerializer()).Serialize(this.templateProperties);
		}
	}
}