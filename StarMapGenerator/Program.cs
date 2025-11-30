using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarMapGenerator
{
    public static class Program
    {
        static void Main(string[] args)
        {
            var gen = new Generator6
            {
                StarCount = 200,
                MaxDistance = 15.0,
                MinDistance = 4.0,
                MeanDistance = 10.0,
                Mode = Generator6.MapMode.Galaxy,
                Thickness = 3,

                CoreScale = 0.5,

                CollisionStage = 0.6,
                CollisionTiltDegrees = 60
            };

            gen.GenerateMany("MyGalaxy6", 5);
        }
    }
}
