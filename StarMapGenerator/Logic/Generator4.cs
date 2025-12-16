using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarMapGenerator;

public class Generator4
{
    public int StarCount { get; set; } = 80;

    // Расстояние
    public double MinDistance { get; set; } = 2.0;
    public double MaxDistance { get; set; } = 8.0;
    public double MeanDistance { get; set; } = 4.0;
    public double Variance { get; set; } = 0.3;

    // Высота (ось Z)
    public double Thickness { get; set; } = 3.0;

    // Режимы карты
    public enum MapMode
    {
        Galaxy,
        Plane,
        CollidingGalaxies
    }
    public MapMode Mode { get; set; } = MapMode.Plane;

    private Random rng = new Random();
    private List<(double x, double y, double z)> stars = new();

    // --------------------------------------------------------------------
    //  PUBLIC API
    // --------------------------------------------------------------------

    public void GenerateMany(string name, int generateCount)
    {
        for (int i = 0; i < generateCount; i++)
        {
            Generate(name, i + 1);
        }
    }

    public void Generate(string name, int? number = null)
    {
        stars.Clear();

        switch (Mode)
        {
            case MapMode.Galaxy:
                GenerateGalaxy();
                break;

            case MapMode.Plane:
                GeneratePlane();
                break;

            case MapMode.CollidingGalaxies:
                GenerateCollidingGalaxies();
                break;
        }

        string filePath = number == null
            ? $"{name} {StarCount}.csv"
            : $"{name} {StarCount} {number}.csv";

        SaveToCsv(filePath);
        Console.WriteLine($"Saved: {filePath}");
    }

    // --------------------------------------------------------------------
    //  РЕЖИМ: ГАЛАКТИКА
    // --------------------------------------------------------------------

    private void GenerateGalaxy()
    {
        double radius = EstimateRadius();
        double sigma = radius / 3.0;

        for (int i = 0; i < StarCount; i++)
        {
            // XY — гаусс вокруг центра
            double x = RandomGaussian() * sigma;
            double y = RandomGaussian() * sigma;

            // расстояние до центра
            double dist = Math.Sqrt(x * x + y * y);

            // Z — тем меньше, чем дальше от центра
            double flatten = Math.Max(0.1, 1.0 - dist / radius);
            double z = (rng.NextDouble() - 0.5) * Thickness * flatten;

            stars.Add((x, y, z));
        }

        ApplyRandomDistances(); 
        ApplyCoreCompression();
    }

    // --------------------------------------------------------------------
    //  РЕЖИМ: ПЛОСКОСТЬ
    // --------------------------------------------------------------------

    private void GeneratePlane()
    {
        double radius = EstimateRadius();

        for (int i = 0; i < StarCount; i++)
        {
            // равномерно, но с небольшой эллиптичностью
            double angle = rng.NextDouble() * Math.PI * 2;
            double r = Math.Sqrt(rng.NextDouble()) * radius;

            double x = Math.Cos(angle) * r;
            double y = Math.Sin(angle) * r;
            double z = (rng.NextDouble() - 0.5) * Thickness;

            stars.Add((x, y, z));
        }

        ApplyRandomDistances();
    }

    // --------------------------------------------------------------------
    //  РЕЖИМ: СТОЛКНОВЕНИЕ ГАЛАКТИК
    // --------------------------------------------------------------------

    private void GenerateCollidingGalaxies()
    {
        double radius = EstimateRadius() * 0.7;
        double offset = radius * 1.5;
        double sigma = radius / 3.0;

        for (int i = 0; i < StarCount; i++)
        {
            bool galaxyA = (i < StarCount / 2);

            double x = RandomGaussian() * sigma;
            double y = RandomGaussian() * sigma;

            if (galaxyA) x -= offset;
            else x += offset;

            double z = (rng.NextDouble() - 0.5) * Thickness;

            stars.Add((x, y, z));
        }

        ApplyRandomDistances();
    }

    // --------------------------------------------------------------------
    //  ВСПОМОГАТЕЛЬНОЕ: создание неоднородных расстояний
    // --------------------------------------------------------------------

    private void ApplyCoreCompression()
    {
        double radius = EstimateRadius();

        // Насколько сильно сжимаем центр
        double centerScale = 0.5; // например 2/10 = 0.2

        // Насколько мягко растёт плотность
        double falloffPower = 3;

        for (int i = 0; i < stars.Count; i++)
        {
            var s = stars[i];

            double r = Math.Sqrt(s.x * s.x + s.y * s.y + s.z * s.z);
            double t = Math.Pow(Math.Min(1.0, r / radius), falloffPower);

            // S(r) = interpolation between compressed center and normal space
            double scale = centerScale + (1.0 - centerScale) * t;

            stars[i] = (s.x * scale, s.y * scale, s.z * scale);
        }
    }

    private void ApplyRandomDistances()
    {
        for (int i = 0; i < stars.Count; i++)
        {
            for (int j = i + 1; j < stars.Count; j++)
            {
                double dx = stars[j].x - stars[i].x;
                double dy = stars[j].y - stars[i].y;
                double dz = stars[j].z - stars[i].z;

                double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                double target = RandomDistance();

                if (dist < target)
                {
                    double factor = target / dist;
                    var a = stars[i];
                    var b = stars[j];

                    // раздвигаем звёзды
                    double nx = dx * factor;
                    double ny = dy * factor;
                    double nz = dz * factor;

                    stars[j] = (a.x + nx, a.y + ny, a.z + nz);
                }
            }
        }
    }


    private double RandomDistance()
    {
        // случайное колебание вокруг среднего
        double randomPart = MinDistance + rng.NextDouble() * (MaxDistance - MinDistance);

        return MeanDistance * (1.0 - Variance)
             + randomPart * Variance;
    }

    // --------------------------------------------------------------------
    //  УТИЛИТЫ
    // --------------------------------------------------------------------

    private double EstimateRadius()
    {
        // габарит вычисляется так:
        // площадь = π R² ≈ StarCount * MeanDistance²
        double area = StarCount * MeanDistance * MeanDistance;
        return Math.Sqrt(area / Math.PI);
    }

    // Боксовское гауссовское распределение
    private double RandomGaussian()
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    private void SaveToCsv(string filePath)
    {
        using var writer = new StreamWriter(filePath);

        foreach (var s in stars)
        {
            writer.WriteLine(
                $"system, {s.x.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                $"{s.y.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                $"{s.z.ToString("0.00", CultureInfo.InvariantCulture)}"
            );
        }
    }
}
