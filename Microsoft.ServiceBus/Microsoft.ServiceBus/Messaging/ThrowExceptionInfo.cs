using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Namespace="http://schemas.microsoft.com/netservices/2011/06/servicebus")]
	internal sealed class ThrowExceptionInfo : FaultInjectionInfo
	{
		[DataMember]
		public string ExceptionMessage
		{
			get;
			set;
		}

		[DataMember]
		public string ExceptionType
		{
			get;
			set;
		}

		[IgnoreDataMember]
		public Type ExceptionTypeClass
		{
			get;
			set;
		}

		[DataMember]
		public int FireOnceInEveryXInvocation
		{
			get;
			set;
		}

		public ThrowExceptionInfo()
		{
		}

		public ThrowExceptionInfo(FaultInjectionTarget target, Type exceptionType, string exceptionMessage) : this()
		{
			base.Target = target;
			this.ExceptionType = exceptionType.FullName;
			this.ExceptionMessage = exceptionMessage;
			this.ExceptionTypeClass = exceptionType;
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] target = new object[] { base.Target, this.ExceptionType, this.ExceptionMessage, base.FireOnce, this.FireOnceInEveryXInvocation, base.ValidAfter, base.ValidBefore };
			return string.Format(invariantCulture, "FaultType: ThrowExceptionInfo, Target: {0}, ExceptionType: {1}, ExceptionMessage: {2}, FireOnce: {3}  FireOnceInEveryXInvocation: {4}, ValidAfter: {5}, ValidBefore: {6}", target);
		}
	}
}