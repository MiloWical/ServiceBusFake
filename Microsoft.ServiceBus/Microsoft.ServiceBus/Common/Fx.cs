using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common.Diagnostics;
using Microsoft.ServiceBus.Properties;
using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Transactions;

namespace Microsoft.ServiceBus.Common
{
	internal static class Fx
	{
		private const string DefaultEventSource = "Microsoft.ServiceBus";

		private static ExceptionTrace exceptionTrace;

		private static DiagnosticTrace diagnosticTrace;

		public static ExceptionTrace Exception
		{
			get
			{
				if (Fx.exceptionTrace == null)
				{
					Fx.exceptionTrace = new ExceptionTrace("Microsoft.ServiceBus");
				}
				return Fx.exceptionTrace;
			}
		}

		internal static bool FailFastInProgress
		{
			get;
			private set;
		}

		public static DiagnosticTrace Trace
		{
			get
			{
				if (Fx.diagnosticTrace == null)
				{
					Fx.diagnosticTrace = Fx.InitializeTracing();
				}
				return Fx.diagnosticTrace;
			}
		}

		public static byte[] AllocateByteArray(int size)
		{
			byte[] numArray;
			try
			{
				numArray = new byte[size];
			}
			catch (OutOfMemoryException outOfMemoryException1)
			{
				OutOfMemoryException outOfMemoryException = outOfMemoryException1;
				ExceptionTrace exception = Fx.Exception;
				string bufferAllocationFailed = Resources.BufferAllocationFailed;
				object[] objArray = new object[] { size };
				throw exception.AsError(new InsufficientMemoryException(Microsoft.ServiceBus.SR.GetString(bufferAllocationFailed, objArray), outOfMemoryException), null);
			}
			return numArray;
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string description)
		{
		}

		[Conditional("DEBUG")]
		public static void Assert(string description)
		{
		}

