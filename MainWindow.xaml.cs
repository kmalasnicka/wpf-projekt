using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace AutomatonEditor;

public partial class MainWindow : Window
{
    private Automaton _automaton = new();
    private State? _selectedState;
    private Transition? _selectedTransition;
    private State? _draggedState; //przeciagany stan
    private Point _dragOffset;
    private ObservableCollection<InputLetter> _inputLetters = new();
    private ObservableCollection<HistoryEntry> _historyEntries = new();

    private int _currentLetterIndex = -1;
    private bool _isComputing = false;
    private State? _currentRuntimeState;
    private Transition? _activeRuntimeTransition;
    private List<State> _runtimeStateHistory = new();
    private DispatcherTimer _animationTimer = new();
    private bool _isAnimationRunning = false;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _automaton; //xaml moze robic bindingi do states i transitions i automatycznie rysowac stany 
        InputLettersItemsControl.ItemsSource = _inputLetters;
        HistoryList.ItemsSource = _historyEntries;
        //timer animacji
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Interval = TimeSpan.FromMilliseconds(1000);

        UpdateStepButtons();
        UpdateAnimationButtons();
    }

    private void AddState_Click(object sender, RoutedEventArgs e)
    {
        int number = 0;
        while (_automaton.States.Any(s => s.Name == $"q{number}")) number++;

        int column = number % 5;
        int row = number / 5;

        double x = 50 + column * 120;
        double y = 50 + row * 120;

        while (_automaton.States.Any(s => Math.Abs(s.X - x) < 5 && Math.Abs(s.Y - y) < 5))
        {
            y += 40;

            if (y > DrawingArea.ActualHeight - 80)
            {
                y = 50;
                x += 40;
            }
        }

        State state = new State
        {
            Name = $"q{number}",
            X = x,
            Y = y,
            IsInitial = _automaton.States.Count == 0
        };

        _automaton.States.Add(state);
        UpdateAnimationButtons();
    }

    private void State_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) //aktywuje klikniety stan
    {
        if (_isComputing) return;
        e.Handled = true;

        if (sender is FrameworkElement element && element.DataContext is State state)
        {
            ClearSelection();

            _selectedState = state;
            _selectedState.IsSelected = true;
            TransitionsList.Tag = _selectedState;
            StatePropertiesBox.IsEnabled = true;

            RefreshTransitionsList(); //odswieza liste checkboxow
        }
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)//jak klikniemy w tlo to aktywny stan przestaje byc aktywny i znika czerwone obramowanie 
    {
        if (_isComputing) return;
        ClearSelection();  //usuwa zaznaczenie stanu albo przejscia
    }
    private void State_MouseRightButtonDown(object sender, MouseButtonEventArgs e) //przeciaganie stanu
    {
        if (_isComputing) return;
        if (sender is FrameworkElement element && element.DataContext is State state)
        {
            _draggedState = state; //przeciagany stan

            Point mousePosition = e.GetPosition(element);
            _dragOffset = new Point(mousePosition.X, mousePosition.Y);  

            element.CaptureMouse();
        }
    }

    private void State_MouseRightButtonUp(object sender, MouseButtonEventArgs e) //konczy przeciaganie stanu 
    {
        if (sender is FrameworkElement element) element.ReleaseMouseCapture();
        _draggedState = null;
    }

    private void ClearSelection() //czysci aktywny stan i aktywne przejscie
    {
        ClearTransitionSelection();
        if (_selectedState != null)
            _selectedState.IsSelected = false;

        _selectedState = null;
        TransitionsList.ItemsSource = null;
        TransitionsList.Tag = null;
        StatePropertiesBox.IsEnabled = false;
    }
    private void State_MouseMove(object sender, MouseEventArgs e) //przelicza nowe x i y dla stanu
    {
        if (_draggedState == null) return;
        if (e.RightButton != MouseButtonState.Pressed) return; //przesuwanie dziala tylko jak trzymamy prawy przycisk

        Point mousePosition = e.GetPosition(DrawingArea);
        //liczymy nowa pozycje
        double newX = mousePosition.X - _dragOffset.X;
        double newY = mousePosition.Y - _dragOffset.Y;
        //ograniczamy zeby kolko nie wyszlo poza drawing area
        newX = Math.Max(0, Math.Min(newX, DrawingArea.ActualWidth - 50)); 
        newY = Math.Max(0, Math.Min(newY, DrawingArea.ActualHeight - 50));
        //ustawiamy nowa pozycje
        _draggedState.X = newX;
        _draggedState.Y = newY;
    }

    private void MarkAsAccepting_Click(object sender, RoutedEventArgs e) //przelacza stan akceptujacy
    {
        State? state = GetStateFromMenu(sender);
        if (state == null) return;
        state.IsAccepting = !state.IsAccepting; //jesli nie byl accepting to jest a jak byl to przestaje byc
    }

    private void MarkAsInitial_Click(object sender, RoutedEventArgs e) //ustawia sytan jako poczatkowy
    {
        State? state = GetStateFromMenu(sender);
        if (state == null) return;
        foreach (State s in _automaton.States) s.IsInitial = false; //najpeirw usuwamy ze wszystkich ze isinitial
        state.IsInitial = true;//klikniety jest initial
    }

    private void DeleteState_Click(object sender, RoutedEventArgs e) //usuwamy stan i wszystkie jego przejscia jakie z niego wychodza albo dochodza 
    {
        State? state = GetStateFromMenu(sender);
        if (state == null) return;
        //najpierw usuwamy wszystkie przejscia zwiazane z tym stanem
        var transitionsToRemove = _automaton.Transitions.Where(t => t.Source == state || t.Target == state).ToList();
        //kazde przejscie usuwamy z listy
        foreach (var transition in transitionsToRemove) _automaton.Transitions.Remove(transition);
        //usuwamy stan
        _automaton.States.Remove(state);
        UpdateCurvedTransitions();
        RefreshAlphabet();

        if (_selectedState == state)  _selectedState = null;
        if (_draggedState == state)  _draggedState = null;
        if (!_automaton.States.Any(s => s.IsInitial) && _automaton.States.Count > 0)  _automaton.States[0].IsInitial = true; //jesli usuniety byl initial to pierwszy z pozostalych robimy initial
    }

    private State? GetStateFromMenu(object sender) //sprawdza dla ktorego stanu bylo klikniete menu i zwraca ten stan 
    {
        if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is FrameworkElement element && element.DataContext is State state)
        {
            return state;
        }
        return null;
    }

    private void RefreshTransitionsList() //odswieza liste stanow w checkboxach, jesli nie ma aktywnego stanu to ja czyscimy 
    {
        if (_selectedState == null)
        {
            TransitionsList.ItemsSource = null; 
            return;
        }

        TransitionsList.ItemsSource = null;
        TransitionsList.ItemsSource = _automaton.States;
    }

    private void Transition_Checked(object sender, RoutedEventArgs e) //dodaje przejscie
    {
        if (_selectedState == null)
            return;

        if (sender is CheckBox checkBox && checkBox.DataContext is State targetState)
        {
            //sprawdzamy czy przejscie juz istnieje 
            bool alreadyExists = _automaton.Transitions.Any(t => t.Source == _selectedState && t.Target == targetState);
            if (alreadyExists) return;

            var newSymbols = SplitSymbols(TransitionLabelTextBox.Text).ToHashSet();

            if (newSymbols.Count == 0) //label jest pusty
            {
                MessageBox.Show( "Transition label cannot be empty.", "Invalid transition", MessageBoxButton.OK, MessageBoxImage.Error);

                checkBox.IsChecked = false;
                return;
            }

            bool determinismConflict = _automaton.Transitions.Any(t => t.Source == _selectedState && t.Target != targetState && SplitSymbols(t.Label).Any(symbol => newSymbols.Contains(symbol)));

            if (determinismConflict) //lamie deterministycznosc, istnieje przejscie do innego stanu o tej samej literze 
            { 
                MessageBox.Show( "This transition would make the automaton non-deterministic.", "Invalid transition", MessageBoxButton.OK, MessageBoxImage.Error);
                checkBox.IsChecked = false;
                return;
            }
            //dodajemy transition
            Transition transition = new Transition
            {
                Source = _selectedState,
                Target = targetState,
                Label = TransitionLabelTextBox.Text
            };

            _automaton.Transitions.Add(transition);
            UpdateCurvedTransitions();
            RefreshAlphabet();
            UpdateAnimationButtons();
        }
    }

    private void Transition_Unchecked(object sender, RoutedEventArgs e) //usuwa przejscir jak odznaczymy checkbox
    {
        if (_selectedState == null) return;

        if (sender is CheckBox checkBox && checkBox.DataContext is State targetState)
        {
            //szukamy transition z aktywnego stanu do docelowego i usuwamy 
            Transition? transition = _automaton.Transitions.FirstOrDefault(t => t.Source == _selectedState && t.Target == targetState);

            if (transition != null){
                _automaton.Transitions.Remove(transition);
                UpdateCurvedTransitions(); 
                RefreshAlphabet();
            }
        }
    }

    private void TransitionCheckBox_Loaded(object sender, RoutedEventArgs e) //ustawia stan checkboxqa po zaladowaniu listy
    {
        if (_selectedState == null) return;

        if (sender is CheckBox checkBox && checkBox.DataContext is State targetState) //przejscie istenieje 
        {
            //zaznaczamy checkbox
            checkBox.IsChecked = _automaton.Transitions.Any(t => t.Source == _selectedState && t.Target == targetState);
        }
    }

    private void RefreshAlphabet() //budujemy alfabet z etykiet przejsc, dzieli po przecinku, usuwa spacje i duplikaty
    {
        var symbols = _automaton.Transitions.SelectMany(t => t.Label.Split(',', StringSplitOptions.RemoveEmptyEntries)).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().OrderBy(s => s);
        AlphabetTextBlock.Text = string.Join(", ", symbols);
    }

    private void UpdateCurvedTransitions() //sprawdzamy czy istnieja przejscia w obie strony jak tak to ustawia iscurved jako true zeby przejscia na siebie nie nachodizlhy 
    {
        foreach (Transition transition in _automaton.Transitions)
        {
            bool hasOpposite = _automaton.Transitions.Any(t => t.Source == transition.Target && t.Target == transition.Source && t != transition);
            transition.IsCurved = hasOpposite && transition.Source != transition.Target;
        }
    }

    private void ClearTransitionSelection() //czyscimy aktywne przejscie 
    {
        if (_selectedTransition != null) _selectedTransition.IsSelected = false; //przejscie bylo zaznaczone wiec ustawiamy zeby juz nie bylo 
        _selectedTransition = null; 
    }

    //aktywuje klikniete przejscie 
    private void Transition_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isComputing) return;
        e.Handled = true;

        if (sender is FrameworkElement element && element.DataContext is Transition transition){
            ClearTransitionSelection();

            transition.IsSelected = true;
            _selectedTransition = transition;
        }
    }

    //usuwanie aktywnego transition
    private void DeleteActiveTransition_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedTransition == null) return;
        _automaton.Transitions.Remove(_selectedTransition);
        _selectedTransition = null;

        UpdateCurvedTransitions();
        RefreshAlphabet();
        RefreshTransitionsList();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "example_automata")
        };

        if (dialog.ShowDialog() != true) return;

        try 
        {
            string json = File.ReadAllText(dialog.FileName); //czytamy json
            //deserializujemy json 
            AutomatonFile? fileAutomaton = JsonSerializer.Deserialize<AutomatonFile>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (fileAutomaton == null) throw new Exception("File is empty or has invalid JSON format.");
            
            ValidateImportedAutomaton(fileAutomaton); //walidacja 

            Automaton importedAutomaton = ConvertToAutomaton(fileAutomaton); //konwertujemy na obiekt Automaton
            //ustawiamy go jako nowy DataContext
            _automaton = importedAutomaton;
            DataContext = _automaton;

            _selectedState = null;
            _selectedTransition = null;
            _draggedState = null;

            TransitionsList.ItemsSource = null;
            TransitionsList.Tag = null;
            StatePropertiesBox.IsEnabled = false;
            //czyscimy runtime, historie itd
            UpdateCurvedTransitions();
            RefreshAlphabet();
            ClearHistory();
            _inputLetters.Clear();
            _currentLetterIndex = -1;
            ClearRuntimeHighlights();
            UpdateStepButtons();
            UpdateAnimationButtons();

            MessageBox.Show("Automaton imported successfully.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ValidateCurrentAutomaton(); //walidujemy aktualny automat
             
            SaveFileDialog dialog = new SaveFileDialog 
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "automaton.json"
            };

            if (dialog.ShowDialog() != true) return;

            AutomatonFile fileAutomaton = ConvertToAutomatonFile(); //konwertujemy

            string json = JsonSerializer.Serialize(
                fileAutomaton,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

            File.WriteAllText(dialog.FileName, json); //zapisujemy jako sformatowany json

            MessageBox.Show("Automaton exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ValidateImportedAutomaton(AutomatonFile automatonFile) //sprawdza poprawnosc importowanego pliku: czy sa stany, czy lista transition istnieje, czy stany maja nazwy, czy nie ma duplikatow nazw, czy jest dokladnie jeden initial state...
    {
        if (automatonFile.States == null || automatonFile.States.Count == 0) throw new Exception("Invalid file: automaton must contain at least one state.");
        if (automatonFile.Transitions == null) throw new Exception("Invalid file: transitions list is missing.");
        var stateNames = new HashSet<string>();

        foreach (StateFile state in automatonFile.States)
        {
            if (string.IsNullOrWhiteSpace(state.Name)) throw new Exception("Invalid file: every state must have a name.");
            if (!stateNames.Add(state.Name)) throw new Exception($"Invalid file: duplicated state name '{state.Name}'.");
            if (state.Radius <= 0) throw new Exception($"Invalid file: state '{state.Name}' has invalid radius.");
            if (state.EdgeThickness <= 0) throw new Exception($"Invalid file: state '{state.Name}' has invalid edge thickness.");
            if (string.IsNullOrWhiteSpace(state.FillColor)) throw new Exception($"Invalid file: state '{state.Name}' has missing fill color.");
            if (string.IsNullOrWhiteSpace(state.EdgeColor)) throw new Exception($"Invalid file: state '{state.Name}' has missing edge color.");
        }

        int initialCount = automatonFile.States.Count(s => s.IsInitial);
        if (initialCount != 1) throw new Exception("Invalid file: automaton must have exactly one initial state.");

        foreach (TransitionFile transition in automatonFile.Transitions)
        {
            if (string.IsNullOrWhiteSpace(transition.Source)) throw new Exception("Invalid file: transition has missing source state.");
            if (string.IsNullOrWhiteSpace(transition.Target)) throw new Exception("Invalid file: transition has missing target state.");
            if (!stateNames.Contains(transition.Source)) throw new Exception($"Invalid file: transition source '{transition.Source}' does not exist.");
            if (!stateNames.Contains(transition.Target)) throw new Exception($"Invalid file: transition target '{transition.Target}' does not exist.");
            if (string.IsNullOrWhiteSpace(transition.Label)) throw new Exception("Invalid file: transition label cannot be empty.");
        }
    }
    
    private Automaton ConvertToAutomaton(AutomatonFile automatonFile) //zmieniamy dane json na automaton
    {
        Automaton automaton = new Automaton();
        Dictionary<string, State> statesByName = new Dictionary<string, State>();

        foreach (StateFile stateFile in automatonFile.States!)
        {
            State state = new State
            {
                Name = stateFile.Name,
                X = stateFile.X,
                Y = stateFile.Y,
                IsInitial = stateFile.IsInitial,
                IsAccepting = stateFile.IsAccepting,
                FillColor = stateFile.FillColor ?? "White",
                EdgeColor = stateFile.EdgeColor ?? "Black",
                Radius = stateFile.Radius,
                EdgeThickness = stateFile.EdgeThickness
            };

            automaton.States.Add(state);
            statesByName[state.Name!] = state;
        }

        foreach (TransitionFile transitionFile in automatonFile.Transitions!)
        {
            Transition transition = new Transition
            {
                Source = statesByName[transitionFile.Source!],
                Target = statesByName[transitionFile.Target!],
                Label = transitionFile.Label ?? ""
            };

            automaton.Transitions.Add(transition);
        }

        return automaton;
    }

    private AutomatonFile ConvertToAutomatonFile() //konwertujemy z automaton na json
    {
        AutomatonFile fileAutomaton = new AutomatonFile
        {
            States = _automaton.States.Select(s => new StateFile
            {
                Name = s.Name,
                X = s.X,
                Y = s.Y,
                IsInitial = s.IsInitial,
                IsAccepting = s.IsAccepting,
                FillColor = s.FillColor,
                EdgeColor = s.EdgeColor,
                Radius = s.Radius,
                EdgeThickness = s.EdgeThickness
            }).ToList(),

            Transitions = _automaton.Transitions.Select(t => new TransitionFile
            {
                Source = t.Source.Name,
                Target = t.Target.Name,
                Label = t.Label
            }).ToList()
        };

        return fileAutomaton;
    }

    private void ValidateCurrentAutomaton() //sprawdzamy czy automat mozna eksportowac
    {
        //musi miec przynajmniej jeden stan
        if (_automaton.States.Count == 0) throw new Exception("Cannot export: automaton must contain at least one state.");
        //dokładnie jeden stan początkowy
        int initialCount = _automaton.States.Count(s => s.IsInitial);
        if (initialCount != 1) throw new Exception("Cannot export: automaton must have exactly one initial state.");

        foreach (State state in _automaton.States) //poprawne promienie i thickness
        {
            if (string.IsNullOrWhiteSpace(state.Name)) throw new Exception("Cannot export: every state must have a name.");
            if (state.Radius <= 0) throw new Exception($"Cannot export: state '{state.Name}' has invalid radius.");
            if (state.EdgeThickness <= 0) throw new Exception($"Cannot export: state '{state.Name}' has invalid edge thickness.");
        }

        foreach (Transition transition in _automaton.Transitions) //niepuste labelki transitionow
        {
            if (transition.Source == null || transition.Target == null) throw new Exception("Cannot export: transition has missing source or target.");
            if (string.IsNullOrWhiteSpace(transition.Label)) throw new Exception("Cannot export: transition label cannot be empty.");
        }
    }

    private void ExportImage_Click(object sender, RoutedEventArgs e) //eksportuje obszar rysowania jako png
    {
        try
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "PNG image (*.png)|*.png",
                FileName = "automaton.png"
            };

            if (dialog.ShowDialog() != true) return;

            double width = DrawingArea.ActualWidth;
            double height = DrawingArea.ActualHeight;
            if (width <= 0 || height <= 0) throw new Exception("Cannot export image: drawing area has invalid size.");

            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(DrawingArea);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using FileStream stream = new FileStream(dialog.FileName, FileMode.Create);
            encoder.Save(stream);

            MessageBox.Show("Image exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private HashSet<string> GetAlphabet() //helper do walidacji input word, zwraca alfabet jako hashset 
    {
        return _automaton.Transitions.SelectMany(t => t.Label.Split(',', StringSplitOptions.RemoveEmptyEntries)).Select(s => s.Trim()).Where(s => s.Length > 0).ToHashSet();
    }

    private void ValidateInputWord(string word) //sprawdza czy slowo wejsciowe sklada sie tylko z dymboli nalezac ych do alfabetu automatu
    {
        HashSet<string> alphabet = GetAlphabet();
        if (alphabet.Count == 0 && word.Length > 0) throw new Exception("Cannot start computation: automaton alphabet is empty.");
        foreach (char letter in word)
        {
            string symbol = letter.ToString();
            if (!alphabet.Contains(symbol)) throw new Exception($"Invalid input word: symbol '{symbol}' is not in the automaton alphabet.");
        }
    }

    private void BuildInputLetters(string word) //rozbija wejsciowe slowo na osobne obiekty InputLetter, zeby kazda litere mozna bylo osobno podswietlic 
    {
        _inputLetters.Clear();
        foreach (char letter in word)
        {
            _inputLetters.Add(new InputLetter
            {
                Symbol = letter.ToString(),
                IsCurrent = false
            });
        }
    }

    private void UpdateCurrentLetterHighlight() //ustawia iscurrent dla aktualnie przetwarzanej litery, xaml daje zolte tlo
    {
        for (int i = 0; i < _inputLetters.Count; i++)
        {
            _inputLetters[i].IsCurrent = i == _currentLetterIndex;
        }
    }

    private void StartComputation_Click(object sender, RoutedEventArgs e) //startuje symulacje krokowa
    {
        try
        {
            PrepareComputation(); //przygotowuje input, initial state i highlighty

            if (_inputLetters.Count == 0 && _currentRuntimeState != null) //puste slowo
            {
                //sprawdzamy czy stan poczatkowy jest accepting
                InputValidationTextBlock.Text = _currentRuntimeState.IsAccepting ? $"Accepted: empty word ended in accepting state '{_currentRuntimeState.Name}'." : $"Rejected: empty word ended in non-accepting state '{_currentRuntimeState.Name}'.";
                return;
            }

            InputValidationTextBlock.Text = "Input word is valid. Computation started.";
        }
        catch (Exception ex)
        {
            _isComputing = false;
            _currentLetterIndex = -1;

            InputWordTextBox.IsEnabled = true;
            SetEditorEnabled(true);

            _inputLetters.Clear();
            ClearRuntimeHighlights();
            UpdateStepButtons();
            UpdateAnimationButtons();

            InputValidationTextBlock.Text = ex.Message;
        }
    }

    private void ResetComputation_Click(object sender, RoutedEventArgs e) //resetuje cala symulacje
    {
        StopAnimationOnly(); //zatrzymujemy animacje
        _isComputing = false;
        _currentLetterIndex = -1;

        _currentRuntimeState = null;
        _runtimeStateHistory.Clear();
        ClearHistory();

        InputWordTextBox.IsEnabled = true; //odblokowuje input
        SetEditorEnabled(true); //odblokowuje edytor
        //czyści aktualny stan, runtime history, podświetlenia i historię tabeli
        ClearRuntimeHighlights();
        UpdateCurrentLetterHighlight();
        UpdateStepButtons();
        UpdateAnimationButtons();

        InputValidationTextBlock.Text = "Computation reset.";
    }

    public class InputLetter : INotifyPropertyChanged //klasa dla pojedynczej litery slowa wejsciowego 
    {
        private bool _isCurrent;
        public string Symbol { get; set; } = "";
        public bool IsCurrent
        {
            get => _isCurrent;
            set {
                _isCurrent = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    private void SetEditorEnabled(bool isEnabled) //wlacza lub wylacza edytor podczas symulacji
    {
        //blokuje dodawanie stanów, przejść i zmianę właściwości, żeby nie zmieniać automatu w trakcie computation
        ManageStatesBox.IsEnabled = isEnabled;
        ManageTransitionsBox.IsEnabled = isEnabled;
        if (isEnabled) StatePropertiesBox.IsEnabled = _selectedState != null;
        else StatePropertiesBox.IsEnabled = false;
    }

    private State? GetInitialState() //zwraca stan poczatkowy
    {
        return _automaton.States.FirstOrDefault(s => s.IsInitial);
    }

    private Transition? FindTransition(State source, string symbol) //szuka przejscia z aktualnego stanu po aktualnie czytanym symbolu
    {
        return _automaton.Transitions.FirstOrDefault(t => t.Source == source && t.Label.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Contains(symbol));
    }

    private void ClearRuntimeHighlights() //usuwa IsCurrent ze wszystkich stanów i IsActive ze wszystkich transitionów
    {
        foreach (State state in _automaton.States)
            state.IsCurrent = false;

        foreach (Transition transition in _automaton.Transitions)
        {
            transition.IsActive = false;
            transition.ActiveSymbol = "";
        }

        _activeRuntimeTransition = null;
    }

    private void SetCurrentRuntimeState(State? state) //ustawia aktualny stan automatu podczas symulacji 
    {
        foreach (State s in _automaton.States) s.IsCurrent = false;
        if (state != null) state.IsCurrent = true;
        _currentRuntimeState = state;
    }

    private void SetActiveRuntimeTransition(Transition? transition, string symbol) //podswietla aktywne przejscie i aktywny symbol labela
    {
        foreach (Transition t in _automaton.Transitions) //czysci wszystkie transitiony
        {
            t.IsActive = false;
            t.ActiveSymbol = "";
        }

        if (transition != null) //ustawia dla aktualnego przejscia
        {
            transition.IsActive = true;
            transition.ActiveSymbol = symbol;
        }

        _activeRuntimeTransition = transition;
    }

    private void UpdateStepButtons()
    {
        //previous button dziala tylko po wykonaniu co najmniej jednego kroku
        PreviousButton.IsEnabled = _isComputing && !_isAnimationRunning && _currentLetterIndex > 0;
        //next button dziala tylko jesli zostaly litery do przetworzenia
        NextButton.IsEnabled = _isComputing && !_isAnimationRunning && _currentLetterIndex < _inputLetters.Count;
    }

    private void NextStep_Click(object sender, RoutedEventArgs e)
    {
        StepForward();
    }

    private void PreviousStep_Click(object sender, RoutedEventArgs e) //cofa jeden krok
    {
        if (!_isComputing) return;
        if (_runtimeStateHistory.Count <= 1) return;

        _runtimeStateHistory.RemoveAt(_runtimeStateHistory.Count - 1); //usuwamy ostatni stan
        //usuwa wpis z historii tabeli 
        if (_historyEntries.Count > 0) _historyEntries.RemoveAt(_historyEntries.Count - 1);
        _currentRuntimeState = _runtimeStateHistory.Last();
        _currentLetterIndex--;
        //cofa index litery
        if (_currentLetterIndex < 0) _currentLetterIndex = 0;
        //ustawiamy poprzedni stan jako aktulany
        SetActiveRuntimeTransition(null, "");
        SetCurrentRuntimeState(_currentRuntimeState);
        UpdateCurrentLetterHighlight();

        InputValidationTextBlock.Text = $"Returned to state '{_currentRuntimeState.Name}'.";
        UpdateStepButtons();
        UpdateAnimationButtons();
    }

    private void PrepareComputation()
    {
        string word = InputWordTextBox.Text.Trim(); 
        //walidujemy slowo i buduje lirery 
        ValidateInputWord(word);
        BuildInputLetters(word);
        //znajdujemy initial state
        State? initialState = GetInitialState();
        if (initialState == null) throw new Exception("Cannot start computation: automaton has no initial state.");
        //blokuje edycję inputu i edytora
        _isComputing = true;
        InputWordTextBox.IsEnabled = false;
        SetEditorEnabled(false);

        ClearSelection();
        ClearRuntimeHighlights();
        //ustawia aktualny stan na initial
        _currentRuntimeState = initialState;
        _runtimeStateHistory.Clear();
        _runtimeStateHistory.Add(initialState);
        ClearHistory();

        _currentLetterIndex = 0;

        SetCurrentRuntimeState(initialState);
        UpdateCurrentLetterHighlight();
        UpdateStepButtons();
        UpdateAnimationButtons();
    }

    private void UpdateAnimationButtons() //steruje przyciskami previous i next: Previous działa tylko po wykonaniu co najmniej jednego kroku, Next tylko jeśli zostały litery do przetworzenia
    {
        bool hasAutomaton = _automaton.States.Count > 0;
        bool hasWord = !string.IsNullOrWhiteSpace(InputWordTextBox.Text);
        bool canContinue = _isComputing && _currentLetterIndex < _inputLetters.Count;

        StartAnimationButton.IsEnabled = !_isAnimationRunning &&((!_isComputing && hasAutomaton && hasWord) || canContinue);
        StopAnimationButton.IsEnabled = _isAnimationRunning;
        ResetAnimationButton.IsEnabled = _isComputing || _isAnimationRunning || _inputLetters.Count > 0;
    }

    private void InputWordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isComputing)
        {
            _inputLetters.Clear();
            _currentLetterIndex = -1;
            ClearRuntimeHighlights();
            ClearHistory();
            UpdateCurrentLetterHighlight();
        }

        UpdateAnimationButtons();
    }

    private bool StepForward() //robi ruch DFA
    {
        if (!_isComputing) return false;
        if (_currentRuntimeState == null) return false;
        if (_currentLetterIndex >= _inputLetters.Count) return false;

        string symbol = _inputLetters[_currentLetterIndex].Symbol; //aktualny symbol

        Transition? transition = FindTransition(_currentRuntimeState, symbol); //szuka transition z aktualnego stanu dla tego symbolu
        //dodajemy wpis do historii
        _historyEntries.Add(new HistoryEntry
        {
            StateName = _currentRuntimeState.Name ?? "",
            Letter = symbol
        });
        //jelsi nie ma przejscia to slowo jest rejected
        if (transition == null)
        {
            ClearRuntimeHighlights();
            SetCurrentRuntimeState(_currentRuntimeState);

            InputValidationTextBlock.Text =
                $"Rejected: no transition from state '{_currentRuntimeState.Name}' for symbol '{symbol}'.";

            UpdateStepButtons();
            UpdateAnimationButtons();

            return false;
        }
        //jest przejscxie 
        SetActiveRuntimeTransition(transition, symbol); //podswietlamy transition
        //idziemy do target
        _currentRuntimeState = transition.Target;
        _runtimeStateHistory.Add(_currentRuntimeState);
        //przesuwamy indeks litery
        _currentLetterIndex++;
        SetCurrentRuntimeState(_currentRuntimeState);
        UpdateCurrentLetterHighlight();

        if (_currentLetterIndex == _inputLetters.Count) //jesli to koniec slowa to sprawdzamy acceoting/rejecting
        {
            if (_currentRuntimeState.IsAccepting) InputValidationTextBlock.Text = $"Accepted: ended in accepting state '{_currentRuntimeState.Name}'.";
            else InputValidationTextBlock.Text = $"Rejected: ended in non-accepting state '{_currentRuntimeState.Name}'.";

            UpdateStepButtons();
            UpdateAnimationButtons();
            return false;
        }

        InputValidationTextBlock.Text = $"Moved to state '{_currentRuntimeState.Name}'. Next symbol: '{_inputLetters[_currentLetterIndex].Symbol}'.";
        UpdateStepButtons();
        UpdateAnimationButtons();
        return true;
    }

    private void StartAnimation_Click(object sender, RoutedEventArgs e) //startuje animacje
    {
        try
        {
            if (!_isComputing) //nie bylo computation
            {
                PrepareComputation();
            }

            if (_currentLetterIndex >= _inputLetters.Count) return;

            _isAnimationRunning = true;
            //timer
            _animationTimer.Interval = TimeSpan.FromMilliseconds(AnimationSpeedSlider.Value);
            _animationTimer.Start();

            UpdateStepButtons();
            UpdateAnimationButtons();

            InputValidationTextBlock.Text = "Animation started.";
        }
        catch (Exception ex)
        {
            _isAnimationRunning = false;
            _animationTimer.Stop();

            InputValidationTextBlock.Text = ex.Message;

            UpdateStepButtons();
            UpdateAnimationButtons();
        }
    }

    private void StopAnimation_Click(object sender, RoutedEventArgs e) //konczy animacje
    {
        StopAnimationOnly();
        InputValidationTextBlock.Text = "Animation stopped.";
    }

    private void StopAnimationOnly() //zatrzymuje timer, odswierza przyciski
    {
        _animationTimer.Stop();
        _isAnimationRunning = false;
        UpdateStepButtons();
        UpdateAnimationButtons();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e) //robi stepforward co kazde tykniecie timera
    {
        bool canContinue = StepForward();
        if (!canContinue) //jesli nie mozna dalej isc to animacja sie zatrzymuje
        {
            StopAnimationOnly();
        }
    }

    private void ResetAnimation_Click(object sender, RoutedEventArgs e) //resetuje animacje do stanu poczatkowego 
    {
        try
        {
            StopAnimationOnly();

            if (_inputLetters.Count == 0)
            {
                PrepareComputation();
            }

            State? initialState = GetInitialState();
            if (initialState == null) throw new Exception("Cannot reset animation: automaton has no initial state.");
            ClearRuntimeHighlights();
            //wracamy do initial state
            _currentRuntimeState = initialState;
            _runtimeStateHistory.Clear();
            _runtimeStateHistory.Add(initialState);
            ClearHistory();

            _currentLetterIndex = 0;

            _isComputing = true;
            InputWordTextBox.IsEnabled = false;
            SetEditorEnabled(false);

            SetCurrentRuntimeState(initialState);
            UpdateCurrentLetterHighlight();

            InputValidationTextBlock.Text = "Animation reset to initial state.";

            UpdateStepButtons();
            UpdateAnimationButtons();
        }
        catch (Exception ex)
        {
            InputValidationTextBlock.Text = ex.Message;
        }

    }

    private void AnimationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_animationTimer != null)
        {
            _animationTimer.Interval = TimeSpan.FromMilliseconds(AnimationSpeedSlider.Value);
        }
    }

    public class HistoryEntry //klasa pomocnicza do tabeli historii 
    {
        public string StateName { get; set; } = "";
        public string Letter { get; set; } = "";
    }

    private void ClearHistory() //czysci tabele historii 
    {
        _historyEntries.Clear();
    }

    private IEnumerable<string> SplitSymbols(string label) //dzielenie labela transition
    {
        return label.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0);
    }

}

public class AutomatonFile
{
    public List<StateFile>? States { get; set; }
    public List<TransitionFile>? Transitions { get; set; }
}

public class StateFile
{
    public string? Name { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsInitial { get; set; }
    public bool IsAccepting { get; set; }
    public string? FillColor { get; set; }
    public string? EdgeColor { get; set; }
    public double Radius { get; set; }
    public double EdgeThickness { get; set; }
}

public class TransitionFile
{
    public string? Source { get; set; }
    public string? Target { get; set; }
    public string? Label { get; set; }
}