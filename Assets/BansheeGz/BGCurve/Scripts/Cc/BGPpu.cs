using System;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>Pixels per unit (unit = 1 meter) </summary>
    [Serializable]
    public struct BGPpu
    {
        private int x;
        private int y;

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public BGPpu(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}