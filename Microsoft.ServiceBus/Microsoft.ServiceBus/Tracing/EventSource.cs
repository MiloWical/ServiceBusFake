using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.ServiceBus.Tracing
{
	internal class EventSource : IDisposable
	{
		private const int MaxTraceBufferLength = 7168;

		private const string EventTraceContinuedTemplate = "Event Trace Continued...";

		private readonly string m_name;

		internal int m_id;

		private readonly System.Guid m_guid;

		internal volatile EventSource.EventMetadata[] m_eventData;

		private volatile byte[] m_rawManifest;

		private bool m_eventSourceEnabled;

		internal EventLevel m_level;

		internal EventKeywords m_matchAnyKeyword;

		internal volatile ulong[] m_channelData;

		internal volatile EventDispatcher m_Dispatchers;

		private volatile EventSource.OverideEventProvider m_provider;

		private bool m_completelyInited;

		private bool m_ETWManifestSent;

		public System.Guid Guid
		{
			get
			{
				return this.m_guid;
			}
		}

		public string Name
		{
			get
			{
				return this.m_name;
			}
		}

		protected EventSource(bool disableTracing = false)
		{
			System.Guid guid = EventSource.GetGuid(this.GetType());
			string name = EventSource.GetName(this.GetType());
			if (guid == System.Guid.Empty)
			{
				throw new ArgumentNullException("EventSource.eventSourceGuid");
			}
			if (name == null)
			{
				throw new ArgumentNullException("EventSource.eventSourceName");
			}
			if (!disableTracing)
			{
				this.m_name = name;
				this.m_guid = guid;
				this.m_provider = new EventSource.OverideEventProvider(this);
				this.m_provider.Register(guid);
				if (this.m_eventSourceEnabled && !this.m_ETWManifestSent)
				{
					this.SendManifest(this.m_rawManifest, null);
					this.m_ETWManifestSent = true;
				}
				EventListener.AddEventSource(this);
				this.m_completelyInited = true;
			}
		}

		private static void AddEventDescriptor(ref EventSource.EventMetadata[] eventData, EventAttribute eventAttribute, ParameterInfo[] eventParameters)
		{
			if (eventData == null || (int)eventData.Length <= eventAttribute.EventId)
			{
				EventSource.EventMetadata[] eventMetadataArray = new EventSource.EventMetadata[Math.Max((int)eventData.Length + 16, eventAttribute.EventId + 1)];
				Array.Copy(eventData, eventMetadataArray, (int)eventData.Length);
				eventData = eventMetadataArray;
			}
			eventData[eventAttribute.EventId].Descriptor = new EventDescriptor(eventAttribute.EventId, eventAttribute.Version, (byte)eventAttribute.Channel, (byte)eventAttribute.Level, (byte)eventAttribute.Opcode, (int)eventAttribute.Task, (long)eventAttribute.Keywords);
			eventData[eventAttribute.EventId].Parameters = eventParameters;
			eventData[eventAttribute.EventId].Message = eventAttribute.Message;
		}

		internal void AddListener(EventListener listener)
		{
			lock (EventListener.EventListenersLock)
			{
				bool[] flagArray = null;
				if (this.m_eventData != null)
				{
					flagArray = new bool[(int)this.m_eventData.Length];
				}
				this.m_Dispatchers = new EventDispatcher(this.m_Dispatchers, flagArray, listener);
				listener.OnEventSourceCreated(this);
			}
		}

		private static void AddProviderEnumKind(ManifestBuilder manifest, FieldInfo staticField, string providerEnumKind)
		{
			Type fieldType = staticField.FieldType;
			if (fieldType == typeof(EventOpcode))
			{
				if (providerEnumKind == "Opcodes")
				{
					int rawConstantValue = (int)staticField.GetRawConstantValue();
					if (rawConstantValue <= 10)
					{
						throw new ArgumentException(EventSourceSR.Event_IllegalOpcode);
					}
					manifest.AddOpcode(staticField.Name, rawConstantValue);
					return;
				}
			}
			else if (fieldType == typeof(EventTask))
			{
				if (providerEnumKind == "Tasks")
				{
					manifest.AddTask(staticField.Name, (int)staticField.GetRawConstantValue());
					return;
				}
			}
			else if (fieldType != typeof(EventKeywords))
			{
				if (fieldType == typeof(EventChannel))
				{
					if (providerEnumKind != "Channels")
					{
						throw new ArgumentException(EventSourceSR.Event_IllegalField(staticField.FieldType.Name, providerEnumKind));
					}
					ChannelAttribute customAttribute = (ChannelAttribute)Attribute.GetCustomAttribute(staticField, typeof(ChannelAttribute), false);
					byte num = (byte)staticField.GetRawConstantValue();
					manifest.AddChannel(staticField.Name, (int)num, customAttribute);
				}
				return;
			}
			else if (providerEnumKind == "Keywords")
			{
				manifest.AddKeyword(staticField.Name, (ulong)staticField.GetRawConstantValue());
				return;
			}
			throw new ArgumentException(EventSourceSR.Event_IllegalField(staticField.FieldType.Name, providerEnumKind));
		}

		private bool AnyEventEnabled()
		{
			for (int i = 0; i < (int)this.m_eventData.Length; i++)
			{
				if (this.m_eventData[i].EnabledForETW || this.m_eventData[i].EnabledForAnyListener)
				{
					return true;
				}
			}
			return false;
		}

		internal static byte[] CreateManifestAndDescriptors(Type eventSourceType, string eventSourceDllName, EventSource source)
		{
			return EventSource.CreateManifestAndDescriptors(eventSourceType, eventSourceDllName, eventSourceDllName, source);
		}

		internal static byte[] CreateManifestAndDescriptors(Type eventSourceType, string eventSourceDllName, string resourceFileName, EventSource source)
		{
			MethodInfo[] methods = eventSourceType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			int num = 1;
			EventSource.EventMetadata[] eventMetadataArray = null;
			Dictionary<string, string> strs = null;
			if (source != null)
			{
				eventMetadataArray = new EventSource.EventMetadata[(int)methods.Length];
			}
			ResourceManager resourceManager = null;
			EventSourceAttribute customAttribute = (EventSourceAttribute)Attribute.GetCustomAttribute(eventSourceType, typeof(EventSourceAttribute), false);
			if (customAttribute != null && customAttribute.LocalizationResources != null)
			{
				resourceManager = new ResourceManager(customAttribute.LocalizationResources, eventSourceType.Assembly);
			}
			ManifestBuilder manifestBuilder = new ManifestBuilder(EventSource.GetName(eventSourceType), EventSource.GetGuid(eventSourceType), eventSourceDllName, resourceFileName, resourceManager);
			string[] strArrays = new string[] { "Keywords", "Tasks", "Opcodes", "Channels" };
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str = strArrays[i];
				Type nestedType = eventSourceType.GetNestedType(str);
				if (nestedType != null)
				{
					FieldInfo[] fields = nestedType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					for (int j = 0; j < (int)fields.Length; j++)
					{
						EventSource.AddProviderEnumKind(manifestBuilder, fields[j], str);
					}
				}
			}
			SortedList<int, Tuple<MethodInfo, EventAttribute>> nums = new SortedList<int, Tuple<MethodInfo, EventAttribute>>();
			for (int k = 0; k < (int)methods.Length; k++)
			{
				MethodInfo methodInfo = methods[k];
				ParameterInfo[] parameters = methodInfo.GetParameters();
				EventAttribute eventAttribute = (EventAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(EventAttribute), false);
				string str1 = methodInfo.Name.Replace("EventWrite", string.Empty);
				if (methodInfo.ReturnType != typeof(void))
				{
					if (eventAttribute != null)
					{
						throw new ArgumentException(EventSourceSR.Event_EventNotReturnVoid(str1));
					}
				}
				else if (!methodInfo.IsVirtual && !methodInfo.IsStatic)
				{
					if (eventAttribute == null)
					{
						if (Attribute.GetCustomAttribute(methodInfo, typeof(NonEventAttribute), false) != null)
						{
							goto Label0;
						}
						eventAttribute = new EventAttribute(num);
					}
					else if (eventAttribute.EventId <= 0)
					{
						throw new ArgumentException(EventSourceSR.Event_IllegalID);
					}
					num++;
					if (eventAttribute.Opcode == EventOpcode.Info && eventAttribute.Task == EventTask.None)
					{
						eventAttribute.Task = (EventTask)(65534 - eventAttribute.EventId);
					}
					nums.Add(eventAttribute.EventId, new Tuple<MethodInfo, EventAttribute>(methodInfo, eventAttribute));
					if (source != null)
					{
						EventAttribute keywords = eventAttribute;
						keywords.Keywords = (EventKeywords)((ulong)keywords.Keywords | manifestBuilder.GetChannelKeyword(eventAttribute.Channel));
						if (source.DoDebugChecks())
						{
							EventSource.DebugCheckEvent(ref strs, eventMetadataArray, methodInfo, eventAttribute);
						}
						EventSource.AddEventDescriptor(ref eventMetadataArray, eventAttribute, parameters);
					}
				}
			Label0:
			}
			foreach (KeyValuePair<int, Tuple<MethodInfo, EventAttribute>> keyValuePair in nums)
			{
				MethodInfo item1 = keyValuePair.Value.Item1;
				EventAttribute item2 = keyValuePair.Value.Item2;
				string str2 = item1.Name.Replace("EventWrite", string.Empty);
				manifestBuilder.StartEvent(str2, item2);
				ParameterInfo[] parameterInfoArray = item1.GetParameters();
				for (int l = 0; l < (int)parameterInfoArray.Length; l++)
				{
					if (parameterInfoArray[l].ParameterType.Name != "EventTraceActivity")
					{
						manifestBuilder.AddEventParameter(parameterInfoArray[l].ParameterType, parameterInfoArray[l].Name);
					}
				}
				manifestBuilder.EndEvent();
			}
			if (source != null)
			{
				EventSource.TrimEventDescriptors(ref eventMetadataArray);
				source.m_eventData = eventMetadataArray;
				source.m_channelData = manifestBuilder.GetChannelData();
			}
			return manifestBuilder.CreateManifest();
		}

		private static void DebugCheckEvent(ref Dictionary<string, string> eventsByName, EventSource.EventMetadata[] eventData, MethodInfo method, EventAttribute eventAttribute)
		{
			int helperCallFirstArg = EventSource.GetHelperCallFirstArg(method);
			string str = method.Name.Replace("EventWrite", string.Empty);
			if (helperCallFirstArg >= 0 && eventAttribute.EventId != helperCallFirstArg)
			{
				throw new ArgumentException(EventSourceSR.Event_IllegalEventArg(str, eventAttribute.EventId, helperCallFirstArg));
			}
			if (eventAttribute.EventId < (int)eventData.Length && eventData[eventAttribute.EventId].Descriptor.EventId != 0)
			{
				throw new ArgumentException(EventSourceSR.Event_UsedEventID(str, eventAttribute.EventId));
			}
			if (eventsByName == null)
			{
				eventsByName = new Dictionary<string, string>();
			}
			if (eventsByName.ContainsKey(str))
			{
				throw new ArgumentException(EventSourceSR.Event_UsedEventName(str));
			}
			eventsByName[str] = str;
		}

		[SecurityCritical]
		private object DecodeObject(int eventId, int parameterId, int dataBytes, IntPtr dataPointer)
		{
			for (Type i = this.m_eventData[eventId].Parameters[parameterId].ParameterType; i != typeof(IntPtr); i = Enum.GetUnderlyingType(i))
			{
				if (i == typeof(int))
				{
					return (int)(*(void*)dataPointer);
				}
				if (i == typeof(uint))
				{
					return (uint)(*(void*)dataPointer);
				}
				if (i == typeof(long))
				{
					return (long)(*(void*)dataPointer);
				}
				if (i == typeof(ulong))
				{
					return (ulong)((long)(*(void*)dataPointer));
				}
				if (i == typeof(byte))
				{
					return (byte)(*(void*)dataPointer);
				}
				if (i == typeof(sbyte))
				{
					return (sbyte)(*(void*)dataPointer);
				}
				if (i == typeof(short))
				{
					return (short)(*(void*)dataPointer);
				}
				if (i == typeof(ushort))
				{
					return (ushort)(*(void*)dataPointer);
				}
				if (i == typeof(float))
				{
					return (float)(*(void*)dataPointer);
				}
				if (i == typeof(double))
				{
					return (double)(*(void*)dataPointer);
				}
				if (i == typeof(decimal))
				{
					return (decimal)(*(void*)dataPointer);
				}
				if (i == typeof(bool))
				{
					return (bool)((sbyte)(*(void*)dataPointer));
				}
				if (i == typeof(System.Guid))
				{
					return (System.Guid)(*(void*)dataPointer);
				}
				if (i == typeof(char))
				{
					return (char)(*(void*)dataPointer);
				}
				if (!i.IsEnum)
				{
					return Marshal.PtrToStringUni(dataPointer, dataBytes / 2);
				}
			}
			return (IntPtr)(*(void*)dataPointer);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing && this.m_provider != null)
			{
				this.m_provider.Dispose();
				this.m_provider = null;
			}
		}

		private bool DoDebugChecks()
		{
			return true;
		}

		internal bool EnableEventForDispatcher(EventDispatcher dispatcher, int eventId, bool value)
		{
			if (dispatcher != null)
			{
				if (eventId >= (int)dispatcher.m_EventEnabled.Length)
				{
					return false;
				}
				dispatcher.m_EventEnabled[eventId] = value;
				if (value)
				{
					this.m_eventData[eventId].EnabledForAnyListener = true;
				}
			}
			else
			{
				if (eventId >= (int)this.m_eventData.Length)
				{
					return false;
				}
				this.m_eventData[eventId].EnabledForETW = value;
			}
			return true;
		}

		~EventSource()
		{
			this.Dispose(false);
		}

		public static string GenerateManifest(Type eventSourceType, string assemblyPathToIncludeInManifest, string resourceFileName)
		{
			byte[] numArray = EventSource.CreateManifestAndDescriptors(eventSourceType, assemblyPathToIncludeInManifest, resourceFileName, null);
			return Encoding.UTF8.GetString(numArray);
		}

		private EventDispatcher GetDispatcher(EventListener listener)
		{
			EventDispatcher i;
			for (i = this.m_Dispatchers; i != null; i = i.m_Next)
			{
				if (i.m_Listener == listener)
				{
					return i;
				}
			}
			return i;
		}

		public static System.Guid GetGuid(Type eventSourceType)
		{
			System.Guid guid;
			EventSourceAttribute customAttribute = (EventSourceAttribute)Attribute.GetCustomAttribute(eventSourceType, typeof(EventSourceAttribute), false);
			string name = eventSourceType.Name;
			if (customAttribute != null)
			{
				if (customAttribute.Guid != null)
				{
					System.Guid empty = System.Guid.Empty;
					try
					{
						guid = new System.Guid(customAttribute.Guid);
					}
					catch (Exception exception)
					{
						goto Label0;
					}
					return guid;
				}
			Label0:
				if (customAttribute.Name != null)
				{
					name = customAttribute.Name;
				}
			}
			throw new ArgumentException(EventSourceSR.ProviderGuidNotSpecified(name));
		}

		private static int GetHelperCallFirstArg(MethodInfo method)
		{
			byte[] lAsByteArray = method.GetMethodBody().GetILAsByteArray();
			int num = -1;
			int num1 = 0;
			while (true)
			{
				if (num1 >= (int)lAsByteArray.Length)
				{
					return -1;
				}
				byte num2 = lAsByteArray[num1];
				if (num2 > 110)
				{
					switch (num2)
					{
						case 140:
						case 141:
						{
							num1 = num1 + 4;
							break;
						}
						default:
						{
							if (num2 == 162)
							{
								break;
							}
							if (num2 == 254)
							{
								num1++;
								if (num1 >= (int)lAsByteArray.Length || lAsByteArray[num1] >= 6)
								{
									return -1;
								}
								else
								{
									break;
								}
							}
							else
							{
								return -1;
							}
						}
					}
				}
				else
				{
					switch (num2)
					{
						case 0:
						case 1:
						case 2:
						case 3:
						case 4:
						case 5:
						case 6:
						case 7:
						case 8:
						case 9:
						case 10:
						case 11:
						case 12:
						case 13:
						case 20:
						case 37:
						{
							break;
						}
						case 14:
						case 16:
						{
							num1++;
							break;
						}
						case 15:
						case 17:
						case 18:
						case 19:
						case 33:
						case 34:
						case 35:
						case 36:
						case 38:
						case 39:
						case 41:
						case 42:
						case 43:
						case 46:
						case 47:
						case 48:
						case 49:
						case 50:
						case 51:
						case 52:
						case 53:
						case 54:
						case 55:
						case 56:
						{
							return -1;
						}
						case 21:
						case 22:
						case 23:
						case 24:
						case 25:
						case 26:
						case 27:
						case 28:
						case 29:
						case 30:
						{
							if (num1 <= 0 || lAsByteArray[num1 - 1] != 2)
							{
								break;
							}
							num = lAsByteArray[num1] - 22;
							break;
						}
						case 31:
						{
							if (num1 > 0 && lAsByteArray[num1 - 1] == 2)
							{
								num = lAsByteArray[num1 + 1];
							}
							num1++;
							break;
						}
						case 32:
						{
							num1 = num1 + 4;
							break;
						}
						case 40:
						{
							num1 = num1 + 4;
							if (num >= 0)
							{
								for (int i = num1 + 1; i < (int)lAsByteArray.Length; i++)
								{
									if (lAsByteArray[i] == 42)
									{
										return num;
									}
									if (lAsByteArray[i] != 0)
									{
										break;
									}
								}
							}
							num = -1;
							break;
						}
						case 44:
						case 45:
						{
							num = -1;
							num1++;
							break;
						}
						case 57:
						case 58:
						{
							num = -1;
							num1 = num1 + 4;
							break;
						}
						default:
						{
							switch (num2)
							{
								case 103:
								case 104:
								case 105:
								case 106:
								case 109:
								case 110:
								{
									break;
								}
								case 107:
								case 108:
								{
									return -1;
								}
								default:
								{
									return -1;
								}
							}
							break;
						}
					}
				}
				num1++;
			}
			return -1;
		}

		public static string GetName(Type eventSourceType)
		{
			EventSourceAttribute customAttribute = (EventSourceAttribute)Attribute.GetCustomAttribute(eventSourceType, typeof(EventSourceAttribute), false);
			if (customAttribute != null && customAttribute.Name != null)
			{
				return customAttribute.Name;
			}
			return eventSourceType.Name;
		}

		public static IEnumerable<EventSource> GetSources()
		{
			List<EventSource> eventSources = new List<EventSource>();
			lock (EventListener.EventListenersLock)
			{
				foreach (WeakReference sEventSource in EventListener.s_EventSources)
				{
					EventSource target = sEventSource.Target as EventSource;
					if (target == null)
					{
						continue;
					}
					eventSources.Add(target);
				}
			}
			return eventSources;
		}

		private void InsureInitialized()
		{
			lock (EventListener.EventListenersLock)
			{
				if (this.m_rawManifest == null)
				{
					this.m_rawManifest = EventSource.CreateManifestAndDescriptors(this.GetType(), "", this);
				}
				if (this.DoDebugChecks())
				{
					foreach (WeakReference sEventSource in EventListener.s_EventSources)
					{
						EventSource target = sEventSource.Target as EventSource;
						if (target == null || !(target.Guid == this.m_guid) || target == this)
						{
							continue;
						}
						throw new ArgumentException(EventSourceSR.Event_SourceWithUsedGuid(this.m_guid));
					}
				}
				for (EventDispatcher i = this.m_Dispatchers; i != null; i = i.m_Next)
				{
					if (i.m_EventEnabled == null)
					{
						i.m_EventEnabled = new bool[(int)this.m_eventData.Length];
					}
				}
			}
		}

		public bool IsEnabled()
		{
			return this.m_eventSourceEnabled;
		}

		public bool IsEnabled(int eventId)
		{
			if (!this.m_eventSourceEnabled)
			{
				return false;
			}
			if (this.m_eventData == null)
			{
				return false;
			}
			if (this.m_eventData[eventId].EnabledForETW)
			{
				return true;
			}
			return this.m_eventData[eventId].EnabledForAnyListener;
		}

		public bool IsEnabled(EventLevel level, EventKeywords keywords, EventChannel channel)
		{
			if (!this.m_eventSourceEnabled)
			{
				return false;
			}
			if (this.m_level != EventLevel.LogAlways && this.m_level < level)
			{
				return false;
			}
			EventKeywords mChannelData = (EventKeywords)(this.m_channelData[(int)channel] | (ulong)keywords);
			if (this.m_matchAnyKeyword == EventKeywords.None)
			{
				return true;
			}
			return (mChannelData & this.m_matchAnyKeyword) != EventKeywords.None;
		}

		private bool IsEnabledByDefault(int eventNum, bool enable, EventLevel currentLevel, EventKeywords currentMatchAnyKeyword)
		{
			if (!enable)
			{
				return false;
			}
			EventLevel level = (EventLevel)this.m_eventData[eventNum].Descriptor.Level;
			EventKeywords keywords = (EventKeywords)this.m_eventData[eventNum].Descriptor.Keywords;
			if (level > currentLevel && currentLevel != EventLevel.LogAlways || keywords != EventKeywords.None && (keywords & currentMatchAnyKeyword) == EventKeywords.None)
			{
				return false;
			}
			return true;
		}

		protected virtual void OnEventCommand(EventCommandEventArgs command)
		{
		}

		public static void SendCommand(EventSource eventSource, EventCommand command, IDictionary<string, string> commandArguments)
		{
			if (eventSource == null)
			{
				throw new ArgumentNullException("eventSource");
			}
			EventSource.SendCommand(eventSource, null, command, true, EventLevel.LogAlways, (EventKeywords)((long)0), commandArguments);
		}

		internal static void SendCommand(EventSource eventSource, EventListener eventListener, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> commandArguments)
		{
			eventSource.SendCommand(eventListener, command, enable, level, matchAnyKeyword, commandArguments);
		}

		internal void SendCommand(EventListener listener, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeyword, IDictionary<string, string> commandArguments)
		{
			this.InsureInitialized();
			EventDispatcher dispatcher = this.GetDispatcher(listener);
			if (dispatcher == null && listener != null)
			{
				throw new ArgumentException(EventSourceSR.Event_ListenerNotFound);
			}
			if (commandArguments == null)
			{
				commandArguments = new Dictionary<string, string>();
			}
			if (command != EventCommand.Update)
			{
				if (command == EventCommand.SendManifest)
				{
					this.SendManifest(this.m_rawManifest, null);
				}
				this.OnEventCommand(new EventCommandEventArgs(command, commandArguments, null, null));
			}
			else
			{
				for (int i = 0; i < (int)this.m_eventData.Length; i++)
				{
					this.EnableEventForDispatcher(dispatcher, i, this.IsEnabledByDefault(i, enable, level, matchAnyKeyword));
				}
				command = EventCommand.Disable;
				if (enable)
				{
					command = EventCommand.Enable;
					if (this.m_eventSourceEnabled)
					{
						if (level > this.m_level)
						{
							this.m_level = level;
						}
						if (matchAnyKeyword == EventKeywords.None)
						{
							this.m_matchAnyKeyword = EventKeywords.None;
						}
						else if (this.m_matchAnyKeyword != EventKeywords.None)
						{
							EventSource mMatchAnyKeyword = this;
							mMatchAnyKeyword.m_matchAnyKeyword = mMatchAnyKeyword.m_matchAnyKeyword | matchAnyKeyword;
						}
					}
					else
					{
						this.m_level = level;
						this.m_matchAnyKeyword = matchAnyKeyword;
					}
					if (dispatcher != null)
					{
						if (!dispatcher.m_ManifestSent)
						{
							dispatcher.m_ManifestSent = true;
							this.SendManifest(this.m_rawManifest, dispatcher.m_Listener);
						}
					}
					else if (!this.m_ETWManifestSent && this.m_completelyInited)
					{
						this.m_ETWManifestSent = true;
						this.SendManifest(this.m_rawManifest, null);
					}
				}
				this.OnEventCommand(new EventCommandEventArgs(command, commandArguments, this, dispatcher));
				if (enable)
				{
					this.m_eventSourceEnabled = true;
					return;
				}
				if (dispatcher == null)
				{
					this.m_ETWManifestSent = false;
				}
				else
				{
					dispatcher.m_ManifestSent = false;
				}
				for (int j = 0; j < (int)this.m_eventData.Length; j++)
				{
					this.m_eventData[j].EnabledForAnyListener = false;
					EventDispatcher mDispatchers = this.m_Dispatchers;
					while (mDispatchers != null)
					{
						if (!mDispatchers.m_EventEnabled[j])
						{
							mDispatchers = mDispatchers.m_Next;
						}
						else
						{
							this.m_eventData[j].EnabledForAnyListener = true;
							break;
						}
					}
				}
				if (!this.AnyEventEnabled())
				{
					this.m_level = EventLevel.LogAlways;
					this.m_matchAnyKeyword = EventKeywords.None;
					this.m_eventSourceEnabled = false;
					return;
				}
			}
		}

		private unsafe bool SendManifest(byte[] rawManifest, EventListener Listener)
		{
			byte* numPointer;
			byte[] numArray = rawManifest;
			byte[] numArray1 = numArray;
			if (numArray == null || (int)numArray1.Length == 0)
			{
				numPointer = null;
			}
			else
			{
				numPointer = &numArray1[0];
			}
			EventDescriptor eventDescriptor = new EventDescriptor(65534, 1, 0, 0, 254, 65534, (long)-1);
			ManifestEnvelope chunkNumber = new ManifestEnvelope()
			{
				Format = ManifestEnvelope.ManifestFormats.SimpleXmlFormat,
				MajorVersion = 1,
				MinorVersion = 0,
				Magic = 91
			};
			int length = (int)rawManifest.Length;
			chunkNumber.TotalChunks = (ushort)((length + 65279) / 65280);
			chunkNumber.ChunkNumber = 0;
			EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(2 * sizeof(EventProviderClone.EventData))];
			(*eventDataPointer).Ptr = (ulong)(&chunkNumber);
			(*eventDataPointer).Size = sizeof(ManifestEnvelope);
			(*eventDataPointer).Reserved = 0;
			(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Ptr = (ulong)numPointer;
			(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Reserved = 0;
			bool flag = true;
			while (length > 0)
			{
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Size = (uint)Math.Min(length, 65280);
				if (Listener == null)
				{
					flag = (this.m_provider == null ? true : !this.m_provider.WriteEvent(ref eventDescriptor, 2, (IntPtr)eventDataPointer));
				}
				if (Listener != null)
				{
					byte[] numArray2 = null;
					byte[] numArray3 = null;
					if (numArray2 == null)
					{
						numArray2 = new byte[(*eventDataPointer).Size];
						numArray3 = new byte[(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Size];
					}
					Marshal.Copy((IntPtr)(*eventDataPointer).Ptr, numArray2, 0, (int)(*eventDataPointer).Size);
					Marshal.Copy((IntPtr)(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Ptr, numArray3, 0, (int)(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Size);
					EventWrittenEventArgs eventWrittenEventArg = new EventWrittenEventArgs(this)
					{
						EventId = eventDescriptor.EventId,
						Payload = new ReadOnlyCollection<object>(new List<object>()
						{
							numArray2,
							numArray3
						})
					};
					Listener.OnEventWritten(eventWrittenEventArg);
				}
				length = length - 65280;
				EventProviderClone.EventData* ptr = eventDataPointer + sizeof(EventProviderClone.EventData);
				(*ptr).Ptr = (*ptr).Ptr + (long)65280;
				chunkNumber.ChunkNumber = (ushort)(chunkNumber.ChunkNumber + 1);
			}
			return flag;
		}

		protected void SetActivityId(ref System.Guid activityId)
		{
			if (this.m_provider != null)
			{
				this.m_provider.SetActivityId(ref activityId);
			}
		}

		public override string ToString()
		{
			object[] name = new object[] { "EventSource(", this.Name, ",", this.Guid, ")" };
			return string.Concat(name);
		}

		[Conditional("DEBUG")]
		private void TraceControllerCommand(EventCommand command, EventLevel level, EventKeywords matchAnyKeyword)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Enabled channels  = ");
			for (int i = 0; i < (int)this.m_channelData.Length; i++)
			{
				if (((ulong)matchAnyKeyword & this.m_channelData[i]) != (ulong)EventKeywords.None)
				{
					stringBuilder.Append("0x").Append(i.ToString("x")).Append("{0x").Append(this.m_channelData[i].ToString("x")).Append("} ");
				}
			}
		}

		private static void TrimEventDescriptors(ref EventSource.EventMetadata[] eventData)
		{
			int length = (int)eventData.Length;
			do
			{
				if (0 >= length)
				{
					break;
				}
				length--;
			}
			while (eventData[length].Descriptor.EventId == 0);
			if ((int)eventData.Length - length > 2)
			{
				EventSource.EventMetadata[] eventMetadataArray = new EventSource.EventMetadata[length + 1];
				Array.Copy(eventData, eventMetadataArray, (int)eventMetadataArray.Length);
				eventData = eventMetadataArray;
			}
		}

		protected void WriteEvent(int eventId)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 0, (IntPtr)0);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				this.WriteToAllListeners(eventId, new object[0]);
			}
		}

		protected unsafe void WriteEvent(int eventId, int arg1)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(1 * sizeof(EventProviderClone.EventData))];
				(*eventDataPointer).Ptr = (ulong)(&arg1);
				(*eventDataPointer).Size = 4;
				(*eventDataPointer).Reserved = 0;
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 1, (IntPtr)eventDataPointer);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				this.WriteToAllListeners(eventId, new object[] { arg1 });
			}
		}

		protected unsafe void WriteEvent(int eventId, int arg1, int arg2)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(2 * sizeof(EventProviderClone.EventData))];
				(*eventDataPointer).Ptr = (ulong)(&arg1);
				(*eventDataPointer).Size = 4;
				(*eventDataPointer).Reserved = 0;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Size = 4;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Reserved = 0;
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 2, (IntPtr)eventDataPointer);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, int arg1, int arg2, int arg3)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(3 * sizeof(EventProviderClone.EventData))];
				(*eventDataPointer).Ptr = (ulong)(&arg1);
				(*eventDataPointer).Size = 4;
				(*eventDataPointer).Reserved = 0;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Size = 4;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Reserved = 0;
				(*(eventDataPointer + 2 * sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg3);
				(*(eventDataPointer + 2 * sizeof(EventProviderClone.EventData))).Size = 4;
				(*(eventDataPointer + 2 * sizeof(EventProviderClone.EventData))).Reserved = 0;
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 3, (IntPtr)eventDataPointer);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2, arg3 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, long arg1)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(1 * sizeof(EventProviderClone.EventData))];
				(*eventDataPointer).Ptr = (ulong)(&arg1);
				(*eventDataPointer).Size = 8;
				(*eventDataPointer).Reserved = 0;
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 1, (IntPtr)eventDataPointer);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				this.WriteToAllListeners(eventId, new object[] { arg1 });
			}
		}

		protected unsafe void WriteEvent(int eventId, long arg1, long arg2)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(2 * sizeof(EventProviderClone.EventData))];
				(*eventDataPointer).Ptr = (ulong)(&arg1);
				(*eventDataPointer).Size = 8;
				(*eventDataPointer).Reserved = 0;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Size = 8;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Reserved = 0;
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 2, (IntPtr)eventDataPointer);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, long arg1, long arg2, long arg3)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(3 * sizeof(EventProviderClone.EventData))];
				(*eventDataPointer).Ptr = (ulong)(&arg1);
				(*eventDataPointer).Size = 8;
				(*eventDataPointer).Reserved = 0;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Size = 8;
				(*(eventDataPointer + sizeof(EventProviderClone.EventData))).Reserved = 0;
				(*(eventDataPointer + 2 * sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg3);
				(*(eventDataPointer + 2 * sizeof(EventProviderClone.EventData))).Size = 8;
				(*(eventDataPointer + 2 * sizeof(EventProviderClone.EventData))).Reserved = 0;
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 3, (IntPtr)eventDataPointer);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2, arg3 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, string arg1)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (arg1 == null)
				{
					arg1 = "";
				}
				fixed (string str = arg1)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					EventProviderClone.EventData* length = stackalloc EventProviderClone.EventData[checked(1 * sizeof(EventProviderClone.EventData))];
					(*length).Ptr = (ulong)chrPointer;
					(*length).Size = (uint)((arg1.Length + 1) * 2);
					(*length).Reserved = 0;
					this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 1, (IntPtr)length);
				}
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				this.WriteToAllListeners(eventId, new object[] { arg1 });
			}
		}

		protected unsafe void WriteEvent(int eventId, string arg1, string arg2)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (arg1 == null)
				{
					arg1 = "";
				}
				if (arg2 == null)
				{
					arg2 = "";
				}
				fixed (string str = arg1)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					fixed (string str1 = arg2)
					{
						string* strPointers = &str1;
						if (strPointers != null)
						{
							strPointers = strPointers + RuntimeHelpers.OffsetToStringData;
						}
						char* chrPointer1 = (char*)strPointers;
						EventProviderClone.EventData* length = stackalloc EventProviderClone.EventData[checked(2 * sizeof(EventProviderClone.EventData))];
						(*length).Ptr = (ulong)chrPointer;
						(*length).Size = (uint)((arg1.Length + 1) * 2);
						(*length).Reserved = 0;
						(*(length + sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer1;
						(*(length + sizeof(EventProviderClone.EventData))).Size = (uint)((arg2.Length + 1) * 2);
						(*(length + sizeof(EventProviderClone.EventData))).Reserved = 0;
						this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 2, (IntPtr)length);
					}
				}
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				this.WriteToAllListeners(eventId, new object[] { arg1, arg2 });
			}
		}

		protected unsafe void WriteEvent(int eventId, string arg1, string arg2, string arg3)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (arg1 == null)
				{
					arg1 = "";
				}
				if (arg2 == null)
				{
					arg2 = "";
				}
				if (arg3 == null)
				{
					arg3 = "";
				}
				fixed (string str = arg1)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					fixed (string str1 = arg2)
					{
						string* strPointers = &str1;
						if (strPointers != null)
						{
							strPointers = strPointers + RuntimeHelpers.OffsetToStringData;
						}
						char* chrPointer1 = (char*)strPointers;
						fixed (string str2 = arg3)
						{
							string* offsetToStringData1 = &str2;
							if (offsetToStringData1 != null)
							{
								offsetToStringData1 = offsetToStringData1 + RuntimeHelpers.OffsetToStringData;
							}
							char* chrPointer2 = (char*)offsetToStringData1;
							EventProviderClone.EventData* length = stackalloc EventProviderClone.EventData[checked(3 * sizeof(EventProviderClone.EventData))];
							(*length).Ptr = (ulong)chrPointer;
							(*length).Size = (uint)((arg1.Length + 1) * 2);
							(*length).Reserved = 0;
							(*(length + sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer1;
							(*(length + sizeof(EventProviderClone.EventData))).Size = (uint)((arg2.Length + 1) * 2);
							(*(length + sizeof(EventProviderClone.EventData))).Reserved = 0;
							(*(length + 2 * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer2;
							(*(length + 2 * sizeof(EventProviderClone.EventData))).Size = (uint)((arg3.Length + 1) * 2);
							(*(length + 2 * sizeof(EventProviderClone.EventData))).Reserved = 0;
							this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 3, (IntPtr)length);
						}
					}
				}
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2, arg3 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, string arg1, int arg2)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (arg1 == null)
				{
					arg1 = "";
				}
				fixed (string str = arg1)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					EventProviderClone.EventData* length = stackalloc EventProviderClone.EventData[checked(2 * sizeof(EventProviderClone.EventData))];
					(*length).Ptr = (ulong)chrPointer;
					(*length).Size = (uint)((arg1.Length + 1) * 2);
					(*length).Reserved = 0;
					(*(length + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
					(*(length + sizeof(EventProviderClone.EventData))).Size = 4;
					(*(length + sizeof(EventProviderClone.EventData))).Reserved = 0;
					this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 2, (IntPtr)length);
				}
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, string arg1, int arg2, int arg3)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (arg1 == null)
				{
					arg1 = "";
				}
				fixed (string str = arg1)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					EventProviderClone.EventData* length = stackalloc EventProviderClone.EventData[checked(3 * sizeof(EventProviderClone.EventData))];
					(*length).Ptr = (ulong)chrPointer;
					(*length).Size = (uint)((arg1.Length + 1) * 2);
					(*length).Reserved = 0;
					(*(length + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
					(*(length + sizeof(EventProviderClone.EventData))).Size = 4;
					(*(length + sizeof(EventProviderClone.EventData))).Reserved = 0;
					(*(length + 2 * sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg3);
					(*(length + 2 * sizeof(EventProviderClone.EventData))).Size = 4;
					(*(length + 2 * sizeof(EventProviderClone.EventData))).Reserved = 0;
					this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 3, (IntPtr)length);
				}
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2, arg3 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, string arg1, int arg2, long arg3)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (arg1 == null)
				{
					arg1 = "";
				}
				fixed (string str = arg1)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					EventProviderClone.EventData* length = stackalloc EventProviderClone.EventData[checked(3 * sizeof(EventProviderClone.EventData))];
					(*length).Ptr = (ulong)chrPointer;
					(*length).Size = (uint)((arg1.Length + 1) * 2);
					(*length).Reserved = 0;
					(*(length + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
					(*(length + sizeof(EventProviderClone.EventData))).Size = 4;
					(*(length + sizeof(EventProviderClone.EventData))).Reserved = 0;
					(*(length + 2 * sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg3);
					(*(length + 2 * sizeof(EventProviderClone.EventData))).Size = 8;
					(*(length + 2 * sizeof(EventProviderClone.EventData))).Reserved = 0;
					this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 3, (IntPtr)length);
				}
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2, arg3 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected unsafe void WriteEvent(int eventId, string arg1, long arg2)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (arg1 == null)
				{
					arg1 = "";
				}
				fixed (string str = arg1)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					EventProviderClone.EventData* length = stackalloc EventProviderClone.EventData[checked(2 * sizeof(EventProviderClone.EventData))];
					(*length).Ptr = (ulong)chrPointer;
					(*length).Size = (uint)((arg1.Length + 1) * 2);
					(*length).Reserved = 0;
					(*(length + sizeof(EventProviderClone.EventData))).Ptr = (ulong)(&arg2);
					(*(length + sizeof(EventProviderClone.EventData))).Size = 8;
					(*(length + sizeof(EventProviderClone.EventData))).Reserved = 0;
					this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, 2, (IntPtr)length);
				}
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[] { arg1, arg2 };
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected void WriteEvent(int eventId, params object[] args)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, args);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				this.WriteToAllListeners(eventId, args);
			}
		}

		protected void WriteEvent(int eventId, EventTraceActivity traceActivityId, params object[] args)
		{
			if (this.m_eventData != null && this.m_eventData[eventId].EnabledForETW && this.m_provider != null)
			{
				if (traceActivityId != null)
				{
					this.m_provider.SetActivityId(ref traceActivityId.ActivityId);
				}
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, args);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				this.WriteToAllListeners(eventId, args);
			}
		}

		[SecurityCritical]
		protected unsafe void WriteEventCore(int eventId, int eventDataCount, EventSource.EventData* data)
		{
			EventProviderClone.EventData* size = stackalloc EventProviderClone.EventData[checked(eventDataCount * sizeof(EventProviderClone.EventData))];
			for (int i = 0; i < eventDataCount; i++)
			{
				(*(size + i * sizeof(EventProviderClone.EventData))).Size = (uint)(*(data + i * sizeof(EventSource.EventData))).Size;
				IntPtr dataPointer = (*(data + i * sizeof(EventSource.EventData))).DataPointer;
				(*(size + i * sizeof(EventProviderClone.EventData))).Ptr = (ulong)dataPointer.ToInt64();
				(*(size + i * sizeof(EventProviderClone.EventData))).Reserved = 0;
			}
			if (this.m_provider != null)
			{
				this.m_provider.WriteEvent(ref this.m_eventData[eventId].Descriptor, eventDataCount, (IntPtr)size);
			}
			if (this.m_Dispatchers != null && this.m_eventData[eventId].EnabledForAnyListener)
			{
				object[] objArray = new object[eventDataCount];
				for (int j = 0; j < eventDataCount; j++)
				{
					objArray[j] = this.DecodeObject(eventId, j, (*(data + j * sizeof(EventSource.EventData))).Size, (*(data + j * sizeof(EventSource.EventData))).DataPointer);
				}
				this.WriteToAllListeners(eventId, objArray);
			}
		}

		protected void WriteSBTraceEvent(int eventId, EventTraceActivity traceActivityId, params object[] list)
		{
			if (list == null || (int)list.Length == 0)
			{
				throw new ArgumentException("list is null or empty");
			}
			int length = (int)list.Length - 1;
			int stringLength = list[length].ToStringLength();
			int num = 0;
			for (int i = 0; i < length; i++)
			{
				int stringLength1 = num + list[i].ToStringLength();
				num = stringLength1;
				if (stringLength1 > 7168)
				{
					this.WriteEvent(eventId, traceActivityId, list);
					return;
				}
			}
			if (stringLength + num < 7168)
			{
				this.WriteEvent(eventId, traceActivityId, list);
				return;
			}
			int num1 = 0;
			int num2 = 7168 - num;
			string str = (list[length] != null ? list[length].ToString() : string.Empty);
			while (num1 < stringLength)
			{
				string str1 = str.Substring(num1, Math.Min(num2, stringLength - num1));
				list[length] = (num1 == 0 ? str1 : string.Concat("Event Trace Continued...", str1));
				this.WriteEvent(eventId, traceActivityId, list);
				num1 = num1 + num2;
			}
		}

		private void WriteToAllListeners(int eventId, params object[] args)
		{
			EventWrittenEventArgs eventWrittenEventArg = new EventWrittenEventArgs(this)
			{
				EventId = eventId,
				Payload = args
			};
			for (EventDispatcher i = this.m_Dispatchers; i != null; i = i.m_Next)
			{
				if (i.m_EventEnabled[eventId])
				{
					i.m_Listener.OnEventWritten(eventWrittenEventArg);
				}
			}
		}

		protected void WriteTransferEvent(int eventId, EventTraceActivity traceActivityId, EventTraceActivity relatedActivityId, params object[] args)
		{
			if (this.m_provider != null)
			{
				this.m_provider.WriteTransfer(ref this.m_eventData[eventId].Descriptor, ref traceActivityId.ActivityId, ref relatedActivityId.ActivityId, args);
			}
		}

		protected internal struct EventData
		{
			public IntPtr DataPointer
			{
				get;
				set;
			}

			public int Size
			{
				get;
				set;
			}
		}

		internal struct EventMetadata
		{
			public EventDescriptor Descriptor;

			public bool EnabledForAnyListener;

			public bool EnabledForETW;

			public string Message;

			public ParameterInfo[] Parameters;
		}

		private class OverideEventProvider : EventProviderClone
		{
			private EventSource m_eventSource;

			public OverideEventProvider(EventSource eventSource)
			{
				this.m_eventSource = eventSource;
			}

			protected override void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments)
			{
				EventListener eventListener = null;
				this.m_eventSource.SendCommand(eventListener, (EventCommand)command, base.IsEnabled(), base.Level, base.MatchAnyKeyword, arguments);
			}
		}
	}
}