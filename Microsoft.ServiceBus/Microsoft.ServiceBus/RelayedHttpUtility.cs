using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Web;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Microsoft.ServiceBus
{
	internal static class RelayedHttpUtility
	{
		private const int DefaultMaxBytesPerRead = 65536;

		private const string StatusDescriptionHeaderName = "FaultReason";

		private const string HttpAddressHeaderName = "HttpAddress";

		internal const string FormatStringWrappedSoap11 = "WrappedSoap11";

		internal const string FormatStringWrappedSoap12 = "WrappedSoap12";

		internal const string ServerBusyExceptionMessagePropertyName = "ServerBusyExceptionMessage";

		public const string AccessControlMaxAge = "3600";

		public const string OptionsMethod = "OPTIONS";

		public const string OriginHeader = "Origin";

		public const string AccessControlRequestMethodHeader = "Access-Control-Request-Method";

		public const string AccessControlRequestHeadersHeader = "Access-Control-Request-Headers";

		public const string AccessControlAllowOriginHeader = "Access-Control-Allow-Origin";

		public const string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";

		public const string AccessControlAllowMethodsHeader = "Access-Control-Allow-Methods";

		public const string AccessControlMaxAgeHeader = "Access-Control-Max-Age";

		public const string AccessControlAllowHeadersHeader = "Access-Control-Allow-Headers";

		public const string AccessControlExposeHeadersHeader = "Access-Control-Expose-Headers";

		private static MessageEncoder encoderSoap11;

		private static MessageWrapper wrapperSoap11;

		private static MessageEncoder encoderSoap12;

		private static MessageWrapper wrapperSoap12;

		internal readonly static string FormatStringDefault;

		internal readonly static string FormatStringXml;

		internal readonly static string FormatStringRaw;

		internal readonly static string FormatStringJson;

		private readonly static XmlDictionaryReaderQuotas MaxQuotas;

		static RelayedHttpUtility()
		{
			RelayedHttpUtility.FormatStringDefault = WebContentFormat.Default.ToString();
			RelayedHttpUtility.FormatStringXml = WebContentFormat.Xml.ToString();
			RelayedHttpUtility.FormatStringRaw = WebContentFormat.Raw.ToString();
			RelayedHttpUtility.FormatStringJson = WebContentFormat.Json.ToString();
			RelayedHttpUtility.MaxQuotas = new XmlDictionaryReaderQuotas();
			TextMessageEncodingBindingElement textMessageEncodingBindingElement = new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8);
			textMessageEncodingBindingElement.ReaderQuotas.MaxBytesPerRead = 65536;
			RelayedHttpUtility.encoderSoap11 = textMessageEncodingBindingElement.CreateMessageEncoderFactory().Encoder;
			TextMessageEncodingBindingElement textMessageEncodingBindingElement1 = new TextMessageEncodingBindingElement(MessageVersion.Soap12WSAddressing10, Encoding.UTF8);
			textMessageEncodingBindingElement1.ReaderQuotas.MaxBytesPerRead = 65536;
			RelayedHttpUtility.encoderSoap12 = textMessageEncodingBindingElement1.CreateMessageEncoderFactory().Encoder;
			RelayedHttpUtility.wrapperSoap11 = new MessageWrapper(RelayedHttpUtility.encoderSoap11);
			RelayedHttpUtility.wrapperSoap12 = new MessageWrapper(RelayedHttpUtility.encoderSoap12);
			XmlDictionaryReaderQuotas.Max.CopyTo(RelayedHttpUtility.MaxQuotas);
		}

		public static bool CompareHttpStatusCode(Message reply, HttpStatusCode httpStatusCode)
		{
			HttpStatusCode? nullable = RelayedHttpUtility.GetHttpStatusCode(reply);
			if (nullable.HasValue)
			{
				HttpStatusCode? nullable1 = nullable;
				if ((nullable1.GetValueOrDefault() != httpStatusCode ? false : nullable1.HasValue))
				{
					return true;
				}
			}
			return false;
		}

		private static void ConvertSoapHeadersToWebProperties(Message soapMessage, WebHeaderCollection httpHeaders)
		{
			for (int i = 0; i < soapMessage.Headers.Count; i++)
			{
				MessageHeaderInfo item = soapMessage.Headers[i];
				if (item.Namespace == "http://schemas.microsoft.com/netservices/2009/05/servicebus/web" && !item.Name.Equals("HttpAddress", StringComparison.Ordinal) && !item.Name.Equals("FaultReason", StringComparison.Ordinal))
				{
					using (XmlDictionaryReader readerAtHeader = soapMessage.Headers.GetReaderAtHeader(i))
					{
						httpHeaders.Add(item.Name, readerAtHeader.ReadString());
					}
				}
			}
		}

		public static Message ConvertSoapRequestToWebRequest(MessageEncoder webEncoder, Message soapRequest)
		{
			bool flag;
			return RelayedHttpUtility.ConvertSoapRequestToWebRequest(webEncoder, soapRequest, out flag, null, false);
		}

		internal static Message ConvertSoapRequestToWebRequest(MessageEncoder webEncoder, Message soapRequest, out bool isSoapRequest, MessageWrapper wrapper, bool preserveRawHttp = false)
		{
			Message action;
			isSoapRequest = false;
			string str = RelayedHttpBodyFormatHeader.ReadHeader(soapRequest);
			if (!preserveRawHttp)
			{
				str = RelayedHttpUtility.UpdateJsonFormatStringFromContentType(soapRequest, str);
			}
			if (str != null && str == "WrappedSoap11")
			{
				wrapper = wrapper ?? RelayedHttpUtility.wrapperSoap11;
				action = wrapper.UnwrapMessage(soapRequest);
				isSoapRequest = true;
			}
			else if (str == null || !(str == "WrappedSoap12"))
			{
				if (!soapRequest.IsEmpty)
				{
					action = Message.CreateMessage(soapRequest.GetReaderAtBodyContents(), 0, MessageVersion.None);
					action.Headers.Action = soapRequest.Headers.Action;
				}
				else
				{
					action = Message.CreateMessage(MessageVersion.None, soapRequest.Headers.Action);
				}
				if (str == RelayedHttpUtility.FormatStringRaw)
				{
					action.Properties.Add("WebBodyFormatMessageProperty", new WebBodyFormatMessageProperty(WebContentFormat.Raw));
				}
			}
			else
			{
				wrapper = wrapper ?? RelayedHttpUtility.wrapperSoap12;
				action = wrapper.UnwrapMessage(soapRequest);
				isSoapRequest = true;
			}
			Uri httpUri = null;
			int num = soapRequest.Headers.FindHeader("HttpAddress", "http://schemas.microsoft.com/netservices/2009/05/servicebus/web");
			if (num < 0)
			{
				httpUri = RelayedHttpUtility.ConvertToHttpUri(soapRequest.Headers.To);
			}
			else
			{
				using (XmlDictionaryReader readerAtHeader = soapRequest.Headers.GetReaderAtHeader(num))
				{
					httpUri = new Uri(readerAtHeader.ReadString());
				}
			}
			if (action.Headers.To == null)
			{
				action.Headers.To = httpUri;
			}
			action.Properties.Via = httpUri;
			HttpRequestMessageProperty httpRequestMessageProperty = new HttpRequestMessageProperty();
			RelayedHttpUtility.ConvertSoapHeadersToWebProperties(soapRequest, httpRequestMessageProperty.Headers);
			if (soapRequest.Headers.Action != null)
			{
				httpRequestMessageProperty.Method = soapRequest.Headers.Action;
			}
			if (httpUri.Query.Length > 1)
			{
				httpRequestMessageProperty.QueryString = httpUri.Query.Substring(1);
			}
			action.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessageProperty);
			if (str == RelayedHttpUtility.FormatStringJson)
			{
				action = RelayedHttpUtility.EncodeRequestToTargetContentType(webEncoder, action);
			}
			return action;
		}

		public static Message ConvertSoapResponseToWebResponse(Message soapResponse)
		{
			Message message;
			HttpResponseMessageProperty httpResponseMessageProperty;
			object obj;
			if (soapResponse.IsFault)
			{
				throw Fx.Exception.AsInformation(new FaultException(MessageFault.CreateFault(soapResponse, 65536)), null);
			}
			HttpStatusCode httpStatusCode = RelayedHttpStatusCodeHeader.ReadHeader(soapResponse);
			string str = RelayedHttpHeader.ReadHeader(soapResponse, "FaultReason", null);
			message = (!soapResponse.IsEmpty ? Message.CreateMessage(MessageVersion.None, null, new RelayedHttpUtility.FixupBase64XmlReader(soapResponse.GetReaderAtBodyContents())) : RelayedHttpUtility.CreateHttpReplyMessage(httpStatusCode, null, null, str));
			if (!message.Properties.TryGetValue(HttpResponseMessageProperty.Name, out obj))
			{
				httpResponseMessageProperty = new HttpResponseMessageProperty()
				{
					StatusCode = httpStatusCode
				};
				message.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty);
			}
			else
			{
				httpResponseMessageProperty = (HttpResponseMessageProperty)obj;
			}
			RelayedHttpUtility.ConvertSoapHeadersToWebProperties(soapResponse, httpResponseMessageProperty.Headers);
			return message;
		}

		public static Message ConvertSoapResponseToWebResponse(Message soapResponse, string contentType)
		{
			if (soapResponse.Version.Addressing == AddressingVersion.None)
			{
				soapResponse.Headers.Action = null;
			}
			HttpResponseMessageProperty httpResponseMessageProperty = new HttpResponseMessageProperty();
			RelayedHttpUtility.ConvertSoapHeadersToWebProperties(soapResponse, httpResponseMessageProperty.Headers);
			httpResponseMessageProperty.Headers[HttpResponseHeader.ContentType] = contentType;
			httpResponseMessageProperty.StatusCode = HttpStatusCode.OK;
			Message message = Message.CreateMessage(MessageVersion.None, (string)null, new RelayedHttpUtility.MessageBodyWriter(soapResponse));
			message.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty);
			return message;
		}

		public static Message ConvertSoapResponseToWrappedSoapResponse(this Message soapResponse, string responseAction, MessageWrapper wrapper)
		{
			object obj;
			Message message = wrapper.WrapMessage(soapResponse, responseAction);
			if (soapResponse.Properties.TryGetValue(HttpResponseMessageProperty.Name, out obj))
			{
				HttpResponseMessageProperty httpResponseMessageProperty = (HttpResponseMessageProperty)obj;
				RelayedHttpUtility.ConvertWebPropertiesToSoapHeaders(message, httpResponseMessageProperty.Headers);
				message.Headers.Add(new RelayedHttpStatusCodeHeader(httpResponseMessageProperty.StatusCode));
			}
			RelayedHttpBodyFormatHeader relayedHttpBodyFormatHeader = new RelayedHttpBodyFormatHeader((soapResponse.Version.Envelope == EnvelopeVersion.Soap11 ? "WrappedSoap11" : "WrappedSoap12"));
			message.Headers.Add(relayedHttpBodyFormatHeader);
			return message;
		}

		public static Uri ConvertToHttpsUri(Uri uri)
		{
			return RelayedHttpUtility.ConvertUriScheme(uri, Uri.UriSchemeHttps, RelayEnvironment.RelayHttpsPort);
		}

		public static Uri ConvertToHttpUri(Uri uri)
		{
			return RelayedHttpUtility.ConvertUriScheme(uri, Uri.UriSchemeHttp, RelayEnvironment.RelayHttpPort);
		}

		public static Uri ConvertToSbUri(Uri uri)
		{
			return RelayedHttpUtility.ConvertUriScheme(uri, "sb", -1);
		}

		private static Uri ConvertUriScheme(Uri uri, string newScheme, int newPort)
		{
			if (uri == null)
			{
				return null;
			}
			if ((newPort == -1 && uri.IsDefaultPort || uri.Port == newPort) && uri.Scheme == newScheme)
			{
				return uri;
			}
			UriBuilder uriBuilder = new UriBuilder(uri)
			{
				Scheme = newScheme,
				Port = newPort
			};
			return uriBuilder.Uri;
		}

		private static void ConvertWebAddressToSoapHeader(Message message, Uri uri)
		{
			RelayedHttpHeader relayedHttpHeader = new RelayedHttpHeader("HttpAddress", uri.AbsoluteUri);
			message.Headers.Add(relayedHttpHeader);
		}

		private static void ConvertWebPropertiesToSoapHeaders(Message message, WebHeaderCollection headers)
		{
			string[] allKeys = headers.AllKeys;
			for (int i = 0; i < (int)allKeys.Length; i++)
			{
				string str = allKeys[i];
				RelayedHttpHeader relayedHttpHeader = new RelayedHttpHeader(str, headers[str]);
				message.Headers.Add(relayedHttpHeader);
			}
		}

		public static Message ConvertWebRequestToSoapRequest(Message webRequest, Uri to, string formatString, bool overrideHttpMethodToGet)
		{
			XmlDictionaryReader xmlDictionaryReader = null;
			return RelayedHttpUtility.ConvertWebRequestToSoapRequest(webRequest, to, formatString, overrideHttpMethodToGet, false, ref xmlDictionaryReader);
		}

		public static Message ConvertWebRequestToSoapRequest(Message webRequest, Uri to, string formatString, bool overrideHttpMethodToGet, bool preserveMessage, ref XmlDictionaryReader bodyReader)
		{
			object obj;
			Message uniqueId;
			webRequest.Properties.TryGetValue(HttpRequestMessageProperty.Name, out obj);
			HttpRequestMessageProperty httpRequestMessageProperty = (HttpRequestMessageProperty)obj;
			long num = (long)0;
			string item = httpRequestMessageProperty.Headers[HttpRequestHeader.ContentLength];
			string str = httpRequestMessageProperty.Headers[HttpRequestHeader.TransferEncoding];
			if (!string.IsNullOrEmpty(item) && !long.TryParse(item, out num))
			{
				num = (long)0;
			}
			if (num == (long)0 && (string.IsNullOrEmpty(str) || !str.Equals("Chunked", StringComparison.OrdinalIgnoreCase)) || overrideHttpMethodToGet)
			{
				uniqueId = (!overrideHttpMethodToGet ? Message.CreateMessage(MessageVersion.Soap12WSAddressing10, httpRequestMessageProperty.Method) : Message.CreateMessage(MessageVersion.Soap12WSAddressing10, "GET"));
			}
			else if (formatString == RelayedHttpUtility.FormatStringXml || formatString == RelayedHttpUtility.FormatStringDefault)
			{
				if (bodyReader == null)
				{
					bodyReader = webRequest.GetReaderAtBodyContents();
				}
				uniqueId = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, httpRequestMessageProperty.Method, bodyReader);
			}
			else if (formatString == "WrappedSoap11" || formatString == "WrappedSoap12")
			{
				uniqueId = webRequest;
			}
			else
			{
				if (bodyReader == null)
				{
					bodyReader = webRequest.GetReaderAtBodyContents();
				}
				Stream stream = StreamMessageHelper.GetStream(bodyReader);
				uniqueId = StreamMessageHelper.CreateMessage(MessageVersion.Soap12WSAddressing10, httpRequestMessageProperty.Method, stream);
			}
			if (uniqueId != webRequest)
			{
				uniqueId.Headers.To = to;
				uniqueId.Headers.MessageId = new UniqueId();
				uniqueId.Headers.ReplyTo = new EndpointAddress(EndpointAddress.AnonymousUri, new AddressHeader[0]);
			}
			RelayedHttpUtility.ConvertWebPropertiesToSoapHeaders(uniqueId, httpRequestMessageProperty.Headers);
			RelayedHttpUtility.ConvertWebAddressToSoapHeader(uniqueId, webRequest.Properties.Via);
			uniqueId.Properties.CopyProperties(webRequest.Properties);
			if (preserveMessage)
			{
				uniqueId = new RelayedHttpUtility.NoCloseMessageWrapper(uniqueId);
			}
			return uniqueId;
		}

		public static Message ConvertWebResponseToSoapResponse(Message webResponse, string responseAction)
		{
			Message allowOutputBatching;
			object obj;
			object obj1;
			object obj2;
			object obj3;
			if (webResponse.IsEmpty)
			{
				allowOutputBatching = Message.CreateMessage(MessageVersion.Soap12WSAddressing10, responseAction);
			}
			else if (!webResponse.Properties.TryGetValue("StreamMessageProperty", out obj))
			{
				Type type = webResponse.GetType();
				if (type.FullName == "System.ServiceModel.Channels.ByteStreamMessage+InternalByteStreamMessage")
				{
					FieldInfo field = type.GetField("reader", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field != null)
					{
						object value = field.GetValue(webResponse);
						FieldInfo fieldInfo = value.GetType().GetField("quotas", BindingFlags.Instance | BindingFlags.NonPublic);
						if (fieldInfo != null && fieldInfo.FieldType.FullName == "System.Xml.XmlDictionaryReaderQuotas")
						{
							fieldInfo.SetValue(value, RelayedHttpUtility.MaxQuotas);
						}
					}
				}
				XmlReader readerAtBodyContents = webResponse.GetReaderAtBodyContents();
				allowOutputBatching = (readerAtBodyContents == null || readerAtBodyContents.EOF ? Message.CreateMessage(MessageVersion.Soap12WSAddressing10, responseAction) : Message.CreateMessage(MessageVersion.Soap12WSAddressing10, responseAction, readerAtBodyContents));
			}
			else
			{
				allowOutputBatching = StreamMessageHelper.CreateMessage(MessageVersion.Soap12WSAddressing10, responseAction, ((StreamMessageProperty)obj).Stream);
			}
			if (webResponse.Properties.TryGetValue(HttpResponseMessageProperty.Name, out obj1))
			{
				HttpResponseMessageProperty httpResponseMessageProperty = (HttpResponseMessageProperty)obj1;
				RelayedHttpUtility.ConvertWebPropertiesToSoapHeaders(allowOutputBatching, httpResponseMessageProperty.Headers);
				if (allowOutputBatching.Headers.FindHeader("StatusCode", "http://schemas.microsoft.com/netservices/2009/05/servicebus/statuscode") < 0)
				{
					allowOutputBatching.Headers.Add(new RelayedHttpStatusCodeHeader(httpResponseMessageProperty.StatusCode));
				}
				if (allowOutputBatching.Headers.FindHeader("FaultReason", "http://schemas.microsoft.com/netservices/2009/05/servicebus/web") < 0 && httpResponseMessageProperty.StatusDescription != null)
				{
					allowOutputBatching.Headers.Add(new RelayedHttpHeader("FaultReason", httpResponseMessageProperty.StatusDescription));
				}
			}
			if (webResponse.Properties.TryGetValue("WebBodyFormatMessageProperty", out obj2))
			{
				WebBodyFormatMessageProperty webBodyFormatMessageProperty = (WebBodyFormatMessageProperty)obj2;
				allowOutputBatching.Headers.Add(new RelayedHttpBodyFormatHeader(webBodyFormatMessageProperty.Format.ToString()));
			}
			else if (webResponse.Properties.TryGetValue("BodyFormat", out obj3))
			{
				allowOutputBatching.Headers.Add((MessageHeader)obj3);
			}
			allowOutputBatching.Properties.AllowOutputBatching = webResponse.Properties.AllowOutputBatching;
			return allowOutputBatching;
		}

		internal static Message ConvertWrappedSoapResponseToWebResponse(Message wrappedSoapResponse)
		{
			Message message;
			Message message1 = null;
			string contentType = null;
			string str = RelayedHttpBodyFormatHeader.ReadHeader(wrappedSoapResponse);
			if (str != null && str == "WrappedSoap11")
			{
				message1 = RelayedHttpUtility.wrapperSoap11.UnwrapMessage(wrappedSoapResponse);
				contentType = RelayedHttpUtility.encoderSoap11.ContentType;
			}
			else if (str != null && str == "WrappedSoap12")
			{
				message1 = RelayedHttpUtility.wrapperSoap12.UnwrapMessage(wrappedSoapResponse);
				contentType = RelayedHttpUtility.encoderSoap12.ContentType;
			}
			HttpResponseMessageProperty httpResponseMessageProperty = new HttpResponseMessageProperty();
			RelayedHttpUtility.ConvertSoapHeadersToWebProperties(wrappedSoapResponse, httpResponseMessageProperty.Headers);
			httpResponseMessageProperty.Headers[HttpResponseHeader.ContentType] = contentType;
			httpResponseMessageProperty.StatusCode = RelayedHttpStatusCodeHeader.ReadHeader(wrappedSoapResponse, HttpStatusCode.OK);
			if (message1 == null)
			{
				message = Message.CreateMessage(MessageVersion.None, (string)null);
				httpResponseMessageProperty.SuppressEntityBody = true;
			}
			else
			{
				message = Message.CreateMessage(MessageVersion.None, (string)null, new RelayedHttpUtility.MessageBodyWriter(message1));
			}
			message.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty);
			return message;
		}

		public static Message CreateHttpFailureReplyMessage(HttpStatusCode statusCode, string faultReason)
		{
			return RelayedHttpUtility.CreateHttpFailureReplyMessage(statusCode, null, faultReason);
		}

		public static Message CreateHttpFailureReplyMessage(HttpStatusCode statusCode, WebHeaderCollection headers, string faultReason)
		{
			RelayedHttpUtility.HttpErrorMessageBodyWriter httpErrorMessageBodyWriter = new RelayedHttpUtility.HttpErrorMessageBodyWriter(statusCode, faultReason);
			Message webBodyFormatMessageProperty = RelayedHttpUtility.CreateHttpReplyMessage(statusCode, headers, httpErrorMessageBodyWriter, null);
			webBodyFormatMessageProperty.Properties["WebBodyFormatMessageProperty"] = new WebBodyFormatMessageProperty(WebContentFormat.Xml);
			webBodyFormatMessageProperty.Properties["RelayedHttpErrorMessageBodyWriter"] = httpErrorMessageBodyWriter;
			if (statusCode == HttpStatusCode.ServiceUnavailable)
			{
				webBodyFormatMessageProperty.Properties["ServerBusyExceptionMessage"] = faultReason;
			}
			return webBodyFormatMessageProperty;
		}

		public static Message CreateHttpReplyMessage(HttpStatusCode statusCode, WebHeaderCollection headers, BodyWriter bodyWriter, string statusDescription = null)
		{
			Message message;
			HttpResponseMessageProperty httpResponseMessageProperty = new HttpResponseMessageProperty()
			{
				StatusCode = statusCode
			};
			if (statusDescription != null)
			{
				httpResponseMessageProperty.StatusDescription = statusDescription;
			}
			if (bodyWriter != null)
			{
				message = Message.CreateMessage(MessageVersion.None, (string)null, bodyWriter);
			}
			else
			{
				message = Message.CreateMessage(MessageVersion.None, "");
				httpResponseMessageProperty.SuppressEntityBody = true;
			}
			if (headers != null)
			{
				httpResponseMessageProperty.Headers.Add(headers);
			}
			message.Properties.Add(HttpResponseMessageProperty.Name, httpResponseMessageProperty);
			return message;
		}

		private static Message EncodeRequestToTargetContentType(MessageEncoder webEncoder, Message fromMessage)
		{
			HttpRequestMessageProperty item = (HttpRequestMessageProperty)fromMessage.Properties[HttpRequestMessageProperty.Name];
			string str = item.Headers["Content-Type"];
			XmlDictionaryReader readerAtBodyContents = fromMessage.GetReaderAtBodyContents();
			readerAtBodyContents.Read();
			Message to = webEncoder.ReadMessage(new XmlDictionaryReaderStream(readerAtBodyContents), 65536, str);
			to.Properties.CopyProperties(fromMessage.Properties);
			to.Headers.To = fromMessage.Headers.To;
			return to;
		}

		public static HttpStatusCode? GetHttpStatusCode(Message reply)
		{
			if (reply == null)
			{
				return null;
			}
			HttpResponseMessageProperty item = (HttpResponseMessageProperty)reply.Properties[HttpResponseMessageProperty.Name];
			if (item != null)
			{
				return new HttpStatusCode?(item.StatusCode);
			}
			return null;
		}

		public static bool IsSoap11Message(Message message)
		{
			return message.Version.Envelope == EnvelopeVersion.Soap11;
		}

		public static bool IsSoap12Message(Message message)
		{
			return message.Version.Envelope == EnvelopeVersion.Soap12;
		}

		public static string UpdateJsonFormatStringFromContentType(Message soapRequest, string formatString)
		{
			if (formatString == RelayedHttpUtility.FormatStringRaw)
			{
				string str = RelayedHttpHeader.ReadHeader(soapRequest, "Content-Type", null);
				if (str != null && str.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
				{
					formatString = RelayedHttpUtility.FormatStringJson;
				}
			}
			return formatString;
		}

		private sealed class FixupBase64XmlReader : RelayedHttpUtility.LayeredXmlReader
		{
			private const int Base64Boundary = 4;

			private const int BinaryElementDepth = 2;

			private const int BinaryTextDepth = 3;

			private const string BinaryElementName = "Binary";

			private readonly static string BinaryElementNamespace;

			private string base64CompatibleValue;

			private string carriedOverChars;

			private bool processingBinaryNode;

			public override string Value
			{
				get
				{
					if (this.processingBinaryNode && base.InnerReader.NodeType == XmlNodeType.Text && base.InnerReader.Depth == 3)
					{
						return this.base64CompatibleValue;
					}
					return base.Value;
				}
			}

			static FixupBase64XmlReader()
			{
				RelayedHttpUtility.FixupBase64XmlReader.BinaryElementNamespace = string.Empty;
			}

			public FixupBase64XmlReader(XmlDictionaryReader innerReader) : base(innerReader)
			{
			}

			public override XmlNodeType MoveToContent()
			{
				XmlNodeType content = base.MoveToContent();
				this.OnMoved();
				return content;
			}

			public override bool MoveToElement()
			{
				bool element = base.MoveToElement();
				this.OnMoved();
				return element;
			}

			private void OnMoved()
			{
				if (this.processingBinaryNode)
				{
					if (base.InnerReader.NodeType == XmlNodeType.Text && base.InnerReader.Depth == 3)
					{
						string str = string.Concat(this.carriedOverChars, base.InnerReader.Value);
						int length = str.Length / 4 * 4;
						if (str.Length == length)
						{
							this.base64CompatibleValue = str;
							this.carriedOverChars = null;
							return;
						}
						this.carriedOverChars = str.Substring(length, str.Length - length);
						this.base64CompatibleValue = str.Substring(0, length);
						return;
					}
					if (base.InnerReader.NodeType == XmlNodeType.EndElement && base.InnerReader.Depth == 2 && base.InnerReader.LocalName == "Binary" && base.InnerReader.NamespaceURI == RelayedHttpUtility.FixupBase64XmlReader.BinaryElementNamespace)
					{
						Fx.AssertAndThrow(this.carriedOverChars == null, "The inner XmlReader contained an invalid length for base64 content!");
						this.processingBinaryNode = false;
					}
				}
				else if (base.InnerReader.NodeType == XmlNodeType.Element && base.InnerReader.Depth == 2 && base.InnerReader.LocalName == "Binary" && base.InnerReader.NamespaceURI == RelayedHttpUtility.FixupBase64XmlReader.BinaryElementNamespace && !base.InnerReader.IsEmptyElement)
				{
					this.processingBinaryNode = true;
					return;
				}
			}

			public override bool Read()
			{
				bool flag = base.Read();
				this.OnMoved();
				return flag;
			}
		}

		public class HttpErrorMessageBodyWriter : BodyWriter
		{
			public const string Name = "RelayedHttpErrorMessageBodyWriter";

			public string FaultReason
			{
				get;
				private set;
			}

			public HttpStatusCode StatusCode
			{
				get;
				private set;
			}

			internal HttpErrorMessageBodyWriter(HttpStatusCode statusCode, string faultReason) : base(true)
			{
				this.StatusCode = statusCode;
				this.FaultReason = faultReason;
			}

			protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
			{
				writer.WriteStartElement("Error");
				writer.WriteStartElement("Code");
				writer.WriteValue(Convert.ToInt32(this.StatusCode, CultureInfo.InvariantCulture));
				writer.WriteEndElement();
				writer.WriteElementString("Detail", this.FaultReason);
				writer.WriteEndElement();
			}
		}

		private abstract class LayeredXmlReader : XmlDictionaryReader, IXmlLineInfo
		{
			public override int AttributeCount
			{
				get
				{
					return this.InnerReader.AttributeCount;
				}
			}

			public override string BaseURI
			{
				get
				{
					return this.InnerReader.BaseURI;
				}
			}

			public override bool CanCanonicalize
			{
				get
				{
					return this.InnerReader.CanCanonicalize;
				}
			}

			public override bool CanReadBinaryContent
			{
				get
				{
					return this.InnerReader.CanReadBinaryContent;
				}
			}

			public override bool CanReadValueChunk
			{
				get
				{
					return this.InnerReader.CanReadValueChunk;
				}
			}

			public override bool CanResolveEntity
			{
				get
				{
					return this.InnerReader.CanResolveEntity;
				}
			}

			public override int Depth
			{
				get
				{
					return this.InnerReader.Depth;
				}
			}

			public override bool EOF
			{
				get
				{
					return this.InnerReader.EOF;
				}
			}

			protected XmlDictionaryReader InnerReader
			{
				get;
				private set;
			}

			public override bool IsDefault
			{
				get
				{
					return this.InnerReader.IsDefault;
				}
			}

			public override bool IsEmptyElement
			{
				get
				{
					return this.InnerReader.IsEmptyElement;
				}
			}

			public override string this[string name]
			{
				get
				{
					return this.InnerReader[name];
				}
			}

			public override string this[string name, string namespaceURI]
			{
				get
				{
					return this.InnerReader[name, namespaceURI];
				}
			}

			public override string this[int i]
			{
				get
				{
					return this.InnerReader[i];
				}
			}

			public virtual int LineNumber
			{
				get
				{
					IXmlLineInfo innerReader = this.InnerReader as IXmlLineInfo;
					if (innerReader == null)
					{
						return 1;
					}
					return innerReader.LineNumber;
				}
			}

			public virtual int LinePosition
			{
				get
				{
					IXmlLineInfo innerReader = this.InnerReader as IXmlLineInfo;
					if (innerReader == null)
					{
						return 1;
					}
					return innerReader.LinePosition;
				}
			}

			public override string LocalName
			{
				get
				{
					return this.InnerReader.LocalName;
				}
			}

			public override string NamespaceURI
			{
				get
				{
					return this.InnerReader.NamespaceURI;
				}
			}

			public override XmlNameTable NameTable
			{
				get
				{
					return this.InnerReader.NameTable;
				}
			}

			public override XmlNodeType NodeType
			{
				get
				{
					return this.InnerReader.NodeType;
				}
			}

			public override string Prefix
			{
				get
				{
					return this.InnerReader.Prefix;
				}
			}

			public override XmlDictionaryReaderQuotas Quotas
			{
				get
				{
					return this.InnerReader.Quotas;
				}
			}

			public override char QuoteChar
			{
				get
				{
					return this.InnerReader.QuoteChar;
				}
			}

			public override System.Xml.ReadState ReadState
			{
				get
				{
					return this.InnerReader.ReadState;
				}
			}

			public override IXmlSchemaInfo SchemaInfo
			{
				get
				{
					return this.InnerReader.SchemaInfo;
				}
			}

			public override XmlReaderSettings Settings
			{
				get
				{
					return this.InnerReader.Settings;
				}
			}

			public override string Value
			{
				get
				{
					return this.InnerReader.Value;
				}
			}

			public override Type ValueType
			{
				get
				{
					return this.InnerReader.ValueType;
				}
			}

			public override string XmlLang
			{
				get
				{
					return this.InnerReader.XmlLang;
				}
			}

			public override XmlSpace XmlSpace
			{
				get
				{
					return this.InnerReader.XmlSpace;
				}
			}

			protected LayeredXmlReader(XmlDictionaryReader innerReader)
			{
				if (innerReader == null)
				{
					throw new ArgumentNullException("innerReader");
				}
				this.InnerReader = innerReader;
			}

			public override void Close()
			{
				this.InnerReader.Close();
			}

			public override void EndCanonicalization()
			{
				this.InnerReader.EndCanonicalization();
			}

			public override string GetAttribute(int i)
			{
				return this.InnerReader.GetAttribute(i);
			}

			public override string GetAttribute(string name)
			{
				return this.InnerReader.GetAttribute(name);
			}

			public override string GetAttribute(string name, string namespaceURI)
			{
				return this.InnerReader.GetAttribute(name, namespaceURI);
			}

			public virtual bool HasLineInfo()
			{
				IXmlLineInfo innerReader = this.InnerReader as IXmlLineInfo;
				if (innerReader == null)
				{
					return false;
				}
				return innerReader.HasLineInfo();
			}

			public override bool IsStartArray(out Type type)
			{
				return this.InnerReader.IsStartArray(out type);
			}

			public override bool IsStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.IsStartElement(localName, namespaceUri);
			}

			public override string LookupNamespace(string prefix)
			{
				return this.InnerReader.LookupNamespace(prefix);
			}

			public override bool MoveToAttribute(string name)
			{
				return this.InnerReader.MoveToAttribute(name);
			}

			public override bool MoveToAttribute(string name, string ns)
			{
				return this.InnerReader.MoveToAttribute(name, ns);
			}

			public override void MoveToAttribute(int i)
			{
				this.InnerReader.MoveToAttribute(i);
			}

			public override XmlNodeType MoveToContent()
			{
				return this.InnerReader.MoveToContent();
			}

			public override bool MoveToElement()
			{
				return this.InnerReader.MoveToElement();
			}

			public override bool MoveToFirstAttribute()
			{
				return this.InnerReader.MoveToFirstAttribute();
			}

			public override bool MoveToNextAttribute()
			{
				return this.InnerReader.MoveToNextAttribute();
			}

			public override void MoveToStartElement()
			{
				this.InnerReader.MoveToStartElement();
			}

			public override bool Read()
			{
				return this.InnerReader.Read();
			}

			public override int ReadArray(string localName, string namespaceUri, bool[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, DateTime[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, decimal[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, double[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, float[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, Guid[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, int[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, long[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, short[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(string localName, string namespaceUri, TimeSpan[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, bool[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, DateTime[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, decimal[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, double[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, float[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, Guid[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, int[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, long[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, short[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override int ReadArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri, TimeSpan[] array, int offset, int count)
			{
				return this.InnerReader.ReadArray(localName, namespaceUri, array, offset, count);
			}

			public override bool ReadAttributeValue()
			{
				return this.InnerReader.ReadAttributeValue();
			}

			public override bool[] ReadBooleanArray(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadBooleanArray(localName, namespaceUri);
			}

			public override bool[] ReadBooleanArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadBooleanArray(localName, namespaceUri);
			}

			public override object ReadContentAs(Type type, IXmlNamespaceResolver namespaceResolver)
			{
				return this.InnerReader.ReadContentAs(type, namespaceResolver);
			}

			public override byte[] ReadContentAsBase64()
			{
				return this.InnerReader.ReadContentAsBase64();
			}

			public override int ReadContentAsBase64(byte[] buffer, int index, int count)
			{
				return this.InnerReader.ReadContentAsBase64(buffer, index, count);
			}

			public override byte[] ReadContentAsBinHex()
			{
				return this.InnerReader.ReadContentAsBinHex();
			}

			public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
			{
				return this.InnerReader.ReadContentAsBinHex(buffer, index, count);
			}

			public override bool ReadContentAsBoolean()
			{
				return this.InnerReader.ReadContentAsBoolean();
			}

			public override int ReadContentAsChars(char[] chars, int offset, int count)
			{
				return this.InnerReader.ReadContentAsChars(chars, offset, count);
			}

			public override DateTime ReadContentAsDateTime()
			{
				return this.InnerReader.ReadContentAsDateTime();
			}

			public override decimal ReadContentAsDecimal()
			{
				return this.InnerReader.ReadContentAsDecimal();
			}

			public override double ReadContentAsDouble()
			{
				return this.InnerReader.ReadContentAsDouble();
			}

			public override float ReadContentAsFloat()
			{
				return this.InnerReader.ReadContentAsFloat();
			}

			public override Guid ReadContentAsGuid()
			{
				return this.InnerReader.ReadContentAsGuid();
			}

			public override int ReadContentAsInt()
			{
				return this.InnerReader.ReadContentAsInt();
			}

			public override long ReadContentAsLong()
			{
				return this.InnerReader.ReadContentAsLong();
			}

			public override object ReadContentAsObject()
			{
				return this.InnerReader.ReadContentAsObject();
			}

			public override void ReadContentAsQualifiedName(out string localName, out string namespaceUri)
			{
				this.InnerReader.ReadContentAsQualifiedName(out localName, out namespaceUri);
			}

			public override string ReadContentAsString()
			{
				return this.InnerReader.ReadContentAsString();
			}

			public override string ReadContentAsString(string[] strings, out int index)
			{
				return this.InnerReader.ReadContentAsString(strings, out index);
			}

			public override string ReadContentAsString(XmlDictionaryString[] strings, out int index)
			{
				return this.InnerReader.ReadContentAsString(strings, out index);
			}

			public override TimeSpan ReadContentAsTimeSpan()
			{
				return this.InnerReader.ReadContentAsTimeSpan();
			}

			public override UniqueId ReadContentAsUniqueId()
			{
				return this.InnerReader.ReadContentAsUniqueId();
			}

			public override DateTime[] ReadDateTimeArray(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadDateTimeArray(localName, namespaceUri);
			}

			public override DateTime[] ReadDateTimeArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadDateTimeArray(localName, namespaceUri);
			}

			public override decimal[] ReadDecimalArray(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadDecimalArray(localName, namespaceUri);
			}

			public override decimal[] ReadDecimalArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadDecimalArray(localName, namespaceUri);
			}

			public override double[] ReadDoubleArray(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadDoubleArray(localName, namespaceUri);
			}

			public override double[] ReadDoubleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadDoubleArray(localName, namespaceUri);
			}

			public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
			{
				return this.InnerReader.ReadElementContentAs(returnType, namespaceResolver);
			}

			public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAs(returnType, namespaceResolver, localName, namespaceURI);
			}

			public override byte[] ReadElementContentAsBase64()
			{
				return this.InnerReader.ReadElementContentAsBase64();
			}

			public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
			{
				return this.InnerReader.ReadElementContentAsBase64(buffer, index, count);
			}

			public override byte[] ReadElementContentAsBinHex()
			{
				return this.InnerReader.ReadElementContentAsBinHex();
			}

			public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
			{
				return this.InnerReader.ReadElementContentAsBinHex(buffer, index, count);
			}

			public override bool ReadElementContentAsBoolean()
			{
				return this.InnerReader.ReadElementContentAsBoolean();
			}

			public override bool ReadElementContentAsBoolean(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsBoolean(localName, namespaceURI);
			}

			public override DateTime ReadElementContentAsDateTime()
			{
				return this.InnerReader.ReadElementContentAsDateTime();
			}

			public override DateTime ReadElementContentAsDateTime(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsDateTime(localName, namespaceURI);
			}

			public override decimal ReadElementContentAsDecimal()
			{
				return this.InnerReader.ReadElementContentAsDecimal();
			}

			public override decimal ReadElementContentAsDecimal(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsDecimal(localName, namespaceURI);
			}

			public override double ReadElementContentAsDouble()
			{
				return this.InnerReader.ReadElementContentAsDouble();
			}

			public override double ReadElementContentAsDouble(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsDouble(localName, namespaceURI);
			}

			public override float ReadElementContentAsFloat()
			{
				return this.InnerReader.ReadElementContentAsFloat();
			}

			public override float ReadElementContentAsFloat(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsFloat(localName, namespaceURI);
			}

			public override Guid ReadElementContentAsGuid()
			{
				return this.InnerReader.ReadElementContentAsGuid();
			}

			public override int ReadElementContentAsInt()
			{
				return this.InnerReader.ReadElementContentAsInt();
			}

			public override int ReadElementContentAsInt(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsInt(localName, namespaceURI);
			}

			public override long ReadElementContentAsLong()
			{
				return this.InnerReader.ReadElementContentAsLong();
			}

			public override long ReadElementContentAsLong(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsLong(localName, namespaceURI);
			}

			public override object ReadElementContentAsObject()
			{
				return this.InnerReader.ReadElementContentAsObject();
			}

			public override object ReadElementContentAsObject(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsObject(localName, namespaceURI);
			}

			public override string ReadElementContentAsString()
			{
				return this.InnerReader.ReadElementContentAsString();
			}

			public override string ReadElementContentAsString(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadElementContentAsString(localName, namespaceURI);
			}

			public override TimeSpan ReadElementContentAsTimeSpan()
			{
				return this.InnerReader.ReadElementContentAsTimeSpan();
			}

			public override UniqueId ReadElementContentAsUniqueId()
			{
				return this.InnerReader.ReadElementContentAsUniqueId();
			}

			public override string ReadElementString()
			{
				return this.InnerReader.ReadElementString();
			}

			public override string ReadElementString(string localname, string ns)
			{
				return this.InnerReader.ReadElementString(localname, ns);
			}

			public override string ReadElementString(string name)
			{
				return this.InnerReader.ReadElementString(name);
			}

			public override void ReadEndElement()
			{
				this.InnerReader.ReadEndElement();
			}

			public override void ReadFullStartElement()
			{
				this.InnerReader.ReadFullStartElement();
			}

			public override void ReadFullStartElement(string localName, string namespaceUri)
			{
				this.InnerReader.ReadFullStartElement(localName, namespaceUri);
			}

			public override void ReadFullStartElement(string name)
			{
				this.InnerReader.ReadFullStartElement(name);
			}

			public override void ReadFullStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				this.InnerReader.ReadFullStartElement(localName, namespaceUri);
			}

			public override Guid[] ReadGuidArray(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadGuidArray(localName, namespaceUri);
			}

			public override Guid[] ReadGuidArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadGuidArray(localName, namespaceUri);
			}

			public override string ReadInnerXml()
			{
				return this.InnerReader.ReadInnerXml();
			}

			public override short[] ReadInt16Array(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadInt16Array(localName, namespaceUri);
			}

			public override short[] ReadInt16Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadInt16Array(localName, namespaceUri);
			}

			public override int[] ReadInt32Array(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadInt32Array(localName, namespaceUri);
			}

			public override int[] ReadInt32Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadInt32Array(localName, namespaceUri);
			}

			public override long[] ReadInt64Array(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadInt64Array(localName, namespaceUri);
			}

			public override long[] ReadInt64Array(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadInt64Array(localName, namespaceUri);
			}

			public override string ReadOuterXml()
			{
				return this.InnerReader.ReadOuterXml();
			}

			public override float[] ReadSingleArray(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadSingleArray(localName, namespaceUri);
			}

			public override float[] ReadSingleArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadSingleArray(localName, namespaceUri);
			}

			public override void ReadStartElement()
			{
				this.InnerReader.ReadStartElement();
			}

			public override void ReadStartElement(string localname, string ns)
			{
				this.InnerReader.ReadStartElement(localname, ns);
			}

			public override void ReadStartElement(string name)
			{
				this.InnerReader.ReadStartElement(name);
			}

			public override void ReadStartElement(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				this.InnerReader.ReadStartElement(localName, namespaceUri);
			}

			public override string ReadString()
			{
				return this.InnerReader.ReadString();
			}

			public override XmlReader ReadSubtree()
			{
				return this.InnerReader.ReadSubtree();
			}

			public override TimeSpan[] ReadTimeSpanArray(string localName, string namespaceUri)
			{
				return this.InnerReader.ReadTimeSpanArray(localName, namespaceUri);
			}

			public override TimeSpan[] ReadTimeSpanArray(XmlDictionaryString localName, XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.ReadTimeSpanArray(localName, namespaceUri);
			}

			public override bool ReadToDescendant(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadToDescendant(localName, namespaceURI);
			}

			public override bool ReadToDescendant(string name)
			{
				return this.InnerReader.ReadToDescendant(name);
			}

			public override bool ReadToFollowing(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadToFollowing(localName, namespaceURI);
			}

			public override bool ReadToFollowing(string name)
			{
				return this.InnerReader.ReadToFollowing(name);
			}

			public override bool ReadToNextSibling(string localName, string namespaceURI)
			{
				return this.InnerReader.ReadToNextSibling(localName, namespaceURI);
			}

			public override bool ReadToNextSibling(string name)
			{
				return this.InnerReader.ReadToNextSibling(name);
			}

			public override int ReadValueAsBase64(byte[] buffer, int offset, int count)
			{
				return this.InnerReader.ReadValueAsBase64(buffer, offset, count);
			}

			public override int ReadValueChunk(char[] buffer, int index, int count)
			{
				return this.InnerReader.ReadValueChunk(buffer, index, count);
			}

			public override void ResolveEntity()
			{
				this.InnerReader.ResolveEntity();
			}

			public override void Skip()
			{
				this.InnerReader.Skip();
			}

			public override void StartCanonicalization(Stream stream, bool includeComments, string[] inclusivePrefixes)
			{
				this.InnerReader.StartCanonicalization(stream, includeComments, inclusivePrefixes);
			}

			public override bool TryGetArrayLength(out int count)
			{
				return this.InnerReader.TryGetArrayLength(out count);
			}

			public override bool TryGetBase64ContentLength(out int length)
			{
				return this.InnerReader.TryGetBase64ContentLength(out length);
			}

			public override bool TryGetLocalNameAsDictionaryString(out XmlDictionaryString localName)
			{
				return this.InnerReader.TryGetLocalNameAsDictionaryString(out localName);
			}

			public override bool TryGetNamespaceUriAsDictionaryString(out XmlDictionaryString namespaceUri)
			{
				return this.InnerReader.TryGetNamespaceUriAsDictionaryString(out namespaceUri);
			}

			public override bool TryGetValueAsDictionaryString(out XmlDictionaryString value)
			{
				return this.InnerReader.TryGetValueAsDictionaryString(out value);
			}
		}

		private class MessageBodyWriter : BodyWriter
		{
			private Message message;

			public MessageBodyWriter(Message message) : base(false)
			{
				this.message = message;
			}

			protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
			{
				this.message.WriteMessage(writer);
			}
		}

		private sealed class NoCloseMessageWrapper : Message
		{
			private Message innerMessage;

			public override MessageHeaders Headers
			{
				get
				{
					return this.innerMessage.Headers;
				}
			}

			public override bool IsEmpty
			{
				get
				{
					return this.innerMessage.IsEmpty;
				}
			}

			public override bool IsFault
			{
				get
				{
					return this.innerMessage.IsFault;
				}
			}

			public override MessageProperties Properties
			{
				get
				{
					return this.innerMessage.Properties;
				}
			}

			public override MessageVersion Version
			{
				get
				{
					return this.innerMessage.Version;
				}
			}

			public NoCloseMessageWrapper(Message innerMessage)
			{
				this.innerMessage = innerMessage;
			}

			public override bool Equals(object obj)
			{
				return this.innerMessage.Equals(obj);
			}

			public override int GetHashCode()
			{
				return this.innerMessage.GetHashCode();
			}

			protected override void OnClose()
			{
				if (this.innerMessage.State != MessageState.Created)
				{
					this.innerMessage.Close();
					base.OnClose();
				}
			}

			protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
			{
				this.innerMessage.WriteBodyContents(writer);
			}
		}
	}
}