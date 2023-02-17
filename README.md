# Canyala.Mercury
Canyala.Mercury is a .NET implementation of RDF with SPARQL, Terse TURTLE and RDF GRAPH DB implementations.
It began as an experiment to find out if it was possible to declare BNF in C# with an internal DSL, which it was, as an alternative to bloated lexer and parser implementations.
The same basic Parser base class is used to implement both terse turtle and sparql engines.
An other interesting candidate for a Mercury Parser based implementation is Lua script.

Mercury is the roman name of the greek god hermes, who has winged feet and delivers information. Mercury was also the name of the initial NASA program.

## 2023-02-17
- Builds OK, all tests OK, moved sparql production lamdbas into an inner private Productions class to create a scope for the production rules.

## 2022-12-31
- Builds OK, including access to internal for test projects.

## 2022-12-24
- This is a port in progress from an early (2012) proprietary and experimental .NET Framework version to .NET 7.
- It does not build yet.


## Build preparations
Run the command below in the solution repository.

$ git submodule update --init --recursive

Canyala.Lagoon
Canyala.Test

Canyala.Mercury
Canyala.Mercury.Rdf
Canyala.Mercury.Storage
Canyala.Mercury.Test


##grab latest commits from server
git submodule update --recursive --remote

##above command will set current branch to detached HEAD. set back to main.
git submodule foreach git checkout main

##now do pull to fast-forward to latest commit
git submodule foreach git pull origin main