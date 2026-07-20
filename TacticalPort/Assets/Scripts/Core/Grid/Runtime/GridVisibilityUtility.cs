#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public static class GridVisibilityUtility
    {
        public static bool HasVisibility(GridCoord pOrigin, GridCoord pTarget, Func<GridCoord, bool> pDoesBlockCell)
        {
            if (pDoesBlockCell == null)
                throw new ArgumentNullException(nameof(pDoesBlockCell));

            foreach (GridCoord lCell in EnumerateCellsCrossedByLineInterior(pOrigin, pTarget))
            {
                if (lCell != pOrigin && lCell != pTarget && pDoesBlockCell(lCell))
                    return false;
            }

            return true;
        }

        public static bool MatchesAlignment(GridCoord pOrigin, GridCoord pTarget, SkillTargetAlignment pAlignment)
        {
            if (pOrigin == pTarget || pAlignment == SkillTargetAlignment.Any)
                return true;

            int lDeltaX = Math.Abs(pTarget.X - pOrigin.X);
            int lDeltaY = Math.Abs(pTarget.Y - pOrigin.Y);

            return pAlignment switch
            {
                SkillTargetAlignment.Orthogonal => pOrigin.X == pTarget.X || pOrigin.Y == pTarget.Y,
                SkillTargetAlignment.Diagonal => lDeltaX == lDeltaY,
                _ => true
            };
        }

        public static bool IsInsideAreaShape(SkillAoeShape pShape, int pOffsetX, int pOffsetY, int pSize)
        {
            return IsInsideAreaShape(pShape, pOffsetX, pOffsetY, pSize, new GridCoord(0, 1));
        }

        public static GridCoord ResolveAreaDirection(GridCoord pOrigin, GridCoord pTarget)
        {
            return NormalizeCardinalDirection(new GridCoord(pTarget.X - pOrigin.X, pTarget.Y - pOrigin.Y));
        }

        public static bool IsInsideAreaShape(SkillAoeShape pShape, int pOffsetX, int pOffsetY, int pSize, GridCoord pDirection)
        {
            if (pShape == SkillAoeShape.Single || pSize <= 0)
                return pOffsetX == 0 && pOffsetY == 0;

            GridCoord lDirection = NormalizeCardinalDirection(pDirection);
            return pShape switch
            {
                SkillAoeShape.Circle => Math.Abs(pOffsetX) + Math.Abs(pOffsetY) <= pSize,
                SkillAoeShape.Cross => (pOffsetX == 0 || pOffsetY == 0) && Math.Abs(pOffsetX) + Math.Abs(pOffsetY) <= pSize,
                SkillAoeShape.Square => Math.Max(Math.Abs(pOffsetX), Math.Abs(pOffsetY)) <= pSize,
                SkillAoeShape.X => Math.Abs(pOffsetX) == Math.Abs(pOffsetY) && Math.Abs(pOffsetX) <= pSize,
                SkillAoeShape.HorizontalLine => pOffsetY == 0 && Math.Abs(pOffsetX) <= pSize,
                SkillAoeShape.VerticalLine => pOffsetX == 0 && Math.Abs(pOffsetY) <= pSize,
                SkillAoeShape.Cone => IsInsideCone(pOffsetX, pOffsetY, pSize, lDirection),
                SkillAoeShape.ConeReverse => IsInsideCone(pOffsetX, pOffsetY, pSize, new GridCoord(-lDirection.X, -lDirection.Y)),
                SkillAoeShape.PerpendicularLine => pOffsetX * lDirection.X + pOffsetY * lDirection.Y == 0
                    && Math.Abs(pOffsetX) + Math.Abs(pOffsetY) <= pSize,
                _ => pOffsetX == 0 && pOffsetY == 0
            };
        }

        private static bool IsInsideCone(int pOffsetX, int pOffsetY, int pSize, GridCoord pDirection)
        {
            int lForward = pOffsetX * pDirection.X + pOffsetY * pDirection.Y;
            if (lForward < 0 || lForward > pSize)
                return false;

            int lSide = pDirection.X != 0 ? Math.Abs(pOffsetY) : Math.Abs(pOffsetX);
            return lSide <= lForward;
        }

        private static System.Collections.Generic.IEnumerable<GridCoord> EnumerateCellsCrossedByLineInterior(GridCoord pOrigin, GridCoord pTarget)
        {
            int lMinX = Math.Min(pOrigin.X, pTarget.X);
            int lMaxX = Math.Max(pOrigin.X, pTarget.X);
            int lMinY = Math.Min(pOrigin.Y, pTarget.Y);
            int lMaxY = Math.Max(pOrigin.Y, pTarget.Y);

            for (int lY = lMinY; lY <= lMaxY; lY++)
            {
                for (int lX = lMinX; lX <= lMaxX; lX++)
                {
                    GridCoord lCell = new GridCoord(lX, lY);
                    if (DoesSegmentCrossCellInterior(pOrigin, pTarget, lCell))
                        yield return lCell;
                }
            }
        }

        private static bool DoesSegmentCrossCellInterior(GridCoord pOrigin, GridCoord pTarget, GridCoord pCell)
        {
            const float lEpsilon = 0.0001f;

            float lOriginX = pOrigin.X + 0.5f;
            float lOriginY = pOrigin.Y + 0.5f;
            float lTargetX = pTarget.X + 0.5f;
            float lTargetY = pTarget.Y + 0.5f;
            float lDeltaX = lTargetX - lOriginX;
            float lDeltaY = lTargetY - lOriginY;
            float lMinX = pCell.X + lEpsilon;
            float lMaxX = pCell.X + 1f - lEpsilon;
            float lMinY = pCell.Y + lEpsilon;
            float lMaxY = pCell.Y + 1f - lEpsilon;
            float lEnter = 0f;
            float lExit = 1f;

            if (!ClipSegmentAxis(lOriginX, lDeltaX, lMinX, lMaxX, ref lEnter, ref lExit))
                return false;

            if (!ClipSegmentAxis(lOriginY, lDeltaY, lMinY, lMaxY, ref lEnter, ref lExit))
                return false;

            return lExit > lEnter;
        }

        private static bool ClipSegmentAxis(float pOrigin, float pDelta, float pMin, float pMax, ref float pEnter, ref float pExit)
        {
            const float lEpsilon = 0.000001f;
            if (Math.Abs(pDelta) < lEpsilon)
                return pOrigin > pMin && pOrigin < pMax;

            float lFirst = (pMin - pOrigin) / pDelta;
            float lSecond = (pMax - pOrigin) / pDelta;
            if (lFirst > lSecond)
            {
                float lTemporary = lFirst;
                lFirst = lSecond;
                lSecond = lTemporary;
            }

            pEnter = Math.Max(pEnter, lFirst);
            pExit = Math.Min(pExit, lSecond);
            return pExit >= pEnter;
        }

        private static GridCoord NormalizeCardinalDirection(GridCoord pDirection)
        {
            int lAbsX = Math.Abs(pDirection.X);
            int lAbsY = Math.Abs(pDirection.Y);

            if (lAbsX == 0 && lAbsY == 0)
                return new GridCoord(0, 1);

            if (lAbsX >= lAbsY)
                return new GridCoord(Math.Sign(pDirection.X), 0);

            return new GridCoord(0, Math.Sign(pDirection.Y));
        }
    }
}
