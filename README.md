# DFA Automaton Editor and Simulator

A WPF desktop application for designing and simulating deterministic finite automata, created as part of the **Programming in Graphical Environment** course. The application allows the user to build an automaton visually, edit its elements, save and load it from a file, and simulate how it processes an input word.

## Project Description

The application consists of an automaton editor and a runtime simulation environment.
In the editor, the user can create states, move them on the canvas, mark them as initial or accepting, create transitions, and customize the appearance of states.
The runtime environment allows testing the automaton on an input word. During simulation, the current state, processed symbol, and active transition are highlighted. After the computation finishes, the application shows whether the word was accepted or rejected.

## Automaton Editor

States are displayed as circles and are automatically named as `q0`, `q1`, `q2`, etc.
The user can select, move, edit, and delete states. A state can also be marked as initial or accepting.
The application keeps exactly one initial state. When another state is marked as initial, the previous one is unmarked automatically.

## Transitions

Transitions describe moves between states for specific input symbols.
The user can create transitions between states and assign labels to them. Labels may contain one symbol or multiple symbols separated by commas.
The application supports regular transitions, self-loops, and transitions in both directions between two states. Arrows and labels are displayed on the canvas.

## State Customization

For the selected state, the user can modify:

- Fill color
- Border color
- Radius
- Border thickness

The changes are immediately visible on the canvas.

## Import and Export

The application supports importing and exporting automata using JSON files.
Imported files are validated before loading. The automaton drawing can also be exported as an image.

## Simulation

The user enters an input word, and the application checks whether it contains only symbols from the automaton alphabet.
The simulation can be performed step by step using `Next` and `Previous` buttons or automatically in animation mode.
In animation mode, the user can start, stop, reset the simulation, and adjust the animation speed.

## Computation History

The application displays the computation history for the processed word.
Each entry shows the active state and the processed symbol, which makes it easier to follow the automaton's behavior.
