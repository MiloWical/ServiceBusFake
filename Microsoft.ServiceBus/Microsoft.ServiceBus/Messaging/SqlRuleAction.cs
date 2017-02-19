using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Filters;
using Microsoft.ServiceBus.Messaging.Filters.ActionLanguage;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Microsoft.ServiceBus.Messaging
{
	[DataContract(Name="SqlRuleAction", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	public sealed class SqlRuleAction : RuleAction
	{
		private readonly static MemoryCache compiledCache;

		private readonly static CacheItemPolicy defaultCachePolicy;

		[DataMember(Name="Parameters", Order=131073, EmitDefaultValue=false, IsRequired=false)]
		private PropertyDictionary parameters;

		[DataMember(Name="CompatibilityLevel", Order=65538, EmitDefaultValue=false, IsRequired=false)]
		public int CompatibilityLevel
		{
			get;
			private set;
		}

		public IDictionary<string, object> Parameters
		{
			get
			{
				PropertyDictionary propertyDictionaries = this.parameters;
				if (propertyDictionaries == null)
				{
					PropertyDictionary propertyDictionaries1 = new PropertyDictionary();
					PropertyDictionary propertyDictionaries2 = propertyDictionaries1;
					this.parameters = propertyDictionaries1;
					propertyDictionaries = propertyDictionaries2;
				}
				return propertyDictionaries;
			}
		}

		public override bool RequiresPreprocessing
		{
			get
			{
				return true;
			}
		}

		[DataMember(Name="SqlExpression", Order=65537, EmitDefaultValue=false, IsRequired=true)]
		public string SqlExpression
		{
			get;
			private set;
		}

		static SqlRuleAction()
		{
			string name = typeof(SqlRuleAction).Name;
			NameValueCollection nameValueCollection = new NameValueCollection()
			{
				{ "CacheMemoryLimitMegabytes", "48" }
			};
			SqlRuleAction.compiledCache = new MemoryCache(name, nameValueCollection);
			CacheItemPolicy cacheItemPolicy = new CacheItemPolicy()
			{
				SlidingExpiration = TimeSpan.FromHours(1)
			};
			SqlRuleAction.defaultCachePolicy = cacheItemPolicy;
		}

		public SqlRuleAction(string sqlExpression) : this(sqlExpression, 20)
		{
		}

		public SqlRuleAction(string sqlExpression, int compatibilityLevel)
		{
			if (string.IsNullOrEmpty(sqlExpression))
			{
				throw Fx.Exception.ArgumentNullOrEmpty("sqlExpression");
			}
			if (sqlExpression.Length > 1024)
			{
				throw Fx.Exception.Argument("sqlExpression", SRClient.SqlFilterActionStatmentTooLong(sqlExpression.Length, 1024));
			}
			this.SqlExpression = sqlExpression;
			this.CompatibilityLevel = compatibilityLevel;
		}

		private void EnsureCompatibilityLevel()
		{
			if (this.CompatibilityLevel != 20)
			{
				throw new RuleActionException(SRClient.NotSupportedCompatibilityLevel(this.CompatibilityLevel));
			}
		}

		public override BrokeredMessage Execute(BrokeredMessage message)
		{
			throw new InvalidOperationException(SRClient.ActionMustBeProcessed);
		}

		internal override BrokeredMessage Execute(BrokeredMessage message, RuleExecutionContext context)
		{
			return this.Execute(message);
		}

		internal override bool IsValidForVersion(ApiVersion version)
		{
			if (!base.IsValidForVersion(version))
			{
				return false;
			}
			if (version < ApiVersion.Two && this.parameters != null)
			{
				return false;
			}
			return true;
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (this.CompatibilityLevel == 0)
			{
				this.CompatibilityLevel = 20;
			}
		}

		public override RuleAction Preprocess()
		{
			RuleAction delegateRuleAction;
			this.EnsureCompatibilityLevel();
			try
			{
				SqlRuleAction.CompiledItem compiledItem = (SqlRuleAction.CompiledItem)SqlRuleAction.compiledCache.Get(this.SqlExpression, null);
				if (compiledItem == null)
				{
					SqlActionParserOutput sqlActionParserOutput = SqlActionParser.Parse(this.SqlExpression);
					Action<BrokeredMessage, IDictionary<string, object>> action = sqlActionParserOutput.ExpressionTree.Compile();
					compiledItem = new SqlRuleAction.CompiledItem(action, sqlActionParserOutput.RequiredParameters);
					SqlRuleAction.CompiledItem compiledItem1 = (SqlRuleAction.CompiledItem)SqlRuleAction.compiledCache.AddOrGetExisting(this.SqlExpression, compiledItem, SqlRuleAction.defaultCachePolicy, null);
					if (compiledItem1 != null)
					{
						compiledItem = compiledItem1;
					}
				}
				ParametersValidator.Validate(false, compiledItem.RequiredParameters, this.Parameters);
				delegateRuleAction = new DelegateRuleAction(compiledItem.Delegate, this.Parameters);
			}
			catch (RuleActionException ruleActionException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new RuleActionException(exception.Message, exception);
				}
				throw;
			}
			return delegateRuleAction;
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] sqlExpression = new object[] { this.SqlExpression };
			return string.Format(invariantCulture, "SqlRuleAction: {0}", sqlExpression);
		}

		internal override void UpdateForVersion(ApiVersion version, RuleAction existingAction = null)
		{
			PropertyDictionary propertyDictionaries;
			SqlRuleAction sqlRuleAction = existingAction as SqlRuleAction;
			base.UpdateForVersion(version, existingAction);
			if (version < ApiVersion.Two)
			{
				if (sqlRuleAction == null)
				{
					propertyDictionaries = null;
				}
				else
				{
					propertyDictionaries = sqlRuleAction.parameters;
				}
				this.parameters = propertyDictionaries;
			}
		}

		public override void Validate()
		{
			if (string.IsNullOrEmpty(this.SqlExpression))
			{
				throw new RuleActionException(SRClient.PropertyIsNullOrEmpty("SqlExpression"));
			}
			if (this.SqlExpression.Length > 1024)
			{
				throw new RuleActionException(SRClient.SqlFilterActionStatmentTooLong(this.SqlExpression.Length, 1024));
			}
			RuleAction ruleAction = this;
			while (ruleAction.RequiresPreprocessing)
			{
				ruleAction = ruleAction.Preprocess();
			}
			try
			{
				ruleAction.Validate();
			}
			catch (RuleActionException ruleActionException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new RuleActionException(exception.Message, exception);
				}
				throw;
			}
		}

		private sealed class CompiledItem
		{
			public Action<BrokeredMessage, IDictionary<string, object>> Delegate
			{
				get;
				private set;
			}

			public IEnumerable<string> RequiredParameters
			{
				get;
				private set;
			}

			public CompiledItem(Action<BrokeredMessage, IDictionary<string, object>> compiledDelegate, IEnumerable<string> requiredParameters)
			{
				this.Delegate = compiledDelegate;
				this.RequiredParameters = requiredParameters;
			}
		}
	}
}