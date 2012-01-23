#region Copyright (c) Lokad 2009-2012
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using System.IO;

namespace Lokad.Cloud.Storage.Test
{
    [TestFixture]
    public class RawFormatterTests
    {
        [Test]
        public void StringRoundtripTest()
        {
            var formatter = new RawFormatter();

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(string.Empty, stream, typeof(string));
                stream.Position = 0;
                Assert.AreEqual(string.Empty, formatter.Deserialize(stream, typeof(string)));
            }

            const string text = "test";
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(text, stream, typeof(string));
                stream.Position = 0;
                Assert.AreEqual(text, formatter.Deserialize(stream, typeof(string)));
            }
        }

        [Test]
        public void XElementRoundtripTest()
        {
            var formatter = new RawFormatter();

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(new XElement("X"), stream, typeof(XElement));
                stream.Position = 0;
                Assert.IsTrue(XNode.DeepEquals(new XElement("X"), (XElement)formatter.Deserialize(stream, typeof(XElement))));
            }

            var xml = new XElement("Abc", new XElement("Def", new XAttribute("ghi", "jkl"), "mno"));
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(xml, stream, typeof(XElement));
                stream.Position = 0;
                Assert.IsTrue(XNode.DeepEquals(xml, (XElement)formatter.Deserialize(stream, typeof(XElement))));
            }
        }

        [Test]
        public void ByteRoundtripTest()
        {
            var formatter = new RawFormatter();

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(new byte[0], stream, typeof(byte[]));
                stream.Position = 0;
                Assert.IsTrue(new byte[0].SequenceEqual((byte[])formatter.Deserialize(stream, typeof(byte[]))));
            }

            var bytes = new byte[] { 2, 0, 240, 3, 255 };
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(bytes, stream, typeof(byte[]));
                stream.Position = 0;
                Assert.IsTrue(bytes.SequenceEqual((byte[])formatter.Deserialize(stream, typeof(byte[]))));
            }
        }

        [Test]
        public void StreamRoundtripTest()
        {
            var formatter = new RawFormatter();

            using(var data = new MemoryStream(new byte[0]))
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(data, stream, typeof(Stream));
                stream.Position = 0;
                Assert.IsTrue(data.ToArray().SequenceEqual(((MemoryStream)formatter.Deserialize(stream, typeof(Stream))).ToArray()));
            }

            using (var data = new MemoryStream(new byte[] { 2, 0, 240, 3, 255 }))
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(data, stream, typeof(Stream));
                stream.Position = 0;
                Assert.IsTrue(data.ToArray().SequenceEqual(((MemoryStream)formatter.Deserialize(stream, typeof(Stream))).ToArray()));
            }
        }
    }
}
