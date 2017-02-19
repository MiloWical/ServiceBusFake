using System;

namespace Microsoft.ServiceBus.Messaging.Channels
{
	internal static class MetadataStrings
	{
		public static class Addressing10
		{
			public const string Prefix = "wsa10";

			public const string NamespaceUri = "http://www.w3.org/2005/08/addressing";

			public static class MetadataPolicy
			{
				public const string Prefix = "wsam";

				public const string NamespaceUri = "http://www.w3.org/2007/05/addressing/metadata";

				public const string Addressing = "Addressing";

				public const string AnonymousResponses = "AnonymousResponses";

				public const string NonAnonymousResponses = "NonAnonymousResponses";
			}

			public static class WsdlBindingPolicy
			{
				public const string Prefix = "wsaw";

				public const string NamespaceUri = "http://www.w3.org/2006/05/addressing/wsdl";

				public const string UsingAddressing = "UsingAddressing";
			}
		}

		public static class Addressing200408
		{
			public const string Prefix = "wsa";

			public const string NamespaceUri = "http://schemas.xmlsoap.org/ws/2004/08/addressing";

			public static class Policy
			{
				public const string Prefix = "wsap";

				public const string NamespaceUri = "http://schemas.xmlsoap.org/ws/2004/08/addressing/policy";

				public const string UsingAddressing = "UsingAddressing";
			}
		}

		public static class WSPolicy
		{
			public const string Prefix = "wsp";

			public const string NamespaceUri15 = "http://www.w3.org/ns/ws-policy";

			public static class Attributes
			{
				public const string PolicyURIs = "PolicyURIs";
			}

			public static class Elements
			{
				public const string All = "All";

				public const string ExactlyOne = "ExactlyOne";

				public const string Policy = "Policy";
			}
		}
	}
}