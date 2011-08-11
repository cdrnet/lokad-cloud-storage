#region Copyright (c) Lokad 2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Lokad.Cloud.Diagnostics
{
    internal static class TockenBucketThrottling
    {
        public class ThrottledItem<T>
        {
            public T Item;
            public bool Delayed;
            public long DroppedItems;
        }

        public static IObservable<ThrottledItem<T>> ThrottleTokenBucket<T>(this IObservable<T> source, TimeSpan tokenInterval, int maxTokens)
        {
            // Note: there are ways to implement this lock-free, but probably not worth the effort.

            return Observable.Create<ThrottledItem<T>>(observer =>
                {
                    var gate = new object();
                    int tokens = maxTokens;
                    long dropped = 0;
                    T lastDropped = default(T);

                    return new CompositeDisposable(
                        Observable.Interval(tokenInterval).Subscribe(id =>
                            {
                                ThrottledItem<T> next = null;

                                if (tokens < maxTokens)
                                {
                                    lock (gate)
                                    {
                                        if (tokens < maxTokens)
                                        {
                                            if (dropped > 0)
                                            {
                                                next = new ThrottledItem<T> { Item = lastDropped, Delayed = true, DroppedItems = dropped-1 };
                                                lastDropped = default(T);
                                                dropped = 0;
                                            }
                                            else
                                            {
                                                tokens++;
                                            }
                                        }
                                    }
                                }

                                if (next != null)
                                {
                                    observer.OnNext(next);
                                }
                            }),
                        source.Subscribe(
                            next =>
                            {
                                bool passthrough = false;
                                lock (gate)
                                {
                                    if (tokens > 0)
                                    {
                                        tokens--;
                                        passthrough = true;
                                    }
                                    else
                                    {
                                        lastDropped = next;
                                        dropped++;
                                    }
                                }

                                if (passthrough)
                                {
                                    observer.OnNext(new ThrottledItem<T> { Item = next, Delayed = false, DroppedItems = 0 });
                                }
                            },
                            observer.OnError,
                            () =>
                            {
                                ThrottledItem<T> next = null;

                                if (dropped > 0)
                                {
                                    lock (gate)
                                    {
                                        if (dropped > 0)
                                        {
                                            next = new ThrottledItem<T> { Item = lastDropped, Delayed = true, DroppedItems = dropped - 1 };
                                            lastDropped = default(T);
                                            dropped = 0;
                                        }
                                    }
                                }

                                if (next != null)
                                {
                                    observer.OnNext(next);
                                }

                                observer.OnCompleted();
                            }));
                });
        }
    }
}
