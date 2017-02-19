using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Microsoft.ServiceBus.Tracing
{
	internal class ManifestBuilder
	{
		private Dictionary<int, ulong> channelKeywords;

		private ulong channelReservedKeywordMask = -9223372036854775808L;

		private Dictionary<int, string> opcodeTab;

		private Dictionary<int, string> taskTab;

		private Dictionary<int, ManifestBuilder.ChannelInfo> channelTab;

		private Dictionary<ulong, string> keywordTab;

		private Dictionary<string, Type> mapsTab;

		private Dictionary<string, string> stringTab;

		private StringBuilder sb;

		private StringBuilder events;

		private StringBuilder templates;

		private string providerName;

		private ResourceManager resources;

		private string templateName;

		private int numParams;

		public ManifestBuilder(string providerName, Guid providerGuid, string dllName, string resourceDll, ResourceManager resources)
		{
			this.providerName = providerName;
			this.resources = resources;
			this.sb = new StringBuilder();
			this.events = new StringBuilder();
			this.templates = new StringBuilder();
			this.opcodeTab = new Dictionary<int, string>();
			this.stringTab = new Dictionary<string, string>();
			this.sb.AppendLine("<instrumentationManifest xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
			this.sb.AppendLine(" <instrumentation xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:win=\"http://manifests.microsoft.com/win/2004/08/windows/events\">");
			this.sb.AppendLine("  <events xmlns=\"http://schemas.microsoft.com/win/2004/08/events\">");
			this.sb.Append("<provider name=\"").Append(providerName).Append("\" guid=\"{").Append(providerGuid.ToString()).Append("}");
			if (resourceDll != null)
			{
				string str = string.Concat(Path.DirectorySeparatorChar, Path.GetFileName(resourceDll));
				this.sb.Append("\" resourceFileName=\"").Append(str).Append("\" messageFileName=\"").Append(str);
			}
			string str1 = providerName.Replace("-", "");
			this.sb.Append("\" symbol=\"").Append(str1);
			this.sb.Append("\" >").AppendLine();
		}

		public void AddChannel(string name, int channelId, ChannelAttribute channelAttribute)
		{
			this.AddChannelKeyword((EventChannel)((byte)channelId));
			if (this.channelTab == null)
			{
				this.channelTab = new Dictionary<int, ManifestBuilder.ChannelInfo>();
			}
			Dictionary<int, ManifestBuilder.ChannelInfo> nums = this.channelTab;
			ManifestBuilder.ChannelInfo channelInfo = new ManifestBuilder.ChannelInfo()
			{
				Name = name,
				Attribs = channelAttribute
			};
			nums[channelId] = channelInfo;
		}

		private ulong AddChannelKeyword(EventChannel channel)
		{
			ulong num;
			if (this.channelKeywords == null)
			{
				this.channelKeywords = new Dictionary<int, ulong>();
			}
			if (!this.channelKeywords.TryGetValue(channel, out num))
			{
				num = this.channelReservedKeywordMask;
				ManifestBuilder manifestBuilder = this;
				manifestBuilder.channelReservedKeywordMask = manifestBuilder.channelReservedKeywordMask >> 1;
				this.channelKeywords[channel] = num;
			}
			return num;
		}

		public void AddEventParameter(Type type, string name)
		{
			if (this.numParams == 0)
			{
				this.templates.Append("  <template tid=\"").Append(this.templateName).Append("\">").AppendLine();
			}
			ManifestBuilder manifestBuilder = this;
			manifestBuilder.numParams = manifestBuilder.numParams + 1;
			this.templates.Append("   <data name=\"").Append(name).Append("\" inType=\"").Append(ManifestBuilder.GetTypeName(type)).Append("\"");
			if (type.IsEnum)
			{
				this.templates.Append(" map=\"").Append(type.Name).Append("\"");
				if (this.mapsTab == null)
				{
					this.mapsTab = new Dictionary<string, Type>();
				}
				if (!this.mapsTab.ContainsKey(type.Name))
				{
					this.mapsTab.Add(type.Name, type);
				}
			}
			this.templates.Append("/>").AppendLine();
		}

		public void AddKeyword(string name, ulong value)
		{
			if ((value & value - (long)1) != (long)0)
			{
				throw new ArgumentException(EventSourceSR.Event_KeywordValue(value, name));
			}
			if (this.keywordTab == null)
			{
				this.keywordTab = new Dictionary<ulong, string>();
			}
			this.keywordTab[value] = name;
		}

		public void AddOpcode(string name, int value)
		{
			this.opcodeTab[value] = name;
		}

		public void AddTask(string name, int value)
		{
			if (this.taskTab == null)
			{
				this.taskTab = new Dictionary<int, string>();
			}
			this.taskTab[value] = name;
		}

		public byte[] CreateManifest()
		{
			string str = this.CreateManifestString();
			return Encoding.UTF8.GetBytes(str);
		}

		private string CreateManifestString()
		{
			if (this.channelTab != null)
			{
				this.sb.Append(" <channels>").AppendLine();
				List<int> nums = new List<int>(this.channelTab.Keys);
				nums.Sort();
				foreach (int num in nums)
				{
					ManifestBuilder.ChannelInfo item = this.channelTab[num];
					string type = null;
					string str = "channel";
					bool enabled = false;
					string isolation = null;
					string importChannel = null;
					if (item.Attribs != null)
					{
						ChannelAttribute attribs = item.Attribs;
						type = attribs.Type;
						if (attribs.ImportChannel != null)
						{
							importChannel = attribs.ImportChannel;
							str = "importChannel";
						}
						enabled = attribs.Enabled;
						isolation = attribs.Isolation;
					}
					if (importChannel == null)
					{
						importChannel = string.Concat(this.providerName, "/", type);
					}
					string str1 = item.Name.Replace('-', '\u005F');
					this.sb.Append("  <").Append(str);
					this.sb.Append(" name=\"").Append(importChannel).Append("\"");
					this.sb.Append(" chid=\"").Append(item.Name).Append("\"");
					this.sb.Append(" symbol=\"").Append(str1).Append("\"");
					this.WriteMessageAttrib(this.sb, str, str1, type);
					this.sb.Append(" value=\"").Append(num).Append("\"");
					if (str == "channel")
					{
						if (type != null)
						{
							this.sb.Append(" type=\"").Append(type).Append("\"");
						}
						this.sb.Append(" enabled=\"").Append(enabled.ToString().ToLowerInvariant()).Append("\"");
						if (isolation != null)
						{
							this.sb.Append(" isolation=\"").Append(isolation).Append("\"");
						}
					}
					if (item.Attribs == null || item.Attribs.BufferSize <= 0)
					{
						this.sb.Append("/>").AppendLine();
					}
					else
					{
						StringBuilder stringBuilder = this.sb.AppendLine(">").Append("    <publishing>");
						CultureInfo invariantCulture = CultureInfo.InvariantCulture;
						object[] bufferSize = new object[] { item.Attribs.BufferSize };
						StringBuilder stringBuilder1 = stringBuilder.AppendFormat(invariantCulture, "      <bufferSize>{0}</bufferSize>", bufferSize).Append("    </publishing>").AppendLine();
						CultureInfo cultureInfo = CultureInfo.InvariantCulture;
						object[] objArray = new object[] { str };
						stringBuilder1.AppendFormat(cultureInfo, "  </{0}>", objArray).AppendLine();
					}
				}
				this.sb.Append(" </channels>").AppendLine();
			}
			if (this.taskTab != null)
			{
				this.sb.Append(" <tasks>").AppendLine();
				List<int> nums1 = new List<int>(this.taskTab.Keys);
				nums1.Sort();
				foreach (int num1 in nums1)
				{
					this.sb.Append("  <task name=\"").Append(this.taskTab[num1]).Append("\" value=\"").Append(num1).Append("\"/>").AppendLine();
				}
				this.sb.Append(" </tasks>").AppendLine();
			}
			if (this.mapsTab != null)
			{
				this.sb.Append(" <maps>").AppendLine();
				foreach (Type value in this.mapsTab.Values)
				{
					string str2 = (Attribute.GetCustomAttribute(value, typeof(FlagsAttribute), false) != null ? "bitMap" : "valueMap");
					this.sb.Append("  <").Append(str2).Append(" name=\"").Append(value.Name).Append("\">").AppendLine();
					FieldInfo[] fields = value.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
					for (int i = 0; i < (int)fields.Length; i++)
					{
						FieldInfo fieldInfo = fields[i];
						object rawConstantValue = fieldInfo.GetRawConstantValue();
						if (rawConstantValue != null)
						{
							string str3 = null;
							if (rawConstantValue is int)
							{
								str3 = ((int)rawConstantValue).ToString("x", CultureInfo.InvariantCulture);
							}
							else if (rawConstantValue is long)
							{
								str3 = ((long)rawConstantValue).ToString("x", CultureInfo.InvariantCulture);
							}
							this.sb.Append("   <map value=\"0x").Append(str3).Append("\"");
							this.WriteMessageAttrib(this.sb, "map", string.Concat(value.Name, ".", fieldInfo.Name), fieldInfo.Name);
							this.sb.Append("/>").AppendLine();
						}
					}
					this.sb.Append("  </").Append(str2).Append(">").AppendLine();
				}
				this.sb.Append(" </maps>").AppendLine();
			}
			this.sb.Append(" <opcodes>").AppendLine();
			List<int> nums2 = new List<int>(this.opcodeTab.Keys);
			nums2.Sort();
			foreach (int num2 in nums2)
			{
				this.sb.Append("  <opcode");
				this.WriteNameAndMessageAttribs(this.sb, "opcode", this.opcodeTab[num2]);
				this.sb.Append(" value=\"").Append(num2).Append("\"/>").AppendLine();
			}
			this.sb.Append(" </opcodes>").AppendLine();
			if (this.keywordTab != null)
			{
				this.sb.Append(" <keywords>").AppendLine();
				List<ulong> nums3 = new List<ulong>(this.keywordTab.Keys);
				nums3.Sort();
				foreach (ulong num3 in nums3)
				{
					this.sb.Append("  <keyword");
					this.WriteNameAndMessageAttribs(this.sb, "keyword", this.keywordTab[num3]);
					this.sb.Append(" mask=\"0x").Append(num3.ToString("x", CultureInfo.InvariantCulture)).Append("\"/>").AppendLine();
				}
				this.sb.Append(" </keywords>").AppendLine();
			}
			this.sb.Append(" <events>").AppendLine();
			this.sb.Append(this.events);
			this.sb.Append(" </events>").AppendLine();
			if (this.templates.Length > 0)
			{
				this.sb.Append(" <templates>").AppendLine();
				this.sb.Append(this.templates);
				this.sb.Append(" </templates>").AppendLine();
			}
			this.sb.Append("</provider>").AppendLine();
			this.sb.Append("</events>").AppendLine();
			this.sb.Append("</instrumentation>").AppendLine();
			this.sb.Append("<localization>").AppendLine();
			this.sb.Append(" <resources culture=\"").Append("en-US").Append("\">").AppendLine();
			this.sb.Append("  <stringTable>").AppendLine();
			List<string> strs = new List<string>(this.stringTab.Keys);
			strs.Sort();
			foreach (string str4 in strs)
			{
				this.sb.Append("   <string id=\"").Append(str4).Append("\" value=\"").Append(this.stringTab[str4]).Append("\"/>").AppendLine();
			}
			this.sb.Append("  </stringTable>").AppendLine();
			this.sb.Append(" </resources>").AppendLine();
			this.sb.Append("</localization>").AppendLine();
			this.sb.AppendLine("</instrumentationManifest>");
			return this.sb.ToString();
		}

		public void EndEvent()
		{
			if (this.numParams > 0)
			{
				this.templates.Append("  </template>").AppendLine();
				this.events.Append(" template=\"").Append(this.templateName).Append("\"");
			}
			this.events.Append("/>").AppendLine();
			this.templateName = null;
			this.numParams = 0;
		}

		public ulong[] GetChannelData()
		{
			int num = -1;
			foreach (int key in this.channelKeywords.Keys)
			{
				if (key <= num)
				{
					continue;
				}
				num = key;
			}
			ulong[] value = new ulong[num + 1];
			foreach (KeyValuePair<int, ulong> channelKeyword in this.channelKeywords)
			{
				value[channelKeyword.Key] = channelKeyword.Value;
			}
			return value;
		}

		public ulong GetChannelKeyword(EventChannel channel)
		{
			ulong num;
			if (this.channelKeywords.TryGetValue(channel, out num))
			{
				return num;
			}
			return (ulong)0;
		}

		private string GetChannelName(EventChannel channel, string eventName)
		{
			ManifestBuilder.ChannelInfo channelInfo = null;
			if (this.channelTab == null || !this.channelTab.TryGetValue(channel, out channelInfo))
			{
				throw new ArgumentException(EventSourceSR.EventSource_UndefinedChannel(channel, eventName));
			}
			return channelInfo.Name;
		}

		private string GetKeywords(ulong keywords, string eventName)
		{
			string str;
			string str1 = "";
			for (ulong i = (ulong)1; i != (long)0; i = i << 1)
			{
				if ((keywords & i) != (long)0)
				{
					if (this.keywordTab == null || !this.keywordTab.TryGetValue(i, out str))
					{
						throw new ArgumentException(EventSourceSR.Event_UndefinedKeyword(i, eventName));
					}
					if (str1.Length != 0)
					{
						str1 = string.Concat(str1, " ");
					}
					str1 = string.Concat(str1, str);
				}
			}
			return str1;
		}

		private static string GetLevelName(EventLevel level)
		{
			return string.Concat(((int)level >= 16 ? "" : "win:"), level.ToString());
		}

		private string GetOpcodeName(EventOpcode opcode, string eventName)
		{
			string str;
			EventOpcode eventOpcode = opcode;
			switch (eventOpcode)
			{
				case EventOpcode.Info:
				{
					return "win:Info";
				}
				case EventOpcode.Start:
				{
					return "win:Start";
				}
				case EventOpcode.Stop:
				{
					return "win:Stop";
				}
				case EventOpcode.DataCollectionStart:
				{
					return "win:DC_Start";
				}
				case EventOpcode.DataCollectionStop:
				{
					return "win:DC_Stop";
				}
				case EventOpcode.Extension:
				{
					return "win:Extension";
				}
				case EventOpcode.Reply:
				{
					return "win:Reply";
				}
				case EventOpcode.Resume:
				{
					return "win:Resume";
				}
				case EventOpcode.Suspend:
				{
					return "win:Suspend";
				}
				case EventOpcode.Send:
				{
					return "win:Send";
				}
				default:
				{
					if (eventOpcode == EventOpcode.Receive)
					{
						break;
					}
					else
					{
						if (this.opcodeTab == null || !this.opcodeTab.TryGetValue(opcode, out str))
						{
							object[] objArray = new object[] { "Use of undefined opcode value ", opcode, " for event ", eventName };
							throw new ArgumentException(string.Concat(objArray));
						}
						return str;
					}
				}
			}
			return "win:Receive";
		}

		private string GetTaskName(EventTask task, string eventName)
		{
			string str;
			if (task == EventTask.None)
			{
				return "";
			}
			if (this.taskTab == null)
			{
				this.taskTab = new Dictionary<int, string>();
			}
			if (!this.taskTab.TryGetValue(task, out str))
			{
				string str1 = eventName;
				string str2 = str1;
				this.taskTab[task] = str1;
				str = str2;
			}
			return str;
		}

		private static string GetTypeName(Type type)
		{
			if (type.IsEnum)
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				string typeName = ManifestBuilder.GetTypeName(fields[0].FieldType);
				return typeName.Replace("win:Int", "win:UInt");
			}
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Object:
				{
					return "win:UnicodeString";
				}
				case TypeCode.DBNull:
				case TypeCode.Char:
				case TypeCode.Decimal:
				case TypeCode.DateTime:
				case TypeCode.Object | TypeCode.DateTime:
				{
					if (type != typeof(Guid))
					{
						throw new ArgumentException(EventSourceSR.Event_UnsupportType(type.Name));
					}
					return "win:GUID";
				}
				case TypeCode.Boolean:
				{
					return "win:Boolean";
				}
				case TypeCode.SByte:
				{
					return "win:Int8";
				}
				case TypeCode.Byte:
				{
					return "win:Uint8";
				}
				case TypeCode.Int16:
				{
					return "win:Int16";
				}
				case TypeCode.UInt16:
				{
					return "win:UInt16";
				}
				case TypeCode.Int32:
				{
					return "win:Int32";
				}
				case TypeCode.UInt32:
				{
					return "win:UInt32";
				}
				case TypeCode.Int64:
				{
					return "win:Int64";
				}
				case TypeCode.UInt64:
				{
					return "win:UInt64";
				}
				case TypeCode.Single:
				{
					return "win:Float";
				}
				case TypeCode.Double:
				{
					return "win:Double";
				}
				case TypeCode.String:
				{
					return "win:UnicodeString";
				}
				default:
				{
					if (type != typeof(Guid))
					{
						throw new ArgumentException(EventSourceSR.Event_UnsupportType(type.Name));
					}
					return "win:GUID";
				}
			}
		}

		public void StartEvent(string eventName, EventAttribute eventAttribute)
		{
			this.templateName = string.Concat(eventName, "Args");
			this.numParams = 0;
			this.events.Append("  <event").Append(" symbol=\"").Append(eventName).Append("\"").Append(" value=\"").Append(eventAttribute.EventId).Append("\"").Append(" version=\"").Append(eventAttribute.Version).Append("\"").Append(" level=\"").Append(ManifestBuilder.GetLevelName(eventAttribute.Level)).Append("\"");
			this.WriteMessageAttrib(this.events, "event", eventName, eventAttribute.Message);
			if (eventAttribute.Keywords != EventKeywords.None)
			{
				ulong channelKeyword = ~this.GetChannelKeyword(eventAttribute.Channel) & (ulong)eventAttribute.Keywords;
				this.events.Append(" keywords=\"").Append(this.GetKeywords(channelKeyword, eventName)).Append("\"");
			}
			if (eventAttribute.Opcode != EventOpcode.Info)
			{
				this.events.Append(" opcode=\"").Append(this.GetOpcodeName(eventAttribute.Opcode, eventName)).Append("\"");
			}
			if (eventAttribute.Task != EventTask.None)
			{
				this.events.Append(" task=\"").Append(this.GetTaskName(eventAttribute.Task, eventName)).Append("\"");
			}
			if (eventAttribute.Channel != EventChannel.Default)
			{
				this.events.Append(" channel=\"").Append(this.GetChannelName(eventAttribute.Channel, eventName)).Append("\"");
			}
		}

		private static string TranslateToManifestConvention(string eventMessage)
		{
			StringBuilder stringBuilder = null;
			int num = 0;
			int num1 = 0;
			while (num1 < eventMessage.Length)
			{
				if (eventMessage[num1] != '{')
				{
					num1++;
				}
				else
				{
					int num2 = num1;
					num1++;
					int num3 = 0;
					while (num1 < eventMessage.Length && char.IsDigit(eventMessage[num1]))
					{
						num3 = num3 + (num3 * 10 + eventMessage[num1] - 48);
						num1++;
					}
					if (num1 >= eventMessage.Length || eventMessage[num1] != '}')
					{
						continue;
					}
					num1++;
					if (stringBuilder == null)
					{
						stringBuilder = new StringBuilder();
					}
					stringBuilder.Append(eventMessage, num, num2 - num);
					stringBuilder.Append('%').Append(num3 + 1);
					num = num1;
				}
			}
			if (stringBuilder == null)
			{
				return eventMessage;
			}
			stringBuilder.Append(eventMessage, num, num1 - num);
			return stringBuilder.ToString();
		}

		private void WriteMessageAttrib(StringBuilder stringBuilder, string elementName, string name, string value)
		{
			string str = string.Concat(elementName, "_", name);
			if (this.resources != null)
			{
				string str1 = this.resources.GetString(str);
				if (str1 != null)
				{
					value = str1;
				}
			}
			if (value == null)
			{
				return;
			}
			if (elementName == "event")
			{
				value = ManifestBuilder.TranslateToManifestConvention(value);
			}
			stringBuilder.Append(" message=\"$(string.").Append(str).Append(")\"");
			this.stringTab.Add(str, value);
		}

		private void WriteNameAndMessageAttribs(StringBuilder stringBuilder, string elementName, string name)
		{
			stringBuilder.Append(" name=\"").Append(name).Append("\" ");
			this.WriteMessageAttrib(this.sb, elementName, name, name);
		}

		private class ChannelInfo
		{
			public string Name;

			public ChannelAttribute Attribs;

			public ChannelInfo()
			{
			}
		}
	}
}