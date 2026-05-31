# DFA Automaton Editor and Simulator

A WPF desktop application for designing and simulating deterministic finite automata.

The project was created as part of the **Programming in Graphical Environment** course. The main goal of the application is to provide an interactive environment where the user can create a finite automaton visually, modify its elements, save it to a file, load it again, and then simulate how the automaton processes a given input word.

## Project Description

The application combines two main functionalities: an automaton editor and a runtime simulation environment.

The editor is used to create and modify the structure of a deterministic finite automaton. The user can add states, move them on the canvas, mark them as initial or accepting, create transitions between them, and customize their visual appearance.

The runtime environment is used to simulate the automaton on a given input word. During the simulation, the application shows the current state, the processed symbol, the active transition, and the final result of the computation.

## Automaton Editor

In the editor, states are displayed as circles on the canvas. Each newly added state is automatically named using consecutive labels such as `q0`, `q1`, `q2`, and so on.

The user can select a state by clicking on it. The selected state is visually highlighted, which makes it clear which element is currently active. States can also be moved around the canvas using mouse interaction.

Each state can be marked as initial or accepting. The application ensures that the automaton has exactly one initial state. When another state is marked as initial, the previous initial state automatically loses this role. Accepting states are visually distinguished from regular states.

The editor also allows deleting states. When a state is deleted, transitions connected with that state are removed as well.

## Transitions

Transitions are used to describe how the automaton moves from one state to another after reading an input symbol.

The user can create transitions between states and assign labels to them. A transition label may contain one symbol or multiple symbols separated by commas. These labels define the alphabet used by the automaton.

The application supports regular transitions, self-loops, and transitions in both directions between two states. Transition arrows and labels are displayed on the canvas, so the structure of the automaton is easy to read. When two states have transitions in both directions, the transitions are drawn in a way that avoids overlapping.

## State Customization

The selected state can be customized using controls available in the user interface.

The user can change:

- Fill color
- Border color
- Radius
- Border thickness

These properties are updated through data binding, so changes are immediately visible on the canvas.

## Import and Export

The application allows saving and loading automata using JSON files.

An automaton can be imported from a JSON file, and the imported data is validated before being loaded into the editor. If the file contains invalid data, the application displays an appropriate error message.

The user can also export a created automaton to a JSON file. Additionally, the automaton drawing can be exported as an image, which can be useful for documentation or reports.

## Simulation

The runtime environment allows the user to test how the automaton processes an input word.

Before the simulation starts, the user enters a word. The application checks whether the word contains only symbols from the current automaton alphabet.

During the simulation, the currently processed symbol is highlighted. The current state and the active transition are also highlighted, which makes it easier to follow the computation.

After the simulation finishes, the application displays information about whether the input word was accepted or rejected by the automaton.

## Step-by-Step Mode

In step-by-step mode, the user controls the simulation manually.

The `Next` button moves the computation to the next step, while the `Previous` button returns to the previous step. This makes it possible to observe how the automaton changes its current state while processing the input word.

The application also updates the computation history during this process.

## Animation Mode

In animation mode, the automaton processes the input word automatically.

The user can start, stop, and reset the simulation. The animation speed can also be adjusted using a slider.

This mode allows observing the full computation without manually clicking through every step.

## Computation History

The application stores the computation history for the processed word.

Each history entry shows which state was active and which symbol was processed at a given step. This helps understand how the automaton reached its final result and makes the simulation easier to analyze.

## Technologies Used

- C#
- WPF
- XAML
- .NET
- JSON serialization
- Data Binding
- ObservableCollection
- INotifyPropertyChanged
