#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

namespace Lokad.Cloud.Storage.Instrumentation.Events
{
    public enum StorageOperationType
    {
        BlobPut,
        BlobGet,
        BlobGetIfModified,
        BlobUpsertOrSkip,
        BlobDelete,
        TableQuery,
        TableInsert,
        TableUpdate,
        TableDelete,
        QueueGet,
        QueuePut,
        QueueDelete,
        QueueAbandon,
        QueuePersist,
        QueueWrap,
        QueueUnwrap
    }
}
