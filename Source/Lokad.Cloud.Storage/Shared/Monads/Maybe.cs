// Imported from Lokad.Shared, 2011-02-08

using System;
using System.Collections.Generic;

namespace Lokad.Cloud.Storage.Shared.Monads
{
	/// <summary>
	/// Helper routines for <see cref="Maybe{T}"/>
	/// </summary>
	public static class Maybe
	{
		/// <summary>
		/// Creates new <see cref="Maybe{T}"/> from the provided value
		/// </summary>
		/// <typeparam name="TSource">The type of the source.</typeparam>
		/// <param name="item">The item.</param>
		/// <returns><see cref="Maybe{T}"/> that matches the provided value</returns>
		/// <exception cref="ArgumentNullException">if argument is a null reference</exception>
		public static Maybe<TSource> From<TSource>(TSource item)
		{
			// ReSharper disable CompareNonConstrainedGenericWithNull
			if (null == item) throw new ArgumentNullException("item");
			// ReSharper restore CompareNonConstrainedGenericWithNull

			return new Maybe<TSource>(item);
		}

		/// <summary>
		/// Optional empty boolean
		/// </summary>
		public static readonly Maybe<bool> Bool = Maybe<bool>.Empty;
		/// <summary>
		/// Optional empty string
		/// </summary>
		public static readonly Maybe<string> String = Maybe<string>.Empty;

        /// <summary>
        /// Retrieves first value from the <paramref name="sequence"/>
        /// </summary>
        /// <typeparam name="TSource">The type of the source sequence.</typeparam>
        /// <param name="sequence">The source.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>first value</returns>
        public static Maybe<TSource> FirstOrEmpty<TSource>(
            this IEnumerable<TSource> sequence,
            Func<TSource, bool> predicate)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            if (predicate == null) throw new ArgumentNullException("predicate");

            foreach (var source in sequence)
            {
                if (predicate(source))
                    return source;
            }
            return Maybe<TSource>.Empty;
        }

        /// <summary>
        /// Retrieves first value from the <paramref name="sequence"/>
        /// </summary>
        /// <typeparam name="TSource">The type of the source sequence.</typeparam>
        /// <param name="sequence">The source.</param>
        /// <returns>first value or empty result, if it is not found</returns>
        public static Maybe<TSource> FirstOrEmpty<TSource>(this IEnumerable<TSource> sequence)
        {
            if (sequence == null) throw new ArgumentNullException("sequence");
            foreach (var source in sequence)
            {
                return source;
            }
            return Maybe<TSource>.Empty;
        }
	}
}