using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Notifications
{
	internal class PushOutcomeCollection
	{
		public Dictionary<string, Dictionary<string, long>> Outcomes
		{
			get;
			internal set;
		}

		public PushOutcomeCollection()
		{
			this.Outcomes = new Dictionary<string, Dictionary<string, long>>(StringComparer.OrdinalIgnoreCase);
		}

		internal void Add(string platform, IEnumerable<string> pushOutcomes)
		{
			IEnumerable<IGrouping<string, string>> groupings = 
				from pushOutcome in pushOutcomes
				group pushOutcome by pushOutcome into pushOutcomeGroup
				select pushOutcomeGroup;
			Dictionary<string, long> dictionary = groupings.ToDictionary<IGrouping<string, string>, string, long>((IGrouping<string, string> s) => s.Key, (IGrouping<string, string> s) => s.LongCount<string>());
			this.Add(platform, dictionary);
		}

		internal void Add(string platform, Dictionary<string, long> outcomeStat)
		{
			if (!this.Outcomes.ContainsKey(platform))
			{
				this.Outcomes[platform] = outcomeStat;
			}
			else
			{
				Dictionary<string, long> item = this.Outcomes[platform];
				foreach (KeyValuePair<string, long> keyValuePair in outcomeStat)
				{
					if (!item.ContainsKey(keyValuePair.Key))
					{
						item[keyValuePair.Key] = keyValuePair.Value;
					}
					else
					{
						item[keyValuePair.Key] = item[keyValuePair.Key] + keyValuePair.Value;
					}
				}
			}
		}

		internal static PushOutcomeCollection Aggregate(IEnumerable<PushOutcomeCollection> outcomeCollection)
		{
			Dictionary<string, long> strs;
			PushOutcomeCollection pushOutcomeCollection = new PushOutcomeCollection();
			foreach (PushOutcomeCollection pushOutcomeCollection1 in outcomeCollection)
			{
				foreach (KeyValuePair<string, Dictionary<string, long>> outcome in pushOutcomeCollection1.Outcomes)
				{
					if (outcome.Key.Equals("AllPNS", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (!pushOutcomeCollection.Outcomes.Keys.Contains<string>(outcome.Key))
					{
						strs = new Dictionary<string, long>();
						pushOutcomeCollection.Outcomes.Add(outcome.Key, strs);
					}
					else
					{
						strs = pushOutcomeCollection.Outcomes[outcome.Key];
					}
					foreach (KeyValuePair<string, long> value in outcome.Value)
					{
						if (!strs.Keys.Contains<string>(value.Key))
						{
							strs[value.Key] = value.Value;
						}
						else
						{
							strs[value.Key] = strs[value.Key] + value.Value;
						}
					}
				}
			}
			return pushOutcomeCollection;
		}

		internal bool IsEmpty()
		{
			return this.Outcomes.Count <= 0;
		}

		internal static PushOutcomeCollection Rollup(IEnumerable<PushOutcomeCollection> outcomeCollection)
		{
			Dictionary<string, long> strs;
			PushOutcomeCollection pushOutcomeCollection = new PushOutcomeCollection();
			Dictionary<string, long> value = new Dictionary<string, long>();
			foreach (PushOutcomeCollection pushOutcomeCollection1 in outcomeCollection)
			{
				foreach (KeyValuePair<string, Dictionary<string, long>> outcome in pushOutcomeCollection1.Outcomes)
				{
					if (outcome.Key.Equals("AllPNS", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (!pushOutcomeCollection.Outcomes.Keys.Contains<string>(outcome.Key))
					{
						strs = new Dictionary<string, long>();
						pushOutcomeCollection.Outcomes.Add(outcome.Key, strs);
					}
					else
					{
						strs = pushOutcomeCollection.Outcomes[outcome.Key];
					}
					foreach (KeyValuePair<string, long> keyValuePair in outcome.Value)
					{
						if (!strs.Keys.Contains<string>(keyValuePair.Key))
						{
							strs[keyValuePair.Key] = keyValuePair.Value;
						}
						else
						{
							strs[keyValuePair.Key] = strs[keyValuePair.Key] + keyValuePair.Value;
						}
						if (!value.Keys.Contains<string>(keyValuePair.Key))
						{
							value[keyValuePair.Key] = keyValuePair.Value;
						}
						else
						{
							value[keyValuePair.Key] = value[keyValuePair.Key] + keyValuePair.Value;
						}
					}
				}
			}
			pushOutcomeCollection.Outcomes["AllPNS"] = value;
			return pushOutcomeCollection;
		}
	}
}