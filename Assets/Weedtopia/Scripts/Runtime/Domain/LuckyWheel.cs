using System;
using System.Collections.Generic;

namespace Weedtopia.Domain
{
    [Serializable] public sealed class WheelPrize
    {
        public string id;
        public int count;
        public bool claimed;
    }
    [Serializable] public sealed class LuckyWheelState
    {
        public bool initialized;
        public long day;
        public int spins;
        public uint randomState;
        public List<WheelPrize> prizes = new List<WheelPrize>();
    }
    public static class LuckyWheelRules
    {
        public const int Size = 10;
        public static int Cost(int spins) => spins <= 0 ? 0 : spins == 1 ? 50 : spins == 2 ? 100 :
            spins == 3 ? 200 : spins == 4 ? 500 : spins == 5 ? 1000 : 2000;
    }
}
