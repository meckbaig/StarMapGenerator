using System.Globalization;

public class Generator1
{
    public int StarCount { get; set; } = 60;
    public double Width { get; set; } = 40;   // размер по X
    public double Height { get; set; } = 40;  // размер по Y
    public double Thickness { get; set; } = 3; // ограничение по Z

    public enum Shape
    {
        Rectangle,
        Circle,
        Ellipse
    }

    public Shape MapShape { get; set; } = Shape.Circle;

    private Random rng = new Random();

    public void Generate(string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            for (int i = 0; i < StarCount; i++)
            {
                (double x, double y) = GenerateXY();
                double z = (rng.NextDouble() - 0.5) * Thickness;

                writer.WriteLine(
                    $"system, {x.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                    $"{y.ToString("0.00", CultureInfo.InvariantCulture)}, " +
                    $"{z.ToString("0.00", CultureInfo.InvariantCulture)}"
                );
            }
        }

        Console.WriteLine($"Map saved to {filePath}");
    }

    private (double x, double y) GenerateXY()
    {
        switch (MapShape)
        {
            case Shape.Rectangle:
                return (
                    (rng.NextDouble() - 0.5) * Width,
                    (rng.NextDouble() - 0.5) * Height
                );

            case Shape.Circle:
                return GenerateCircle();

            case Shape.Ellipse:
                return GenerateEllipse();

            default:
                throw new Exception("Unknown shape");
        }
    }

    private (double x, double y) GenerateCircle()
    {
        double r = Math.Sqrt(rng.NextDouble()) * (Width / 2);
        double angle = rng.NextDouble() * Math.PI * 2;
        return (r * Math.Cos(angle), r * Math.Sin(angle));
    }

    private (double x, double y) GenerateEllipse()
    {
        double r = Math.Sqrt(rng.NextDouble());
        double angle = rng.NextDouble() * Math.PI * 2;
        double a = Width / 2;
        double b = Height / 2;
        return (r * a * Math.Cos(angle), r * b * Math.Sin(angle));
    }
}