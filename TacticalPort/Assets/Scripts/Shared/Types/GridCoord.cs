using System;
using System.Collections.Generic;

namespace TacticalPort.Shared
{
    [Serializable]
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        #region _____________________________| VALUES

        public int X { get; }
        public int Y { get; }

        #endregion

        #region _____________________________| INIT

        public GridCoord(int pX, int pY)
        {
            X = pX;
            Y = pY;
        }

        #endregion

        #region _____________________________| HELPERS

        public int ManhattanDistanceTo(GridCoord pOther) => Math.Abs(X - pOther.X) + Math.Abs(Y - pOther.Y);

        public IEnumerable<GridCoord> GetCardinalNeighbors()
        {
            yield return new GridCoord(X + 1, Y);
            yield return new GridCoord(X - 1, Y);
            yield return new GridCoord(X, Y + 1);
            yield return new GridCoord(X, Y - 1);
        }

        public bool Equals(GridCoord pOther) => X == pOther.X && Y == pOther.Y;
        public override bool Equals(object pObject) => pObject is GridCoord lOther && Equals(lOther);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(GridCoord pLeft, GridCoord pRight) => pLeft.Equals(pRight);
        public static bool operator !=(GridCoord pLeft, GridCoord pRight) => !pLeft.Equals(pRight);

        #endregion
    }
}