		public static void AssertAndFailFastService(bool condition, string description)
		{
			if (!condition)
			{
				Fx.AssertAndFailFastService(description);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static System.Exception AssertAndFailFastService(string description)
		{
			string str = SRCore.FailFastMessage(description);
			try
			{
				try
				{
					MessagingClientEtwProvider.Provider.EventWriteFailFastOccurred(description);
					Fx.Exception.TraceFailFast(str);
					Fx.FailFastInProgress = true;
					Thread.Sleep(TimeSpan.FromSeconds(15));
				}
				finally
				{
					Thread thread = new Thread(() => throw new FatalException(str));
					thread.Start();
					thread.Join();
				}
			}
			catch
			{
				throw;
			}
			return null;
		}

		public static void AssertAndThrow(bool condition, string description)
		{
			if (!condition)
			{
				Fx.AssertAndThrow(description);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static System.Exception AssertAndThrow(string description)
		{
			throw Fx.Exception.AsError(new AssertionFailedException(description), null);
		}

		public static void AssertAndThrowFatal(bool condition, string description)
		{
			if (!condition)
			{
				Fx.AssertAndThrowFatal(description);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static System.Exception AssertAndThrowFatal(string description)
		{
			throw Fx.Exception.AsError(new FatalException(description), null);
		}

		public static void AssertIsNotNull(object objectMayBeNull, string description)
		{
			if (objectMayBeNull == null)
			{
				Fx.AssertAndThrow(description);
			}
		}

		public static void CompleteTransactionScope(ref TransactionScope scope)
		{
			TransactionScope transactionScope = scope;
			if (transactionScope != null)
			{
				scope = null;
				try
				{
					transactionScope.Complete();
				}
				finally
				{
					transactionScope.Dispose();
				}
			}
		}

		public static TransactionScope CreateTransactionScope(Transaction transaction)
		{
			TransactionScope transactionScope;
			TransactionScope transactionScope1;
			try
			{
				if (transaction == null)
				{
					transactionScope1 = null;
				}
				else
				{
					transactionScope1 = new TransactionScope(transaction);
				}
				transactionScope = transactionScope1;
			}
			catch (TransactionAbortedException transactionAbortedException)
			{
				CommittableTransaction committableTransaction = new CommittableTransaction();
				try
				{
					transactionScope = new TransactionScope(committableTransaction.Clone());
				}
				finally
				{
					committableTransaction.Rollback();
				}
			}
			return transactionScope;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private static bool HandleAtThreadBase(System.Exception exception)
		{
			if (exception == null)
			{
				return false;
			}
			Fx.TraceExceptionNoThrow(exception);
			return false;
		}

		private static DiagnosticTrace InitializeTracing()
		{
			return new DiagnosticTrace("Microsoft.ServiceBus", DiagnosticTrace.DefaultEtwProviderId);
		}

		public static bool IsFatal(System.Exception exception)
		{
			bool flag;
			while (exception != null)
			{
				if (exception is FatalException || exception is OutOfMemoryException && !(exception is InsufficientMemoryException) || exception is ThreadAbortException || exception is AccessViolationException || exception is SEHException)
				{
					return true;
				}
				if (exception is TypeInitializationException || exception is TargetInvocationException)
				{
					exception = exception.InnerException;
				}
				else if (!(exception is AggregateException))
				{
					if (!(exception is NullReferenceException))
					{
						break;
					}
					MessagingClientEtwProvider.Provider.EventWriteNullReferenceErrorOccurred(exception.ToString());
					break;
				}
				else
				{
					using (IEnumerator<System.Exception> enumerator = ((AggregateException)exception).InnerExceptions.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (!Fx.IsFatal(enumerator.Current))
							{
								continue;
							}
							flag = true;
							return flag;
						}
						break;
					}
					return flag;
				}
			}
			return false;
		}

		[SecurityCritical]
		public static IOCompletionCallback ThunkCallback(IOCompletionCallback callback)
		{
			return (new Fx.IOCompletionThunk(callback)).ThunkFrame;
		}

		public static TransactionCompletedEventHandler ThunkTransactionEventHandler(TransactionCompletedEventHandler handler)
		{
			return (new Fx.TransactionEventHandlerThunk(handler)).ThunkFrame;
		}

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private static void TraceExceptionNoThrow(System.Exception exception)
		{
			try
			{
				Fx.Exception.TraceUnhandled(exception);
			}
			catch
			{
			}
		}

		[SecurityCritical]
		private sealed class IOCompletionThunk
		{
			private IOCompletionCallback callback;

			public IOCompletionCallback ThunkFrame
			{
				get
				{
					return new IOCompletionCallback(this.UnhandledExceptionFrame);
				}
			}

			public IOCompletionThunk(IOCompletionCallback callback)
			{
				this.callback = callback;
			}

			private unsafe void UnhandledExceptionFrame(uint error, uint bytesRead, NativeOverlapped* nativeOverlapped)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.callback(error, bytesRead, nativeOverlapped);
				}
				catch (System.Exception exception)
				{
					if (!Fx.HandleAtThreadBase(exception))
					{
						throw;
					}
				}
			}
		}

		public static class Tag
		{
			[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class BlockingAttribute : Attribute
			{
				public Type CancelDeclaringType
				{
					get;
					set;
				}

				public string CancelMethod
				{
					get;
					set;
				}

				public string Conditional
				{
					get;
					set;
				}

				public BlockingAttribute()
				{
				}
			}

			[Flags]
			public enum BlocksUsing
			{
				MonitorEnter,
				MonitorWait,
				ManualResetEvent,
				AutoResetEvent,
				AsyncResult,
				IAsyncResult,
				PInvoke,
				InputQueue,
				ThreadNeutralSemaphore,
				PrivatePrimitive,
				OtherInternalPrimitive,
				OtherFrameworkPrimitive,
				OtherInterop,
				Other,
				NonBlocking
			}

			[AttributeUsage(AttributeTargets.Field)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class CacheAttribute : Attribute
			{
				private readonly Type elementType;

				private readonly Fx.Tag.CacheAttrition cacheAttrition;

				public Fx.Tag.CacheAttrition CacheAttrition
				{
					get
					{
						return this.cacheAttrition;
					}
				}

				public Type ElementType
				{
					get
					{
						return this.elementType;
					}
				}

				public string Scope
				{
					get;
					set;
				}

				public string SizeLimit
				{
					get;
					set;
				}

				public string Timeout
				{
					get;
					set;
				}

				public CacheAttribute(Type elementType, Fx.Tag.CacheAttrition cacheAttrition)
				{
					this.Scope = "instance of declaring class";
					this.SizeLimit = "unbounded";
					this.Timeout = "infinite";
					if (elementType == null)
					{
						throw Fx.Exception.ArgumentNull("elementType");
					}
					this.elementType = elementType;
					this.cacheAttrition = cacheAttrition;
				}
			}

			public enum CacheAttrition
			{
				None,
				ElementOnTimer,
				ElementOnGC,
				ElementOnCallback,
				FullPurgeOnTimer,
				FullPurgeOnEachAccess,
				PartialPurgeOnTimer,
				PartialPurgeOnEachAccess
			}

			[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple=true, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class ExternalResourceAttribute : Attribute
			{
				private readonly Fx.Tag.Location location;

				private readonly string description;

				public string Description
				{
					get
					{
						return this.description;
					}
				}

				public Fx.Tag.Location Location
				{
					get
					{
						return this.location;
					}
				}

				public ExternalResourceAttribute(Fx.Tag.Location location, string description)
				{
					this.location = location;
					this.description = description;
				}
			}

			[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class GuaranteeNonBlockingAttribute : Attribute
			{
				public GuaranteeNonBlockingAttribute()
				{
				}
			}

			[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class InheritThrowsAttribute : Attribute
			{
				public string From
				{
					get;
					set;
				}

				public Type FromDeclaringType
				{
					get;
					set;
				}

				public InheritThrowsAttribute()
				{
				}
			}

			public enum Location
			{
				InProcess,
				OutOfProcess,
				LocalSystem,
				LocalOrRemoteSystem,
				RemoteSystem
			}

			[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class NonThrowingAttribute : Attribute
			{
				public NonThrowingAttribute()
				{
				}
			}

			[AttributeUsage(AttributeTargets.Field)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class QueueAttribute : Attribute
			{
				private readonly Type elementType;

				public Type ElementType
				{
					get
					{
						return this.elementType;
					}
				}

				public bool EnqueueThrowsIfFull
				{
					get;
					set;
				}

				public string Scope
				{
					get;
					set;
				}

				public string SizeLimit
				{
					get;
					set;
				}

				public bool StaleElementsRemovedImmediately
				{
					get;
					set;
				}

				public QueueAttribute(Type elementType)
				{
					this.Scope = "instance of declaring class";
					this.SizeLimit = "unbounded";
					if (elementType == null)
					{
						throw Fx.Exception.ArgumentNull("elementType");
					}
					this.elementType = elementType;
				}
			}

			[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple=false, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class SecurityNoteAttribute : Attribute
			{
				public string Critical
				{
					get;
					set;
				}

				public string Miscellaneous
				{
					get;
					set;
				}

				public string Safe
				{
					get;
					set;
				}

				public SecurityNoteAttribute()
				{
				}
			}

			public static class Strings
			{
				internal const string ExternallyManaged = "externally managed";

				internal const string AppDomain = "AppDomain";

				internal const string DeclaringInstance = "instance of declaring class";

				internal const string Unbounded = "unbounded";

				internal const string Infinite = "infinite";
			}

			public enum SynchronizationKind
			{
				LockStatement,
				MonitorWait,
				MonitorExplicit,
				InterlockedNoSpin,
				InterlockedWithSpin,
				FromFieldType
			}

			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class SynchronizationObjectAttribute : Attribute
			{
				public bool Blocking
				{
					get;
					set;
				}

				public Fx.Tag.SynchronizationKind Kind
				{
					get;
					set;
				}

				public string Scope
				{
					get;
					set;
				}

				public SynchronizationObjectAttribute()
				{
					this.Blocking = true;
					this.Scope = "instance of declaring class";
					this.Kind = Fx.Tag.SynchronizationKind.FromFieldType;
				}
			}

			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited=true)]
			[Conditional("CODE_ANALYSIS")]
			public sealed class SynchronizationPrimitiveAttribute : Attribute
			{
				private readonly Fx.Tag.BlocksUsing blocksUsing;

				public Fx.Tag.BlocksUsing BlocksUsing
				{
					get
					{
						return this.blocksUsing;
					}
				}

				public string ReleaseMethod
				{
					get;
					set;
				}

				public bool Spins
				{
					get;
					set;
				}

				public bool SupportsAsync
				{
					get;
					set;
				}

				public SynchronizationPrimitiveAttribute(Fx.Tag.BlocksUsing blocksUsing)
				{
					this.blocksUsing = blocksUsing;
				}
			}

			[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple=true, Inherited=false)]
			[Conditional("CODE_ANALYSIS")]
			public class ThrowsAttribute : Attribute
			{
				private readonly Type exceptionType;

				private readonly string diagnosis;

				public string Diagnosis
				{
					get
					{
						return this.diagnosis;
					}
				}

				public Type ExceptionType
				{
					get
					{
						return this.exceptionType;
					}
				}

				public ThrowsAttribute(Type exceptionType, string diagnosis)
				{
					if (exceptionType == null)
					{
						throw Fx.Exception.ArgumentNull("exceptionType");
					}
					if (string.IsNullOrEmpty(diagnosis))
					{
						throw Fx.Exception.ArgumentNullOrEmpty("diagnosis");
					}
					this.exceptionType = exceptionType;
					this.diagnosis = diagnosis;
				}
			}
		}

		private sealed class TransactionEventHandlerThunk
		{
			private readonly TransactionCompletedEventHandler callback;

			public TransactionCompletedEventHandler ThunkFrame
			{
				get
				{
					return new TransactionCompletedEventHandler(this.UnhandledExceptionFrame);
				}
			}

			public TransactionEventHandlerThunk(TransactionCompletedEventHandler callback)
			{
				this.callback = callback;
			}

			private void UnhandledExceptionFrame(object sender, TransactionEventArgs args)
			{
				RuntimeHelpers.PrepareConstrainedRegions();
				try
				{
					this.callback(sender, args);
				}
				catch (System.Exception exception)
				{
					throw Fx.AssertAndFailFastService(exception.ToString());
				}
			}
		}
	}
}