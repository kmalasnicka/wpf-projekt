using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Globalization;

namespace AutomatonEditor;

public class Automaton : INotifyPropertyChanged //automaton przechowuje wszystkie stany i przejscia
{ public ObservableCollection<State> States { get; set; } = []; 
  public ObservableCollection<Transition> Transitions { get; set; } = [];
  public event PropertyChangedEventHandler? PropertyChanged; 
}

public class State : INotifyPropertyChanged
{
    private double _x, _y;
    private bool _isInitial, _isAccepting, _isSelected;
    private string _fillColor = "White";
    private string _edgeColor = "Black";
    private double _radius = 25;
    private double _edgeThickness = 2;
    public string? Name { get; set; }
    public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }
    public bool IsInitial { get => _isInitial; set { _isInitial = value; OnPropertyChanged(); } }
    public bool IsAccepting { get => _isAccepting; set { _isAccepting = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    //stan w ktorym aktualnie znajduje sie automat podczas obliczen
    private bool _isCurrent;

    public bool IsCurrent
    {
        get => _isCurrent;
        set { _isCurrent = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged; 
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string FillColor{
        get => _fillColor;
        set { _fillColor = value; OnPropertyChanged(); }
    }

    public string EdgeColor{
        get => _edgeColor;
        set { _edgeColor = value; OnPropertyChanged(); }
    }

    public double Radius{
        get => _radius;
        set {
            _radius = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Diameter));
        }
    }

    public double EdgeThickness{
        get => _edgeThickness;
        set { _edgeThickness = value; OnPropertyChanged(); }
    }

    public double Diameter => Radius * 2;

}
public class Transition : INotifyPropertyChanged
{
    private State _source = null!; 
    private State _target = null!; 
    public State Source { get => _source;
        set { 
            _source?.PropertyChanged -= State_PropertyChanged; 
            _source = value; _source?.PropertyChanged += State_PropertyChanged; RefreshCoordinates(); 
        } 
    }
    public State Target { 
        get => _target; 
        set { 
            _target?.PropertyChanged -= State_PropertyChanged; 
            _target = value; 
            _target?.PropertyChanged += State_PropertyChanged; 
            RefreshCoordinates(); 
        } 
    }

    public double SourceCenterX => (Source?.X ?? 0) + (Source?.Radius ?? 25);
    public double SourceCenterY => (Source?.Y ?? 0) + (Source?.Radius ?? 25);
    public double TargetCenterX => (Target?.X ?? 0) + (Target?.Radius ?? 25);
    public double TargetCenterY => (Target?.Y ?? 0) + (Target?.Radius ?? 25);

    private double Dx => TargetCenterX - SourceCenterX;
    private double Dy => TargetCenterY - SourceCenterY;
    private double Distance => Math.Sqrt(Dx * Dx + Dy * Dy);

    public double X1{
        get{
            if (Distance == 0) return SourceCenterX;
            return SourceCenterX + Dx / Distance * Source.Radius;
        }
    }

    public double Y1{
        get{
            if (Distance == 0) return SourceCenterY;
            return SourceCenterY + Dy / Distance * Source.Radius;
        }
    }

    public double X2 {
        get{
            if (Distance == 0) return TargetCenterX;
            return TargetCenterX - Dx / Distance * Target.Radius;
        }
    }

    public double Y2{
        get{
            if (Distance == 0) return TargetCenterY;
            return TargetCenterY - Dy / Distance * Target.Radius;
        }
    }
    public double LabelX
    {
        get
        {
            if (IsLoop) return LoopX + 10;
            if (IsCurved) return ControlX - 5;
            return (X1 + X2) / 2 - 5;
        }
    }

    public double LabelY
    {
        get
        {
            if (IsLoop) return LoopY - 18;
            if (IsCurved) return ControlY - 8;
            return (Y1 + Y2) / 2 - 18;
        }
    }

    private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == "X" || e.PropertyName == "Y" || e.PropertyName == "Radius")
        {
            RefreshCoordinates();
        }
    }
    private void RefreshCoordinates() { 
        OnPropertyChanged(nameof(X1)); 
        OnPropertyChanged(nameof(Y1)); 
        OnPropertyChanged(nameof(X2));
        OnPropertyChanged(nameof(Y2));
        OnPropertyChanged(nameof(LabelX));
        OnPropertyChanged(nameof(LabelY));
        OnPropertyChanged(nameof(ControlX));
        OnPropertyChanged(nameof(ControlY));
        OnPropertyChanged(nameof(PathData));
        OnPropertyChanged(nameof(LoopX));
        OnPropertyChanged(nameof(LoopY));
        OnPropertyChanged(nameof(ArrowData));
    }
    public event PropertyChangedEventHandler? PropertyChanged; 
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private string _label = "";
    private bool _isSelected;
    public string Label
    {
        get => _label;
        set
        {
            _label = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayLabel));
        }
    }
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }


    public bool IsLoop => Source == Target;

    public double LoopX => X1 - 15;
    public double LoopY => Y1 - 45;

    private bool _isCurved;
    public bool IsCurved
    {
        get => _isCurved;
        set { _isCurved = value; OnPropertyChanged(); OnPropertyChanged(nameof(PathData)); }
    }

    public double ControlX
    {
        get
        {
            double midX = (X1 + X2) / 2;
            double dy = Y2 - Y1;
            double length = Math.Sqrt((X2 - X1) * (X2 - X1) + dy * dy);
            if (length == 0) return midX;
            return midX - dy / length * 40;
        }
    }

    public double ControlY
    {
        get
        {
            double midY = (Y1 + Y2) / 2;
            double dx = X2 - X1;
            double length = Math.Sqrt(dx * dx + (Y2 - Y1) * (Y2 - Y1));
            if (length == 0) return midY;
            return midY + dx / length * 40;
        }
    }

    public string PathData
    {
        get
        {
            if (IsCurved)
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "M {0} {1} Q {2} {3} {4} {5}",
                    X1, Y1, ControlX, ControlY, X2, Y2);

            return string.Format(
                CultureInfo.InvariantCulture,
                "M {0} {1} L {2} {3}",
                X1, Y1, X2, Y2);
        }
    }

    private bool _isActive;
    private string _activeSymbol = "";

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayLabel));
        }
    }

    public string ActiveSymbol
    {
        get => _activeSymbol;
        set
        {
            _activeSymbol = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayLabel));
        }
    }

    public string DisplayLabel
    {
        get
        {
            if (IsActive && !string.IsNullOrWhiteSpace(ActiveSymbol))
                return $"{Label} [{ActiveSymbol}]";

            return Label;
        }
    }

    private const double ArrowLength = 12;
    private const double ArrowWidth = 6;

    private string BuildArrowData(double tipX, double tipY, double fromX, double fromY)
    {
        double dx = tipX - fromX;
        double dy = tipY - fromY;
        double len = Math.Sqrt(dx * dx + dy * dy);

        if (len == 0)
            return "";

        dx /= len;
        dy /= len;

        double baseX = tipX - ArrowLength * dx;
        double baseY = tipY - ArrowLength * dy;

        double perpX = -dy;
        double perpY = dx;

        double leftX = baseX + ArrowWidth * perpX;
        double leftY = baseY + ArrowWidth * perpY;

        double rightX = baseX - ArrowWidth * perpX;
        double rightY = baseY - ArrowWidth * perpY;

        return string.Format(
            CultureInfo.InvariantCulture,
            "M {0} {1} L {2} {3} L {4} {5} Z",
            tipX, tipY,
            leftX, leftY,
            rightX, rightY);
    }

    public string ArrowData
    {
        get
        {
            if (IsLoop)
                return "";

            if (IsCurved)
                return BuildArrowData(X2, Y2, ControlX, ControlY);

            return BuildArrowData(X2, Y2, X1, Y1);
        }
    }
}