﻿using Verse;

namespace ProjectRimFactory.CultivatorTools
{
    public class SquareCellIterator
    {
        int rangeInt;
        public int Range => rangeInt;
        public IntVec3[] cellPattern;

        public SquareCellIterator(int range)
        {
            rangeInt = range;
            cellPattern = new IntVec3[(range * 2 + 1) * (range * 2 + 1)];
            var currentIter = 0;
            for (var i = -range; i <= range; i++)
            {
                if ((i & 1) == 0)
                {
                    for (var j = -range; j <= range; j++, currentIter++)
                    {
                        cellPattern[currentIter] = new IntVec3(i, 0, j);
                    }
                }
                else
                {
                    for (var j = range; j >= -range; j--, currentIter++)
                    {
                        cellPattern[currentIter] = new IntVec3(i, 0, j);
                    }
                }
            }
        }
    }
}