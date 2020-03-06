# FutureLoadAnalyzer (FLA)

FLA came out of a research project that analyzed the low voltage grid in the Swiss town of Burgdorf.

This is a very early version and is only useable for developers. It still contains a lot of hardcoded paths and assumptions from the research project.

The purpose for this is to serve as a foundation for future development. It is not really useable for other projects in the current form.

# License

MIT

# Assessment

About 25% of the code is really, really neat. Some of the ideas in the project worked out very well, 
such as the stepwise processing, the individual, testable steps, the built in visualisations, the Excel export functions and a lot of the heuristics for extending the present to the future.
The copy-on-write approach to the database was extremly helpful as well.

50% of the code is ok and does the job. For example the data base layer is mostly functional, the data merging for the present model did the job, but could be better. The structured logging 
is mostly ok.

25% of the code was either the result of explorative programming back when the requirements were a bit unclear, hacks or project-specific implementations and should be urgently replaced.
For example the entire building complex generation part would be much better replaced with a generic graph model of prosumers that get connected to different suppliers of energy. 
Some of the charting is pretty ugly. A lot of paths are hardcoded. A lot of assumptions are specific to the project. The entire data import is hardcoded to exactly the files 
we recieved at the beginning of the project.

# Plans

- Port to python.
- Split into a generic load profile provider and a scenario creation tool
- Add a gui
- Use all the learnings from the project to fix all the broken things and all the bad architecture decisions

# Acknowledgements

This software was developed from 2018 to 2020  at

__Berner Fachhochschule - Labor für Photovoltaik-Systeme__

Part of the Development was funded by the

__Swiss Federal Office of Energy__

This happend in the project "SimZukunft".

Currently development is supported by the

__Forschungszentrum Jülich - IEK 3__

I am very grateful for the support!