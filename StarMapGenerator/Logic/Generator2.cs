using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StarMapGenerator
{
    internal class Generator2
    {
        public int StarCount { get; set; } = 60;
        public double MinDistance { get; set; } = 3.0; // минимальная дистанция
        public double Thickness { get; set; } = 4.0;   // высота карты (ось Z)

        public enum Shape
        {
            Circle,
            Ellipse,
            Rectangle
        }

        public Shape MapShape { get; set; } = Shape.Circle;

        private Random rng = new Random();
        private List<(double x, double y)> stars = new List<(double x, double y)>();


        // Вычисление оптимальных габаритов карты
        private void CalculateMapSize(out double w, out double h)
        {
            // Для прямоугольника: плотность = MinDistance^2
            double areaPerStar = MinDistance * MinDistance * 2.2;

            double totalArea = areaPerStar * StarCount;

            switch (MapShape)
            {
                case Shape.Rectangle:
                    w = Math.Sqrt(totalArea);
                    h = w;
                    break;

                case Shape.Circle:
                    // площадь круга = π r^2 → r = sqrt(area / π)
                    double radius = Math.Sqrt(totalArea / Math.PI);
                    w = h = radius * 2;
                    break;

                case Shape.Ellipse:
                    // задаём эллипс вытянутый 2:1
                    double a = Math.Sqrt(totalArea / (Math.PI * 0.5));
                    w = a * 2;    // большая ось
                    h = a;        // малая ось
                    break;

                default:
                    w = h = 50;
                    break;
            }
        }

        public void GenerateMany(string name, int generateCount)
        {
            for (int i = 0; i < generateCount; i++)
            {
                Generate(name, i + 1);
            }
        }

        public void Generate(string name, int? number = null)
        {
            string fileName = number == null 
                ? $"{name} {StarCount}.csv"
                : $"{name} {StarCount} {number}.csv";

            stars.Clear();
            CalculateMapSize(out double width, out double height);

            int maxAttempts = 50000;
            int attempts = 0;

            while (stars.Count < StarCount && attempts < maxAttempts)
            {
                attempts++;

                (double x, double y) = GenerateCandidate(width, height);

                if (IsTooClose(x, y))
                    continue;

                stars.Add((x, y));
            }

            if (stars.Count < StarCount)
            {
                Console.WriteLine("WARNING: Не удалось разместить все звёзды — увеличь MinDistance или уменьшай StarCount");
            }

            // Сохранение
            using (var writer = new StreamWriter(fileName))
            {
                foreach (var s in stars)
                {
                    double z = (rng.NextDouble() - 0.5) * Thickness;
                    writer.WriteLine(
                        $"system, {s.x.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                        $"{s.y.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                        $"{z.ToString("0.00", CultureInfo.InvariantCulture)}"
                    );
                }
            }

            Console.WriteLine($"Map saved to {fileName}");
        }


        private (double x, double y) GenerateCandidate(double w, double h)
        {
            switch (MapShape)
            {
                case Shape.Rectangle:
                    return (
                        (rng.NextDouble() - 0.5) * w,
                        (rng.NextDouble() - 0.5) * h
                    );

                case Shape.Circle:
                    return GenerateCircle(w / 2);

                case Shape.Ellipse:
                    return GenerateEllipse(w / 2, h / 2);

                default:
                    return (0, 0);
            }
        }


        private (double x, double y) GenerateCircle(double radius)
        {
            double r = Math.Sqrt(rng.NextDouble()) * radius;
            double angle = rng.NextDouble() * Math.PI * 2;
            return (r * Math.Cos(angle), r * Math.Sin(angle));
        }

        private (double x, double y) GenerateEllipse(double a, double b)
        {
            double r = Math.Sqrt(rng.NextDouble());
            double angle = rng.NextDouble() * Math.PI * 2;
            return (r * a * Math.Cos(angle), r * b * Math.Sin(angle));
        }


        private bool IsTooClose(double x, double y)
        {
            foreach (var s in stars)
            {
                double dx = s.x - x;
                double dy = s.y - y;
                if (dx * dx + dy * dy < MinDistance * MinDistance)
                    return true;
            }
            return false;
        }
    }
}
