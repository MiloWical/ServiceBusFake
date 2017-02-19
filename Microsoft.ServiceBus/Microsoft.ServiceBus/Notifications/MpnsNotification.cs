using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Microsoft.ServiceBus.Notifications
{
	public sealed class MpnsNotification : Notification
	{
		protected override string PlatformType
		{
			get
			{
				return "windowsphone";
			}
		}

		public MpnsNotification(XmlDocument payLoad) : this(payLoad, (IDictionary<string, string>)null)
		{
		}

		[Obsolete("This method is obsolete.")]
		public MpnsNotification(XmlDocument payLoad, string tag) : this(payLoad, null, tag)
		{
		}

		public MpnsNotification(string payLoad) : this(payLoad, (IDictionary<string, string>)null)
		{
		}

		[Obsolete("This method is obsolete.")]
		public MpnsNotification(string payLoad, string tag) : this(payLoad, null, tag)
		{
		}

		public MpnsNotification(XmlDocument payLoad, IDictionary<string, string> mpnsHeaders) : this(payLoad.InnerXml, mpnsHeaders)
		{
		}

		public MpnsNotification(string payLoad, IDictionary<string, string> mpnsHeaders) : base(mpnsHeaders, null)
		{
			if (string.IsNullOrWhiteSpace(payLoad))
			{
				throw new ArgumentNullException("payLoad");
			}
			base.Body = payLoad;
		}

		[Obsolete("This method is obsolete.")]
		public MpnsNotification(XmlDocument payLoad, IDictionary<string, string> mpnsHeaders, string tag) : this(payLoad.InnerXml, mpnsHeaders, tag)
		{
		}

		[Obsolete("This method is obsolete.")]
		public MpnsNotification(string payLoad, IDictionary<string, string> mpnsHeaders, string tag) : base(mpnsHeaders, tag)
		{
			if (string.IsNullOrWhiteSpace(payLoad))
			{
				throw new ArgumentNullException("payLoad");
			}
			base.Body = payLoad;
		}

		private void AddNotificationTypeHeader(MpnsTemplateBodyType bodyType)
		{
			switch (bodyType)
			{
				case MpnsTemplateBodyType.Toast:
				{
					base.AddOrUpdateHeader("X-WindowsPhone-Target", "toast");
					base.AddOrUpdateHeader("X-NotificationClass", "2");
					return;
				}
				case MpnsTemplateBodyType.Tile:
				{
					base.AddOrUpdateHeader("X-WindowsPhone-Target", "token");
					base.AddOrUpdateHeader("X-NotificationClass", "1");
					return;
				}
				case MpnsTemplateBodyType.Raw:
				{
					base.AddOrUpdateHeader("X-NotificationClass", "3");
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
			if (!base.Headers.ContainsKey("X-NotificationClass"))
			{
				this.AddNotificationTypeHeader(RegistrationSDKHelper.DetectMpnsTemplateRegistationType(base.Body, SRClient.NotSupportedXMLFormatAsPayloadForMpns));
				base.Body = RegistrationSDKHelper.AddDeclarationToXml(base.Body);
			}
		}
	}
}