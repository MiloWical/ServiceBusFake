using Microsoft.ServiceBus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.ServiceBus.Diagnostics
{
	[DebuggerDisplay("")]
	internal class TraceXPathNavigator : XPathNavigator
	{
		private const int UnlimitedSize = -1;

		private TraceXPathNavigator.ElementNode root;

		private TraceXPathNavigator.TraceNode current;

		private bool closed;

		private XPathNodeType state = XPathNodeType.Element;

		private int maxSize;

		private long currentSize;

		public override string BaseURI
		{
			get
			{
				return string.Empty;
			}
		}

		private TraceXPathNavigator.CommentNode CurrentComment
		{
			get
			{
				return this.current as TraceXPathNavigator.CommentNode;
			}
		}

		private TraceXPathNavigator.ElementNode CurrentElement
		{
			get
			{
				return this.current as TraceXPathNavigator.ElementNode;
			}
		}

		private TraceXPathNavigator.ProcessingInstructionNode CurrentProcessingInstruction
		{
			get
			{
				return this.current as TraceXPathNavigator.ProcessingInstructionNode;
			}
		}

		public override bool IsEmptyElement
		{
			get
			{
				bool flag = true;
				if (this.current != null)
				{
					flag = (this.CurrentElement.text != null ? true : this.CurrentElement.childNodes.Count > 0);
				}
				return flag;
			}
		}

		[DebuggerDisplay("")]
		public override string LocalName
		{
			get
			{
				return this.Name;
			}
		}

		[DebuggerDisplay("")]
		public override string Name
		{
			get
			{
				string empty = string.Empty;
				if (this.current != null)
				{
					XPathNodeType xPathNodeType = this.state;
					switch (xPathNodeType)
					{
						case XPathNodeType.Element:
						{
							empty = this.CurrentElement.name;
							break;
						}
						case XPathNodeType.Attribute:
						{
							empty = this.CurrentElement.CurrentAttribute.name;
							break;
						}
						default:
						{
							if (xPathNodeType == XPathNodeType.ProcessingInstruction)
							{
								empty = this.CurrentProcessingInstruction.name;
								break;
							}
							else
							{
								break;
							}
						}
					}
				}
				return empty;
			}
		}

		[DebuggerDisplay("")]
		public override string NamespaceURI
		{
			get
			{
				string empty = string.Empty;
				if (this.current != null)
				{
					switch (this.state)
					{
						case XPathNodeType.Element:
						{
							empty = this.CurrentElement.xmlns;
							break;
						}
						case XPathNodeType.Attribute:
						{
							empty = this.CurrentElement.CurrentAttribute.xmlns;
							break;
						}
						case XPathNodeType.Namespace:
						{
							empty = null;
							break;
						}
					}
				}
				return empty;
			}
		}

		public override XmlNameTable NameTable
		{
			get
			{
				return null;
			}
		}

		[DebuggerDisplay("")]
		public override XPathNodeType NodeType
		{
			get
			{
				return this.state;
			}
		}

		[DebuggerDisplay("")]
		public override string Prefix
		{
			get
			{
				string empty = string.Empty;
				if (this.current != null)
				{
					switch (this.state)
					{
						case XPathNodeType.Element:
						{
							empty = this.CurrentElement.prefix;
							break;
						}
						case XPathNodeType.Attribute:
						{
							empty = this.CurrentElement.CurrentAttribute.prefix;
							break;
						}
						case XPathNodeType.Namespace:
						{
							empty = null;
							break;
						}
					}
				}
				return empty;
			}
		}

		[DebuggerDisplay("")]
		public override string Value
		{
			get
			{
				string empty = string.Empty;
				if (this.current != null)
				{
					switch (this.state)
					{
						case XPathNodeType.Attribute:
						{
							empty = this.CurrentElement.CurrentAttribute.nodeValue;
							break;
						}
						case XPathNodeType.Text:
						{
							empty = this.CurrentElement.text.nodeValue;
							break;
						}
						case XPathNodeType.ProcessingInstruction:
						{
							empty = this.CurrentProcessingInstruction.text;
							break;
						}
						case XPathNodeType.Comment:
						{
							empty = this.CurrentComment.nodeValue;
							break;
						}
					}
				}
				return empty;
			}
		}

		internal System.Xml.WriteState WriteState
		{
			get
			{
				System.Xml.WriteState writeState = System.Xml.WriteState.Error;
				if (this.current == null)
				{
					writeState = System.Xml.WriteState.Start;
				}
				else if (!this.closed)
				{
					XPathNodeType xPathNodeType = this.state;
					switch (xPathNodeType)
					{
						case XPathNodeType.Element:
						{
							writeState = System.Xml.WriteState.Element;
							break;
						}
						case XPathNodeType.Attribute:
						{
							writeState = System.Xml.WriteState.Attribute;
							break;
						}
						case XPathNodeType.Namespace:
						{
							break;
						}
						case XPathNodeType.Text:
						{
							writeState = System.Xml.WriteState.Content;
							break;
						}
						default:
						{
							if (xPathNodeType == XPathNodeType.Comment)
							{
								writeState = System.Xml.WriteState.Content;
								break;
							}
							else
							{
								break;
							}
						}
					}
				}
				else
				{
					writeState = System.Xml.WriteState.Closed;
				}
				return writeState;
			}
		}

		public TraceXPathNavigator(int maxSize)
		{
			this.maxSize = maxSize;
		}

		internal void AddAttribute(string name, string value, string xmlns, string prefix)
		{
			if (this.closed)
			{
				throw new InvalidOperationException();
			}
			if (this.current == null)
			{
				throw new InvalidOperationException();
			}
			TraceXPathNavigator.AttributeNode attributeNode = new TraceXPathNavigator.AttributeNode(name, prefix, value, xmlns);
			this.VerifySize(attributeNode);
			this.CurrentElement.attributes.Add(attributeNode);
		}

		internal void AddComment(string text)
		{
			if (this.closed)
			{
				throw new InvalidOperationException();
			}
			if (this.current == null)
			{
				throw new InvalidOperationException();
			}
			TraceXPathNavigator.CommentNode commentNode = new TraceXPathNavigator.CommentNode(text, this.CurrentElement);
			this.VerifySize(commentNode);
			this.CurrentElement.Add(commentNode);
		}

		internal void AddElement(string prefix, string name, string xmlns)
		{
			if (this.closed)
			{
				throw new InvalidOperationException();
			}
			TraceXPathNavigator.ElementNode elementNode = new TraceXPathNavigator.ElementNode(name, prefix, this.CurrentElement, xmlns);
			if (this.current == null)
			{
				this.VerifySize(elementNode);
				this.root = elementNode;
				this.current = this.root;
				return;
			}
			if (!this.closed)
			{
				this.VerifySize(elementNode);
				this.CurrentElement.Add(elementNode);
				this.current = elementNode;
			}
		}

		internal void AddProcessingInstruction(string name, string text)
		{
			if (this.current == null)
			{
				return;
			}
			TraceXPathNavigator.ProcessingInstructionNode processingInstructionNode = new TraceXPathNavigator.ProcessingInstructionNode(name, text, this.CurrentElement);
			this.VerifySize(processingInstructionNode);
			this.CurrentElement.Add(processingInstructionNode);
		}

		internal void AddText(string value)
		{
			if (this.closed)
			{
				throw new InvalidOperationException();
			}
			if (this.current == null)
			{
				return;
			}
			if (this.CurrentElement.text == null)
			{
				TraceXPathNavigator.TextNode textNode = new TraceXPathNavigator.TextNode(value);
				this.VerifySize(textNode);
				this.CurrentElement.text = textNode;
				return;
			}
			if (!string.IsNullOrEmpty(value))
			{
				this.VerifySize(value);
				TraceXPathNavigator.TextNode currentElement = this.CurrentElement.text;
				currentElement.nodeValue = string.Concat(currentElement.nodeValue, value);
			}
		}

		public override XPathNavigator Clone()
		{
			return this;
		}

		internal void CloseElement()
		{
			if (this.closed)
			{
				throw new InvalidOperationException();
			}
			this.current = this.CurrentElement.parent;
			if (this.current == null)
			{
				this.closed = true;
			}
		}

		public override bool IsSamePosition(XPathNavigator other)
		{
			return false;
		}

		public override string LookupPrefix(string ns)
		{
			return this.LookupPrefix(ns, this.CurrentElement);
		}

		private string LookupPrefix(string ns, TraceXPathNavigator.ElementNode node)
		{
			string str = null;
			if (string.Compare(ns, node.xmlns, StringComparison.Ordinal) != 0)
			{
				foreach (TraceXPathNavigator.AttributeNode attribute in node.attributes)
				{
					if (string.Compare("xmlns", attribute.prefix, StringComparison.Ordinal) != 0 || string.Compare(ns, attribute.nodeValue, StringComparison.Ordinal) != 0)
					{
						continue;
					}
					str = attribute.name;
					break;
				}
			}
			else
			{
				str = node.prefix;
			}
			if (string.IsNullOrEmpty(str) && node.parent != null)
			{
				str = this.LookupPrefix(ns, node.parent);
			}
			return str;
		}

		private static void MaskElement(TraceXPathNavigator.ElementNode element)
		{
			if (element != null)
			{
				element.childNodes.Clear();
				element.Add(new TraceXPathNavigator.CommentNode("Removed", element));
				element.text = null;
				element.attributes = null;
			}
		}

		private static void MaskSubnodes(TraceXPathNavigator.ElementNode element, string[] elementNames)
		{
			TraceXPathNavigator.MaskSubnodes(element, elementNames, false);
		}

		private static void MaskSubnodes(TraceXPathNavigator.ElementNode element, string[] elementNames, bool processNodeItself)
		{
			if (elementNames == null)
			{
				throw new ArgumentNullException("elementNames");
			}
			if (element != null)
			{
				bool flag = true;
				if (processNodeItself)
				{
					string[] strArrays = elementNames;
					int num = 0;
					while (num < (int)strArrays.Length)
					{
						if (string.CompareOrdinal(strArrays[num], element.name) != 0)
						{
							num++;
						}
						else
						{
							TraceXPathNavigator.MaskElement(element);
							flag = false;
							break;
						}
					}
				}
				if (flag && element.childNodes != null)
				{
					foreach (TraceXPathNavigator.ElementNode childNode in element.childNodes)
					{
						TraceXPathNavigator.MaskSubnodes(childNode, elementNames, true);
					}
				}
			}
		}

		public override bool MoveTo(XPathNavigator other)
		{
			return false;
		}

		public override bool MoveToFirstAttribute()
		{
			if (this.current == null)
			{
				throw new InvalidOperationException();
			}
			bool firstAttribute = this.CurrentElement.MoveToFirstAttribute();
			if (firstAttribute)
			{
				this.state = XPathNodeType.Attribute;
			}
			return firstAttribute;
		}

		public override bool MoveToFirstChild()
		{
			if (this.current == null)
			{
				throw new InvalidOperationException();
			}
			bool flag = false;
			if (this.CurrentElement.childNodes != null && this.CurrentElement.childNodes.Count > 0)
			{
				this.current = this.CurrentElement.childNodes[0];
				this.state = this.current.NodeType;
				flag = true;
			}
			else if ((this.CurrentElement.childNodes == null || this.CurrentElement.childNodes.Count == 0) && this.CurrentElement.text != null)
			{
				this.state = XPathNodeType.Text;
				this.CurrentElement.movedToText = true;
				flag = true;
			}
			return flag;
		}

		public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
		{
			return false;
		}

		public override bool MoveToId(string id)
		{
			return false;
		}

		public override bool MoveToNext()
		{
			if (this.current == null)
			{
				throw new InvalidOperationException();
			}
			bool flag = false;
			if (this.state != XPathNodeType.Text)
			{
				TraceXPathNavigator.ElementNode elementNode = this.current.parent;
				if (elementNode != null)
				{
					TraceXPathNavigator.TraceNode next = elementNode.MoveToNext();
					if (next == null && elementNode.text != null && !elementNode.movedToText)
					{
						this.state = XPathNodeType.Text;
						elementNode.movedToText = true;
						this.current = elementNode;
						flag = true;
					}
					else if (next != null)
					{
						this.state = next.NodeType;
						flag = true;
						this.current = next;
					}
				}
			}
			return flag;
		}

		public override bool MoveToNextAttribute()
		{
			if (this.current == null)
			{
				throw new InvalidOperationException();
			}
			bool nextAttribute = this.CurrentElement.MoveToNextAttribute();
			if (nextAttribute)
			{
				this.state = XPathNodeType.Attribute;
			}
			return nextAttribute;
		}

		public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
		{
			return false;
		}

		public override bool MoveToParent()
		{
			if (this.current == null)
			{
				throw new InvalidOperationException();
			}
			bool flag = false;
			switch (this.state)
			{
				case XPathNodeType.Element:
				case XPathNodeType.ProcessingInstruction:
				case XPathNodeType.Comment:
				{
					if (this.current.parent == null)
					{
						return flag;
					}
					this.current = this.current.parent;
					this.state = this.current.NodeType;
					flag = true;
					return flag;
				}
				case XPathNodeType.Attribute:
				{
					this.state = XPathNodeType.Element;
					flag = true;
					return flag;
				}
				case XPathNodeType.Namespace:
				{
					this.state = XPathNodeType.Element;
					flag = true;
					return flag;
				}
				case XPathNodeType.Text:
				{
					this.state = XPathNodeType.Element;
					flag = true;
					return flag;
				}
				case XPathNodeType.SignificantWhitespace:
				case XPathNodeType.Whitespace:
				{
					return flag;
				}
				default:
				{
					return flag;
				}
			}
		}

		public override bool MoveToPrevious()
		{
			return false;
		}

		public override void MoveToRoot()
		{
			this.current = this.root;
			this.state = XPathNodeType.Element;
			this.root.Reset();
		}

		public void RemovePii(string[][] paths)
		{
			if (paths == null)
			{
				throw new ArgumentNullException("paths");
			}
			string[][] strArrays = paths;
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				this.RemovePii(strArrays[i]);
			}
		}

		public void RemovePii(string[] path)
		{
			this.RemovePii(path, DiagnosticStrings.PiiList);
		}

		public void RemovePii(string[] headersPath, string[] piiList)
		{
			if (this.root == null)
			{
				throw new ArgumentNullException(SRClient.NullRoot);
			}
			foreach (TraceXPathNavigator.ElementNode elementNode in this.root.FindSubnodes(headersPath))
			{
				TraceXPathNavigator.MaskSubnodes(elementNode, piiList);
			}
		}

		public override string ToString()
		{
			this.MoveToRoot();
			StringBuilder stringBuilder = new StringBuilder();
			XmlTextWriter xmlTextWriter = new XmlTextWriter(new StringWriter(stringBuilder, CultureInfo.CurrentCulture));
			xmlTextWriter.WriteNode(this, false);
			return stringBuilder.ToString();
		}

		private void VerifySize(TraceXPathNavigator.IMeasurable node)
		{
			this.VerifySize(node.Size);
		}

		private void VerifySize(string node)
		{
			this.VerifySize(node.Length);
		}

		private void VerifySize(int nodeSize)
		{
			if (this.maxSize != -1 && this.currentSize + (long)nodeSize > (long)this.maxSize)
			{
				throw new PlainXmlWriter.MaxSizeExceededException();
			}
			TraceXPathNavigator traceXPathNavigator = this;
			traceXPathNavigator.currentSize = traceXPathNavigator.currentSize + (long)nodeSize;
		}

		private class AttributeNode : TraceXPathNavigator.IMeasurable
		{
			internal string name;

			internal string nodeValue;

			internal string prefix;

			internal string xmlns;

			public int Size
			{
				get
				{
					int length = this.name.Length + this.nodeValue.Length + 5;
					if (!string.IsNullOrEmpty(this.prefix))
					{
						length = length + this.prefix.Length + 1;
					}
					if (!string.IsNullOrEmpty(this.xmlns))
					{
						length = length + this.xmlns.Length + 9;
					}
					return length;
				}
			}

			internal AttributeNode(string name, string prefix, string value, string xmlns)
			{
				this.name = name;
				this.prefix = prefix;
				this.nodeValue = value;
				this.xmlns = xmlns;
			}
		}

		private class CommentNode : TraceXPathNavigator.TraceNode, TraceXPathNavigator.IMeasurable
		{
			internal string nodeValue;

			public int Size
			{
				get
				{
					return this.nodeValue.Length + 8;
				}
			}

			internal CommentNode(string nodeValue, TraceXPathNavigator.ElementNode parent) : base(XPathNodeType.Comment, parent)
			{
				this.nodeValue = nodeValue;
			}
		}

		private class ElementNode : TraceXPathNavigator.TraceNode, TraceXPathNavigator.IMeasurable
		{
			private int attributeIndex;

			private int elementIndex;

			internal string name;

			internal string prefix;

			internal string xmlns;

			internal List<TraceXPathNavigator.TraceNode> childNodes;

			internal List<TraceXPathNavigator.AttributeNode> attributes;

			internal TraceXPathNavigator.TextNode text;

			internal bool movedToText;

			internal TraceXPathNavigator.AttributeNode CurrentAttribute
			{
				get
				{
					return this.attributes[this.attributeIndex];
				}
			}

			public int Size
			{
				get
				{
					int length = 2 * this.name.Length + 6;
					if (!string.IsNullOrEmpty(this.prefix))
					{
						length = length + this.prefix.Length + 1;
					}
					if (!string.IsNullOrEmpty(this.xmlns))
					{
						length = length + this.xmlns.Length + 9;
					}
					return length;
				}
			}

			internal ElementNode(string name, string prefix, TraceXPathNavigator.ElementNode parent, string xmlns) : base(XPathNodeType.Element, parent)
			{
				this.name = name;
				this.prefix = prefix;
				this.xmlns = xmlns;
			}

			internal void Add(TraceXPathNavigator.TraceNode node)
			{
				this.childNodes.Add(node);
			}

			internal IEnumerable<TraceXPathNavigator.ElementNode> FindSubnodes(string[] headersPath)
			{
				TraceXPathNavigator.ElementNode elementNode;
				if (headersPath == null)
				{
					throw new ArgumentNullException("headersPath");
				}
				TraceXPathNavigator.ElementNode elementNode1 = this;
				if (string.CompareOrdinal(elementNode1.name, headersPath[0]) != 0)
				{
					elementNode1 = null;
				}
				int num = 0;
				while (elementNode1 != null)
				{
					int num1 = num + 1;
					int num2 = num1;
					num = num1;
					if (num2 >= (int)headersPath.Length)
					{
						break;
					}
					TraceXPathNavigator.ElementNode elementNode2 = null;
					if (elementNode1.childNodes != null)
					{
						List<TraceXPathNavigator.TraceNode>.Enumerator enumerator = elementNode1.childNodes.GetEnumerator();
						try
						{
							while (enumerator.MoveNext())
							{
								TraceXPathNavigator.TraceNode current = enumerator.Current;
								if (current.NodeType != XPathNodeType.Element)
								{
									continue;
								}
								elementNode = current as TraceXPathNavigator.ElementNode;
								if (elementNode == null || string.CompareOrdinal(elementNode.name, headersPath[num]) != 0)
								{
									continue;
								}
								if ((int)headersPath.Length != num + 1)
								{
									goto Label0;
								}
								yield return elementNode;
							}
							goto Label1;
						Label0:
							elementNode2 = elementNode;
						}
						finally
						{
							((IDisposable)enumerator).Dispose();
						}
					}
				Label1:
					elementNode1 = elementNode2;
				}
			}

			internal bool MoveToFirstAttribute()
			{
				this.attributeIndex = 0;
				if (this.attributes == null)
				{
					return false;
				}
				return this.attributes.Count > 0;
			}

			internal TraceXPathNavigator.TraceNode MoveToNext()
			{
				TraceXPathNavigator.TraceNode item = null;
				if (this.elementIndex + 1 < this.childNodes.Count)
				{
					TraceXPathNavigator.ElementNode elementNode = this;
					elementNode.elementIndex = elementNode.elementIndex + 1;
					item = this.childNodes[this.elementIndex];
				}
				return item;
			}

			internal bool MoveToNextAttribute()
			{
				bool flag = false;
				if (this.attributeIndex + 1 < this.attributes.Count)
				{
					TraceXPathNavigator.ElementNode elementNode = this;
					elementNode.attributeIndex = elementNode.attributeIndex + 1;
					flag = true;
				}
				return flag;
			}

			internal void Reset()
			{
				this.attributeIndex = 0;
				this.elementIndex = 0;
				this.movedToText = false;
				if (this.childNodes != null)
				{
					foreach (TraceXPathNavigator.TraceNode childNode in this.childNodes)
					{
						if (childNode.NodeType != XPathNodeType.Element)
						{
							continue;
						}
						TraceXPathNavigator.ElementNode elementNode = childNode as TraceXPathNavigator.ElementNode;
						if (elementNode == null)
						{
							continue;
						}
						elementNode.Reset();
					}
				}
			}
		}

		private interface IMeasurable
		{
			int Size
			{
				get;
			}
		}

		private class ProcessingInstructionNode : TraceXPathNavigator.TraceNode, TraceXPathNavigator.IMeasurable
		{
			internal string name;

			internal string text;

			public int Size
			{
				get
				{
					return this.name.Length + this.text.Length + 12;
				}
			}

			internal ProcessingInstructionNode(string name, string text, TraceXPathNavigator.ElementNode parent) : base(XPathNodeType.ProcessingInstruction, parent)
			{
				this.name = name;
				this.text = text;
			}
		}

		private class TextNode : TraceXPathNavigator.IMeasurable
		{
			internal string nodeValue;

			public int Size
			{
				get
				{
					return this.nodeValue.Length;
				}
			}

			internal TextNode(string value)
			{
				this.nodeValue = value;
			}
		}

		private class TraceNode
		{
			private XPathNodeType nodeType;

			internal TraceXPathNavigator.ElementNode parent;

			internal XPathNodeType NodeType
			{
				get
				{
					return this.nodeType;
				}
			}

			protected TraceNode(XPathNodeType nodeType, TraceXPathNavigator.ElementNode parent)
			{
				this.nodeType = nodeType;
				this.parent = parent;
			}
		}
	}
}