using System;

namespace Microsoft.ServiceBus
{
	public static class TokenConstants
	{
		public const char DefaultCompoundClaimDelimiter = ',';

		public const char HttpAuthParameterSeparator = ',';

		public const string HttpMethodPost = "POST";

		public const string HttpMethodGet = "GET";

		public const string HttpMethodHead = "HEAD";

		public const string HttpMethodTrace = "TRACE";

		public const string ManagementIssuerName = "owner";

		public const int MaxIssuerNameSize = 128;

		public const int MaxIssuerSecretSize = 128;

		public const string OutputClaimIssuerId = "ACS";

		public const string ServiceBusIssuerName = "owner";

		public const string WrapAppliesTo = "wrap_scope";

		public const string WrapRequestedLifetime = "requested_lifetime";

		public const string WrapAccessToken = "wrap_access_token";

		public const string WrapAssertion = "wrap_assertion";

		public const string WrapAssertionFormat = "wrap_assertion_format";

		public const string WrapAuthenticationType = "WRAP";

		public const string WrapAuthorizationHeaderKey = "access_token";

		public const string WrapName = "wrap_name";

		public const string WrapPassword = "wrap_password";

		public const string WrapContentType = "application/x-www-form-urlencoded";

		public const string WrapSamlAssertionFormat = "SAML";

		public const string WrapSwtAssertionFormat = "SWT";

		public const string WrapTokenExpiresIn = "wrap_access_token_expires_in";

		public const string Saml11ConfirmationMethodBearerToken = "urn:oasis:names:tc:SAML:1.0:cm:bearer";

		public const string TokenAudience = "Audience";

		public const string TokenExpiresOn = "ExpiresOn";

		public const string TokenIssuer = "Issuer";

		public const string TokenDigest256 = "HMACSHA256";

		public const string TrackingIdHeaderName = "x-ms-request-id";

		public const char UrlParameterSeparator = '&';

		public const char KeyValueSeparator = '=';

		public readonly static DateTime WrapBaseTime;

		internal readonly static string TokenServiceRealmFormat;

		internal readonly static string[] WrapContentTypes;

		static TokenConstants()
		{
			TokenConstants.WrapBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			TokenConstants.TokenServiceRealmFormat = "https://{0}.accesscontrol.windows.net/";
			TokenConstants.WrapContentTypes = new string[] { "*/*", "application/*", "application/x-www-form-urlencoded" };
		}
	}
}