# DFA Automaton Editor and Simulator

A WPF desktop application for designing and simulating deterministic finite automata.

The project was created as part of the **Programming in Graphical Environment** course. The main goal of the application is to provide an interactive environment where the user can create a finite automaton visually, modify its elements, save it to a file, load it again, and then simulate how the automaton processes a given input word.

## Project Description

The application combines two main functionalities: an automaton editor and a runtime simulation environment.

In the editor, the user can build an automaton directly on the canvas. States are displayed as circles and can be added, selected, moved, edited, and deleted. Each state can be marked as initial or accepting. The application ensures that a valid automaton has exactly one initial state.

Transitions are created between states and can contain labels representing input symbols. The application supports normal transitions, transitions in both directions between two states, and self-loops. Transition arrows and labels are displayed on the canvas, so the structure of the automaton is easy to understand visually.

The editor also allows the user to customize the appearance of states. For the currently selected state, it is possible to change properties such as fill color, border color, radius, and border thickness. These changes are immediately visible in the graphical representation of the automaton.

The application supports importing automata from JSON files and exporting created automata back to JSON files. This makes it possible to save the current work and continue it later. The automaton can also be exported as an image, which can be useful for documentation or reports.

The runtime environment is used to simulate the behavior of a deterministic finite automaton. The user enters an input word, and the application checks whether the word can be processed using the automaton's alphabet. During the simulation, the current state, the processed symbol, and the active transition are highlighted. After the simulation finishes, the user receives information about whether the word was accepted or rejected.

The simulation can be performed manually in step-by-step mode or automatically in animation mode. In step-by-step mode, the user can move forward and backward through the computation using the `Next` and `Previous` buttons. In animation mode, the computation is performed automatically, and the animation speed can be adjusted.

The application also stores the computation history, showing which state was active for each processed symbol. This makes it easier to understand how the automaton reached its final result.

## Technologies Used

- C#
- WPF
- XAML
- .NET
- JSON serialization
- Data Binding
- ObservableCollection
- INotifyPropertyChanged
