using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="TrueFilter", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class TrueFilter : SqlFilter
	{
		internal readonly static TrueFilter Default;

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		static TrueFilter()
		{
			TrueFilter.Default = new TrueFilter();
		}

		public TrueFilter() : base("1=1")
		{
		}

		public override bool Match(BrokeredMessage message)
		{
			return true;
		}

		public override Filter Preprocess()
		{
			throw new InvalidOperationException();
		}

		public override string ToString()
		{
			return "TrueFilter";
		}

		public override void Validate()
		{
		}
	}
}