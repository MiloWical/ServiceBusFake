using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="FalseFilter", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class FalseFilter : SqlFilter
	{
		internal readonly static FalseFilter Default;

		public override bool RequiresPreprocessing
		{
			get
			{
				return false;
			}
		}

		static FalseFilter()
		{
			FalseFilter.Default = new FalseFilter();
		}

		public FalseFilter() : base("1=0")
		{
		}

		public override bool Match(BrokeredMessage message)
		{
			return false;
		}

		public override Filter Preprocess()
		{
			throw new InvalidOperationException();
		}

		public override string ToString()
		{
			return "FalseFilter";
		}

		public override void Validate()
		{
		}
	}
}