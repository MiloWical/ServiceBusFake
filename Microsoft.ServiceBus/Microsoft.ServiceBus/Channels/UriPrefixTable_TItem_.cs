using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Diagnostics;
using Microsoft.ServiceBus.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.ServiceModel;

namespace Microsoft.ServiceBus.Channels
{
	internal sealed class UriPrefixTable<TItem>
	where TItem : class
	{
		private const int HopperSize = 128;

		private int count;

		private HopperCache lookupCache;

		private SegmentHierarchyNode<TItem> root;

		private bool useWeakReferences;

		private bool includePortInComparison;

		public int Count
		{
			get
			{
				return this.count;
			}
		}

		private object ThisLock
		{
			get
			{
				return this;
			}
		}

		public UriPrefixTable() : this(false)
		{
		}

		public UriPrefixTable(bool includePortInComparison) : this(includePortInComparison, false)
		{
		}

		public UriPrefixTable(bool includePortInComparison, bool useWeakReferences)
		{
			this.includePortInComparison = includePortInComparison;
			this.useWeakReferences = useWeakReferences;
			this.root = new SegmentHierarchyNode<TItem>(null, useWeakReferences);
			this.lookupCache = new HopperCache(128, useWeakReferences);
		}

		internal UriPrefixTable(UriPrefixTable<TItem> objectToClone) : this(objectToClone.includePortInComparison, objectToClone.useWeakReferences)
		{
			if (objectToClone.Count > 0)
			{
				foreach (KeyValuePair<BaseUriWithWildcard, TItem> all in objectToClone.GetAll())
				{
					this.RegisterUri(all.Key.BaseAddress, all.Key.HostNameComparisonMode, all.Value);
				}
			}
		}

		private void AddToCache(BaseUriWithWildcard key, TItem item)
		{
			object value;
			HopperCache hopperCache = this.lookupCache;
			BaseUriWithWildcard baseUriWithWildcard = key;
			TItem tItem = item;
			if (tItem != null)
			{
				value = tItem;
			}
			else
			{
				value = DBNull.Value;
			}
			hopperCache.Add(baseUriWithWildcard, value);
		}

		private void ClearCache()
		{
			this.lookupCache = new HopperCache(128, this.useWeakReferences);
		}

		private SegmentHierarchyNode<TItem> FindDataNode(string[] path, out bool exactMatch)
		{
			SegmentHierarchyNode<TItem> segmentHierarchyNode;
			exactMatch = false;
			SegmentHierarchyNode<TItem> segmentHierarchyNode1 = this.root;
			SegmentHierarchyNode<TItem> segmentHierarchyNode2 = null;
			for (int i = 0; i < (int)path.Length && segmentHierarchyNode1.TryGetChild(path[i], out segmentHierarchyNode); i++)
			{
				if (segmentHierarchyNode.Data != null)
				{
					segmentHierarchyNode2 = segmentHierarchyNode;
					exactMatch = i == (int)path.Length - 1;
				}
				segmentHierarchyNode1 = segmentHierarchyNode;
			}
			return segmentHierarchyNode2;
		}

		private SegmentHierarchyNode<TItem> FindOrCreateNode(BaseUriWithWildcard baseUri)
		{
			SegmentHierarchyNode<TItem> segmentHierarchyNode;
			string[] path = UriPrefixTable<TItem>.UriSegmenter.ToPath(baseUri.BaseAddress, baseUri.HostNameComparisonMode, this.includePortInComparison);
			SegmentHierarchyNode<TItem> segmentHierarchyNode1 = this.root;
			for (int i = 0; i < (int)path.Length; i++)
			{
				if (!segmentHierarchyNode1.TryGetChild(path[i], out segmentHierarchyNode))
				{
					segmentHierarchyNode = new SegmentHierarchyNode<TItem>(path[i], this.useWeakReferences);
					segmentHierarchyNode1.SetChildNode(path[i], segmentHierarchyNode);
				}
				segmentHierarchyNode1 = segmentHierarchyNode;
			}
			return segmentHierarchyNode1;
		}

		public IEnumerable<KeyValuePair<BaseUriWithWildcard, TItem>> GetAll()
		{
			IEnumerable<KeyValuePair<BaseUriWithWildcard, TItem>> keyValuePairs;
			lock (this.ThisLock)
			{
				List<KeyValuePair<BaseUriWithWildcard, TItem>> keyValuePairs1 = new List<KeyValuePair<BaseUriWithWildcard, TItem>>();
				this.root.Collect(keyValuePairs1);
				keyValuePairs = keyValuePairs1;
			}
			return keyValuePairs;
		}

		public bool IsRegistered(BaseUriWithWildcard key)
		{
			bool flag;
			SegmentHierarchyNode<TItem> segmentHierarchyNode;
			Uri baseAddress = key.BaseAddress;
			string[] path = UriPrefixTable<TItem>.UriSegmenter.ToPath(baseAddress, key.HostNameComparisonMode, this.includePortInComparison);
			lock (this.ThisLock)
			{
				segmentHierarchyNode = this.FindDataNode(path, out flag);
			}
			if (!flag || segmentHierarchyNode == null)
			{
				return false;
			}
			return segmentHierarchyNode.Data != null;
		}

		public void RegisterUri(Uri uri, HostNameComparisonMode hostNameComparisonMode, TItem item)
		{
			lock (this.ThisLock)
			{
				this.ClearCache();
				BaseUriWithWildcard baseUriWithWildcard = new BaseUriWithWildcard(uri, hostNameComparisonMode);
				SegmentHierarchyNode<TItem> segmentHierarchyNode = this.FindOrCreateNode(baseUriWithWildcard);
				if (segmentHierarchyNode.Data != null)
				{
					ExceptionUtility exceptionUtility = Microsoft.ServiceBus.Diagnostics.DiagnosticUtility.ExceptionUtility;
					string duplicateRegistration = Resources.DuplicateRegistration;
					object[] objArray = new object[] { uri };
					throw exceptionUtility.ThrowHelperError(new InvalidOperationException(Microsoft.ServiceBus.SR.GetString(duplicateRegistration, objArray)));
				}
				segmentHierarchyNode.SetData(item, baseUriWithWildcard);
				UriPrefixTable<TItem> uriPrefixTable = this;
				uriPrefixTable.count = uriPrefixTable.count + 1;
			}
		}

		private bool TryCacheLookup(BaseUriWithWildcard key, out TItem item)
		{
			object value = this.lookupCache.GetValue(this.ThisLock, key);
			item = (value == DBNull.Value ? default(TItem) : (TItem)value);
			return value != null;
		}

		public bool TryLookupUri(Uri uri, HostNameComparisonMode hostNameComparisonMode, out TItem item)
		{
			bool flag;
			bool flag1;
			BaseUriWithWildcard baseUriWithWildcard = new BaseUriWithWildcard(uri, hostNameComparisonMode);
			if (this.TryCacheLookup(baseUriWithWildcard, out item))
			{
				return item != null;
			}
			lock (this.ThisLock)
			{
				SegmentHierarchyNode<TItem> segmentHierarchyNode = this.FindDataNode(UriPrefixTable<TItem>.UriSegmenter.ToPath(baseUriWithWildcard.BaseAddress, hostNameComparisonMode, this.includePortInComparison), out flag);
				if (segmentHierarchyNode != null)
				{
					item = segmentHierarchyNode.Data;
				}
				this.AddToCache(baseUriWithWildcard, item);
				flag1 = item != null;
			}
			return flag1;
		}

		public void UnregisterUri(Uri uri, HostNameComparisonMode hostNameComparisonMode)
		{
			lock (this.ThisLock)
			{
				this.ClearCache();
				string[] path = UriPrefixTable<TItem>.UriSegmenter.ToPath(uri, hostNameComparisonMode, this.includePortInComparison);
				if ((int)path.Length != 0)
				{
					this.root.RemovePath(path, 0);
				}
				else
				{
					this.root.RemoveData();
				}
				UriPrefixTable<TItem> uriPrefixTable = this;
				uriPrefixTable.count = uriPrefixTable.count - 1;
			}
		}

		private static class UriSegmenter
		{
			internal static string[] ToPath(Uri uriPath, HostNameComparisonMode hostNameComparisonMode, bool includePortInComparison)
			{
				if (null == uriPath)
				{
					return new string[0];
				}
				return (new UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum(uriPath)).GetSegments(hostNameComparisonMode, includePortInComparison);
			}

			private struct UriSegmentEnum
			{
				private string segment;

				private int segmentStartAt;

				private int segmentLength;

				private UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType type;

				private Uri uri;

				internal UriSegmentEnum(Uri uri)
				{
					this.uri = uri;
					this.type = UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Unknown;
					this.segment = null;
					this.segmentStartAt = 0;
					this.segmentLength = 0;
				}

				private void ClearSegment()
				{
					this.type = UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.None;
					this.segment = string.Empty;
					this.segmentStartAt = 0;
					this.segmentLength = 0;
				}

				public string[] GetSegments(HostNameComparisonMode hostNameComparisonMode, bool includePortInComparison)
				{
					List<string> strs = new List<string>();
					while (this.Next())
					{
						switch (this.type)
						{
							case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Host:
							{
								if (hostNameComparisonMode == HostNameComparisonMode.StrongWildcard)
								{
									strs.Add("+");
									continue;
								}
								else if (hostNameComparisonMode != HostNameComparisonMode.Exact)
								{
									strs.Add("*");
									continue;
								}
								else
								{
									strs.Add(this.segment);
									continue;
								}
							}
							case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Port:
							{
								if (!includePortInComparison && hostNameComparisonMode != HostNameComparisonMode.Exact)
								{
									continue;
								}
								strs.Add(this.segment);
								continue;
							}
							case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Path:
							{
								strs.Add(this.segment.Substring(this.segmentStartAt, this.segmentLength));
								continue;
							}
						}
						strs.Add(this.segment);
					}
					return strs.ToArray();
				}

				public bool Next()
				{
					switch (this.type)
					{
						case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Unknown:
						{
							this.type = UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Scheme;
							this.SetSegment(this.uri.Scheme);
							return true;
						}
						case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Scheme:
						{
							this.type = UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Host;
							string host = this.uri.Host;
							string userInfo = this.uri.UserInfo;
							if (userInfo != null && userInfo.Length > 0)
							{
								host = string.Concat(userInfo, '@', host);
							}
							this.SetSegment(host);
							return true;
						}
						case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Host:
						{
							this.type = UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Port;
							int port = this.uri.Port;
							this.SetSegment(port.ToString(CultureInfo.InvariantCulture));
							return true;
						}
						case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Port:
						{
							this.type = UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Path;
							string absolutePath = this.uri.AbsolutePath;
							if (absolutePath.Length == 0)
							{
								this.ClearSegment();
								return false;
							}
							this.segment = absolutePath;
							this.segmentStartAt = 0;
							this.segmentLength = 0;
							return this.NextPathSegment();
						}
						case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.Path:
						{
							return this.NextPathSegment();
						}
						case (UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType)UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum.UriSegmentType.None:
						{
							return false;
						}
					}
					return false;
				}

				public bool NextPathSegment()
				{
					UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum uriSegmentEnum = this;
					uriSegmentEnum.segmentStartAt = uriSegmentEnum.segmentStartAt + this.segmentLength;
					while (this.segmentStartAt < this.segment.Length && this.segment[this.segmentStartAt] == '/')
					{
						UriPrefixTable<TItem>.UriSegmenter.UriSegmentEnum uriSegmentEnum1 = this;
						uriSegmentEnum1.segmentStartAt = uriSegmentEnum1.segmentStartAt + 1;
					}
					if (this.segmentStartAt >= this.segment.Length)
					{
						this.ClearSegment();
						return false;
					}
					int num = this.segment.IndexOf('/', this.segmentStartAt);
					if (-1 != num)
					{
						this.segmentLength = num - this.segmentStartAt;
					}
					else
					{
						this.segmentLength = this.segment.Length - this.segmentStartAt;
					}
					return true;
				}

				private void SetSegment(string segment)
				{
					this.segment = segment;
					this.segmentStartAt = 0;
					this.segmentLength = segment.Length;
				}

				private enum UriSegmentType
				{
					Unknown,
					Scheme,
					Host,
					Port,
					Path,
					None
				}
			}
		}
	}
}