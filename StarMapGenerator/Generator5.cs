using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarMapGenerator;

/// <summary>
/// Умеет генерировать галактику с более плотным ядром, 
/// но не умеет пересчитывать толщину для компенсации повышенной плотности.
/// </summary>
public class Generator5
{
    /// <summary>
    /// Количество звёзд.
    /// </summary>
    public int StarCount { get; set; } = 150;

    /// <summary>
    /// Минимальное расстояние между звёздами.
    /// </summary>
    public double MinDistance { get; set; } = 5.0;

    /// <summary>
    /// Максимальное расстояние между звёздами.
    /// </summary>
    public double MaxDistance { get; set; } = 15.0;

    /// <summary>
    /// Среднее расстояние между звёздами.
    /// </summary>
    public double MeanDistance { get; set; } = 10.0;

    /// <summary>
    /// Количество случайных отклонений от среднего расстояния.
    /// </summary>
    public double Variance { get; set; } = 0.3;

    /// <summary>
    /// Количество звёзд по оси Z.
    /// </summary>
    public double Thickness { get; set; } = 3.0;

    /// <summary>
    /// Количество звёзд в ядре галактики (как доля от радиуса).
    /// </summary>
    public double CoreRadiusFraction { get; set; } = 0.5;

    /// <summary>
    /// Плавность перехода от ядра к диску.
    /// </summary>
    public double CoreScale { get; set; } = 0.3;

    /// <summary>
    /// Плавность перехода от ядра к диску.
    /// </summary>
    public double FalloffPower { get; set; } = 2.2;
   
    /// <summary>
    /// Режимы карты.
    /// </summary>
    public enum MapMode
    {
        Galaxy,
        Plane,
        CollidingGalaxies
    }
    public MapMode Mode { get; set; } = MapMode.Plane;

    private readonly Random _rng = new Random();
    private readonly List<(double x, double y, double z)> _stars = new();

    public void GenerateMany(string name, int generateCount)
    {
        for (int i = 0; i < generateCount; i++)
        {
            Generate(name, i + 1);
        }
    }

    public void Generate(string name, int? number = null)
    {
        _stars.Clear();

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
            double z = (_rng.NextDouble() - 0.5) * Thickness * flatten;

            _stars.Add((x, y, z));
        }

        ApplyRandomDistances(); 
        ApplyCoreCompression();
    }

    private void GeneratePlane()
    {
        double radius = EstimateRadius();

        for (int i = 0; i < StarCount; i++)
        {
            // равномерно, но с небольшой эллиптичностью
            double angle = _rng.NextDouble() * Math.PI * 2;
            double r = Math.Sqrt(_rng.NextDouble()) * radius;

            double x = Math.Cos(angle) * r;
            double y = Math.Sin(angle) * r;
            double z = (_rng.NextDouble() - 0.5) * Thickness;

            _stars.Add((x, y, z));
        }

        ApplyRandomDistances();
    }

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

            double z = (_rng.NextDouble() - 0.5) * Thickness;

            _stars.Add((x, y, z));
        }

        ApplyRandomDistances();
    }

    #region ВСПОМОГАТЕЛЬНОЕ: создание неоднородных расстояний

    private void ApplyCoreCompression()
    {
        double radius = EstimateRadius();

        for (int i = 0; i < _stars.Count; i++)
        {
            var s = _stars[i];
            double r = Math.Sqrt(s.x * s.x + s.y * s.y + s.z * s.z);

            // нормализуем до ядра
            double t = Math.Min(1.0, r / (radius * CoreRadiusFraction));

            // применяем плавный градиент
            double scale = CoreScale + (1.0 - CoreScale) * Math.Pow(t, FalloffPower);

            _stars[i] = (s.x * scale, s.y * scale, s.z * scale);
        }
    }

    private void ApplyRandomDistances()
    {
        for (int i = 0; i < _stars.Count; i++)
        {
            for (int j = i + 1; j < _stars.Count; j++)
            {
                double dx = _stars[j].x - _stars[i].x;
                double dy = _stars[j].y - _stars[i].y;
                double dz = _stars[j].z - _stars[i].z;

                double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                double target = RandomDistance();

                if (dist < target)
                {
                    double factor = target / dist;
                    var a = _stars[i];
                    var b = _stars[j];

                    // раздвигаем звёзды
                    double nx = dx * factor;
                    double ny = dy * factor;
                    double nz = dz * factor;

                    _stars[j] = (a.x + nx, a.y + ny, a.z + nz);
                }
            }
        }
    }

    private double RandomDistance()
    {
        // случайное колебание вокруг среднего
        double randomPart = MinDistance + _rng.NextDouble() * (MaxDistance - MinDistance);

        return MeanDistance * (1.0 - Variance)
             + randomPart * Variance;
    }

    #endregion

    #region УТИЛИТЫ

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
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
    }

    private void SaveToCsv(string filePath)
    {
        using var writer = new StreamWriter(filePath);

        foreach (var s in _stars)
        {
            writer.WriteLine(
                $"system, {s.x.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                $"{s.y.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                $"{s.z.ToString("0.00", CultureInfo.InvariantCulture)}"
            );
        }
    }

    #endregion
}
