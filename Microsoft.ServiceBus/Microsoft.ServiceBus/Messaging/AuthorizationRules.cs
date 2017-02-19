using Microsoft.ServiceBus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[CollectionDataContract(Name="AuthorizationRules", ItemName="AuthorizationRule", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public class AuthorizationRules : ICollection<AuthorizationRule>, IEnumerable<AuthorizationRule>, IEnumerable
	{
		public readonly static DataContractSerializer Serializer;

		public readonly ICollection<AuthorizationRule> innerCollection;

		private readonly IDictionary<string, SharedAccessAuthorizationRule> nameToSharedAccessAuthorizationRuleMap;

		private bool duplicateAddForSharedAccessAuthorizationRule;

		public int Count
		{
			get
			{
				return this.innerCollection.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return this.innerCollection.IsReadOnly;
			}
		}

		public bool RequiresEncryption
		{
			get
			{
				return this.nameToSharedAccessAuthorizationRuleMap.Any<KeyValuePair<string, SharedAccessAuthorizationRule>>();
			}
		}

		static AuthorizationRules()
		{
			AuthorizationRules.Serializer = new DataContractSerializer(typeof(AuthorizationRules));
		}

		public AuthorizationRules()
		{
			this.nameToSharedAccessAuthorizationRuleMap = new Dictionary<string, SharedAccessAuthorizationRule>(StringComparer.OrdinalIgnoreCase);
			this.innerCollection = new List<AuthorizationRule>();
		}

		public AuthorizationRules(IEnumerable<AuthorizationRule> enumerable)
		{
			if (enumerable == null)
			{
				throw new ArgumentNullException("enumerable");
			}
			this.nameToSharedAccessAuthorizationRuleMap = new Dictionary<string, SharedAccessAuthorizationRule>(StringComparer.OrdinalIgnoreCase);
			this.innerCollection = new List<AuthorizationRule>();
			foreach (AuthorizationRule authorizationRule in enumerable)
			{
				this.Add(authorizationRule);
			}
		}

		public void Add(AuthorizationRule item)
		{
			SharedAccessAuthorizationRule sharedAccessAuthorizationRule;
			if (item is SharedAccessAuthorizationRule)
			{
				SharedAccessAuthorizationRule sharedAccessAuthorizationRule1 = item as SharedAccessAuthorizationRule;
				if (this.nameToSharedAccessAuthorizationRuleMap.TryGetValue(sharedAccessAuthorizationRule1.KeyName, out sharedAccessAuthorizationRule))
				{
					this.nameToSharedAccessAuthorizationRuleMap.Remove(sharedAccessAuthorizationRule1.KeyName);
					this.innerCollection.Remove(sharedAccessAuthorizationRule);
					this.duplicateAddForSharedAccessAuthorizationRule = true;
				}
				this.nameToSharedAccessAuthorizationRuleMap.Add(sharedAccessAuthorizationRule1.KeyName, sharedAccessAuthorizationRule1);
			}
			this.innerCollection.Add(item);
		}

		public void Clear()
		{
			this.nameToSharedAccessAuthorizationRuleMap.Clear();
			this.innerCollection.Clear();
		}

		public bool Contains(AuthorizationRule item)
		{
			return this.innerCollection.Contains(item);
		}

		public void CopyTo(AuthorizationRule[] array, int arrayIndex)
		{
			this.innerCollection.CopyTo(array, arrayIndex);
		}

		public IEnumerator<AuthorizationRule> GetEnumerator()
		{
			return this.innerCollection.GetEnumerator();
		}

		public List<AuthorizationRule> GetRules(Predicate<AuthorizationRule> match)
		{
			return ((List<AuthorizationRule>)this.innerCollection).FindAll(match);
		}

		public List<AuthorizationRule> GetRules(string claimValue)
		{
			return ((List<AuthorizationRule>)this.innerCollection).FindAll((AuthorizationRule rule) => string.Equals(claimValue, rule.ClaimValue, StringComparison.OrdinalIgnoreCase));
		}

		public bool HasEqualRuntimeBehavior(AuthorizationRules comparand)
		{
			bool flag;
			if (comparand == null)
			{
				return false;
			}
			AuthorizationRuleEqualityComparer authorizationRuleEqualityComparer = new AuthorizationRuleEqualityComparer();
			HashSet<AuthorizationRule> authorizationRules = new HashSet<AuthorizationRule>(this.innerCollection, authorizationRuleEqualityComparer);
			HashSet<AuthorizationRule> authorizationRules1 = new HashSet<AuthorizationRule>(comparand.innerCollection, authorizationRuleEqualityComparer);
			if (authorizationRules.Count != authorizationRules1.Count)
			{
				return false;
			}
			HashSet<AuthorizationRule>.Enumerator enumerator = authorizationRules.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					if (authorizationRules1.Contains(enumerator.Current))
					{
						continue;
					}
					flag = false;
					return flag;
				}
				return true;
			}
			finally
			{
				((IDisposable)enumerator).Dispose();
			}
			return flag;
		}

		internal bool IsValidForVersion(ApiVersion version)
		{
			if (version < ApiVersion.Three && this.nameToSharedAccessAuthorizationRuleMap.Any<KeyValuePair<string, SharedAccessAuthorizationRule>>())
			{
				return false;
			}
			return true;
		}

		public bool Remove(AuthorizationRule item)
		{
			return this.innerCollection.Remove(item);
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.innerCollection.GetEnumerator();
		}

		public bool TryGetSharedAccessAuthorizationRule(string keyName, out SharedAccessAuthorizationRule rule)
		{
			return this.nameToSharedAccessAuthorizationRuleMap.TryGetValue(keyName, out rule);
		}

		internal void UpdateForVersion(ApiVersion version, AuthorizationRules existingAuthorizationRules = null)
		{
			if (version < ApiVersion.Three)
			{
				foreach (SharedAccessAuthorizationRule value in this.nameToSharedAccessAuthorizationRuleMap.Values)
				{
					this.innerCollection.Remove(value);
				}
				this.nameToSharedAccessAuthorizationRuleMap.Clear();
				if (existingAuthorizationRules != null)
				{
					foreach (SharedAccessAuthorizationRule sharedAccessAuthorizationRule in existingAuthorizationRules.nameToSharedAccessAuthorizationRuleMap.Values)
					{
						this.Add(sharedAccessAuthorizationRule);
					}
				}
			}
		}

		internal void Validate()
		{
			foreach (AuthorizationRule authorizationRule in this.innerCollection)
			{
				authorizationRule.Validate();
			}
			if (this.duplicateAddForSharedAccessAuthorizationRule)
			{
				throw new InvalidDataContractException(SRClient.CannotHaveDuplicateSAARule);
			}
		}
	}
}