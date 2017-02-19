using Microsoft.ServiceBus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Microsoft.ServiceBus.Notifications
{
	internal static class RegistrationSDKHelper
	{
		internal const int TemplateMaxLength = 200;

		public static string AddDeclarationToXml(string content)
		{
			string innerXml;
			XmlDocument xmlDocument = new XmlDocument();
			using (XmlReader xmlReader = XmlReader.Create(new StringReader(content)))
			{
				xmlDocument.Load(xmlReader);
				if (xmlDocument.FirstChild.NodeType != XmlNodeType.XmlDeclaration)
				{
					XmlNode xmlNodes = xmlDocument.CreateXmlDeclaration("1.0", "utf-16", null);
					xmlDocument.InsertBefore(xmlNodes, xmlDocument.DocumentElement);
				}
				innerXml = xmlDocument.InnerXml;
			}
			return innerXml;
		}

		private static void AddOrUpdateHeader(SortedDictionary<string, string> headers, string key, string value)
		{
			if (!headers.ContainsKey(key))
			{
				headers.Add(key, value);
				return;
			}
			headers[key] = value;
		}

		public static MpnsTemplateBodyType DetectMpnsTemplateRegistationType(string body, string errorMsg)
		{
			MpnsTemplateBodyType mpnsTemplateBodyType;
			MpnsTemplateBodyType mpnsTemplateBodyType1;
			XmlDocument xmlDocument = new XmlDocument();
			using (XmlReader xmlReader = XmlReader.Create(new StringReader(body)))
			{
				try
				{
					xmlDocument.Load(xmlReader);
				}
				catch (XmlException xmlException)
				{
					throw new ArgumentException(errorMsg);
				}
				XmlNode firstChild = xmlDocument.FirstChild;
				while (firstChild != null && firstChild.NodeType != XmlNodeType.Element)
				{
					firstChild = firstChild.NextSibling;
				}
				if (firstChild == null)
				{
					throw new ArgumentException(errorMsg);
				}
				if (!firstChild.NamespaceURI.Equals("WPNotification", StringComparison.OrdinalIgnoreCase) || !firstChild.LocalName.Equals("Notification", StringComparison.OrdinalIgnoreCase))
				{
					throw new ArgumentException(errorMsg);
				}
				XmlNode xmlNodes = firstChild.FirstChild;
				if (xmlNodes == null || !Enum.TryParse<MpnsTemplateBodyType>(xmlNodes.LocalName, true, out mpnsTemplateBodyType))
				{
					throw new ArgumentException(errorMsg);
				}
				mpnsTemplateBodyType1 = mpnsTemplateBodyType;
			}
			return mpnsTemplateBodyType1;
		}

		public static WindowsTemplateBodyType DetectWindowsTemplateRegistationType(string body, string errorMsg)
		{
			WindowsTemplateBodyType windowsTemplateBodyType;
			WindowsTemplateBodyType windowsTemplateBodyType1;
			XmlDocument xmlDocument = new XmlDocument();
			using (XmlReader xmlReader = XmlReader.Create(new StringReader(body)))
			{
				try
				{
					xmlDocument.Load(xmlReader);
				}
				catch (XmlException xmlException)
				{
					throw new ArgumentException(errorMsg);
				}
				XmlNode firstChild = xmlDocument.FirstChild;
				while (firstChild != null && firstChild.NodeType != XmlNodeType.Element)
				{
					firstChild = firstChild.NextSibling;
				}
				if (firstChild == null)
				{
					throw new ArgumentException(errorMsg);
				}
				if (firstChild == null || !Enum.TryParse<WindowsTemplateBodyType>(firstChild.Name, true, out windowsTemplateBodyType))
				{
					throw new ArgumentException(errorMsg);
				}
				windowsTemplateBodyType1 = windowsTemplateBodyType;
			}
			return windowsTemplateBodyType1;
		}

		private static void SetMpnsType(this MpnsTemplateRegistrationDescription registration)
		{
			if (registration == null || registration.IsJsonObjectPayLoad())
			{
				return;
			}
			if (registration.MpnsHeaders != null && registration.MpnsHeaders.ContainsKey("X-NotificationClass"))
			{
				int num = int.Parse(registration.MpnsHeaders["X-NotificationClass"], CultureInfo.InvariantCulture);
				if (num >= 3 && num <= 10 || num >= 13 && num <= 20 || num >= 23 && num <= 31)
				{
					return;
				}
			}
			if (registration.IsXmlPayLoad())
			{
				if (registration.MpnsHeaders == null)
				{
					registration.MpnsHeaders = new MpnsHeaderCollection();
				}
				switch (RegistrationSDKHelper.DetectMpnsTemplateRegistationType(registration.BodyTemplate, SRClient.NotSupportedXMLFormatAsBodyTemplateForMpns))
				{
					case MpnsTemplateBodyType.Toast:
					{
						RegistrationSDKHelper.AddOrUpdateHeader(registration.MpnsHeaders, "X-WindowsPhone-Target", "toast");
						RegistrationSDKHelper.AddOrUpdateHeader(registration.MpnsHeaders, "X-NotificationClass", "2");
						break;
					}
					case MpnsTemplateBodyType.Tile:
					{
						RegistrationSDKHelper.AddOrUpdateHeader(registration.MpnsHeaders, "X-WindowsPhone-Target", "token");
						RegistrationSDKHelper.AddOrUpdateHeader(registration.MpnsHeaders, "X-NotificationClass", "1");
						return;
					}
					default:
					{
						return;
					}
				}
			}
		}

		private static void SetWnsType(this WindowsTemplateRegistrationDescription registration)
		{
			if (registration == null || registration.IsJsonObjectPayLoad())
			{
				return;
			}
			if (registration.IsXmlPayLoad())
			{
				if (registration.WnsHeaders == null)
				{
					registration.WnsHeaders = new WnsHeaderCollection();
				}
				if (!registration.WnsHeaders.ContainsKey("X-WNS-Type") || !registration.WnsHeaders["X-WNS-Type"].Equals("wns/raw", StringComparison.OrdinalIgnoreCase))
				{
					switch (RegistrationSDKHelper.DetectWindowsTemplateRegistationType(registration.BodyTemplate, SRClient.NotSupportedXMLFormatAsBodyTemplate))
					{
						case WindowsTemplateBodyType.Toast:
						{
							RegistrationSDKHelper.AddOrUpdateHeader(registration.WnsHeaders, "X-WNS-Type", "wns/toast");
							return;
						}
						case WindowsTemplateBodyType.Tile:
						{
							RegistrationSDKHelper.AddOrUpdateHeader(registration.WnsHeaders, "X-WNS-Type", "wns/tile");
							return;
						}
						case WindowsTemplateBodyType.Badge:
						{
							RegistrationSDKHelper.AddOrUpdateHeader(registration.WnsHeaders, "X-WNS-Type", "wns/badge");
							break;
						}
						default:
						{
							return;
						}
					}
				}
				else
				{
					try
					{
						XmlDocument xmlDocument = new XmlDocument();
						using (XmlReader xmlReader = XmlReader.Create(new StringReader(registration.BodyTemplate)))
						{
							xmlDocument.Load(xmlReader);
						}
					}
					catch (XmlException xmlException)
					{
						throw new ArgumentException(SRClient.NotSupportedXMLFormatAsBodyTemplate);
					}
				}
			}
		}

		internal static void ValidateRegistration(RegistrationDescription registration)
		{
			string str;
			WindowsTemplateRegistrationDescription windowsTemplateRegistrationDescription = registration as WindowsTemplateRegistrationDescription;
			if (windowsTemplateRegistrationDescription == null)
			{
				MpnsTemplateRegistrationDescription mpnsTemplateRegistrationDescription = registration as MpnsTemplateRegistrationDescription;
				if (mpnsTemplateRegistrationDescription != null)
				{
					mpnsTemplateRegistrationDescription.SetMpnsType();
				}
			}
			else
			{
				windowsTemplateRegistrationDescription.SetWnsType();
			}
			RegistrationDescription registrationDescription = registration;
			if (registration.Tags == null || registration.Tags.Count == 0)
			{
				str = null;
			}
			else
			{
				str = string.Join(",", registration.Tags);
			}
			registrationDescription.TagsString = str;
			registration.Validate(true, ApiVersion.Four, true);
		}
	}
}