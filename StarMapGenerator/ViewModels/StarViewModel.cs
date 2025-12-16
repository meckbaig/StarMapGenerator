using StarMapGenerator.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace StarMapGenerator.ViewModels;

public class StarViewModel : INotifyPropertyChanged
{
    public StarData Model { get; set; }
    public double ViewX { get; set; }
    public double ViewY { get; set; }

    private int? _ownerIndex;
    public int? OwnerIndex
    {
        get => _ownerIndex;
        set
        {
            _ownerIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(OwnerColorBrush));
        }
    }

    public bool IsSelected => OwnerIndex.HasValue;

    public Brush OwnerColorBrush
    {
        get
        {
            if (!OwnerIndex.HasValue) return Brushes.Transparent;
            return OwnerIndex.Value switch
            {
                0 => Brushes.Red,
                1 => Brushes.Cyan,
                2 => Brushes.LimeGreen,
                3 => Brushes.Orange,
                4 => Brushes.Magenta,
                5 => Brushes.Yellow,
                6 => Brushes.White,
                7 => Brushes.Purple,
                _ => Brushes.Gray
            };
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
