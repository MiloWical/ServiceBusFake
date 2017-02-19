using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace Microsoft.ServiceBus.Tracing
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort=true)]
	internal class EventProviderClone : IDisposable
	{
		private const int s_basicTypeAllocationBufferSize = 16;

		private const int s_etwMaxMumberArguments = 32;

		private const int s_etwAPIMaxStringCount = 16;

		private const int s_maxEventDataDescriptors = 128;

		private const int s_traceEventMaximumSize = 65482;

		private const int s_traceEventMaximumStringSize = 32724;

		internal const string ADVAPI32 = "advapi32.dll";

		private EventProviderClone.ManifestEtw.EtwEnableCallback m_etwCallback;

		private long m_regHandle;

		private byte m_level;

		private long m_anyKeywordMask;

		private long m_allKeywordMask;

		private int m_enabled;

		private Guid m_providerId;

		private int m_disposed;

		[ThreadStatic]
		private static EventProviderClone.WriteEventErrorCode s_returnCode;

		protected EventLevel Level
		{
			get
			{
				return (EventLevel)this.m_level;
			}
			set
			{
				this.m_level = (byte)value;
			}
		}

		protected EventKeywords MatchAllKeyword
		{
			get
			{
				return (EventKeywords)this.m_allKeywordMask;
			}
			set
			{
				this.m_allKeywordMask = (long)value;
			}
		}

		protected EventKeywords MatchAnyKeyword
		{
			get
			{
				return (EventKeywords)this.m_anyKeywordMask;
			}
			set
			{
				this.m_anyKeywordMask = (long)value;
			}
		}

		[PermissionSet(SecurityAction.Demand, Unrestricted=true)]
		[SecurityCritical]
		protected EventProviderClone(Guid providerGuid)
		{
			this.m_providerId = providerGuid;
			this.Register(providerGuid);
		}

		internal EventProviderClone()
		{
		}

		public virtual void Close()
		{
			this.Dispose();
		}

		[SecurityCritical]
		private void Deregister()
		{
			if (this.m_regHandle != (long)0)
			{
				this.EventUnregister();
				this.m_regHandle = (long)0;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		[SecurityCritical]
		protected virtual void Dispose(bool disposing)
		{
			if (this.m_disposed == 1)
			{
				return;
			}
			if (Interlocked.Exchange(ref this.m_disposed, 1) != 0)
			{
				return;
			}
			this.m_enabled = 0;
			this.Deregister();
		}

		[SecurityCritical]
		private static unsafe string EncodeObject(ref object data, EventProviderClone.EventData* dataDescriptor, byte* dataBuffer)
		{
			string str;
			while (true)
			{
				(*dataDescriptor).Reserved = 0;
				str = data as string;
				if (str != null)
				{
					(*dataDescriptor).Size = (uint)((str.Length + 1) * 2);
					return str;
				}
				if (data is IntPtr)
				{
					(*dataDescriptor).Size = sizeof(IntPtr);
					IntPtr* intPtrPointer = (IntPtr*)dataBuffer;
					*intPtrPointer = (IntPtr)data;
					(*dataDescriptor).Ptr = (ulong)intPtrPointer;
				}
				else if (data is int)
				{
					(*dataDescriptor).Size = 4;
					int* numPointer = (int*)dataBuffer;
					*numPointer = (int)data;
					(*dataDescriptor).Ptr = (ulong)numPointer;
				}
				else if (data is long)
				{
					(*dataDescriptor).Size = 8;
					long* numPointer1 = (long*)dataBuffer;
					*numPointer1 = (long)data;
					(*dataDescriptor).Ptr = (ulong)numPointer1;
				}
				else if (data is uint)
				{
					(*dataDescriptor).Size = 4;
					uint* numPointer2 = (uint*)dataBuffer;
					*numPointer2 = (uint)data;
					(*dataDescriptor).Ptr = (ulong)numPointer2;
				}
				else if (data is ulong)
				{
					(*dataDescriptor).Size = 8;
					ulong* numPointer3 = (ulong*)dataBuffer;
					*numPointer3 = (ulong)data;
					(*dataDescriptor).Ptr = (ulong)numPointer3;
				}
				else if (data is char)
				{
					(*dataDescriptor).Size = 2;
					char* chrPointer = (char*)dataBuffer;
					*chrPointer = (char)data;
					(*dataDescriptor).Ptr = (ulong)chrPointer;
				}
				else if (data is byte)
				{
					(*dataDescriptor).Size = 1;
					byte* numPointer4 = dataBuffer;
					*numPointer4 = (byte)data;
					(*dataDescriptor).Ptr = (ulong)numPointer4;
				}
				else if (data is short)
				{
					(*dataDescriptor).Size = 2;
					short* numPointer5 = (short*)dataBuffer;
					*numPointer5 = (short)data;
					(*dataDescriptor).Ptr = (ulong)numPointer5;
				}
				else if (data is sbyte)
				{
					(*dataDescriptor).Size = 1;
					sbyte* numPointer6 = (sbyte*)dataBuffer;
					*numPointer6 = (sbyte)data;
					(*dataDescriptor).Ptr = (ulong)numPointer6;
				}
				else if (data is ushort)
				{
					(*dataDescriptor).Size = 2;
					ushort* numPointer7 = (ushort*)dataBuffer;
					*numPointer7 = (ushort)data;
					(*dataDescriptor).Ptr = (ulong)numPointer7;
				}
				else if (data is float)
				{
					(*dataDescriptor).Size = 4;
					float* singlePointer = (float*)dataBuffer;
					*singlePointer = (float)data;
					(*dataDescriptor).Ptr = (ulong)singlePointer;
				}
				else if (data is double)
				{
					(*dataDescriptor).Size = 8;
					double* numPointer8 = (double*)dataBuffer;
					*numPointer8 = (double)data;
					(*dataDescriptor).Ptr = (ulong)numPointer8;
				}
				else if (data is bool)
				{
					(*dataDescriptor).Size = 4;
					int* numPointer9 = (int*)dataBuffer;
					if (!(bool)data)
					{
						*numPointer9 = 0;
					}
					else
					{
						*numPointer9 = 1;
					}
					(*dataDescriptor).Ptr = (ulong)numPointer9;
				}
				else if (data is Guid)
				{
					(*dataDescriptor).Size = sizeof(Guid);
					Guid* guidPointer = (Guid*)dataBuffer;
					*guidPointer = (Guid)data;
					(*dataDescriptor).Ptr = (ulong)guidPointer;
				}
				else if (data is decimal)
				{
					(*dataDescriptor).Size = 16;
					decimal* numPointer10 = (decimal*)dataBuffer;
					*numPointer10 = (decimal)data;
					(*dataDescriptor).Ptr = (ulong)numPointer10;
				}
				else if (!(data is bool))
				{
					if (!(data is Enum))
					{
						break;
					}
					Type underlyingType = Enum.GetUnderlyingType(data.GetType());
					if (underlyingType != typeof(int))
					{
						if (underlyingType != typeof(long))
						{
							break;
						}
						data = ((IConvertible)data).ToInt64(null);
						continue;
					}
					else
					{
						data = ((IConvertible)data).ToInt32(null);
						continue;
					}
				}
				else
				{
					(*dataDescriptor).Size = 1;
					bool* flagPointer = (bool*)dataBuffer;
					*flagPointer = (bool)data;
					(*dataDescriptor).Ptr = (ulong)flagPointer;
				}
				return null;
			}
			str = (data != null ? data.ToString() : "");
			(*dataDescriptor).Size = (uint)((str.Length + 1) * 2);
			return str;
		}

		[SecurityCritical]
		private unsafe void EtwEnableCallBack([In] ref Guid sourceId, [In] int isEnabled, [In] byte setLevel, [In] long anyKeyword, [In] long allKeyword, [In] EventProviderClone.ManifestEtw.EVENT_FILTER_DESCRIPTOR* filterData, [In] void* callbackContext)
		{
			byte[] numArray;
			int num;
			this.m_enabled = isEnabled;
			this.m_level = setLevel;
			this.m_anyKeywordMask = anyKeyword;
			this.m_allKeywordMask = allKeyword;
			ControllerCommand controllerCommand = ControllerCommand.Update;
			IDictionary<string, string> strs = null;
			if (this.GetDataFromController(filterData, out controllerCommand, out numArray, out num))
			{
				strs = new Dictionary<string, string>(4);
				while (num < (int)numArray.Length)
				{
					int num1 = EventProviderClone.FindNull(numArray, num);
					int num2 = num1 + 1;
					int num3 = EventProviderClone.FindNull(numArray, num2);
					if (num3 < (int)numArray.Length)
					{
						string str = Encoding.UTF8.GetString(numArray, num, num1 - num);
						string str1 = Encoding.UTF8.GetString(numArray, num2, num3 - num2);
						strs[str] = str1;
					}
					num = num3 + 1;
				}
			}
			this.OnControllerCommand(controllerCommand, strs);
		}

		private uint EventRegister(ref Guid providerId, EventProviderClone.ManifestEtw.EtwEnableCallback enableCallback)
		{
			unsafe
			{
				this.m_providerId = providerId;
				this.m_etwCallback = enableCallback;
				return EventProviderClone.ManifestEtw.EventRegister(ref providerId, enableCallback, 0, ref this.m_regHandle);
			}
		}

		private uint EventUnregister()
		{
			uint num = EventProviderClone.ManifestEtw.EventUnregister(this.m_regHandle);
			this.m_regHandle = (long)0;
			return num;
		}

		private unsafe uint EventWrite(ref EventDescriptor eventDescriptor, uint userDataCount, EventProviderClone.EventData* userData)
		{
			return EventProviderClone.ManifestEtw.EventWrite(this.m_regHandle, ref eventDescriptor, userDataCount, userData);
		}

		private unsafe uint EventWriteString(byte level, long keywords, char* message)
		{
			return EventProviderClone.ManifestEtw.EventWriteString(this.m_regHandle, level, keywords, message);
		}

		private unsafe uint EventWriteTransfer(ref EventDescriptor eventDescriptor, ref Guid activityId, ref Guid relatedActivityId, uint userDataCount, EventProviderClone.EventData* userData)
		{
			return EventProviderClone.ManifestEtw.EventWriteTransfer(this.m_regHandle, ref eventDescriptor, ref activityId, ref relatedActivityId, userDataCount, userData);
		}

		~EventProviderClone()
		{
			this.Dispose(false);
		}

		private static int FindNull(byte[] buffer, int idx)
		{
			while (idx < (int)buffer.Length && buffer[idx] != 0)
			{
				idx++;
			}
			return idx;
		}

		private unsafe bool GetDataFromController(EventProviderClone.ManifestEtw.EVENT_FILTER_DESCRIPTOR* filterData, out ControllerCommand command, out byte[] data, out int dataStart)
		{
			data = null;
			if (filterData != null)
			{
				if ((*filterData).Ptr != (long)0 && 0 < (*filterData).Size && (*filterData).Size <= 1024)
				{
					data = new byte[(*filterData).Size];
					Marshal.Copy((IntPtr)(*filterData).Ptr, data, 0, (int)data.Length);
				}
				command = (ControllerCommand)(*filterData).Type;
				dataStart = 0;
				return true;
			}
			string str = string.Concat("\\Microsoft\\Windows\\CurrentVersion\\Winevt\\Publishers\\{", this.m_providerId, "}");
			str = (Marshal.SizeOf(typeof(IntPtr)) != 8 ? string.Concat("HKEY_LOCAL_MACHINE\\Software", str) : string.Concat("HKEY_LOCAL_MACHINE\\Software\\Wow6432Node", str));
			data = Registry.GetValue(str, "ControllerData", null) as byte[];
			if (data == null || (int)data.Length < 4)
			{
				dataStart = 0;
				command = ControllerCommand.Update;
				return false;
			}
			command = (ControllerCommand)((((data[3] << 8) + data[2] << 8) + data[1] << 8) + data[0]);
			dataStart = 4;
			return true;
		}

		public static EventProviderClone.WriteEventErrorCode GetLastWriteEventError()
		{
			return EventProviderClone.s_returnCode;
		}

		public bool IsEnabled()
		{
			if (this.m_enabled == 0)
			{
				return false;
			}
			return true;
		}

		public bool IsEnabled(byte level, long keywords)
		{
			if (this.m_enabled == 0)
			{
				return false;
			}
			if (level > this.m_level && this.m_level != 0 || keywords != (long)0 && ((keywords & this.m_anyKeywordMask) == (long)0 || (keywords & this.m_allKeywordMask) != this.m_allKeywordMask))
			{
				return false;
			}
			return true;
		}

		protected virtual void OnControllerCommand(ControllerCommand command, IDictionary<string, string> arguments)
		{
		}

		[SecurityCritical]
		internal void Register(Guid providerGuid)
		{
			this.m_providerId = providerGuid;
			this.m_etwCallback = new EventProviderClone.ManifestEtw.EtwEnableCallback(this.EtwEnableCallBack);
			uint num = this.EventRegister(ref this.m_providerId, this.m_etwCallback);
			if (num != 0)
			{
				throw new InvalidOperationException(EventSourceSR.Event_FailedWithErrorCode(num));
			}
		}

		[SecurityCritical]
		protected internal uint SetActivityId(ref Guid activityId)
		{
			return EventProviderClone.ManifestEtw.EventActivityIdControl(2, ref activityId);
		}

		private static void SetLastError(int error)
		{
			int num = error;
			if (num == 8)
			{
				EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.NoFreeBuffers;
				return;
			}
			if (num != 234 && num != 534)
			{
				return;
			}
			EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.EventTooBig;
		}

		[SecurityCritical]
		public unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, params object[] eventPayload)
		{
			EventProviderClone.EventData eventDatum = new EventProviderClone.EventData();
			uint num = 0;
			if (this.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
			{
				int length = 0;
				if (eventPayload == null || (int)eventPayload.Length == 0 || (int)eventPayload.Length == 1)
				{
					string str = null;
					byte* numPointer = stackalloc byte[16];
					eventDatum.Size = 0;
					if (eventPayload != null && (int)eventPayload.Length != 0)
					{
						str = EventProviderClone.EncodeObject(ref eventPayload[0], ref eventDatum, numPointer);
						length = 1;
					}
					if (eventDatum.Size > 65482)
					{
						EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.EventTooBig;
						return false;
					}
					if (str == null)
					{
						num = (length != 0 ? this.EventWrite(ref eventDescriptor, (uint)length, ref eventDatum) : this.EventWrite(ref eventDescriptor, 0, 0));
					}
					else
					{
						fixed (string str1 = str)
						{
							string* offsetToStringData = &str1;
							if (offsetToStringData != null)
							{
								offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
							}
							eventDatum.Ptr = (ulong)((char*)offsetToStringData);
							num = this.EventWrite(ref eventDescriptor, (uint)length, ref eventDatum);
						}
					}
				}
				else
				{
					length = (int)eventPayload.Length;
					if (length > 32)
					{
						throw new ArgumentOutOfRangeException("eventPayload", EventSourceSR.ArgumentOutOfRange_MaxArgExceeded(32));
					}
					uint size = 0;
					int num1 = 0;
					int[] numArray = new int[16];
					string[] strArrays = new string[16];
					EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(length * sizeof(EventProviderClone.EventData))];
					EventProviderClone.EventData* eventDataPointer1 = eventDataPointer;
					byte* numPointer1 = stackalloc byte[16 * length];
					for (int i = 0; i < (int)eventPayload.Length; i++)
					{
						if (eventPayload[i] != null)
						{
							string str2 = EventProviderClone.EncodeObject(ref eventPayload[i], eventDataPointer1, numPointer1);
							numPointer1 = numPointer1 + 16;
							size = size + (*eventDataPointer1).Size;
							eventDataPointer1 = eventDataPointer1 + sizeof(EventProviderClone.EventData);
							if (str2 != null)
							{
								if (num1 >= 16)
								{
									throw new ArgumentOutOfRangeException("eventPayload", EventSourceSR.ArgumentOutOfRange_MaxStringsExceeded(16));
								}
								strArrays[num1] = str2;
								numArray[num1] = i;
								num1++;
							}
						}
					}
					if (size > 65482)
					{
						EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.EventTooBig;
						return false;
					}
					fixed (string str3 = strArrays[0])
					{
						string* strPointers = &str3;
						if (strPointers != null)
						{
							strPointers = strPointers + RuntimeHelpers.OffsetToStringData;
						}
						char* chrPointer = (char*)strPointers;
						fixed (string str4 = strArrays[1])
						{
							string* offsetToStringData1 = &str4;
							if (offsetToStringData1 != null)
							{
								offsetToStringData1 = offsetToStringData1 + RuntimeHelpers.OffsetToStringData;
							}
							char* chrPointer1 = (char*)offsetToStringData1;
							fixed (string str5 = strArrays[2])
							{
								string* strPointers1 = &str5;
								if (strPointers1 != null)
								{
									strPointers1 = strPointers1 + RuntimeHelpers.OffsetToStringData;
								}
								char* chrPointer2 = (char*)strPointers1;
								fixed (string str6 = strArrays[3])
								{
									string* offsetToStringData2 = &str6;
									if (offsetToStringData2 != null)
									{
										offsetToStringData2 = offsetToStringData2 + RuntimeHelpers.OffsetToStringData;
									}
									char* chrPointer3 = (char*)offsetToStringData2;
									fixed (string str7 = strArrays[4])
									{
										string* strPointers2 = &str7;
										if (strPointers2 != null)
										{
											strPointers2 = strPointers2 + RuntimeHelpers.OffsetToStringData;
										}
										char* chrPointer4 = (char*)strPointers2;
										fixed (string str8 = strArrays[5])
										{
											string* offsetToStringData3 = &str8;
											if (offsetToStringData3 != null)
											{
												offsetToStringData3 = offsetToStringData3 + RuntimeHelpers.OffsetToStringData;
											}
											char* chrPointer5 = (char*)offsetToStringData3;
											fixed (string str9 = strArrays[6])
											{
												string* strPointers3 = &str9;
												if (strPointers3 != null)
												{
													strPointers3 = strPointers3 + RuntimeHelpers.OffsetToStringData;
												}
												char* chrPointer6 = (char*)strPointers3;
												fixed (string str10 = strArrays[7])
												{
													string* offsetToStringData4 = &str10;
													if (offsetToStringData4 != null)
													{
														offsetToStringData4 = offsetToStringData4 + RuntimeHelpers.OffsetToStringData;
													}
													char* chrPointer7 = (char*)offsetToStringData4;
													fixed (string str11 = strArrays[8])
													{
														string* strPointers4 = &str11;
														if (strPointers4 != null)
														{
															strPointers4 = strPointers4 + RuntimeHelpers.OffsetToStringData;
														}
														char* chrPointer8 = (char*)strPointers4;
														fixed (string str12 = strArrays[9])
														{
															string* offsetToStringData5 = &str12;
															if (offsetToStringData5 != null)
															{
																offsetToStringData5 = offsetToStringData5 + RuntimeHelpers.OffsetToStringData;
															}
															char* chrPointer9 = (char*)offsetToStringData5;
															fixed (string str13 = strArrays[10])
															{
																string* strPointers5 = &str13;
																if (strPointers5 != null)
																{
																	strPointers5 = strPointers5 + RuntimeHelpers.OffsetToStringData;
																}
																char* chrPointer10 = (char*)strPointers5;
																fixed (string str14 = strArrays[11])
																{
																	string* offsetToStringData6 = &str14;
																	if (offsetToStringData6 != null)
																	{
																		offsetToStringData6 = offsetToStringData6 + RuntimeHelpers.OffsetToStringData;
																	}
																	char* chrPointer11 = (char*)offsetToStringData6;
																	fixed (string str15 = strArrays[12])
																	{
																		string* strPointers6 = &str15;
																		if (strPointers6 != null)
																		{
																			strPointers6 = strPointers6 + RuntimeHelpers.OffsetToStringData;
																		}
																		char* chrPointer12 = (char*)strPointers6;
																		fixed (string str16 = strArrays[13])
																		{
																			string* offsetToStringData7 = &str16;
																			if (offsetToStringData7 != null)
																			{
																				offsetToStringData7 = offsetToStringData7 + RuntimeHelpers.OffsetToStringData;
																			}
																			char* chrPointer13 = (char*)offsetToStringData7;
																			fixed (string str17 = strArrays[14])
																			{
																				string* strPointers7 = &str17;
																				if (strPointers7 != null)
																				{
																					strPointers7 = strPointers7 + RuntimeHelpers.OffsetToStringData;
																				}
																				char* chrPointer14 = (char*)strPointers7;
																				fixed (string str18 = strArrays[15])
																				{
																					string* offsetToStringData8 = &str18;
																					if (offsetToStringData8 != null)
																					{
																						offsetToStringData8 = offsetToStringData8 + RuntimeHelpers.OffsetToStringData;
																					}
																					char* chrPointer15 = (char*)offsetToStringData8;
																					eventDataPointer1 = eventDataPointer;
																					if (strArrays[0] != null)
																					{
																						(*(eventDataPointer1 + numArray[0] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer;
																					}
																					if (strArrays[1] != null)
																					{
																						(*(eventDataPointer1 + numArray[1] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer1;
																					}
																					if (strArrays[2] != null)
																					{
																						(*(eventDataPointer1 + numArray[2] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer2;
																					}
																					if (strArrays[3] != null)
																					{
																						(*(eventDataPointer1 + numArray[3] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer3;
																					}
																					if (strArrays[4] != null)
																					{
																						(*(eventDataPointer1 + numArray[4] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer4;
																					}
																					if (strArrays[5] != null)
																					{
																						(*(eventDataPointer1 + numArray[5] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer5;
																					}
																					if (strArrays[6] != null)
																					{
																						(*(eventDataPointer1 + numArray[6] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer6;
																					}
																					if (strArrays[7] != null)
																					{
																						(*(eventDataPointer1 + numArray[7] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer7;
																					}
																					if (strArrays[8] != null)
																					{
																						(*(eventDataPointer1 + numArray[8] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer8;
																					}
																					if (strArrays[9] != null)
																					{
																						(*(eventDataPointer1 + numArray[9] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer9;
																					}
																					if (strArrays[10] != null)
																					{
																						(*(eventDataPointer1 + numArray[10] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer10;
																					}
																					if (strArrays[11] != null)
																					{
																						(*(eventDataPointer1 + numArray[11] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer11;
																					}
																					if (strArrays[12] != null)
																					{
																						(*(eventDataPointer1 + numArray[12] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer12;
																					}
																					if (strArrays[13] != null)
																					{
																						(*(eventDataPointer1 + numArray[13] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer13;
																					}
																					if (strArrays[14] != null)
																					{
																						(*(eventDataPointer1 + numArray[14] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer14;
																					}
																					if (strArrays[15] != null)
																					{
																						(*(eventDataPointer1 + numArray[15] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer15;
																					}
																					num = this.EventWrite(ref eventDescriptor, (uint)length, eventDataPointer);
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
					str4 = null;
					str5 = null;
					str6 = null;
					str7 = null;
					str8 = null;
					str9 = null;
					str10 = null;
					str11 = null;
					str12 = null;
					str13 = null;
					str14 = null;
					str15 = null;
					str16 = null;
					str17 = null;
					str18 = null;
				}
			}
			if (num == 0)
			{
				return true;
			}
			EventProviderClone.SetLastError((int)num);
			return false;
		}

		[SecurityCritical]
		public unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, string data)
		{
			EventProviderClone.EventData length = new EventProviderClone.EventData();
			uint num = 0;
			if (data == null)
			{
				throw new ArgumentNullException("dataString");
			}
			if (this.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
			{
				if (data.Length > 32724)
				{
					EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.EventTooBig;
					return false;
				}
				length.Size = (uint)((data.Length + 1) * 2);
				length.Reserved = 0;
				fixed (string str = data)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					length.Ptr = (ulong)((char*)offsetToStringData);
					num = this.EventWrite(ref eventDescriptor, 1, ref length);
				}
			}
			if (num == 0)
			{
				return true;
			}
			EventProviderClone.SetLastError((int)num);
			return false;
		}

		[SecurityCritical]
		protected internal bool WriteEvent(ref EventDescriptor eventDescriptor, int dataCount, IntPtr data)
		{
			uint num = 0;
			num = this.EventWrite(ref eventDescriptor, (uint)dataCount, (void*)data);
			if (num == 0)
			{
				return true;
			}
			EventProviderClone.SetLastError((int)num);
			return false;
		}

		[SecurityCritical]
		public unsafe bool WriteMessageEvent(string eventMessage, byte eventLevel, long eventKeywords)
		{
			int num = 0;
			if (eventMessage == null)
			{
				throw new ArgumentNullException("eventMessage");
			}
			if (this.IsEnabled(eventLevel, eventKeywords))
			{
				if (eventMessage.Length > 32724)
				{
					EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.EventTooBig;
					return false;
				}
				fixed (string str = eventMessage)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					num = (int)this.EventWriteString(eventLevel, eventKeywords, (char*)offsetToStringData);
				}
				if (num != 0)
				{
					EventProviderClone.SetLastError(num);
					return false;
				}
			}
			return true;
		}

		public bool WriteMessageEvent(string eventMessage)
		{
			return this.WriteMessageEvent(eventMessage, 0, (long)0);
		}

		[SecurityCritical]
		protected internal unsafe bool WriteTransfer(ref EventDescriptor eventDescriptor, ref Guid activityId, ref Guid relatedActivityId, params object[] eventPayload)
		{
			EventProviderClone.EventData eventDatum = new EventProviderClone.EventData();
			uint num = 0;
			if (this.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
			{
				int length = 0;
				if (eventPayload == null || (int)eventPayload.Length == 0 || (int)eventPayload.Length == 1)
				{
					string str = null;
					byte* numPointer = stackalloc byte[16];
					eventDatum.Size = 0;
					if (eventPayload != null && (int)eventPayload.Length != 0)
					{
						str = EventProviderClone.EncodeObject(ref eventPayload[0], ref eventDatum, numPointer);
						length = 1;
					}
					if (eventDatum.Size > 65482)
					{
						EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.EventTooBig;
						return false;
					}
					if (str == null)
					{
						num = (length != 0 ? this.EventWriteTransfer(ref eventDescriptor, ref activityId, ref relatedActivityId, (uint)length, ref eventDatum) : this.EventWriteTransfer(ref eventDescriptor, ref activityId, ref relatedActivityId, 0, 0));
					}
					else
					{
						fixed (string str1 = str)
						{
							string* offsetToStringData = &str1;
							if (offsetToStringData != null)
							{
								offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
							}
							eventDatum.Ptr = (ulong)((char*)offsetToStringData);
							num = this.EventWriteTransfer(ref eventDescriptor, ref activityId, ref relatedActivityId, (uint)length, ref eventDatum);
						}
					}
				}
				else
				{
					length = (int)eventPayload.Length;
					if (length > 32)
					{
						throw new ArgumentOutOfRangeException("eventPayload", EventSourceSR.ArgumentOutOfRange_MaxArgExceeded(32));
					}
					uint size = 0;
					int num1 = 0;
					int[] numArray = new int[16];
					string[] strArrays = new string[16];
					EventProviderClone.EventData* eventDataPointer = stackalloc EventProviderClone.EventData[checked(length * sizeof(EventProviderClone.EventData))];
					EventProviderClone.EventData* eventDataPointer1 = eventDataPointer;
					byte* numPointer1 = stackalloc byte[16 * length];
					for (int i = 0; i < (int)eventPayload.Length; i++)
					{
						if (eventPayload[i] != null)
						{
							string str2 = EventProviderClone.EncodeObject(ref eventPayload[i], eventDataPointer1, numPointer1);
							numPointer1 = numPointer1 + 16;
							size = size + (*eventDataPointer1).Size;
							eventDataPointer1 = eventDataPointer1 + sizeof(EventProviderClone.EventData);
							if (str2 != null)
							{
								if (num1 >= 16)
								{
									throw new ArgumentOutOfRangeException("eventPayload", EventSourceSR.ArgumentOutOfRange_MaxStringsExceeded(16));
								}
								strArrays[num1] = str2;
								numArray[num1] = i;
								num1++;
							}
						}
					}
					if (size > 65482)
					{
						EventProviderClone.s_returnCode = EventProviderClone.WriteEventErrorCode.EventTooBig;
						return false;
					}
					fixed (string str3 = strArrays[0])
					{
						string* strPointers = &str3;
						if (strPointers != null)
						{
							strPointers = strPointers + RuntimeHelpers.OffsetToStringData;
						}
						char* chrPointer = (char*)strPointers;
						fixed (string str4 = strArrays[1])
						{
							string* offsetToStringData1 = &str4;
							if (offsetToStringData1 != null)
							{
								offsetToStringData1 = offsetToStringData1 + RuntimeHelpers.OffsetToStringData;
							}
							char* chrPointer1 = (char*)offsetToStringData1;
							fixed (string str5 = strArrays[2])
							{
								string* strPointers1 = &str5;
								if (strPointers1 != null)
								{
									strPointers1 = strPointers1 + RuntimeHelpers.OffsetToStringData;
								}
								char* chrPointer2 = (char*)strPointers1;
								fixed (string str6 = strArrays[3])
								{
									string* offsetToStringData2 = &str6;
									if (offsetToStringData2 != null)
									{
										offsetToStringData2 = offsetToStringData2 + RuntimeHelpers.OffsetToStringData;
									}
									char* chrPointer3 = (char*)offsetToStringData2;
									fixed (string str7 = strArrays[4])
									{
										string* strPointers2 = &str7;
										if (strPointers2 != null)
										{
											strPointers2 = strPointers2 + RuntimeHelpers.OffsetToStringData;
										}
										char* chrPointer4 = (char*)strPointers2;
										fixed (string str8 = strArrays[5])
										{
											string* offsetToStringData3 = &str8;
											if (offsetToStringData3 != null)
											{
												offsetToStringData3 = offsetToStringData3 + RuntimeHelpers.OffsetToStringData;
											}
											char* chrPointer5 = (char*)offsetToStringData3;
											fixed (string str9 = strArrays[6])
											{
												string* strPointers3 = &str9;
												if (strPointers3 != null)
												{
													strPointers3 = strPointers3 + RuntimeHelpers.OffsetToStringData;
												}
												char* chrPointer6 = (char*)strPointers3;
												fixed (string str10 = strArrays[7])
												{
													string* offsetToStringData4 = &str10;
													if (offsetToStringData4 != null)
													{
														offsetToStringData4 = offsetToStringData4 + RuntimeHelpers.OffsetToStringData;
													}
													char* chrPointer7 = (char*)offsetToStringData4;
													eventDataPointer1 = eventDataPointer;
													if (strArrays[0] != null)
													{
														(*(eventDataPointer1 + numArray[0] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer;
													}
													if (strArrays[1] != null)
													{
														(*(eventDataPointer1 + numArray[1] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer1;
													}
													if (strArrays[2] != null)
													{
														(*(eventDataPointer1 + numArray[2] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer2;
													}
													if (strArrays[3] != null)
													{
														(*(eventDataPointer1 + numArray[3] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer3;
													}
													if (strArrays[4] != null)
													{
														(*(eventDataPointer1 + numArray[4] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer4;
													}
													if (strArrays[5] != null)
													{
														(*(eventDataPointer1 + numArray[5] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer5;
													}
													if (strArrays[6] != null)
													{
														(*(eventDataPointer1 + numArray[6] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer6;
													}
													if (strArrays[7] != null)
													{
														(*(eventDataPointer1 + numArray[7] * sizeof(EventProviderClone.EventData))).Ptr = (ulong)chrPointer7;
													}
													num = this.EventWriteTransfer(ref eventDescriptor, ref activityId, ref relatedActivityId, (uint)length, eventDataPointer);
												}
											}
										}
									}
								}
							}
						}
					}
					str4 = null;
					str5 = null;
					str6 = null;
					str7 = null;
					str8 = null;
					str9 = null;
					str10 = null;
				}
			}
			if (num == 0)
			{
				return true;
			}
			EventProviderClone.SetLastError((int)num);
			return false;
		}

		[DllImport("kernel32.dll", CharSet=CharSet.None, ExactSpelling=false, SetLastError=true)]
		[SuppressUnmanagedCodeSecurity]
		internal static extern void ZeroMemory(IntPtr handle, int length);

		private enum ActivityControl : uint
		{
			EVENT_ACTIVITY_CTRL_GET_ID = 1,
			EVENT_ACTIVITY_CTRL_SET_ID = 2,
			EVENT_ACTIVITY_CTRL_CREATE_ID = 3,
			EVENT_ACTIVITY_CTRL_GET_SET_ID = 4,
			EVENT_ACTIVITY_CTRL_CREATE_SET_ID = 5
		}

		internal struct EventData
		{
			internal ulong Ptr;

			internal uint Size;

			internal uint Reserved;
		}

		[SuppressUnmanagedCodeSecurity]
		internal static class ManifestEtw
		{
			private const int WindowsVistaMajorNumber = 6;

			internal const int ERROR_ARITHMETIC_OVERFLOW = 534;

			internal const int ERROR_NOT_ENOUGH_MEMORY = 8;

			internal const int ERROR_MORE_DATA = 234;

			private readonly static bool IsVistaOrGreater;

			static ManifestEtw()
			{
				EventProviderClone.ManifestEtw.IsVistaOrGreater = Environment.OSVersion.Version.Major >= 6;
			}

			[DllImport("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="EventActivityIdControl", ExactSpelling=true)]
			private static extern uint _EventActivityIdControl([In] int ControlCode, [In][Out] ref Guid ActivityId);

			[DllImport("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="EventRegister", ExactSpelling=true)]
			private static extern unsafe uint _EventRegister([In] ref Guid providerId, [In] EventProviderClone.ManifestEtw.EtwEnableCallback enableCallback, [In] void* callbackContext, [In][Out] ref long registrationHandle);

			[DllImport("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="EventUnregister", ExactSpelling=true)]
			private static extern uint _EventUnregister([In] long registrationHandle);

			[DllImport("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="EventWrite", ExactSpelling=true)]
			private static extern unsafe uint _EventWrite([In] long registrationHandle, [In] ref EventDescriptor eventDescriptor, [In] uint userDataCount, [In] EventProviderClone.EventData* userData);

			[DllImport("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="EventWriteString", ExactSpelling=true)]
			private static extern unsafe uint _EventWriteString([In] long registrationHandle, [In] byte level, [In] long keywords, [In] char* message);

			[DllImport("advapi32.dll", CharSet=CharSet.Unicode, EntryPoint="EventWriteTransfer", ExactSpelling=true)]
			private static extern unsafe uint _EventWriteTransfer([In] long registrationHandle, [In] ref EventDescriptor eventDescriptor, [In] ref Guid activityId, [In] ref Guid relatedActivityId, [In] uint userDataCount, [In] EventProviderClone.EventData* userData);

			internal static uint EventActivityIdControl(int ControlCode, ref Guid ActivityId)
			{
				if (!EventProviderClone.ManifestEtw.IsVistaOrGreater)
				{
					return (uint)0;
				}
				return EventProviderClone.ManifestEtw._EventActivityIdControl(ControlCode, ref ActivityId);
			}

			internal static unsafe uint EventRegister(ref Guid providerId, EventProviderClone.ManifestEtw.EtwEnableCallback enableCallback, void* callbackContext, ref long registrationHandle)
			{
				if (!EventProviderClone.ManifestEtw.IsVistaOrGreater)
				{
					return (uint)0;
				}
				return EventProviderClone.ManifestEtw._EventRegister(ref providerId, enableCallback, callbackContext, ref registrationHandle);
			}

			internal static uint EventUnregister(long registrationHandle)
			{
				if (!EventProviderClone.ManifestEtw.IsVistaOrGreater)
				{
					return (uint)0;
				}
				return EventProviderClone.ManifestEtw._EventUnregister(registrationHandle);
			}

			internal static unsafe uint EventWrite([In] long registrationHandle, [In] ref EventDescriptor eventDescriptor, [In] uint userDataCount, [In] EventProviderClone.EventData* userData)
			{
				if (!EventProviderClone.ManifestEtw.IsVistaOrGreater)
				{
					return (uint)0;
				}
				return EventProviderClone.ManifestEtw._EventWrite(registrationHandle, ref eventDescriptor, userDataCount, userData);
			}

			internal static unsafe uint EventWriteString([In] long registrationHandle, [In] byte level, [In] long keywords, [In] char* message)
			{
				if (!EventProviderClone.ManifestEtw.IsVistaOrGreater)
				{
					return (uint)0;
				}
				return EventProviderClone.ManifestEtw._EventWriteString(registrationHandle, level, keywords, message);
			}

			internal static unsafe uint EventWriteTransfer(long registrationHandle, ref EventDescriptor eventDescriptor, ref Guid activityId, ref Guid relatedActivityId, uint userDataCount, EventProviderClone.EventData* userData)
			{
				if (!EventProviderClone.ManifestEtw.IsVistaOrGreater)
				{
					return (uint)0;
				}
				return EventProviderClone.ManifestEtw._EventWriteTransfer(registrationHandle, ref eventDescriptor, ref activityId, ref relatedActivityId, userDataCount, userData);
			}

			internal delegate void EtwEnableCallback([In] ref Guid sourceId, [In] int isEnabled, [In] byte level, [In] long matchAnyKeywords, [In] long matchAllKeywords, [In] EventProviderClone.ManifestEtw.EVENT_FILTER_DESCRIPTOR* filterData, [In] void* callbackContext);

			internal struct EVENT_FILTER_DESCRIPTOR
			{
				public long Ptr;

				public int Size;

				public int Type;
			}
		}

		public enum WriteEventErrorCode
		{
			NoError,
			NoFreeBuffers,
			EventTooBig
		}
	}
}