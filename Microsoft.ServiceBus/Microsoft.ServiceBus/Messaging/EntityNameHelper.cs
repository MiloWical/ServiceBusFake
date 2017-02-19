using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.ServiceBus.Messaging
{
	internal static class EntityNameHelper
	{
		public static string CanonicalizeEntityName(string entityName)
		{
			if (entityName == null)
			{
				throw new ArgumentNullException("entityName");
			}
			return entityName.ToLowerInvariant();
		}

		public static string FormatConsumerGroupCheckpointEntityName(string eventHubEntityName, string consumerGroupName, string partitionId)
		{
			string[] strArrays = new string[] { eventHubEntityName, "|", consumerGroupName, "|", partitionId };
			return string.Concat(strArrays);
		}

		public static string FormatConsumerGroupCheckpointEntityName(string consumerGroupEntityName, string partitionId)
		{
			return string.Concat(consumerGroupEntityName, "|", partitionId);
		}

		public static string FormatConsumerGroupEntityName(string eventHubEntityName, string consumerGroupName)
		{
			return string.Concat(eventHubEntityName, "|", consumerGroupName);
		}

		public static string FormatConsumerGroupPath(string path, string name)
		{
			string[] strArrays = new string[] { path, "/", "ConsumerGroups", "/", name };
			return string.Concat(strArrays);
		}

		public static string FormatPartitionReceiverPath(string consumerGroupPath, string partitionName)
		{
			string[] strArrays = new string[] { consumerGroupPath, "/", "Partitions", "/", partitionName };
			return string.Concat(strArrays);
		}

		public static string FormatPartitionSenderPath(string eventHubName, string partitionName)
		{
			string[] strArrays = new string[] { eventHubName, "/", "Partitions", "/", partitionName };
			return string.Concat(strArrays);
		}

		public static string FormatPublisherPath(string path, string name)
		{
			string[] strArrays = new string[] { path, "/", "Publishers", "/", name };
			return string.Concat(strArrays);
		}

		public static string FormatSubQueueEntityName(string pathDelimitedEntityName)
		{
			return pathDelimitedEntityName.Replace("/", "|");
		}

		public static string FormatSubQueuePath(string entityPath, string subQueueName)
		{
			return string.Concat(entityPath, "/", subQueueName);
		}

		public static string FormatSubscriptionPath(string topicPath, string subscriptionName)
		{
			string[] strArrays = new string[] { topicPath, "/", "Subscriptions", "/", subscriptionName };
			return string.Concat(strArrays);
		}

		public static string NormalizeEntityName(string entityName)
		{
			return entityName.ToUpperInvariant();
		}

		public static string[] SplitSubQueuePath(string path)
		{
			if (!path.Contains("$"))
			{
				return new string[] { path, string.Empty };
			}
			if (!path.Contains("/") && !path.Contains("|"))
			{
				return new string[] { string.Empty, path };
			}
			int num = path.LastIndexOf("$", StringComparison.OrdinalIgnoreCase);
			if (num == -1)
			{
				num = path.LastIndexOf("|", StringComparison.OrdinalIgnoreCase);
			}
			string[] strArrays = new string[] { path.Substring(0, num - 1), path.Substring(num) };
			return strArrays;
		}

		public static bool ValidateSubQueueName(string subQueueName)
		{
			return Constants.SupportedSubQueueNames.Any<string>((string s) => s.Equals(subQueueName, StringComparison.OrdinalIgnoreCase));
		}
	}
}