using Microsoft.ServiceBus.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.PerformanceData;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.PerformanceCounters
{
	internal abstract class PerformanceCounter
	{
		public abstract int CounterEnd
		{
			get;
		}

		public abstract List<string> CounterNames
		{
			get;
		}

		protected abstract CounterData[] Counters
		{
			get;
		}

		protected abstract System.Diagnostics.PerformanceData.CounterSetInstance CounterSetInstance
		{
			get;
		}

		public abstract int CounterStart
		{
			get;
		}

		public bool Initialized
		{
			get;
			private set;
		}

		public abstract string InstanceName
		{
			get;
		}

		public ClientPerformanceCounterScope Scope
		{
			get;
			protected set;
		}

		protected PerformanceCounter()
		{
		}

		public Hashtable CollectCurrentValueSet()
		{
			if (!this.Initialized)
			{
				throw new ObjectDisposedException(this.GetType().Name);
			}
			CounterSetInstanceCounterDataSet counters = this.CounterSetInstance.Counters;
			Hashtable hashtables = new Hashtable();
			foreach (string counterName in this.CounterNames)
			{
				hashtables.Add(counterName, counters[counterName].RawValue);
			}
			return hashtables;
		}

		protected void Initialize(ClientPerformanceCounterLevel compareLevel)
		{
			try
			{
				if (this.Scope.Level == compareLevel || this.Scope.Level == ClientPerformanceCounterLevel.All)
				{
					this.OnInitialize();
					this.Initialized = (this.CounterSetInstance == null ? false : this.Counters != null);
					MessagingClientEtwProvider.TraceClient(() => {
					});
				}
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!PerformanceCounter.IsCounterSetException(exception))
				{
					throw;
				}
				MessagingClientEtwProvider.TraceClient(() => MessagingClientEtwProvider.Provider.EventWritePerformanceCounterCreationFailed(exception.ToString()));
			}
		}

		private static bool IsCounterSetException(Exception e)
		{
			Type type = e.GetType();
			if (!(type == typeof(ArgumentException)) && !(type == typeof(InsufficientMemoryException)) && !(type == typeof(Win32Exception)) && !(type == typeof(PlatformNotSupportedException)))
			{
				return false;
			}
			return true;
		}

		protected virtual bool IsMinimalCountersEnabled()
		{
			return this.Initialized;
		}

		protected virtual bool IsVerboseCountersEnabled()
		{
			if (this.Scope.Detail != ClientPerformanceCounterDetail.Verbose)
			{
				return false;
			}
			return this.Initialized;
		}

		protected abstract void OnInitialize();

		internal static void ThrowInvalidOperationException()
		{
			throw new InvalidOperationException("Performance counter is already initialized.");
		}
	}
}