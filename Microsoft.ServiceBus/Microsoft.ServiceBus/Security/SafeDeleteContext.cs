using Microsoft.ServiceBus.Common;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.ServiceBus.Security
{
	internal class SafeDeleteContext : SafeHandle
	{
		private const string SECURITY = "security.Dll";

		private const string dummyStr = " ";

		internal SSPIHandle _handle;

		private SafeFreeCredentials _EffectiveCredential;

		public override bool IsInvalid
		{
			get
			{
				if (base.IsClosed)
				{
					return true;
				}
				return this._handle.IsZero;
			}
		}

		protected SafeDeleteContext() : base(IntPtr.Zero, true)
		{
			this._handle = new SSPIHandle();
		}

		internal static int AcceptSecurityContext(SafeFreeCredentials inCredentials, ref SafeDeleteContext refContext, SspiContextFlags inFlags, Endianness endianness, SecurityBuffer inSecBuffer, SecurityBuffer[] inSecBuffers, SecurityBuffer outSecBuffer, ref SspiContextFlags outFlags)
		{
			unsafe
			{
				IntPtr* intPtrPointer;
				IntPtr* intPtrPointer1;
				if (inCredentials == null)
				{
					throw new ArgumentNullException("inCredentials");
				}
				SecurityBufferDescriptor securityBufferDescriptor = null;
				if (inSecBuffer != null)
				{
					securityBufferDescriptor = new SecurityBufferDescriptor(1);
				}
				else if (inSecBuffers != null)
				{
					securityBufferDescriptor = new SecurityBufferDescriptor((int)inSecBuffers.Length);
				}
				SecurityBufferDescriptor securityBufferDescriptor1 = new SecurityBufferDescriptor(1);
				bool flag = ((inFlags & SspiContextFlags.AllocateMemory) != SspiContextFlags.Zero ? true : false);
				int num = -1;
				SSPIHandle sSPIHandle = new SSPIHandle();
				if (refContext != null)
				{
					sSPIHandle = refContext._handle;
				}
				GCHandle[] gCHandleArray = null;
				GCHandle gCHandle = new GCHandle();
				SafeFreeContextBuffer safeFreeContextBuffer = null;
				try
				{
					gCHandle = GCHandle.Alloc(outSecBuffer.token, GCHandleType.Pinned);
					SecurityBufferStruct[] zero = new SecurityBufferStruct[(securityBufferDescriptor == null ? 1 : securityBufferDescriptor.Count)];
					try
					{
						SecurityBufferStruct[] securityBufferStructArray = zero;
						SecurityBufferStruct[] securityBufferStructArray1 = securityBufferStructArray;
						if (securityBufferStructArray == null || (int)securityBufferStructArray1.Length == 0)
						{
							intPtrPointer = null;
						}
						else
						{
							intPtrPointer = &securityBufferStructArray1[0];
						}
						if (securityBufferDescriptor != null)
						{
							securityBufferDescriptor.UnmanagedPointer = (void*)intPtrPointer;
							gCHandleArray = new GCHandle[securityBufferDescriptor.Count];
							for (int i = 0; i < securityBufferDescriptor.Count; i++)
							{
								SecurityBuffer securityBuffer = (inSecBuffer != null ? inSecBuffer : inSecBuffers[i]);
								if (securityBuffer != null)
								{
									zero[i].count = securityBuffer.size;
									zero[i].type = securityBuffer.type;
									if (securityBuffer.unmanagedToken != null)
									{
										zero[i].token = securityBuffer.unmanagedToken.DangerousGetHandle();
									}
									else if (securityBuffer.token == null || (int)securityBuffer.token.Length == 0)
									{
										zero[i].token = IntPtr.Zero;
									}
									else
									{
										gCHandleArray[i] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
										zero[i].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
									}
								}
							}
						}
						SecurityBufferStruct[] zero1 = new SecurityBufferStruct[1];
						try
						{
							SecurityBufferStruct[] securityBufferStructArray2 = zero1;
							SecurityBufferStruct[] securityBufferStructArray3 = securityBufferStructArray2;
							if (securityBufferStructArray2 == null || (int)securityBufferStructArray3.Length == 0)
							{
								intPtrPointer1 = null;
							}
							else
							{
								intPtrPointer1 = &securityBufferStructArray3[0];
							}
							securityBufferDescriptor1.UnmanagedPointer = (void*)intPtrPointer1;
							zero1[0].count = outSecBuffer.size;
							zero1[0].type = outSecBuffer.type;
							if (outSecBuffer.token == null || (int)outSecBuffer.token.Length == 0)
							{
								zero1[0].token = IntPtr.Zero;
							}
							else
							{
								zero1[0].token = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
							}
							if (flag)
							{
								safeFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
							}
							if (refContext == null || refContext.IsInvalid)
							{
								refContext = new SafeDeleteContext();
							}
							num = SafeDeleteContext.MustRunAcceptSecurityContext(inCredentials, (sSPIHandle.IsZero ? (void*)0 : (void*)(&sSPIHandle)), securityBufferDescriptor, inFlags, endianness, refContext, securityBufferDescriptor1, ref outFlags, safeFreeContextBuffer);
							outSecBuffer.size = zero1[0].count;
							outSecBuffer.type = zero1[0].type;
							if (outSecBuffer.size <= 0)
							{
								outSecBuffer.token = null;
							}
							else
							{
								outSecBuffer.token = new byte[outSecBuffer.size];
								Marshal.Copy(zero1[0].token, outSecBuffer.token, 0, outSecBuffer.size);
							}
						}
						finally
						{
							intPtrPointer1 = null;
						}
					}
					finally
					{
						intPtrPointer = null;
					}
				}
				finally
				{
					if (gCHandleArray != null)
					{
						for (int j = 0; j < (int)gCHandleArray.Length; j++)
						{
							if (gCHandleArray[j].IsAllocated)
							{
								gCHandleArray[j].Free();
							}
						}
					}
					if (gCHandle.IsAllocated)
					{
						gCHandle.Free();
					}
					if (safeFreeContextBuffer != null)
					{
						safeFreeContextBuffer.Close();
					}
				}
				return num;
			}
		}

		[DllImport("security.Dll", CharSet=CharSet.None, ExactSpelling=true, SetLastError=true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		private static extern unsafe int AcceptSecurityContext(ref SSPIHandle credentialHandle, [In] void* inContextPtr, [In] SecurityBufferDescriptor inputBuffer, [In] SspiContextFlags inFlags, [In] Endianness endianness, ref SSPIHandle outContextPtr, [In][Out] SecurityBufferDescriptor outputBuffer, [In][Out] ref SspiContextFlags attributes, out long timestamp);

		[DllImport("security.Dll", CharSet=CharSet.None, ExactSpelling=true, SetLastError=true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		internal static extern int DeleteSecurityContext(ref SSPIHandle handlePtr);

		internal static unsafe int InitializeSecurityContext(SafeFreeCredentials inCredentials, ref SafeDeleteContext refContext, string targetName, SspiContextFlags inFlags, Endianness endianness, SecurityBuffer inSecBuffer, SecurityBuffer[] inSecBuffers, SecurityBuffer outSecBuffer, ref SspiContextFlags outFlags)
		{
			IntPtr* intPtrPointer;
			IntPtr* intPtrPointer1;
			if (inCredentials == null)
			{
				throw new ArgumentNullException("inCredentials");
			}
			SecurityBufferDescriptor securityBufferDescriptor = null;
			if (inSecBuffer != null)
			{
				securityBufferDescriptor = new SecurityBufferDescriptor(1);
			}
			else if (inSecBuffers != null)
			{
				securityBufferDescriptor = new SecurityBufferDescriptor((int)inSecBuffers.Length);
			}
			SecurityBufferDescriptor securityBufferDescriptor1 = new SecurityBufferDescriptor(1);
			bool flag = ((inFlags & SspiContextFlags.AllocateMemory) != SspiContextFlags.Zero ? true : false);
			int num = -1;
			SSPIHandle sSPIHandle = new SSPIHandle();
			if (refContext != null)
			{
				sSPIHandle = refContext._handle;
			}
			GCHandle[] gCHandleArray = null;
			GCHandle gCHandle = new GCHandle();
			SafeFreeContextBuffer safeFreeContextBuffer = null;
			try
			{
				gCHandle = GCHandle.Alloc(outSecBuffer.token, GCHandleType.Pinned);
				SecurityBufferStruct[] zero = new SecurityBufferStruct[(securityBufferDescriptor == null ? 1 : securityBufferDescriptor.Count)];
				try
				{
					SecurityBufferStruct[] securityBufferStructArray = zero;
					SecurityBufferStruct[] securityBufferStructArray1 = securityBufferStructArray;
					if (securityBufferStructArray == null || (int)securityBufferStructArray1.Length == 0)
					{
						intPtrPointer = null;
					}
					else
					{
						intPtrPointer = &securityBufferStructArray1[0];
					}
					if (securityBufferDescriptor != null)
					{
						securityBufferDescriptor.UnmanagedPointer = (void*)intPtrPointer;
						gCHandleArray = new GCHandle[securityBufferDescriptor.Count];
						for (int i = 0; i < securityBufferDescriptor.Count; i++)
						{
							SecurityBuffer securityBuffer = (inSecBuffer != null ? inSecBuffer : inSecBuffers[i]);
							if (securityBuffer != null)
							{
								zero[i].count = securityBuffer.size;
								zero[i].type = securityBuffer.type;
								if (securityBuffer.unmanagedToken != null)
								{
									zero[i].token = securityBuffer.unmanagedToken.DangerousGetHandle();
								}
								else if (securityBuffer.token == null || (int)securityBuffer.token.Length == 0)
								{
									zero[i].token = IntPtr.Zero;
								}
								else
								{
									gCHandleArray[i] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
									zero[i].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
								}
							}
						}
					}
					SecurityBufferStruct[] zero1 = new SecurityBufferStruct[1];
					try
					{
						SecurityBufferStruct[] securityBufferStructArray2 = zero1;
						SecurityBufferStruct[] securityBufferStructArray3 = securityBufferStructArray2;
						if (securityBufferStructArray2 == null || (int)securityBufferStructArray3.Length == 0)
						{
							intPtrPointer1 = null;
						}
						else
						{
							intPtrPointer1 = &securityBufferStructArray3[0];
						}
						securityBufferDescriptor1.UnmanagedPointer = (void*)intPtrPointer1;
						zero1[0].count = outSecBuffer.size;
						zero1[0].type = outSecBuffer.type;
						if (outSecBuffer.token == null || (int)outSecBuffer.token.Length == 0)
						{
							zero1[0].token = IntPtr.Zero;
						}
						else
						{
							zero1[0].token = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
						}
						if (flag)
						{
							safeFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
						}
						if (refContext == null || refContext.IsInvalid)
						{
							refContext = new SafeDeleteContext();
						}
						if (targetName == null || targetName.Length == 0)
						{
							targetName = " ";
						}
						try
						{
							fixed (string str = targetName)
							{
								string* offsetToStringData = &str;
								if (offsetToStringData != null)
								{
									offsetToStringData = offsetToStringData + RuntimeHelpers.OffsetToStringData;
								}
								char* chrPointer = (char*)offsetToStringData;
								num = SafeDeleteContext.MustRunInitializeSecurityContext(inCredentials, (sSPIHandle.IsZero ? (void*)0 : (void*)(&sSPIHandle)), ((object)targetName == (object)" " ? (byte*)0 : (byte*)chrPointer), inFlags, endianness, securityBufferDescriptor, refContext, securityBufferDescriptor1, ref outFlags, safeFreeContextBuffer);
							}
						}
						finally
						{
							str = null;
						}
						outSecBuffer.size = zero1[0].count;
						outSecBuffer.type = zero1[0].type;
						if (outSecBuffer.size <= 0)
						{
							outSecBuffer.token = null;
						}
						else
						{
							outSecBuffer.token = new byte[outSecBuffer.size];
							Marshal.Copy(zero1[0].token, outSecBuffer.token, 0, outSecBuffer.size);
						}
					}
					finally
					{
						intPtrPointer1 = null;
					}
				}
				finally
				{
					intPtrPointer = null;
				}
			}
			finally
			{
				if (gCHandleArray != null)
				{
					for (int j = 0; j < (int)gCHandleArray.Length; j++)
					{
						if (gCHandleArray[j].IsAllocated)
						{
							gCHandleArray[j].Free();
						}
					}
				}
				if (gCHandle.IsAllocated)
				{
					gCHandle.Free();
				}
				if (safeFreeContextBuffer != null)
				{
					safeFreeContextBuffer.Close();
				}
			}
			return num;
		}

		[DllImport("security.Dll", CharSet=CharSet.None, ExactSpelling=true, SetLastError=true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		internal static extern unsafe int InitializeSecurityContextW(ref SSPIHandle credentialHandle, [In] void* inContextPtr, [In] byte* targetName, [In] SspiContextFlags inFlags, [In] int reservedI, [In] Endianness endianness, [In] SecurityBufferDescriptor inputBuffer, [In] int reservedII, ref SSPIHandle outContextPtr, [In][Out] SecurityBufferDescriptor outputBuffer, [In][Out] ref SspiContextFlags attributes, out long timestamp);

		private static unsafe int MustRunAcceptSecurityContext(SafeFreeCredentials inCredentials, void* inContextPtr, SecurityBufferDescriptor inputBuffer, SspiContextFlags inFlags, Endianness endianness, SafeDeleteContext outContext, SecurityBufferDescriptor outputBuffer, ref SspiContextFlags outFlags, SafeFreeContextBuffer handleTemplate)
		{
			long num;
			int num1 = -1;
			bool flag = false;
			bool flag1 = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				try
				{
					inCredentials.DangerousAddRef(ref flag);
					outContext.DangerousAddRef(ref flag1);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					if (flag)
					{
						inCredentials.DangerousRelease();
						flag = false;
					}
					if (flag1)
					{
						outContext.DangerousRelease();
						flag1 = false;
					}
					if (!(exception is ObjectDisposedException))
					{
						throw;
					}
				}
			}
			finally
			{
				if (!flag)
				{
					inCredentials = null;
				}
				else if (flag && flag1)
				{
					SSPIHandle sSPIHandle = inCredentials._handle;
					num1 = SafeDeleteContext.AcceptSecurityContext(ref sSPIHandle, inContextPtr, inputBuffer, inFlags, endianness, ref outContext._handle, outputBuffer, ref outFlags, out num);
					if (outContext._EffectiveCredential == inCredentials || ((long)num1 & (ulong)-2147483648) != (long)0)
					{
						inCredentials.DangerousRelease();
					}
					else
					{
						if (outContext._EffectiveCredential != null)
						{
							outContext._EffectiveCredential.DangerousRelease();
						}
						outContext._EffectiveCredential = inCredentials;
					}
					outContext.DangerousRelease();
					if (handleTemplate != null)
					{
						handleTemplate.Set((*(outputBuffer.UnmanagedPointer)).token);
						if (handleTemplate.IsInvalid)
						{
							handleTemplate.SetHandleAsInvalid();
						}
					}
					if (inContextPtr == null && ((long)num1 & (ulong)-2147483648) != (long)0)
					{
						outContext._handle.SetToInvalid();
					}
				}
			}
			return num1;
		}

		private static unsafe int MustRunInitializeSecurityContext(SafeFreeCredentials inCredentials, void* inContextPtr, byte* targetName, SspiContextFlags inFlags, Endianness endianness, SecurityBufferDescriptor inputBuffer, SafeDeleteContext outContext, SecurityBufferDescriptor outputBuffer, ref SspiContextFlags attributes, SafeFreeContextBuffer handleTemplate)
		{
			long num;
			int num1 = -1;
			bool flag = false;
			bool flag1 = false;
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
				try
				{
					inCredentials.DangerousAddRef(ref flag);
					outContext.DangerousAddRef(ref flag1);
				}
				catch (Exception exception1)
				{
					Exception exception = exception1;
					if (Fx.IsFatal(exception))
					{
						throw;
					}
					if (flag)
					{
						inCredentials.DangerousRelease();
						flag = false;
					}
					if (flag1)
					{
						outContext.DangerousRelease();
						flag1 = false;
					}
					if (!(exception is ObjectDisposedException))
					{
						throw;
					}
				}
			}
			finally
			{
				if (!flag)
				{
					inCredentials = null;
				}
				else if (flag && flag1)
				{
					SSPIHandle sSPIHandle = inCredentials._handle;
					num1 = SafeDeleteContext.InitializeSecurityContextW(ref sSPIHandle, inContextPtr, targetName, inFlags, 0, endianness, inputBuffer, 0, ref outContext._handle, outputBuffer, ref attributes, out num);
					if (outContext._EffectiveCredential == inCredentials || ((long)num1 & (ulong)-2147483648) != (long)0)
					{
						inCredentials.DangerousRelease();
					}
					else
					{
						if (outContext._EffectiveCredential != null)
						{
							outContext._EffectiveCredential.DangerousRelease();
						}
						outContext._EffectiveCredential = inCredentials;
					}
					outContext.DangerousRelease();
					if (handleTemplate != null)
					{
						handleTemplate.Set((*(outputBuffer.UnmanagedPointer)).token);
						if (handleTemplate.IsInvalid)
						{
							handleTemplate.SetHandleAsInvalid();
						}
					}
				}
				if (inContextPtr == null && ((long)num1 & (ulong)-2147483648) != (long)0)
				{
					outContext._handle.SetToInvalid();
				}
			}
			return num1;
		}

		protected override bool ReleaseHandle()
		{
			if (this._EffectiveCredential != null)
			{
				this._EffectiveCredential.DangerousRelease();
			}
			return SafeDeleteContext.DeleteSecurityContext(ref this._handle) == 0;
		}
	}
}