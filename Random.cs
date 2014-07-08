using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GamesLibrary
{
    public class MyRandom
    {
        private int m_Value = 0;
        public MyRandom() { }
        public MyRandom(int aSeed)
        {
            m_Value = aSeed;
        }
        private int Next()
        {
            m_Value = m_Value * 0x08088405 + 1;
            return m_Value;
        }
        private int AbsNext()
        {
            return Math.Abs(Next());
        }
        public int Next(int maxValue)
        {
            return Range(0, maxValue - 1); // TODO: Should be 0..maxValue-1, does this do it?
        }
        public double NextDouble()
        {
            return (double)((double)Next(100 - 1) / (double)100);
        }
        public int Range(int aMin, int aMax)
        {
            //return aMin + Next() % (aMax - aMin);
            return aMin + AbsNext() % (aMax - aMin);
        }
    }
}
