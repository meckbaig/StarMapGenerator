using StarMapGenerator.ViewModels;
using System.Windows;

namespace StarMapGenerator.Pages;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
