#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cloud.Storage;
using Lokad.Quality;

namespace Lokad.Cloud.Diagnostics.Persistence
{
	/// <summary>
	/// Diagnostics Cloud Data Repository to Blob Storage
	/// </summary>
	/// <remarks>
	/// In order for retention to work correctly, time segments need to be strictly
	/// ordered ascending by time and date when compared as string.
	/// </remarks>
	[UsedImplicitly]
	public class BlobDiagnosticsRepository : ICloudDiagnosticsRepository
	{
		readonly IBlobStorageProvider _blobs;

		/// <summary>
		/// Creates an Instance of the <see cref="BlobDiagnosticsRepository"/> class.
		/// </summary>
		public BlobDiagnosticsRepository(IBlobStorageProvider blobStorageProvider)
		{
			_blobs = blobStorageProvider;
		}

		void Upsert<T>(BlobName<T> name, Func<Maybe<T>, T> updater)
		{
			_blobs.UpsertBlob(
				name,
				() => updater(Maybe<T>.Empty),
				t => updater(t));
		}

		void RemoveWhile<TName>(TName prefix, Func<TName, string> segmentProvider, string timeSegmentBefore)
			where TName : UntypedBlobName
		{
			// since the blobs are strictly ordered we can stop once we reach the condition.
			var matchingBlobs = _blobs
				.ListBlobNames(prefix)
				.TakeWhile(blobName => String.Compare(segmentProvider(blobName), timeSegmentBefore, StringComparison.Ordinal) < 0);

			foreach (var blob in matchingBlobs)
			{
				_blobs.DeleteBlobIfExist(blob.ContainerName, blob.ToString());
			}
		}

		/// <summary>
		/// Get the statistics of all execution profiles.
		/// </summary>
		public IEnumerable<ExecutionProfilingStatistics> GetExecutionProfilingStatistics(string timeSegment)
		{
			return _blobs.ListBlobs(ExecutionProfilingStatisticsName.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all cloud partitions.
		/// </summary>
		public IEnumerable<PartitionStatistics> GetAllPartitionStatistics(string timeSegment)
		{
			return _blobs.ListBlobs(PartitionStatisticsName.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Get the statistics of all cloud services.
		/// </summary>
		public IEnumerable<ServiceStatistics> GetAllServiceStatistics(string timeSegment)
		{
			return _blobs.ListBlobs(ServiceStatisticsName.GetPrefix(timeSegment));
		}

		/// <summary>
		/// Upsert the statistics of an execution profile.
		/// </summary>
		public void UpdateExecutionProfilingStatistics(string timeSegment, string contextName, Func<Maybe<ExecutionProfilingStatistics>, ExecutionProfilingStatistics> updater)
		{
			Upsert(ExecutionProfilingStatisticsName.New(timeSegment, contextName), updater);
		}

		/// <summary>
		/// Upsert the statistics of a cloud partition.
		/// </summary>
		public void UpdatePartitionStatistics(string timeSegment, string partitionName, Func<Maybe<PartitionStatistics>, PartitionStatistics> updater)
		{
			Upsert(PartitionStatisticsName.New(timeSegment, partitionName), updater);
		}

		/// <summary>
		/// Upsert the statistics of a cloud service.
		/// </summary>
		public void UpdateServiceStatistics(string timeSegment, string serviceName, Func<Maybe<ServiceStatistics>, ServiceStatistics> updater)
		{
			Upsert(ServiceStatisticsName.New(timeSegment, serviceName), updater);
		}

		/// <summary>
		/// Remove old statistics of execution profiles.
		/// </summary>
		public void RemoveExecutionProfilingStatistics(string timeSegmentPrefix, string timeSegmentBefore)
		{
			RemoveWhile(
				ExecutionProfilingStatisticsName.GetPrefix(timeSegmentPrefix),
				blobRef => blobRef.TimeSegment,
				timeSegmentBefore);
		}

		/// <summary>
		/// Remove old statistics of cloud partitions.
		/// </summary>
		public void RemovePartitionStatistics(string timeSegmentPrefix, string timeSegmentBefore)
		{
			RemoveWhile(
				PartitionStatisticsName.GetPrefix(timeSegmentPrefix),
				blobRef => blobRef.TimeSegment,
				timeSegmentBefore);
		}

		/// <summary>
		/// Remove old statistics of cloud services.
		/// </summary>
		public void RemoveServiceStatistics(string timeSegmentPrefix, string timeSegmentBefore)
		{
			RemoveWhile(
				ServiceStatisticsName.GetPrefix(timeSegmentPrefix),
				blobRef => blobRef.TimeSegment,
				timeSegmentBefore);
		}
	}
}
