using System;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract]
	public enum TileType
	{
		[EnumMember]
		TileSquareImage,
		[EnumMember]
		TileWideImage,
		[EnumMember]
		TileWideImageAndText,
		[EnumMember]
		TileWideImageCollection,
		[EnumMember]
		TileWideText01,
		[EnumMember]
		TileWideText02,
		[EnumMember]
		TileWideText03,
		[EnumMember]
		TileWideText04,
		[EnumMember]
		TileWideText05,
		[EnumMember]
		TileWideText06,
		[EnumMember]
		TileWideText07,
		[EnumMember]
		TileWideText08,
		[EnumMember]
		TileWideText09,
		[EnumMember]
		TileWideSmallImageAndText01,
		[EnumMember]
		TileWideSmallImageAndText02,
		[EnumMember]
		TileWideSmallImageAndText03,
		[EnumMember]
		TileWideSmallImageAndText04,
		[EnumMember]
		TileWidePeekImageAndText,
		[EnumMember]
		TileWidePeekImage01,
		[EnumMember]
		TileWidePeekImage02,
		[EnumMember]
		TileWidePeekImage03,
		[EnumMember]
		TileWidePeekImage04,
		[EnumMember]
		TileWidePeekImage05,
		[EnumMember]
		TileWidePeekImage06,
		[EnumMember]
		TileWidePeekImageCollection01,
		[EnumMember]
		TileWidePeekImageCollection02,
		[EnumMember]
		TileWidePeekImageCollection03,
		[EnumMember]
		TileWidePeekImageCollection04,
		[EnumMember]
		TileWidePeekImageCollection05,
		[EnumMember]
		TileWidePeekImageCollection06,
		[EnumMember]
		TileWideBlockAndText01
	}
}