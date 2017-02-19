using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Properties;
using System;
using System.Runtime.Serialization;
using System.Xml;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class PlainXmlWriter : XmlWriter
	{
		private TraceXPathNavigator navigator;

		private bool writingAttribute;

		private string currentAttributeName;

		private string currentAttributePrefix;

		private string currentAttributeNs;

		private string currentAttributeText = string.Empty;

		public TraceXPathNavigator Navigator
		{
			get
			{
				return this.navigator;
			}
		}

		public override System.Xml.WriteState WriteState
		{
			get
			{
				return this.navigator.WriteState;
			}
		}

		public override string XmlLang
		{
			get
			{
				return string.Empty;
			}
		}

		public override System.Xml.XmlSpace XmlSpace
		{
			get
			{
				return System.Xml.XmlSpace.Default;
			}
		}

		public PlainXmlWriter() : this(-1)
		{
		}

		public PlainXmlWriter(int maxSize)
		{
			this.navigator = new TraceXPathNavigator(maxSize);
		}

		public override void Close()
		{
		}

		public override void Flush()
		{
		}

		public override string LookupPrefix(string ns)
		{
			return this.navigator.LookupPrefix(ns);
		}

		public override void WriteBase64(byte[] buffer, int offset, int count)
		{
		}

		public override void WriteCData(string text)
		{
			this.WriteRaw(string.Concat("<![CDATA[", text, "]]>"));
		}

		public override void WriteCharEntity(char ch)
		{
		}

		public override void WriteChars(char[] buffer, int index, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}
			if (index < 0)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			if ((int)buffer.Length - index < count)
			{
				throw new ArgumentException(Microsoft.ServiceBus.SR.GetString(Resources.WriteCharsInvalidContent, new object[0]));
			}
			this.WriteString(new string(buffer, index, count));
		}

		public override void WriteComment(string text)
		{
			this.navigator.AddComment(text);
		}

		public override void WriteDocType(string name, string pubid, string sysid, string subset)
		{
		}

		public override void WriteEndAttribute()
		{
			if (!this.writingAttribute)
			{
				throw new InvalidOperationException();
			}
			this.navigator.AddAttribute(this.currentAttributeName, this.currentAttributeText, this.currentAttributeNs, this.currentAttributePrefix);
			this.writingAttribute = false;
		}

		public override void WriteEndDocument()
		{
		}

		public override void WriteEndElement()
		{
			this.navigator.CloseElement();
		}

		public override void WriteEntityRef(string name)
		{
		}

		public override void WriteFullEndElement()
		{
			this.WriteEndElement();
		}

		public override void WriteProcessingInstruction(string name, string text)
		{
			this.navigator.AddProcessingInstruction(name, text);
		}

		public override void WriteRaw(string data)
		{
			this.WriteString(data);
		}

		public override void WriteRaw(char[] buffer, int index, int count)
		{
			this.WriteChars(buffer, index, count);
		}

		public override void WriteStartAttribute(string prefix, string localName, string ns)
		{
			if (this.writingAttribute)
			{
				throw new InvalidOperationException();
			}
			this.currentAttributeName = localName;
			this.currentAttributePrefix = prefix;
			this.currentAttributeNs = ns;
			this.currentAttributeText = string.Empty;
			this.writingAttribute = true;
		}

		public override void WriteStartDocument()
		{
		}

		public override void WriteStartDocument(bool standalone)
		{
		}

		public override void WriteStartElement(string prefix, string localName, string ns)
		{
			if (string.IsNullOrEmpty(localName))
			{
				throw new ArgumentNullException("localName");
			}
			this.navigator.AddElement(prefix, localName, ns);
		}

		public override void WriteString(string text)
		{
			if (!this.writingAttribute)
			{
				this.WriteValue(text);
				return;
			}
			PlainXmlWriter plainXmlWriter = this;
			plainXmlWriter.currentAttributeText = string.Concat(plainXmlWriter.currentAttributeText, text);
		}

		public override void WriteSurrogateCharEntity(char lowChar, char highChar)
		{
		}

		public override void WriteValue(object value)
		{
			this.navigator.AddText(value.ToString());
		}

		public override void WriteValue(string value)
		{
			this.navigator.AddText(value);
		}

		public override void WriteWhitespace(string ws)
		{
		}

		[Serializable]
		internal class MaxSizeExceededException : Exception
		{
			public MaxSizeExceededException()
			{
			}

			public MaxSizeExceededException(string message) : base(message)
			{
			}

			public MaxSizeExceededException(string message, Exception inner) : base(message, inner)
			{
			}

			protected MaxSizeExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
			{
			}
		}
	}
}