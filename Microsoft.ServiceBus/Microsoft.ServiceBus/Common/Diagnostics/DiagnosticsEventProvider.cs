using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Common.Interop;
using System;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Threading;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort=true)]
	internal class DiagnosticsEventProvider : IDisposable
	{
		private const int basicTypeAllocationBufferSize = 16;

		private const int etwMaxNumberArguments = 32;

		private const int etwAPIMaxStringCount = 8;

		private const int maxEventDataDescriptors = 128;

		private const int traceEventMaximumSize = 65482;

		private const int traceEventMaximumStringSize = 32724;

		private const int WindowsVistaMajorNumber = 6;

		[SecurityCritical]
		private UnsafeNativeMethods.EtwEnableCallback etwCallback;

		private long traceRegistrationHandle;

		private byte currentTraceLevel;

		private long anyKeywordMask;

		private long allKeywordMask;

		private bool isProviderEnabled;

		private Guid providerId;

		private int isDisposed;

		[ThreadStatic]
		private static DiagnosticsEventProvider.WriteEventErrorCode errorCode;

		[PermissionSet(SecurityAction.Demand, Unrestricted=true)]
		[SecurityCritical]
		protected DiagnosticsEventProvider(Guid providerGuid)
		{
			this.providerId = providerGuid;
			this.EtwRegister();
		}

		public virtual void Close()
		{
			this.Dispose();
		}

		[SecurityCritical]
		public static Guid CreateActivityId()
		{
			Guid guid = new Guid();
			UnsafeNativeMethods.EventActivityIdControl(3, ref guid);
			return guid;
		}

		[SecurityCritical]
		private void Deregister()
		{
			if (this.traceRegistrationHandle != (long)0)
			{
				UnsafeNativeMethods.EventUnregister(this.traceRegistrationHandle);
				this.traceRegistrationHandle = (long)0;
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (this.isDisposed != 1 && Interlocked.Exchange(ref this.isDisposed, 1) == 0)
			{
				this.isProviderEnabled = false;
				this.Deregister();
			}
		}

		[SecurityCritical]
		private static unsafe string EncodeObject(ref object data, UnsafeNativeMethods.EventData* dataDescriptor, byte* dataBuffer)
		{
			(*dataDescriptor).Reserved = 0;
			string str = data as string;
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
				(*dataDescriptor).DataPointer = (ulong)intPtrPointer;
			}
			else if (data is int)
			{
				(*dataDescriptor).Size = 4;
				int* numPointer = (int*)dataBuffer;
				*numPointer = (int)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer;
			}
			else if (data is long)
			{
				(*dataDescriptor).Size = 8;
				long* numPointer1 = (long*)dataBuffer;
				*numPointer1 = (long)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer1;
			}
			else if (data is uint)
			{
				(*dataDescriptor).Size = 4;
				uint* numPointer2 = (uint*)dataBuffer;
				*numPointer2 = (uint)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer2;
			}
			else if (data is ulong)
			{
				(*dataDescriptor).Size = 8;
				ulong* numPointer3 = (ulong*)dataBuffer;
				*numPointer3 = (ulong)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer3;
			}
			else if (data is char)
			{
				(*dataDescriptor).Size = 2;
				char* chrPointer = (char*)dataBuffer;
				*chrPointer = (char)data;
				(*dataDescriptor).DataPointer = (ulong)chrPointer;
			}
			else if (data is byte)
			{
				(*dataDescriptor).Size = 1;
				byte* numPointer4 = dataBuffer;
				*numPointer4 = (byte)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer4;
			}
			else if (data is short)
			{
				(*dataDescriptor).Size = 2;
				short* numPointer5 = (short*)dataBuffer;
				*numPointer5 = (short)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer5;
			}
			else if (data is sbyte)
			{
				(*dataDescriptor).Size = 1;
				sbyte* numPointer6 = (sbyte*)dataBuffer;
				*numPointer6 = (sbyte)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer6;
			}
			else if (data is ushort)
			{
				(*dataDescriptor).Size = 2;
				ushort* numPointer7 = (ushort*)dataBuffer;
				*numPointer7 = (ushort)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer7;
			}
			else if (data is float)
			{
				(*dataDescriptor).Size = 4;
				float* singlePointer = (float*)dataBuffer;
				*singlePointer = (float)data;
				(*dataDescriptor).DataPointer = (ulong)singlePointer;
			}
			else if (data is double)
			{
				(*dataDescriptor).Size = 8;
				double* numPointer8 = (double*)dataBuffer;
				*numPointer8 = (double)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer8;
			}
			else if (data is bool)
			{
				(*dataDescriptor).Size = 1;
				bool* flagPointer = (bool*)dataBuffer;
				*flagPointer = (bool)data;
				(*dataDescriptor).DataPointer = (ulong)flagPointer;
			}
			else if (data is Guid)
			{
				(*dataDescriptor).Size = sizeof(Guid);
				Guid* guidPointer = (Guid*)dataBuffer;
				*guidPointer = (Guid)data;
				(*dataDescriptor).DataPointer = (ulong)guidPointer;
			}
			else if (!(data is decimal))
			{
				if (!(data is bool))
				{
					str = data.ToString();
					(*dataDescriptor).Size = (uint)((str.Length + 1) * 2);
					return str;
				}
				(*dataDescriptor).Size = 1;
				bool* flagPointer1 = (bool*)dataBuffer;
				*flagPointer1 = (bool)data;
				(*dataDescriptor).DataPointer = (ulong)flagPointer1;
			}
			else
			{
				(*dataDescriptor).Size = 16;
				decimal* numPointer9 = (decimal*)dataBuffer;
				*numPointer9 = (decimal)data;
				(*dataDescriptor).DataPointer = (ulong)numPointer9;
			}
			return null;
		}

		[SecurityCritical]
		private unsafe void EtwEnableCallBack([In] ref Guid sourceId, [In] int isEnabled, [In] byte setLevel, [In] long anyKeyword, [In] long allKeyword, [In] void* filterData, [In] void* callbackContext)
		{
			this.isProviderEnabled = isEnabled != 0;
			this.currentTraceLevel = setLevel;
			this.anyKeywordMask = anyKeyword;
			this.allKeywordMask = allKeyword;
			this.OnControllerCommand();
		}

		[SecurityCritical]
		private void EtwRegister()
		{
			unsafe
			{
				this.etwCallback = new UnsafeNativeMethods.EtwEnableCallback(this.EtwEnableCallBack);
				uint num = UnsafeNativeMethods.EventRegister(ref this.providerId, this.etwCallback, 0, ref this.traceRegistrationHandle);
				if (num != 0)
				{
					throw new InvalidOperationException(SRCore.EtwRegistrationFailed(num.ToString("x", CultureInfo.CurrentCulture)));
				}
			}
		}

		~DiagnosticsEventProvider()
		{
			this.Dispose(false);
		}

		[SecurityCritical]
		private static Guid GetActivityId()
		{
			object activityId = Trace.CorrelationManager.ActivityId;
			if (activityId == null)
			{
				return Guid.Empty;
			}
			return (Guid)activityId;
		}

		public static DiagnosticsEventProvider.WriteEventErrorCode GetLastWriteEventError()
		{
			return DiagnosticsEventProvider.errorCode;
		}

		public bool IsEnabled()
		{
			return this.isProviderEnabled;
		}

		public bool IsEnabled(byte level, long keywords)
		{
			if (this.isProviderEnabled && (level <= this.currentTraceLevel || this.currentTraceLevel == 0) && (keywords == (long)0 || (keywords & this.anyKeywordMask) != (long)0 && (keywords & this.allKeywordMask) == this.allKeywordMask))
			{
				return true;
			}
			return false;
		}

		protected virtual void OnControllerCommand()
		{
		}

		[SecurityCritical]
		public static void SetActivityId(ref Guid id)
		{
			UnsafeNativeMethods.EventActivityIdControl(2, ref id);
		}

		private static void SetLastError(int error)
		{
			int num = error;
			if (num == 8)
			{
				DiagnosticsEventProvider.errorCode = DiagnosticsEventProvider.WriteEventErrorCode.NoFreeBuffers;
				return;
			}
			if (num != 234 && num != 534)
			{
				return;
			}
			DiagnosticsEventProvider.errorCode = DiagnosticsEventProvider.WriteEventErrorCode.EventTooBig;
		}

		[SecurityCritical]
		public unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, params object[] eventPayload)
		{
			UnsafeNativeMethods.EventData eventDatum = new UnsafeNativeMethods.EventData();
			uint num = 0;
			if (this.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
			{
				int length = 0;
				Guid activityId = DiagnosticsEventProvider.GetActivityId();
				DiagnosticsEventProvider.SetActivityId(ref activityId);
				if (eventPayload == null || (int)eventPayload.Length == 0 || (int)eventPayload.Length == 1)
				{
					string str = null;
					byte* numPointer = stackalloc byte[16];
					eventDatum.Size = 0;
					if (eventPayload != null && (int)eventPayload.Length != 0)
					{
						str = DiagnosticsEventProvider.EncodeObject(ref eventPayload[0], ref eventDatum, numPointer);
						length = 1;
					}
					if (eventDatum.Size > 65482)
					{
						DiagnosticsEventProvider.errorCode = DiagnosticsEventProvider.WriteEventErrorCode.EventTooBig;
						return false;
					}
					if (str == null)
					{
						num = (length != 0 ? UnsafeNativeMethods.EventWrite(this.traceRegistrationHandle, ref eventDescriptor, (uint)length, ref eventDatum) : UnsafeNativeMethods.EventWrite(this.traceRegistrationHandle, ref eventDescriptor, 0, 0));
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
							eventDatum.DataPointer = (ulong)((char*)offsetToStringData);
							num = UnsafeNativeMethods.EventWrite(this.traceRegistrationHandle, ref eventDescriptor, (uint)length, ref eventDatum);
						}
					}
				}
				else
				{
					length = (int)eventPayload.Length;
					if (length > 32)
					{
						throw Fx.Exception.AsError(new ArgumentOutOfRangeException("eventPayload", SRCore.EtwMaxNumberArgumentsExceeded(32)), null);
					}
					uint size = 0;
					int num1 = 0;
					int[] numArray = new int[8];
					string[] strArrays = new string[8];
					UnsafeNativeMethods.EventData* eventDataPointer = stackalloc UnsafeNativeMethods.EventData[checked(length * sizeof(UnsafeNativeMethods.EventData))];
					UnsafeNativeMethods.EventData* eventDataPointer1 = eventDataPointer;
					byte* numPointer1 = stackalloc byte[16 * length];
					for (int i = 0; i < (int)eventPayload.Length; i++)
					{
						if (eventPayload[i] != null)
						{
							string str2 = DiagnosticsEventProvider.EncodeObject(ref eventPayload[i], eventDataPointer1, numPointer1);
							numPointer1 = numPointer1 + 16;
							size = size + (*eventDataPointer1).Size;
							eventDataPointer1 = eventDataPointer1 + sizeof(UnsafeNativeMethods.EventData);
							if (str2 != null)
							{
								if (num1 >= 8)
								{
									throw Fx.Exception.AsError(new ArgumentOutOfRangeException("eventPayload", SRCore.EtwAPIMaxStringCountExceeded(8)), null);
								}
								strArrays[num1] = str2;
								numArray[num1] = i;
								num1++;
							}
						}
					}
					if (size > 65482)
					{
						DiagnosticsEventProvider.errorCode = DiagnosticsEventProvider.WriteEventErrorCode.EventTooBig;
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
														(*(eventDataPointer1 + numArray[0] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer;
													}
													if (strArrays[1] != null)
													{
														(*(eventDataPointer1 + numArray[1] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer1;
													}
													if (strArrays[2] != null)
													{
														(*(eventDataPointer1 + numArray[2] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer2;
													}
													if (strArrays[3] != null)
													{
														(*(eventDataPointer1 + numArray[3] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer3;
													}
													if (strArrays[4] != null)
													{
														(*(eventDataPointer1 + numArray[4] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer4;
													}
													if (strArrays[5] != null)
													{
														(*(eventDataPointer1 + numArray[5] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer5;
													}
													if (strArrays[6] != null)
													{
														(*(eventDataPointer1 + numArray[6] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer6;
													}
													if (strArrays[7] != null)
													{
														(*(eventDataPointer1 + numArray[7] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer7;
													}
													num = UnsafeNativeMethods.EventWrite(this.traceRegistrationHandle, ref eventDescriptor, (uint)length, eventDataPointer);
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
			DiagnosticsEventProvider.SetLastError((int)num);
			return false;
		}

		[SecurityCritical]
		public unsafe bool WriteEvent(ref EventDescriptor eventDescriptor, string data)
		{
			UnsafeNativeMethods.EventData length = new UnsafeNativeMethods.EventData();
			uint num = 0;
			data = data ?? string.Empty;
			if (this.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
			{
				if (data.Length > 32724)
				{
					DiagnosticsEventProvider.errorCode = DiagnosticsEventProvider.WriteEventErrorCode.EventTooBig;
					return false;
				}
				Guid activityId = DiagnosticsEventProvider.GetActivityId();
				DiagnosticsEventProvider.SetActivityId(ref activityId);
				length.Size = (uint)((data.Length + 1) * 2);
				length.Reserved = 0;
				fixed (string str = data)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					length.DataPointer = (ulong)((char*)offsetToStringData);
					num = UnsafeNativeMethods.EventWrite(this.traceRegistrationHandle, ref eventDescriptor, 1, ref length);
				}
			}
			if (num == 0)
			{
				return true;
			}
			DiagnosticsEventProvider.SetLastError((int)num);
			return false;
		}

		[SecurityCritical]
		protected internal bool WriteEvent(ref EventDescriptor eventDescriptor, int dataCount, IntPtr data)
		{
			uint num = 0;
			Guid activityId = DiagnosticsEventProvider.GetActivityId();
			DiagnosticsEventProvider.SetActivityId(ref activityId);
			num = UnsafeNativeMethods.EventWrite(this.traceRegistrationHandle, ref eventDescriptor, (uint)dataCount, (void*)data);
			if (num == 0)
			{
				return true;
			}
			DiagnosticsEventProvider.SetLastError((int)num);
			return false;
		}

		[SecurityCritical]
		public unsafe bool WriteMessageEvent(string eventMessage, byte eventLevel, long eventKeywords)
		{
			int num = 0;
			if (eventMessage == null)
			{
				throw Fx.Exception.AsError(new ArgumentNullException("eventMessage"), null);
			}
			if (this.IsEnabled(eventLevel, eventKeywords))
			{
				if (eventMessage.Length > 32724)
				{
					DiagnosticsEventProvider.errorCode = DiagnosticsEventProvider.WriteEventErrorCode.EventTooBig;
					return false;
				}
				fixed (string str = eventMessage)
				{
					string* offsetToStringData = &str;
					if (offsetToStringData != null)
					{
						offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
					}
					char* chrPointer = (char*)offsetToStringData;
					num = (int)UnsafeNativeMethods.EventWriteString(this.traceRegistrationHandle, eventLevel, eventKeywords, chrPointer);
				}
				if (num != 0)
				{
					DiagnosticsEventProvider.SetLastError(num);
					return false;
				}
			}
			return true;
		}

		[SecurityCritical]
		public bool WriteMessageEvent(string eventMessage)
		{
			return this.WriteMessageEvent(eventMessage, 0, (long)0);
		}

		[SecurityCritical]
		public unsafe bool WriteTransferEvent(ref EventDescriptor eventDescriptor, Guid relatedActivityId, params object[] eventPayload)
		{
			uint num = 0;
			if (this.IsEnabled(eventDescriptor.Level, eventDescriptor.Keywords))
			{
				Guid activityId = DiagnosticsEventProvider.GetActivityId();
				if (eventPayload == null || (int)eventPayload.Length == 0)
				{
					num = UnsafeNativeMethods.EventWriteTransfer(this.traceRegistrationHandle, ref eventDescriptor, ref activityId, ref relatedActivityId, 0, 0);
				}
				else
				{
					int length = (int)eventPayload.Length;
					if (length > 32)
					{
						throw Fx.Exception.AsError(new ArgumentOutOfRangeException("eventPayload", SRCore.EtwMaxNumberArgumentsExceeded(32)), null);
					}
					uint size = 0;
					int num1 = 0;
					int[] numArray = new int[8];
					string[] strArrays = new string[8];
					UnsafeNativeMethods.EventData* eventDataPointer = stackalloc UnsafeNativeMethods.EventData[checked(length * sizeof(UnsafeNativeMethods.EventData))];
					UnsafeNativeMethods.EventData* eventDataPointer1 = eventDataPointer;
					byte* numPointer = stackalloc byte[16 * length];
					for (int i = 0; i < (int)eventPayload.Length; i++)
					{
						if (eventPayload[i] != null)
						{
							string str = DiagnosticsEventProvider.EncodeObject(ref eventPayload[i], eventDataPointer1, numPointer);
							numPointer = numPointer + 16;
							size = size + (*eventDataPointer1).Size;
							eventDataPointer1 = eventDataPointer1 + sizeof(UnsafeNativeMethods.EventData);
							if (str != null)
							{
								if (num1 >= 8)
								{
									throw Fx.Exception.AsError(new ArgumentOutOfRangeException("eventPayload", SRCore.EtwAPIMaxStringCountExceeded(8)), null);
								}
								strArrays[num1] = str;
								numArray[num1] = i;
								num1++;
							}
						}
					}
					if (size > 65482)
					{
						DiagnosticsEventProvider.errorCode = DiagnosticsEventProvider.WriteEventErrorCode.EventTooBig;
						return false;
					}
					fixed (string str1 = strArrays[0])
					{
						string* offsetToStringData = &str1;
						if (offsetToStringData != null)
						{
							offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
						}
						char* chrPointer = (char*)offsetToStringData;
						fixed (string str2 = strArrays[1])
						{
							string* strPointers = &str2;
							if (strPointers != null)
							{
								strPointers = strPointers + RuntimeHelpers.OffsetToStringData;
							}
							char* chrPointer1 = (char*)strPointers;
							fixed (string str3 = strArrays[2])
							{
								string* offsetToStringData1 = &str3;
								if (offsetToStringData1 != null)
								{
									offsetToStringData1 = offsetToStringData1 + RuntimeHelpers.OffsetToStringData;
								}
								char* chrPointer2 = (char*)offsetToStringData1;
								fixed (string str4 = strArrays[3])
								{
									string* strPointers1 = &str4;
									if (strPointers1 != null)
									{
										strPointers1 = strPointers1 + RuntimeHelpers.OffsetToStringData;
									}
									char* chrPointer3 = (char*)strPointers1;
									fixed (string str5 = strArrays[4])
									{
										string* offsetToStringData2 = &str5;
										if (offsetToStringData2 != null)
										{
											offsetToStringData2 = offsetToStringData2 + RuntimeHelpers.OffsetToStringData;
										}
										char* chrPointer4 = (char*)offsetToStringData2;
										fixed (string str6 = strArrays[5])
										{
											string* strPointers2 = &str6;
											if (strPointers2 != null)
											{
												strPointers2 = strPointers2 + RuntimeHelpers.OffsetToStringData;
											}
											char* chrPointer5 = (char*)strPointers2;
											fixed (string str7 = strArrays[6])
											{
												string* offsetToStringData3 = &str7;
												if (offsetToStringData3 != null)
												{
													offsetToStringData3 = offsetToStringData3 + RuntimeHelpers.OffsetToStringData;
												}
												char* chrPointer6 = (char*)offsetToStringData3;
												fixed (string str8 = strArrays[7])
												{
													string* strPointers3 = &str8;
													if (strPointers3 != null)
													{
														strPointers3 = strPointers3 + RuntimeHelpers.OffsetToStringData;
													}
													char* chrPointer7 = (char*)strPointers3;
													eventDataPointer1 = eventDataPointer;
													if (strArrays[0] != null)
													{
														(*(eventDataPointer1 + numArray[0] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer;
													}
													if (strArrays[1] != null)
													{
														(*(eventDataPointer1 + numArray[1] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer1;
													}
													if (strArrays[2] != null)
													{
														(*(eventDataPointer1 + numArray[2] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer2;
													}
													if (strArrays[3] != null)
													{
														(*(eventDataPointer1 + numArray[3] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer3;
													}
													if (strArrays[4] != null)
													{
														(*(eventDataPointer1 + numArray[4] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer4;
													}
													if (strArrays[5] != null)
													{
														(*(eventDataPointer1 + numArray[5] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer5;
													}
													if (strArrays[6] != null)
													{
														(*(eventDataPointer1 + numArray[6] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer6;
													}
													if (strArrays[7] != null)
													{
														(*(eventDataPointer1 + numArray[7] * sizeof(UnsafeNativeMethods.EventData))).DataPointer = (ulong)chrPointer7;
													}
													num = UnsafeNativeMethods.EventWriteTransfer(this.traceRegistrationHandle, ref eventDescriptor, ref activityId, ref relatedActivityId, (uint)length, eventDataPointer);
												}
											}
										}
									}
								}
							}
						}
					}
					str2 = null;
					str3 = null;
					str4 = null;
					str5 = null;
					str6 = null;
					str7 = null;
					str8 = null;
				}
			}
			if (num == 0)
			{
				return true;
			}
			DiagnosticsEventProvider.SetLastError((int)num);
			return false;
		}

		[SecurityCritical]
		protected bool WriteTransferEvent(ref EventDescriptor eventDescriptor, Guid relatedActivityId, int dataCount, IntPtr data)
		{
			uint num = 0;
			Guid activityId = DiagnosticsEventProvider.GetActivityId();
			num = UnsafeNativeMethods.EventWriteTransfer(this.traceRegistrationHandle, ref eventDescriptor, ref activityId, ref relatedActivityId, (uint)dataCount, (void*)data);
			if (num == 0)
			{
				return true;
			}
			DiagnosticsEventProvider.SetLastError((int)num);
			return false;
		}

		private enum ActivityControl : uint
		{
			EventActivityControlGetId = 1,
			EventActivityControlSetId = 2,
			EventActivityControlCreateId = 3,
			EventActivityControlGetSetId = 4,
			EventActivityControlCreateSetId = 5
		}

		public enum WriteEventErrorCode
		{
			NoError,
			NoFreeBuffers,
			EventTooBig
		}
	}
}