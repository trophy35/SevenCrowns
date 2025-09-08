using System.Collections.Generic;
using NUnit.Framework;
using SevenCrowns.Map;

namespace SevenCrowns.Tests.EditMode.Map
{
    public class GridCoordTests
    {
        [Test]
        public void Equality_SameValues_AreEqual()
        {
            var a = new GridCoord(3, -2);
            var b = new GridCoord(3, -2);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
            Assert.IsTrue(a.Equals((object)b));
        }

        [Test]
        public void Equality_DifferentValues_AreNotEqual()
        {
            var a = new GridCoord(3, -2);
            var b = new GridCoord(4, -2);
            var c = new GridCoord(3, 5);
            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.IsTrue(a != b);
            Assert.IsTrue(a != c);
        }

        [Test]
        public void HashCode_SameValues_AreSame()
        {
            var a = new GridCoord(1, 2);
            var b = new GridCoord(1, 2);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Works_AsDictionaryKey()
        {
            var dict = new Dictionary<GridCoord, int>
            {
                [new GridCoord(0, 0)] = 1,
                [new GridCoord(1, 0)] = 2,
                [new GridCoord(0, 1)] = 3,
            };

            Assert.AreEqual(3, dict.Count);
            Assert.AreEqual(2, dict[new GridCoord(1, 0)]);
        }

        [Test]
        public void ToString_IsFormatted()
        {
            var a = new GridCoord(-7, 9);
            Assert.AreEqual("(-7,9)", a.ToString());
        }

        [Test]
        public void Deconstruct_Works()
        {
            var a = new GridCoord(10, 11);
            var (x, y) = a;
            Assert.AreEqual(10, x);
            Assert.AreEqual(11, y);
        }
    }
}

