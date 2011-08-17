#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

// Imported from Lokad.Shared, 2011-02-08

using System;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Test.Shared.Monads
{
    [TestFixture]
    public sealed class MaybeTests
    {
        // ReSharper disable InconsistentNaming
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Empty_protects_value()
        {
            Assert.IsFalse(Maybe<int>.Empty.HasValue);
            Assert.Fail(Maybe<int>.Empty.Value.ToString());
        }

        [Test]
        public void Implicit_conversion()
        {
            Maybe<int> maybe = 10;
            Assert.IsTrue(maybe.HasValue);
            Assert.AreEqual(10, maybe.Value);
        }

        [Test]
        public void Apply1()
        {
            Maybe<int>.Empty.Apply(i => Assert.Fail());
            int applied = 0;
            Maybe10.Apply(i => applied = i);
            Assert.AreEqual(10, applied);
        }

        static readonly Maybe<int> Maybe10 = 10;
        static readonly Maybe<int> MaybeEmpty = Maybe<int>.Empty;

        [Test]
        public void GetValue()
        {
            Assert.AreEqual(4, MaybeEmpty.GetValue(4));
            Assert.AreEqual(10, Maybe10.GetValue(4));

            Assert.AreEqual(4, MaybeEmpty.GetValue(() => 4));
            Assert.AreEqual(10, Maybe10.GetValue(() => 4));


            Assert.AreEqual(4, MaybeEmpty.GetValue(MaybeEmpty).GetValue(4));
            Assert.AreEqual(10, Maybe10.GetValue(Maybe10).GetValue(4));

            Assert.AreEqual(4, MaybeEmpty.GetValue(() => MaybeEmpty).GetValue(() => 4));
            Assert.AreEqual(10, Maybe10.GetValue(() => Maybe10).GetValue(() => 4));
        }

        [Test]
        public void Convert()
        {
            Assert.AreEqual(new Maybe<string>("10"), Maybe10.Convert(i => i.ToString()));
            Assert.AreEqual(Maybe<string>.Empty, MaybeEmpty.Convert(i => i.ToString()));

            Assert.AreEqual("10", Maybe10.Convert(i => i.ToString(), () => "none"));
            Assert.AreEqual("none", MaybeEmpty.Convert(i => i.ToString(), () => "none"));

            Assert.AreEqual("10", Maybe10.Convert(i => i.ToString(), "none"));
            Assert.AreEqual("none", MaybeEmpty.Convert(i => i.ToString(), "none"));
        }


        [Test]
        public void Equals()
        {
            Assert.IsTrue(Maybe10 == new Maybe<int>(10));
            Assert.IsTrue(Maybe10 != MaybeEmpty);
        }

        [Test]
        public void Check_GetHashCode()
        {
            Assert.AreEqual(Maybe10.GetHashCode(), (new Maybe<int>(10)).GetHashCode());
        }

        static void Throw()
        {
            throw new InvalidOperationException();
        }

        [Test]
        public void Handle_and_apply()
        {
            var i = 0;
            Maybe10
                .Handle(Throw)
                .Apply(x => i = x)
                .Handle(Throw);

            Assert.AreEqual(Maybe10.Value, i);
        }
    }
}