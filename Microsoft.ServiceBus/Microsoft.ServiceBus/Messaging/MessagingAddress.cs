using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal sealed class MessagingAddress
	{
		internal const string UriScheme = "sb";

		private const string EntityVariableName = "entityname";

		private readonly static UriTemplate CanonicalAddressTemplate;

		private readonly static UriTemplate CanonicalAddressWithEntityNameTemplate;

		private readonly static UriTemplate PublishedAddressTemplate;

		private readonly static UriTemplate PublishedAddressWithEntityNameTemplate;

		private readonly bool preservePathSegmentAsBaseAddress;

		public string EntityName
		{
			get;
			private set;
		}

		public Uri NamingAuthority
		{
			get;
			private set;
		}

		public Uri NormalizedLogicalAddress
		{
			get;
			private set;
		}

		public Uri ResourceAddress
		{
			get;
			private set;
		}

		public MessagingAddressType Type
		{
			get;
			private set;
		}

		static MessagingAddress()
		{
			MessagingAddress.CanonicalAddressTemplate = new UriTemplate("applications/{appid}/modules/{modname}/components/{compname}/exports/{exportname}");
			MessagingAddress.CanonicalAddressWithEntityNameTemplate = new UriTemplate("applications/{appid}/modules/{modname}/components/{compname}/exports/{exportname}/{entityname}");
			MessagingAddress.PublishedAddressTemplate = new UriTemplate("applications/{appid}/{containername}");
			MessagingAddress.PublishedAddressWithEntityNameTemplate = new UriTemplate("applications/{appid}/{containername}/{entityname}");
		}

		public MessagingAddress(Uri logicalAddress, bool preservePathSegmentAsBaseAddress)
		{
			this.preservePathSegmentAsBaseAddress = preservePathSegmentAsBaseAddress;
			this.Initialize(logicalAddress);
		}

		private Uri GetResourceAddress(Uri normalizedAddress)
		{
			return new Uri(this.NamingAuthority, normalizedAddress.PathAndQuery);
		}

		private void Initialize(Uri logicalAddress)
		{
			this.SetNamingAuthority(logicalAddress);
			Uri uri = new Uri(string.Concat(logicalAddress.Scheme, Uri.SchemeDelimiter, logicalAddress.Authority));
			logicalAddress = MessagingAddress.RemoveTrailingSlash(logicalAddress);
			UriTemplateMatch uriTemplateMatch = MessagingAddress.CanonicalAddressTemplate.Match(uri, logicalAddress);
			UriTemplateMatch uriTemplateMatch1 = uriTemplateMatch;
			if (uriTemplateMatch == null)
			{
				UriTemplateMatch uriTemplateMatch2 = MessagingAddress.CanonicalAddressWithEntityNameTemplate.Match(uri, logicalAddress);
				uriTemplateMatch1 = uriTemplateMatch2;
				if (uriTemplateMatch2 == null)
				{
					UriTemplateMatch uriTemplateMatch3 = MessagingAddress.PublishedAddressTemplate.Match(uri, logicalAddress);
					uriTemplateMatch1 = uriTemplateMatch3;
					if (uriTemplateMatch3 == null)
					{
						UriTemplateMatch uriTemplateMatch4 = MessagingAddress.PublishedAddressWithEntityNameTemplate.Match(uri, logicalAddress);
						uriTemplateMatch1 = uriTemplateMatch4;
						if (uriTemplateMatch4 == null)
						{
							string[] segments = logicalAddress.Segments;
							int num = 1;
							if (this.preservePathSegmentAsBaseAddress && (int)segments.Length > 2)
							{
								num = 2;
							}
							this.Type = MessagingAddressType.Entity;
							this.EntityName = string.Join(string.Empty, segments, num, (int)segments.Length - num);
							this.NormalizedLogicalAddress = new Uri(uri, string.Join(string.Empty, segments, 0, num));
						}
						else
						{
							string[] strArrays = logicalAddress.Segments;
							this.NormalizedLogicalAddress = new Uri(uri, string.Join(string.Empty, strArrays, 0, (int)strArrays.Length - 1));
							this.Type = MessagingAddressType.Entity;
							this.EntityName = uriTemplateMatch1.BoundVariables["entityname"];
						}
					}
					else
					{
						this.NormalizedLogicalAddress = logicalAddress;
						this.Type = MessagingAddressType.Container;
					}
				}
				else
				{
					string[] segments1 = logicalAddress.Segments;
					this.NormalizedLogicalAddress = new Uri(uri, string.Join(string.Empty, segments1, 0, (int)segments1.Length - 1));
					this.Type = MessagingAddressType.Entity;
					this.EntityName = uriTemplateMatch1.BoundVariables["entityname"];
				}
			}
			else
			{
				this.NormalizedLogicalAddress = logicalAddress;
				this.Type = MessagingAddressType.Container;
			}
			this.NormalizedLogicalAddress = MessagingAddress.RemoveTrailingSlash(this.NormalizedLogicalAddress);
			this.ResourceAddress = this.GetResourceAddress(this.NormalizedLogicalAddress);
		}

		private static Uri RemoveTrailingSlash(Uri uri)
		{
			string absoluteUri = uri.AbsoluteUri;
			if (!absoluteUri.EndsWith("/", StringComparison.Ordinal))
			{
				return uri;
			}
			return new Uri(absoluteUri.Substring(0, absoluteUri.Length - 1));
		}

		private void SetNamingAuthority(Uri logicalAddress)
		{
			Uri uri = (new UriBuilder(logicalAddress.Scheme, logicalAddress.Host, logicalAddress.Port)).Uri;
			this.NamingAuthority = uri;
		}

		public override string ToString()
		{
			return this.NormalizedLogicalAddress.AbsoluteUri;
		}
	}
}