using StarMapGenerator.Help;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarMapGenerator.Models;

public class StarData
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string OriginalLine { get; set; }
    public string Name 
    {
        get
        {
            if (string.IsNullOrEmpty(_name))
            {
                _name = StarNames.GetRandomName();
            }
            return _name;
        }
    }
    private string _name;

    public override string ToString() => OriginalLine;
}
