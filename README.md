# DFA Automaton Editor and Simulator

A WPF desktop application for designing and simulating deterministic finite automata, created as part of the **Programming in Graphical Environment** course.

The application allows the user to visually build an automaton, edit its states and transitions, save and load it from a JSON file, and simulate how it processes an input word.

## Features

- Creating and editing states on a canvas
- Moving states using mouse interaction
- Marking states as initial or accepting
- Creating labeled transitions
- Supporting self-loops and bidirectional transitions
- Customizing state appearance
- Importing and exporting automata as JSON files
- Exporting the automaton drawing as an image
- Simulating the automaton step by step or automatically
- Highlighting the current state, processed symbol, and active transition
- Displaying computation history and final result

## State Customization

For the selected state, the user can modify:

- Fill color
- Border color
- Radius
- Border thickness

## Transitions

Transitions define how the automaton moves between states after reading input symbols. Each transition can have a label with one or more symbols separated by commas.

The application supports regular transitions, self-loops, and transitions in both directions between two states. Arrows and labels are displayed on the canvas to make the automaton structure clear.

## Simulation

The user can enter an input word and run the automaton simulation. The application validates the word using the automaton alphabet and then shows the computation process.

The simulation can be performed manually using `Next` and `Previous` buttons or automatically in animation mode with adjustable speed. After the computation finishes, the application displays whether the word was accepted or rejected.

