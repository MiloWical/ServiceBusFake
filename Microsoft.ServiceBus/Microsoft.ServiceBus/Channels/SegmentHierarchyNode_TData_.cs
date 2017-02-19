using System;
using System.Collections.Generic;

namespace Microsoft.ServiceBus.Channels
{
	internal class SegmentHierarchyNode<TData>
	where TData : class
	{
		private BaseUriWithWildcard path;

		private TData data;

		private string name;

		private Dictionary<string, SegmentHierarchyNode<TData>> children;

		private WeakReference weakData;

		private bool useWeakReferences;

		public TData Data
		{
			get
			{
				if (!this.useWeakReferences)
				{
					return this.data;
				}
				if (this.weakData == null)
				{
					return default(TData);
				}
				return (TData)(this.weakData.Target as TData);
			}
		}

		public SegmentHierarchyNode(string name, bool useWeakReferences)
		{
			this.name = name;
			this.useWeakReferences = useWeakReferences;
			this.children = new Dictionary<string, SegmentHierarchyNode<TData>>(StringComparer.OrdinalIgnoreCase);
		}

		public void Collect(List<KeyValuePair<BaseUriWithWildcard, TData>> result)
		{
			TData data = this.Data;
			if (data != null)
			{
				result.Add(new KeyValuePair<BaseUriWithWildcard, TData>(this.path, data));
			}
			foreach (SegmentHierarchyNode<TData> value in this.children.Values)
			{
				value.Collect(result);
			}
		}

		public void RemoveData()
		{
			this.SetData(default(TData), null);
		}

		public bool RemovePath(string[] path, int seg)
		{
			SegmentHierarchyNode<TData> segmentHierarchyNode;
			if (seg == (int)path.Length)
			{
				this.RemoveData();
				return this.children.Count == 0;
			}
			if (!this.TryGetChild(path[seg], out segmentHierarchyNode))
			{
				if (this.children.Count != 0)
				{
					return false;
				}
				return this.Data == null;
			}
			if (!segmentHierarchyNode.RemovePath(path, seg + 1))
			{
				return false;
			}
			this.children.Remove(path[seg]);
			if (this.children.Count != 0)
			{
				return false;
			}
			return this.Data == null;
		}

		public void SetChildNode(string name, SegmentHierarchyNode<TData> node)
		{
			this.children[name] = node;
		}

		public void SetData(TData data, BaseUriWithWildcard path)
		{
			this.path = path;
			if (!this.useWeakReferences)
			{
				this.data = data;
				return;
			}
			if (data == null)
			{
				this.weakData = null;
				return;
			}
			this.weakData = new WeakReference((object)data);
		}

		public bool TryGetChild(string segment, out SegmentHierarchyNode<TData> value)
		{
			return this.children.TryGetValue(segment, out value);
		}
	}
}