using System.Drawing;

namespace CSIS_CW_Server
{
    class ServerCircle
    {
        public ServerCircle()
        {
            this.X = 0;
            this.Y = 0;
            this.LifeTime = 1000;
            this.RadiusPix = 40;
        }
        public ServerCircle(Point point, int lifetimeMilisec, int radius)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.LifeTime = lifetimeMilisec;
            this.RadiusPix = radius;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public int LifeTime { get; set; }
        public int RadiusPix { get; set; }
        public Point Point
        {
            get
            {
                return new Point(X, Y);
            }
        }
        public Rectangle Rectangle
        {
            get
            {
                return new Rectangle(this.X, this.Y, this.RadiusPix, this.RadiusPix);
            }
        }
        public bool InterrectsWithPoint(Point point)
        {
            return this.Rectangle.IntersectsWith(new Rectangle(point, Size.Empty));
        }
        new public string ToString()
        {
            return '{' + $"{X},{Y},{LifeTime},{RadiusPix}" + '}';
        }
    }
}
