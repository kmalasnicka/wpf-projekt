# DFA Automaton Editor and Simulator

A WPF desktop application for designing and simulating deterministic finite automata.

The project was created as part of the **Programming in Graphical Environment** course. The application allows the user to create a finite automaton visually, edit its elements, save and load it from a file, and simulate how it processes an input word.

## Project Description

The application consists of two main parts: an automaton editor and a runtime simulation environment.

The editor is used to build the automaton on a canvas. The user can add states, move them, mark them as initial or accepting, create transitions between them, and customize their appearance.

The runtime environment is used to test the automaton on a given input word. During the simulation, the application highlights the current state, processed symbol, and active transition. At the end, it shows whether the word was accepted or rejected.

## Automaton Editor

States are displayed as circles and are automatically named as `q0`, `q1`, `q2`, etc.

The user can select a state, move it on the canvas, mark it as initial or accepting, and delete it. The application keeps exactly one initial state, so when a new state is marked as initial, the previous one is unmarked.

Accepting states and the currently selected state are visually highlighted.

## Transitions

Transitions define how the automaton moves between states after reading input symbols.

The user can create transitions between states and assign labels to them. Labels may contain one symbol or multiple symbols separated by commas.

The application supports regular transitions, self-loops, and transitions in both directions between two states. Arrows and labels are displayed on the canvas to make the automaton easier to read.

## State Customization

For the selected state, the user can modify:

- Fill color
- Border color
- Radius
- Border thickness

The changes are immediately visible on the canvas.

## Import and Export

The application supports importing and exporting automata using JSON files.

Imported files are validated before loading. The user can also export the automaton drawing as an image.

## Simulation

The user enters an input word, and the application checks whether it contains only symbols from the automaton alphabet.

During simulation, the current state, processed symbol, and active transition are highlighted. After the computation finishes, the application displays whether the word was accepted or rejected.

## Step-by-Step Mode

In step-by-step mode, the user controls the simulation manually.

The `Next` button moves to the next step, and the `Previous` button returns to the previous step. The computation history is updated together with the simulation progress.

## Animation Mode

In animation mode, the automaton processes the word automatically.

The user can start, stop, reset the simulation, and adjust the animation speed using a slider.

## Computation History

The application displays the computation history for the processed word.

Each entry shows the active state and the processed symbol, which makes it easier to follow the automaton's behavior.

## Technologies Used

- C#
- WPF
- XAML
- .NET
- JSON serialization
- Data Binding
- ObservableCollection
- INotifyPropertyChanged
