### Build script

Implemented using [FAKE](http://fsharp.github.io/FAKE/).

### Targets

Main (composite) targets are:
- Clean: cleans outputs, run by clean.bat
- Build: build all projects, except tests
- Tests: build tests and run them (both NUnit and MSTest)
- All: all above, default one, run by build.bat
- Release: all above + creates NuGet packages and publish it if access key is specified, run by publish.bat

If one of the target fails then all build (including all next targets) fails.
For example, when run 'All' target (by build.bat) and build fails, then tests would not be run.
Or if you run 'Release' target (by publish.bat) and some test fails, then NuGet packages would not be created and published.

### NuGet packages

NuGet access key should be specified in order to publish packages.
It could be achieved in two ways:
- Set in build.fsx (line 17, commit 091fea9e6d4515fcb69e14d4cd910a5b17393bb0):
```
	let nugetAccessKey = "somevalidaccesskey"
```
- Pass via cmd parameter in publish.bat (line 6, commit 091fea9e6d4515fcb69e14d4cd910a5b17393bb0):
```
	build.fsx %* Release  "nugetkey=somevalidaccesskey"
```

If NuGet access key is not specified, packages would be created, but not published.
