using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract]
	public enum ToastType
	{
		[EnumMember]
		ToastImageAndText01,
		[EnumMember]
		ToastImageAndText02,
		[EnumMember]
		ToastImageAndText03,
		[EnumMember]
		ToastImageAndText04,
		[EnumMember]
		ToastSmallImageAndText01,
		[EnumMember]
		ToastSmallImageAndText02,
		[EnumMember]
		ToastSmallImageAndText03,
		[EnumMember]
		ToastSmallImageAndText04,
		[EnumMember]
		ToastText01,
		[EnumMember]
		ToastText02,
		[EnumMember]
		ToastText03,
		[EnumMember]
		ToastText04
	}
}