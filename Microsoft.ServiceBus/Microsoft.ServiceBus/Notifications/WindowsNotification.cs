using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class WindowsNotification : Notification
	{
		private const string WnsTypeName = "X-WNS-Type";

		private const string Raw = "wns/raw";

		private const string Badge = "wns/badge";

		private const string Tile = "wns/tile";

		private const string Toast = "wns/toast";

		protected override string PlatformType
		{
			get
			{
				return "windows";
			}
		}

		public WindowsNotification(XmlDocument payLoad) : this(payLoad, (IDictionary<string, string>)null)
		{
		}

		public WindowsNotification(string payLoad) : this(payLoad, (IDictionary<string, string>)null)
		{
		}

		public WindowsNotification(XmlDocument payLoad, IDictionary<string, string> wnsHeaders) : this(payLoad.InnerXml, wnsHeaders)
		{
		}

		public WindowsNotification(string payLoad, IDictionary<string, string> wnsHeaders) : base(wnsHeaders, null)
		{
			if (string.IsNullOrWhiteSpace(payLoad))
			{
				throw new ArgumentNullException("payLoad");
			}
			base.Body = payLoad;
		}

		[Obsolete("This method is obsolete.")]
		public WindowsNotification(XmlDocument payLoad, string tag) : this(payLoad.InnerXml, null, tag)
		{
		}

		[Obsolete("This method is obsolete.")]
		public WindowsNotification(string payLoad, string tag) : this(payLoad, null, tag)
		{
		}

		[Obsolete("This method is obsolete.")]
		public WindowsNotification(XmlDocument payLoad, IDictionary<string, string> wnsHeaders, string tag) : this(payLoad.InnerXml, wnsHeaders, tag)
		{
		}

		[Obsolete("This method is obsolete.")]
		public WindowsNotification(string payLoad, IDictionary<string, string> wnsHeaders, string tag) : base(wnsHeaders, tag)
		{
			if (string.IsNullOrWhiteSpace(payLoad))
			{
				throw new ArgumentNullException("payLoad");
			}
			base.Body = payLoad;
		}

		private void AddNotificationTypeHeader(WindowsTemplateBodyType bodyType)
		{
			switch (bodyType)
			{
				case WindowsTemplateBodyType.Toast:
				{
					base.AddOrUpdateHeader("X-WNS-Type", "wns/toast");
					return;
				}
				case WindowsTemplateBodyType.Tile:
				{
					base.AddOrUpdateHeader("X-WNS-Type", "wns/tile");
					return;
				}
				case WindowsTemplateBodyType.Badge:
				{
					base.AddOrUpdateHeader("X-WNS-Type", "wns/badge");
					return;
				}
				case WindowsTemplateBodyType.Raw:
				{
					base.AddOrUpdateHeader("X-WNS-Type", "wns/raw");
					return;
				}
				default:
				{
					return;
				}
			}
		}

		protected override void OnValidateAndPopulateHeaders()
		{
			if (base.Headers.ContainsKey("X-WNS-Type") && base.Headers["X-WNS-Type"].Equals("wns/raw", StringComparison.OrdinalIgnoreCase))
			{
				this.AddNotificationTypeHeader(WindowsTemplateBodyType.Raw);
				base.ContentType = "application/octet-stream";
				return;
			}
			this.AddNotificationTypeHeader(RegistrationSDKHelper.DetectWindowsTemplateRegistationType(base.Body, SRClient.NotSupportedXMLFormatAsPayload));
			base.Body = RegistrationSDKHelper.AddDeclarationToXml(base.Body);
		}
	}
}