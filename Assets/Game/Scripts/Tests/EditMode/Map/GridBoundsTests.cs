using System;
using NUnit.Framework;
using SevenCrowns.Map;

namespace SevenCrowns.Tests.EditMode.Map
{
    public class GridBoundsTests
    {
        [Test]
        public void Construct_Valid_SetsProperties()
        {
            var b = new GridBounds(10, 5);
            Assert.AreEqual(10, b.Width);
            Assert.AreEqual(5, b.Height);
            Assert.IsFalse(b.IsEmpty);
            Assert.AreEqual(50, b.Area);
        }

        [Test]
        public void Construct_Zero_MarksEmpty()
        {
            var b1 = new GridBounds(0, 5);
            var b2 = new GridBounds(7, 0);
            Assert.IsTrue(b1.IsEmpty);
            Assert.IsTrue(b2.IsEmpty);
            Assert.IsFalse(b1.Contains(0, 0));
            Assert.IsFalse(b2.Contains(0, 0));
            Assert.AreEqual(new GridCoord(0, 0), b1.Clamp(3, 4));
            Assert.AreEqual(new GridCoord(0, 0), b2.Clamp(-1, -1));
        }

        [Test]
        public void Construct_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridBounds(-1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridBounds(4, -2));
        }

        [Test]
        public void Contains_InsideEdges_True()
        {
            var b = new GridBounds(3, 2); // x:0..2, y:0..1
            Assert.IsTrue(b.Contains(0, 0));
            Assert.IsTrue(b.Contains(2, 1));
            Assert.IsTrue(b.Contains(new GridCoord(1, 0)));
        }

        [Test]
        public void Contains_Outside_False()
        {
            var b = new GridBounds(3, 2);
            Assert.IsFalse(b.Contains(-1, 0));
            Assert.IsFalse(b.Contains(3, 0));
            Assert.IsFalse(b.Contains(0, -1));
            Assert.IsFalse(b.Contains(0, 2));
        }

        [Test]
        public void Clamp_Inside_Unchanged()
        {
            var b = new GridBounds(10, 10);
            var c = new GridCoord(4, 6);
            Assert.AreEqual(c, b.Clamp(c));
        }

        [Test]
        public void Clamp_OutOfRange_BroughtInside()
        {
            var b = new GridBounds(4, 3); // x:0..3, y:0..2
            Assert.AreEqual(new GridCoord(0, 0), b.Clamp(-5, -7));
            Assert.AreEqual(new GridCoord(3, 2), b.Clamp(9, 99));
            Assert.AreEqual(new GridCoord(3, 0), b.Clamp(99, 0));
            Assert.AreEqual(new GridCoord(0, 2), b.Clamp(0, 999));
        }

        [Test]
        public void ToString_ContainsDimensions()
        {
            var b = new GridBounds(8, 6);
            var s = b.ToString();
            StringAssert.Contains("W=8", s);
            StringAssert.Contains("H=6", s);
        }
    }
}

