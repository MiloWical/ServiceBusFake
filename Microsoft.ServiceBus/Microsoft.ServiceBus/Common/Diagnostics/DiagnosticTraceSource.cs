using System;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Microsoft.ServiceBus.Common.Diagnostics
{
	internal class DiagnosticTraceSource : TraceSource
	{
		private const string PropagateActivityValue = "propagateActivity";

		internal bool PropagateActivity
		{
			get
			{
				bool flag = false;
				string item = base.Attributes["propagateActivity"];
				if (!string.IsNullOrEmpty(item) && !bool.TryParse(item, out flag))
				{
					flag = false;
				}
				return flag;
			}
			set
			{
				base.Attributes["propagateActivity"] = value.ToString();
			}
		}

		internal DiagnosticTraceSource(string name) : base(name)
		{
		}

		protected override string[] GetSupportedAttributes()
		{
			return new string[] { "propagateActivity" };
		}
	}
}