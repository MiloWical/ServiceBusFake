using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Common;
using Microsoft.ServiceBus.Messaging.Filters;
using Microsoft.ServiceBus.Messaging.Filters.FilterLanguage;
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
	[DataContract(Name="SqlFilter", Namespace="http://schemas.microsoft.com/netservices/2010/10/servicebus/connect")]
	[KnownType(typeof(FalseFilter))]
	[KnownType(typeof(TrueFilter))]
	[KnownType(typeof(DateTimeOffset))]
	public class SqlFilter : Filter
	{
		private readonly static MemoryCache compiledDelegateCache;

		private readonly static CacheItemPolicy defaultCachePolicy;

		[DataMember(Name="Parameters", Order=131073, EmitDefaultValue=false, IsRequired=false)]
		private PropertyDictionary parameters;

		[DataMember(Name="CompatibilityLevel", Order=65538, EmitDefaultValue=false, IsRequired=false)]
		public int CompatibilityLevel
		{
			get;
			private set;
		}

		internal override Microsoft.ServiceBus.Messaging.FilterType FilterType
		{
			get
			{
				return Microsoft.ServiceBus.Messaging.FilterType.SqlFilter;
			}
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

		[DataMember(Name="SqlExpression", Order=65537, EmitDefaultValue=false, IsRequired=false)]
		public string SqlExpression
		{
			get;
			private set;
		}

		static SqlFilter()
		{
			string name = typeof(SqlFilter).Name;
			NameValueCollection nameValueCollection = new NameValueCollection()
			{
				{ "CacheMemoryLimitMegabytes", "48" }
			};
			SqlFilter.compiledDelegateCache = new MemoryCache(name, nameValueCollection);
			CacheItemPolicy cacheItemPolicy = new CacheItemPolicy()
			{
				SlidingExpiration = TimeSpan.FromHours(1)
			};
			SqlFilter.defaultCachePolicy = cacheItemPolicy;
		}

		public SqlFilter(string sqlExpression) : this(sqlExpression, 20)
		{
		}

		private SqlFilter(string sqlExpression, int compatibilityLevel)
		{
			if (string.IsNullOrEmpty(sqlExpression))
			{
				throw Fx.Exception.ArgumentNull("sqlExpression");
			}
			if (sqlExpression.Length > 1024)
			{
				throw Fx.Exception.Argument("sqlExpression", SRClient.SqlFilterStatmentTooLong(sqlExpression.Length, 1024));
			}
			this.SqlExpression = sqlExpression;
			this.CompatibilityLevel = compatibilityLevel;
		}

		private void EnsureCompatibilityLevel()
		{
			if (this.CompatibilityLevel != 20)
			{
				throw new FilterException(SRClient.NotSupportedCompatibilityLevel(this.CompatibilityLevel));
			}
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

		public override bool Match(BrokeredMessage message)
		{
			throw new InvalidOperationException(SRClient.FilterMustBeProcessed);
		}

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			if (this.CompatibilityLevel == 0)
			{
				this.CompatibilityLevel = 20;
			}
		}

		public override Filter Preprocess()
		{
			Filter delegateFilter;
			if (string.IsNullOrEmpty(this.SqlExpression))
			{
				throw new FilterException(SRClient.PropertyIsNullOrEmpty("SqlExpression"));
			}
			if (this.SqlExpression.Length > 1024)
			{
				throw new FilterException(SRClient.SqlFilterStatmentTooLong(this.SqlExpression.Length, 1024));
			}
			this.EnsureCompatibilityLevel();
			try
			{
				SqlFilter.CompiledItem compiledItem = (SqlFilter.CompiledItem)SqlFilter.compiledDelegateCache.Get(this.SqlExpression, null);
				if (compiledItem == null)
				{
					SqlFilterParserOutput sqlFilterParserOutput = SqlFilterParser.Parse(this.SqlExpression);
					(new SqlFilter.ExpressionValidator()).Validate(sqlFilterParserOutput.ExpressionTree);
					Func<BrokeredMessage, IDictionary<string, object>, bool?> func = sqlFilterParserOutput.ExpressionTree.Compile();
					compiledItem = new SqlFilter.CompiledItem(func, sqlFilterParserOutput.RequiredParameters);
					SqlFilter.CompiledItem compiledItem1 = (SqlFilter.CompiledItem)SqlFilter.compiledDelegateCache.AddOrGetExisting(this.SqlExpression, compiledItem, SqlFilter.defaultCachePolicy, null);
					if (compiledItem1 != null)
					{
						compiledItem = compiledItem1;
					}
				}
				ParametersValidator.Validate(true, compiledItem.RequiredParameters, this.Parameters);
				delegateFilter = new DelegateFilter(compiledItem.Delegate, this.Parameters);
			}
			catch (FilterException filterException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new FilterException(exception.Message, exception);
				}
				throw;
			}
			return delegateFilter;
		}

		public override string ToString()
		{
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			object[] sqlExpression = new object[] { this.SqlExpression };
			return string.Format(invariantCulture, "SqlFilter: {0}", sqlExpression);
		}

		internal override void UpdateForVersion(ApiVersion version, Filter existingFilter = null)
		{
			PropertyDictionary propertyDictionaries;
			SqlFilter sqlFilter = existingFilter as SqlFilter;
			base.UpdateForVersion(version, existingFilter);
			if (version < ApiVersion.Two)
			{
				if (sqlFilter == null)
				{
					propertyDictionaries = null;
				}
				else
				{
					propertyDictionaries = sqlFilter.parameters;
				}
				this.parameters = propertyDictionaries;
			}
		}

		public override void Validate()
		{
			if (string.IsNullOrEmpty(this.SqlExpression))
			{
				throw new FilterException(SRClient.PropertyIsNullOrEmpty("SqlExpression"));
			}
			if (this.SqlExpression.Length > 1024)
			{
				throw new FilterException(SRClient.SqlFilterStatmentTooLong(this.SqlExpression.Length, 1024));
			}
			Filter filter = this;
			while (filter.RequiresPreprocessing)
			{
				filter = filter.Preprocess();
			}
			try
			{
				filter.Validate();
			}
			catch (FilterException filterException)
			{
				throw;
			}
			catch (Exception exception1)
			{
				Exception exception = exception1;
				if (!Fx.IsFatal(exception))
				{
					throw new FilterException(exception.Message, exception);
				}
				throw;
			}
		}

		private sealed class CompiledItem
		{
			public Func<BrokeredMessage, IDictionary<string, object>, bool?> Delegate
			{
				get;
				private set;
			}

			public IEnumerable<string> RequiredParameters
			{
				get;
				private set;
			}

			public CompiledItem(Func<BrokeredMessage, IDictionary<string, object>, bool?> compiledDelegate, IEnumerable<string> requiredParameters)
			{
				this.Delegate = compiledDelegate;
				this.RequiredParameters = requiredParameters;
			}
		}

		private sealed class ExpressionValidator : ExpressionVisitor
		{
			private int nodeCount;

			private int currentDepth;

			public ExpressionValidator()
			{
			}

			public void Validate(Expression expression)
			{
				this.nodeCount = 0;
				this.currentDepth = 0;
				try
				{
					this.Visit(expression);
				}
				catch (FilterException filterException)
				{
					throw filterException;
				}
			}

			public override Expression Visit(Expression node)
			{
				Expression expression;
				SqlFilter.ExpressionValidator expressionValidator = this;
				expressionValidator.nodeCount = expressionValidator.nodeCount + 1;
				SqlFilter.ExpressionValidator expressionValidator1 = this;
				expressionValidator1.currentDepth = expressionValidator1.currentDepth + 1;
				if (this.currentDepth > 32)
				{
					throw new FilterException(SRClient.FilterExpressionTooComplex);
				}
				if (this.nodeCount > 1024)
				{
					throw new FilterException(SRClient.FilterExpressionTooComplex);
				}
				try
				{
					expression = base.Visit(node);
				}
				finally
				{
					SqlFilter.ExpressionValidator expressionValidator2 = this;
					expressionValidator2.currentDepth = expressionValidator2.currentDepth - 1;
				}
				return expression;
			}
		}
	}
}