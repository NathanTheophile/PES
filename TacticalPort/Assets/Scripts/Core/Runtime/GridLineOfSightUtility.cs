using System;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public static class GridLineOfSightUtility
    {
        public static bool HasLineOfSight(GridCoord pOrigin, GridCoord pTarget, Func<GridCoord, bool> pDoesBlockCell)
        {
            if (pDoesBlockCell == null)
                throw new ArgumentNullException(nameof(pDoesBlockCell));

            int lX0 = pOrigin.X;
            int lY0 = pOrigin.Y;
            int lX1 = pTarget.X;
            int lY1 = pTarget.Y;
            int lDeltaX = Math.Abs(lX1 - lX0);
            int lDeltaY = Math.Abs(lY1 - lY0);
            int lStepX = lX0 < lX1 ? 1 : -1;
            int lStepY = lY0 < lY1 ? 1 : -1;
            int lError = lDeltaX - lDeltaY;
            int lX = lX0;
            int lY = lY0;

            while (lX != lX1 || lY != lY1)
            {
                int lDoubleError = lError * 2;

                if (lDoubleError > -lDeltaY)
                {
                    lError -= lDeltaY;
                    lX += lStepX;
                }

                if (lDoubleError < lDeltaX)
                {
                    lError += lDeltaX;
                    lY += lStepY;
                }

                if (lX == lX1 && lY == lY1)
                    break;

                if (pDoesBlockCell(new GridCoord(lX, lY)))
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
            if (pShape == SkillAoeShape.Single || pSize <= 0)
                return pOffsetX == 0 && pOffsetY == 0;

            return pShape switch
            {
                SkillAoeShape.Circle => Math.Abs(pOffsetX) + Math.Abs(pOffsetY) <= pSize,
                SkillAoeShape.Cross => (pOffsetX == 0 || pOffsetY == 0) && Math.Abs(pOffsetX) + Math.Abs(pOffsetY) <= pSize,
                _ => pOffsetX == 0 && pOffsetY == 0
            };
        }
    }
}
