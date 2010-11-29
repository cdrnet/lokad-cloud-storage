#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml.XPath;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud.Diagnostics
{
	/// <summary>
	/// Log entry (when retrieving logs with the <see cref="CloudLogger"/>).
	/// </summary>
	public class LogEntry
	{
		public DateTime DateTimeUtc { get; set; }
		public string Level { get; set; }
		public string Message { get; set; }
		public string Error { get; set; }
		public string Source { get; set; }
	}

	/// <summary>
	/// Logger built on top of the Blob Storage.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Logs are formatted in XML with
	/// <code>
	/// &lt;log&gt;
	///   &lt;message&gt; {0} &lt;/message&gt;
	///   &lt;error&gt; {1} &lt;/error&gt;
	/// &lt;/log&gt;
	/// </code>
	/// Also, the logger is relying on date prefix in order to facilitate large
	/// scale enumeration of the logs. Yet, in order to facilitate fast enumeration
	/// of recent logs, a prefix inversion trick is used.
	/// </para>
	/// <para>
	/// We put entries to different containers depending on the log level. This helps
	/// reading only interesting entries and easily skipping those below the threshold.
	/// An entry is put to one container matching the level only, not to all containers
	/// with the matching or a lower level. This is a tradeoff to avoid optimizing
	/// for read spead at the cost of write speed, because we assume more frequent
	/// writes than reads and, more importantly, writes to happen in time-critical
	/// code paths while reading is almost never time-critical.
	/// </para>
	/// </remarks>
	public class CloudLogger : ILog
	{
		private const string ContainerNamePrefix = "lokad-cloud-logs";
		private const int DeleteBatchSize = 50;

		private readonly IBlobStorageProvider _blobStorage;
		private readonly string _source;

		/// <summary>Minimal log level (inclusive), below this level,
		/// notifications are ignored.</summary>
		public LogLevel LogLevelThreshold { get; set; }

		public CloudLogger(IBlobStorageProvider blobStorage, string source)
		{
			_blobStorage = blobStorage;
			_source = source;

			LogLevelThreshold = LogLevel.Min;
		}

		public bool IsEnabled(LogLevel level)
		{
			return level >= LogLevelThreshold;
		}

		public void Log(LogLevel level, object message)
		{
			Log(level, null, message);
		}

		public void Log(LogLevel level, Exception ex, object message)
		{
			if (!IsEnabled(level))
			{
				return;
			}

			var now = DateTime.UtcNow;

			var logEntry = new LogEntry
				{
					DateTimeUtc = now,
					Level = level.ToString(),
					Message = message.ToString(),
					Error = ex != null ? ex.ToString() : string.Empty,
					Source = _source ?? string.Empty
				};

			var blobContent = FormatLogEntry(logEntry);
			var blobName = string.Format("{0}/{1}/", FormatDateTimeNamePrefix(logEntry.DateTimeUtc), logEntry.Level);
			var blobContainer = LevelToContainer(level);

			var attempt = 0;
			while (!_blobStorage.PutBlob(blobContainer, blobName + attempt, blobContent, false))
			{
				attempt++;
			}
		}

		/// <summary>
		/// Lazily enuerate all logs of the specified level, ordered with the newest entry first.
		/// </summary>
		public IEnumerable<LogEntry> GetLogsOfLevel(LogLevel level, int skip = 0)
		{
			return _blobStorage
				.ListBlobs<string>(LevelToContainer(level), skip: skip)
				.Select(ParseLogEntry);
		}

		/// <summary>
		/// Lazily enuerate all logs of the specified level or higher, ordered with the newest entry first.
		/// </summary>
		public IEnumerable<LogEntry> GetLogsOfLevelOrHigher(LogLevel levelThreshold, int skip = 0)
		{
			// We need to sort by date (desc), but want to do it lazily based on
			// the guarantee that the enumerators themselves are ordered alike.
			// To do that we always select the newest value, move next, and repeat.

			var enumerators = EnumUtil<LogLevel>.Values
				.Where(l => l >= levelThreshold && l < LogLevel.Max && l > LogLevel.Min)
				.Select(level =>
					{
						var containerName = LevelToContainer(level);
						return _blobStorage.ListBlobNames(containerName, string.Empty)
							.Select(blobName => Tuple.From(containerName, blobName))
							.GetEnumerator();
					})
				.ToList();

			for (var i = enumerators.Count - 1; i >= 0; i--)
			{
				if (!enumerators[i].MoveNext())
				{
					enumerators.RemoveAt(i);
				}
			}

			// Skip
			for (var i = skip; i > 0 && enumerators.Count > 0; i--)
			{
				var max = enumerators.Aggregate((left, right) => string.Compare(left.Current.Value, right.Current.Value) < 0 ? left : right);
				if (!max.MoveNext())
				{
					enumerators.Remove(max);
				}
			}

			// actual iterator
			while (enumerators.Count > 0)
			{
				var max = enumerators.Aggregate((left, right) => string.Compare(left.Current.Value, right.Current.Value) < 0 ? left : right);
				var blob = _blobStorage.GetBlob<string>(max.Current.Key, max.Current.Value);
				if (blob.HasValue)
				{
					yield return ParseLogEntry(blob.Value);
				}

				if (!max.MoveNext())
				{
					enumerators.Remove(max);
				}
			}
		}

		/// <summary>Lazily enumerates over the entire logs.</summary>
		/// <returns></returns>
		public IEnumerable<LogEntry> GetLogs(int skip = 0)
		{
			return GetLogsOfLevelOrHigher(LogLevel.Min, skip);
		}

		/// <summary>
		/// Deletes all logs of all levels.
		/// </summary>
		public void DeleteAllLogs()
		{
			foreach (var level in EnumUtil<LogLevel>.Values.Where(l => l < LogLevel.Max && l > LogLevel.Min))
			{
				_blobStorage.DeleteContainerIfExist(LevelToContainer(level));
			}
		}

		/// <summary>
		/// Deletes all the logs older than the provided date.
		/// </summary>
		public void DeleteOldLogs(DateTime olderThanUtc)
		{
			foreach (var level in EnumUtil<LogLevel>.Values.Where(l => l < LogLevel.Max && l > LogLevel.Min))
			{
				DeleteOldLogsOfLevel(level, olderThanUtc);
			}
		}

		/// <summary>
		/// Deletes all the logs of a level and older than the provided date.
		/// </summary>
		public void DeleteOldLogsOfLevel(LogLevel level, DateTime olderThanUtc)
		{
			// Algorithm:
			// Iterate over the logs, queuing deletions up to 50 items at a time,
			// then restart; continue until no deletions are queued

			var deleteQueue = new List<string>(DeleteBatchSize);
			var blobContainer = LevelToContainer(level);

			do
			{
				deleteQueue.Clear();

				foreach (var blobName in _blobStorage.ListBlobNames(blobContainer, string.Empty))
				{
					var dateTime = ParseDateTimeFromName(blobName);
					if (dateTime < olderThanUtc) deleteQueue.Add(blobName);

					if (deleteQueue.Count == DeleteBatchSize) break;
				}

				foreach (var blobName in deleteQueue)
				{
					_blobStorage.DeleteBlobIfExist(blobContainer, blobName);
				}

			} while (deleteQueue.Count > 0);
		}

		private static string LevelToContainer(LogLevel level)
		{
			return ContainerNamePrefix + "-" + level.ToString().ToLower();
		}

		private static string FormatLogEntry(LogEntry logEntry)
		{
			return string.Format(
				@"
<log>
  <level>{0}</level>
  <timestamp>{1}</timestamp>
  <message>{2}</message>
  <error>{3}</error>
  <source>{4}</source>
</log>
",
				logEntry.Level,
				logEntry.DateTimeUtc.ToString("o", CultureInfo.InvariantCulture),
				SecurityElement.Escape(logEntry.Message),
				SecurityElement.Escape(logEntry.Error),
				SecurityElement.Escape(logEntry.Source));
		}

		private static LogEntry ParseLogEntry(string blobContent)
		{
			using (var stream = new StringReader(blobContent))
			{
				var xpath = new XPathDocument(stream);
				var nav = xpath.CreateNavigator();

				return new LogEntry
					{
						Level = nav.SelectSingleNode("/log/level").InnerXml,
						DateTimeUtc = DateTime.ParseExact(nav.SelectSingleNode("/log/timestamp").InnerXml, "o", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime(),
						Message = nav.SelectSingleNode("/log/message").InnerXml,
						Error = nav.SelectSingleNode("/log/error").InnerXml,
						Source = nav.SelectSingleNode("/log/source").InnerXml,
					};
			}
		}

		/// <summary>Time prefix with inversion in order to enumerate
		/// starting from the most recent.</summary>
		/// <remarks>This method is the symmetric of <see cref="ParseDateTimeFromName"/>.</remarks>
		public static string FormatDateTimeNamePrefix(DateTime dateTimeUtc)
		{
			// yyyy/MM/dd/hh/mm/ss/fff
			return string.Format("{0}/{1}/{2}/{3}/{4}/{5}/{6}",
				(10000 - dateTimeUtc.Year).ToString(CultureInfo.InvariantCulture),
				(12 - dateTimeUtc.Month).ToString("00"),
				(31 - dateTimeUtc.Day).ToString("00"),
				(24 - dateTimeUtc.Hour).ToString("00"),
				(60 - dateTimeUtc.Minute).ToString("00"),
				(60 - dateTimeUtc.Second).ToString("00"),
				(999 - dateTimeUtc.Millisecond).ToString("000"));
		}

		/// <summary>Convert a prefix with inversion into a <c>DateTime</c>.</summary>
		/// <remarks>This method is the symmetric of <see cref="FormatDateTimeNamePrefix"/>.</remarks>
		public static DateTime ParseDateTimeFromName(string nameOrPrefix)
		{
			// prefix is always 23 char long
			var tokens = nameOrPrefix.Substring(0, 23).Split('/');

			if (tokens.Length != 7) throw new ArgumentException("Incorrect prefix.", "nameOrPrefix");

			var year = 10000 - int.Parse(tokens[0], CultureInfo.InvariantCulture);
			var month = 12 - int.Parse(tokens[1], CultureInfo.InvariantCulture);
			var day = 31 - int.Parse(tokens[2], CultureInfo.InvariantCulture);
			var hour = 24 - int.Parse(tokens[3], CultureInfo.InvariantCulture);
			var minute = 60 - int.Parse(tokens[4], CultureInfo.InvariantCulture);
			var second = 60 - int.Parse(tokens[5], CultureInfo.InvariantCulture);
			var millisecond = 999 - int.Parse(tokens[6], CultureInfo.InvariantCulture);

			return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
		}
	}

	///<summary>
	/// Log provider for the cloud logger
	///</summary>
	public class CloudLogProvider : ILogProvider
	{
		readonly IBlobStorageProvider _provider;

		public CloudLogProvider(IBlobStorageProvider provider)
		{
			_provider = provider;
		}

		ILog IProvider<string, ILog>.Get(string key)
		{
			return new CloudLogger(_provider, key);
		}
	}
}