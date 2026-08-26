using NUnit.Framework;
using UnityEngine;
using PMF.Grid;

namespace PMF.Tests
{
    /// <summary>GridCoord 구조체 자체의 계약 테스트.</summary>
    public sealed class GridCoordTests
    {
        [Test]
        public void Equals_GetHashCode_Contract()
        {
            var a = new GridCoord(3, -7);
            var b = new GridCoord(3, -7);
            var c = new GridCoord(-3, 7);

            Assert.IsTrue(a == b);
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsFalse(a == c);
            Assert.IsFalse(a.Equals(c));
            // 주석: 해시 충돌은 허용되므로 다른 좌표의 해시가 같을 수 있다는 전제로만 검증한다.
        }

        [Test]
        public void Operators_Add_Subtract()
        {
            var a = new GridCoord(2, 3);
            var b = new GridCoord(5, -1);

            Assert.AreEqual(new GridCoord(7, 2), a + b);
            Assert.AreEqual(new GridCoord(-3, 4), a - b);
        }

        [Test]
        public void ManhattanDistance_NotChebyshev()
        {
            Assert.AreEqual(5, GridCoord.ManhattanDistance(
                new GridCoord(1, 2), new GridCoord(4, 4)));
        }

        [Test]
        public void ToString_Format()
        {
            Assert.AreEqual("(3, -7)", new GridCoord(3, -7).ToString());
        }
    }
}
