using System;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Microsoft.ServiceBus.Diagnostics
{
	internal class DiagnosticTraceSource : PiiTraceSource
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

		internal DiagnosticTraceSource(string name, string eventSourceName) : base(name, eventSourceName)
		{
		}

		internal DiagnosticTraceSource(string name, string eventSourceName, SourceLevels level) : base(name, eventSourceName, level)
		{
		}

		protected override string[] GetSupportedAttributes()
		{
			string[] supportedAttributes = base.GetSupportedAttributes();
			string[] strArrays = new string[(int)supportedAttributes.Length + 1];
			for (int i = 0; i < (int)supportedAttributes.Length; i++)
			{
				strArrays[i] = supportedAttributes[i];
			}
			strArrays[(int)supportedAttributes.Length] = "propagateActivity";
			return strArrays;
		}
	}
}