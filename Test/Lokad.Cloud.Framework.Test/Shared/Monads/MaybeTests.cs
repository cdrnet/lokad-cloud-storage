// Imported from Lokad.Shared, 2011-02-08

using System;
using NUnit.Framework;

namespace Lokad.Cloud.Storage.Shared.Monads.Tests
{
    [TestFixture]
    public sealed class MaybeTests
    {
        // ReSharper disable InconsistentNaming
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Nullables_are_detected()
        {
            const object o = null;
            Maybe.From(o);
        }

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
            Assert.AreEqual(Maybe.From("10"), Maybe10.Convert(i => i.ToString()));
            Assert.AreEqual(Maybe<string>.Empty, MaybeEmpty.Convert(i => i.ToString()));

            Assert.AreEqual("10", Maybe10.Convert(i => i.ToString(), () => "none"));
            Assert.AreEqual("none", MaybeEmpty.Convert(i => i.ToString(), () => "none"));

            Assert.AreEqual("10", Maybe10.Convert(i => i.ToString(), "none"));
            Assert.AreEqual("none", MaybeEmpty.Convert(i => i.ToString(), "none"));
        }

        static Maybe<string> SayHi(int value)
        {
            if (value > 3) return Maybe<string>.Empty;
            return "Hi x " + value;
        }

        [Test]
        public void Equals()
        {
            Assert.IsTrue(Maybe10 == Maybe.From(10));
            Assert.IsTrue(Maybe10 != MaybeEmpty);
        }

        [Test]
        public void Check_GetHashCode()
        {
            Assert.AreEqual(Maybe10.GetHashCode(), Maybe.From(10).GetHashCode());
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

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Exposing1()
        {
            MaybeEmpty.ExposeException("Fail");
        }

        // HACK: tests commented out, because of the Lokad.Shared import

        //[Test, ExpectedException(typeof(KeyInvalidException))]
        //public void Exposing2()
        //{
        //    MaybeEmpty.ExposeException(Errors.KeyInvalid);
        //}

        //[Test]
        //public void Exposing_Valid()
        //{
        //    Maybe10
        //        .ExposeException("Fail")
        //        .ShouldBeEqualTo(10);

        //    Maybe10
        //        .ExposeException(Errors.KeyInvalid)
        //        .ShouldBeEqualTo(10);
        //}

        //[Test]
        //public void Equations()
        //{
        //    Maybe<DateTime> m1 = SystemUtil.UtcNow;
        //    Maybe<DateTime> m2 = Maybe<DateTime>.Empty;

        //    Assert.IsTrue(m1.EqualsTo(m1));
        //    Assert.IsTrue(m2.EqualsTo(m2));
        //    Assert.IsFalse(m1.EqualsTo(m2));
        //    Assert.IsFalse(m2.EqualsTo(m1));

        //}

        enum SomeFailure
        {
            None, Fail
        }
    }
}