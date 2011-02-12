#region (c)2009-2011 Lokad - New BSD license
// Company: http://www.lokad.com
// This code is released under the terms of the new BSD licence
#endregion

using System.ComponentModel;

namespace Lokad.Cloud.Storage.Shared.Policies
{
    /// <summary>
    /// Helper class for creating fluent APIs, that hides unused signatures.
    /// </summary>
    public abstract class Syntax
    {
        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <paramref name="obj"/> parameter is null.
        /// </exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Creates the syntax for the specified target
        /// </summary>
        /// <typeparam name="TTarget">The type of the target.</typeparam>
        /// <param name="inner">The inner.</param>
        /// <returns>new syntax instance</returns>
        public static Syntax<TTarget> For<TTarget>(TTarget inner)
        {
            return new Syntax<TTarget>(inner);
        }
    }

    /// <summary>
    /// Helper class for creating fluent APIs, that hides unused signatures.
    /// </summary>
    public sealed class Syntax<TTarget> : Syntax
    {
        readonly TTarget _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="Syntax{T}"/> class.
        /// </summary>
        /// <param name="inner">The underlying instance.</param>
        public Syntax(TTarget inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Gets the underlying object.
        /// </summary>
        /// <value>The underlying object.</value>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public TTarget Target
        {
            get { return _inner; }
        }
    }
}
