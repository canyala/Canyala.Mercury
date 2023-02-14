# Canyala.Mercury
The Canyala.Mercury .NET Base Class Libraries

## 2023-01-21
- 

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