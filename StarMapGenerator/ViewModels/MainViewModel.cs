using StarMapGenerator.Help;
using StarMapGenerator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace StarMapGenerator.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private Generator6 _generator;

    public ObservableCollection<StarViewModel> Stars { get; set; } = new ObservableCollection<StarViewModel>();

    public int StarCount 
    {
        get => _generator.StarCount; 
        set { _generator.StarCount = value; OnPropertyChanged(); }
    }
    public double MinDistance { get => _generator.MinDistance; set { _generator.MinDistance = value; OnPropertyChanged(); } }
    public double MaxDistance { get => _generator.MaxDistance; set { _generator.MaxDistance = value; OnPropertyChanged(); } }
    public double MeanDistance { get => _generator.MeanDistance; set { _generator.MeanDistance = value; OnPropertyChanged(); } }
    public double Variance { get => _generator.Variance; set { _generator.Variance = value; OnPropertyChanged(); } }
    public double Thickness { get => _generator.Thickness; set { _generator.Thickness = value; OnPropertyChanged(); } }
    public double CoreRadiusFraction { get => _generator.CoreRadiusFraction; set { _generator.CoreRadiusFraction = value; OnPropertyChanged(); } }
    public double CoreScale { get => _generator.CoreScale; set { _generator.CoreScale = value; OnPropertyChanged(); } }
    public double FalloffPower { get => _generator.FalloffPower; set { _generator.FalloffPower = value; OnPropertyChanged(); } }
    public double CollisionStage { get => _generator.CollisionStage; set { _generator.CollisionStage = value; OnPropertyChanged(); } }
    public double CollisionTiltDegrees { get => _generator.CollisionTiltDegrees; set { _generator.CollisionTiltDegrees = value; OnPropertyChanged(); } }

    public Generator6.MapMode SelectedMode
    {
        get => _generator.Mode;
        set { _generator.Mode = value; OnPropertyChanged(); }
    }
    public IEnumerable<Generator6.MapMode> MapModes => Enum.GetValues(typeof(Generator6.MapMode)).Cast<Generator6.MapMode>();

    private string _mapName = "My Galaxy";
    public string MapName { get => _mapName; set { _mapName = value; OnPropertyChanged(); } }

    private string _outputPath = @"C:\SteamLibrary\steamapps\common\Sword of the Stars Complete Collection\Maps";
    public string OutputPath { get => _outputPath; set { _outputPath = value; OnPropertyChanged(); } }

    private string _openedMapPath;

    public string OpenedMapPath { get => _openedMapPath; set { _openedMapPath = value; OnPropertyChanged(); } }

    // Команды
    public ICommand GenerateCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ToggleStarCommand { get; }

    public MainViewModel()
    {
        _generator = new Generator6
        {
            StarCount = 170,
            MaxDistance = 18,
            MinDistance = 6,
            MeanDistance = 14,
            Mode = Generator6.MapMode.Galaxy,
            Thickness = 3,

            CoreRadiusFraction = 0.3,
            CoreScale = 0.5,

            CollisionStage = 0.40,
            CollisionTiltDegrees = 40
        };

        GenerateCommand = new RelayCommand(_ => GenerateMap());
        SaveCommand = new RelayCommand(_ => SaveMap());
        ToggleStarCommand = new RelayCommand(param => OnStarClicked(param as StarViewModel));

        // Генерируем тестовую карту при старте
        GenerateMap();
    }

    private void GenerateMap()
    {
        Stars.Clear();

        string generatedFilePath = _generator.Generate(MapName);
        string newFilePath = Path.Combine(OutputPath, Path.GetFileName(generatedFilePath));
        File.Copy(generatedFilePath, newFilePath, true);
        File.Delete(generatedFilePath);
        string csvData = File.ReadAllText(newFilePath);
        OpenedMapPath = newFilePath;

        var lines = csvData.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        double minX = 0, maxX = 0, minY = 0, maxY = 0;
        var rawStars = new List<StarData>();

        int idCounter = 0;
        foreach (var line in lines)
        {
            if (!line.StartsWith("system")) continue;

            var parts = line.Split(',');
            if (parts.Length < 4) continue;

            if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double y) &&
                double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double z))
            {
                rawStars.Add(new StarData { X = x, Y = y, Z = z, OriginalLine = line });
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
        }

        double canvasSize = 600;
        double scale = canvasSize / (Math.Max(maxX - minX, maxY - minY) * 1.2);
        double offsetX = canvasSize / 2;
        double offsetY = canvasSize / 2;

        foreach (var s in rawStars)
        {
            Stars.Add(new StarViewModel
            {
                Model = s,
                // Инвертируем Y, так как на экране Y растет вниз
                ViewX = (s.X * scale) + offsetX,
                ViewY = (-s.Y * scale) + offsetY
            });
        }
    }

    private void OnStarClicked(StarViewModel star)
    {
        if (star == null) return;

        if (star.OwnerIndex.HasValue)
        {
            star.OwnerIndex = null;
        }
        else
        {
            var usedIndices = Stars.Where(s => s.OwnerIndex.HasValue).Select(s => s.OwnerIndex.Value).ToList();
            for (int i = 0; i < 8; i++)
            {
                if (!usedIndices.Contains(i))
                {
                    star.OwnerIndex = i;
                    break;
                }
            }
        }
    }

    private void SaveMap()
    {
        StringBuilder sb = new StringBuilder();

        foreach (var star in Stars)
        {
            string line = star.Model.OriginalLine;
            if (star.OwnerIndex.HasValue)
            {
                sb.AppendLine($"{line},{star.Model.Name}");
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        sb.AppendLine();

        bool hasColonies = false;
        foreach (var star in Stars.Where(s => s.OwnerIndex.HasValue))
        {
            sb.AppendLine($"colony,player{star.OwnerIndex.Value},{star.Model.Name},,");
            hasColonies = true;
        }

        // 4. Финальный блок
        if (hasColonies)
        {
            sb.AppendLine();
            sb.AppendLine("randomize_colonies,0,,,");
        }

        File.WriteAllText(OpenedMapPath, sb.ToString());
    }

    // Реализация INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
