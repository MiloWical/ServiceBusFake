using System;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.Xml;

namespace Microsoft.ServiceBus
{
	internal static class ClientMessageUtility
	{
		private static BinaryMessageEncodingBindingElement defaultEncoderBindingElement;

		private static MessageEncoderFactory defaultEncoderFactory;

		internal static MessageEncoderFactory DefaultBinaryMessageEncoderFactory
		{
			get
			{
				return ClientMessageUtility.defaultEncoderFactory;
			}
		}

		internal static BinaryMessageEncodingBindingElement DefaultBinaryMessageEncodingBindingElement
		{
			get
			{
				return ClientMessageUtility.defaultEncoderBindingElement.Clone() as BinaryMessageEncodingBindingElement;
			}
		}

		static ClientMessageUtility()
		{
			ClientMessageUtility.defaultEncoderBindingElement = new BinaryMessageEncodingBindingElement();
			ClientMessageUtility.defaultEncoderBindingElement.ReaderQuotas.MaxArrayLength = 61440;
			ClientMessageUtility.defaultEncoderBindingElement.ReaderQuotas.MaxStringContentLength = 61440;
			ClientMessageUtility.defaultEncoderBindingElement.ReaderQuotas.MaxDepth = 32;
			ClientMessageUtility.defaultEncoderBindingElement.MaxReadPoolSize = 128;
			ClientMessageUtility.defaultEncoderBindingElement.MaxWritePoolSize = 128;
			ClientMessageUtility.defaultEncoderFactory = ClientMessageUtility.defaultEncoderBindingElement.CreateMessageEncoderFactory();
		}

		internal static BinaryMessageEncodingBindingElement CreateInnerEncodingBindingElement(BindingContext context)
		{
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement;
			if (context == null)
			{
				return ClientMessageUtility.defaultEncoderBindingElement;
			}
			BinaryMessageEncodingBindingElement binaryMessageEncodingBindingElement1 = context.BindingParameters.Find<BinaryMessageEncodingBindingElement>();
			if (binaryMessageEncodingBindingElement1 == null)
			{
				binaryMessageEncodingBindingElement = new BinaryMessageEncodingBindingElement();
				TextMessageEncodingBindingElement textMessageEncodingBindingElement = context.BindingParameters.Find<TextMessageEncodingBindingElement>();
				if (textMessageEncodingBindingElement == null)
				{
					MtomMessageEncodingBindingElement mtomMessageEncodingBindingElement = context.BindingParameters.Find<MtomMessageEncodingBindingElement>();
					if (mtomMessageEncodingBindingElement == null)
					{
						WebMessageEncodingBindingElement webMessageEncodingBindingElement = context.BindingParameters.Find<WebMessageEncodingBindingElement>();
						if (webMessageEncodingBindingElement != null)
						{
							webMessageEncodingBindingElement.ReaderQuotas.CopyTo(binaryMessageEncodingBindingElement.ReaderQuotas);
							binaryMessageEncodingBindingElement.MaxReadPoolSize = webMessageEncodingBindingElement.MaxReadPoolSize;
							binaryMessageEncodingBindingElement.MaxWritePoolSize = webMessageEncodingBindingElement.MaxWritePoolSize;
						}
					}
					else
					{
						mtomMessageEncodingBindingElement.ReaderQuotas.CopyTo(binaryMessageEncodingBindingElement.ReaderQuotas);
						binaryMessageEncodingBindingElement.MaxReadPoolSize = mtomMessageEncodingBindingElement.MaxReadPoolSize;
						binaryMessageEncodingBindingElement.MaxWritePoolSize = mtomMessageEncodingBindingElement.MaxWritePoolSize;
					}
				}
				else
				{
					textMessageEncodingBindingElement.ReaderQuotas.CopyTo(binaryMessageEncodingBindingElement.ReaderQuotas);
					binaryMessageEncodingBindingElement.MaxReadPoolSize = textMessageEncodingBindingElement.MaxReadPoolSize;
					binaryMessageEncodingBindingElement.MaxWritePoolSize = textMessageEncodingBindingElement.MaxWritePoolSize;
				}
			}
			else
			{
				binaryMessageEncodingBindingElement = binaryMessageEncodingBindingElement1.Clone() as BinaryMessageEncodingBindingElement;
			}
			return binaryMessageEncodingBindingElement;
		}
	}
}