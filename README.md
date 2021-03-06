# Junit.Expecto.TestLogger

A `dotnet test --logger` designed to work with Expecto test output.

Here is an example of the XML produced by this logger:

```
<?xml version="1.0" encoding="utf-8"?>
<testsuites>
  <properties>
    <property name="clr-version" value="5.0.0" />
    <property name="os-version" value="Unix 10.15.7" />
  </properties>
  <testsuite>
    <testsuite timestamp="1/4/2021 6:38:02 PM" tests="3" hostname="Maxs-MacBook-Pro">
      <testcase classname="samples" name="I'm always fail (should fail)" time="0.007">
        <failure message="This was expected..." type="failure">
This was expected...
   at Tests.Samples.tests@21-3.Invoke(Unit _arg4) in /Users/maxpaige/git/BinaryDefense.Junit.Expecto.TestLogger/tests/Example.Tests/Sample.fs:line 22
   at Expecto.Impl.execTestAsync@692-1.Invoke(Unit unitVar)
   at Microsoft.FSharp.Control.AsyncPrimitives.CallThenInvoke[T,TResult](AsyncActivation`1 ctxt, TResult result1, FSharpFunc`2 part2) in F:\workspace\_work\1\s\src\fsharp\FSharp.Core\async.fs:line 386
   at &lt;StartupCode$FSharp-Core&gt;.$Async.StartChild@1663-5.Invoke(AsyncActivation`1 ctxt) in F:\workspace\_work\1\s\src\fsharp\FSharp.Core\async.fs:line 1663
   at Microsoft.FSharp.Control.Trampoline.Execute(FSharpFunc`2 firstAction) in F:\workspace\_work\1\s\src\fsharp\FSharp.Core\async.fs:line 105</failure>
      </testcase>
            <testcase classname="samples" name="I'm skipped (should skip)" time="0">
        <skipped message="Skipped: Yup, waiting for a sunny day..." />
      </testcase>
    </testsuite>
  </testsuite>
</testsuites>
```

## To install

Reference `BinaryDefense.Junit.Expecto.TestLogger` in your `paket.dependencies`, add it to your unit test project references, then run it during `dotnet test`:

```
dotnet test --logger:"junit"
```

## Parameters

Note that as Expecto doesn't do the usual class/method testing and handles test results differently, the common junit options for `MethodClassName` and `FailureBodyFormat` do not apply. Instead, there are different input options.


### Name Format

If you use `testList`, you may want to put the test list name in the `classname` or in the `name` of a test case.

```
NameFormat=RootList
NameFormat=AllLists
```

For RootList, the classname of the test will be the root list, or, if there is not one, the test name. The name will be all lists the test is in, separated by a '.', and the test case name.

For AllLists,  name of the test will be the test case. The classname will be all the lists the testcase is in, separated by a '/'.

Given this structure
```
testList "A TestList" [
  testList "A Nested TestList" [
    testCase "A Test" [...]
  ]
  testCase "Another Test" [...]
]
```

`RootList` would produce
```
<testcase classname="A TestList" name="A Nested Testlist/A Test" />
<testcase classname="A TestList" name="Another Test" />
```

And `AllLists` would produce
```
<testcase classname="A TestList/A Nested Testlist" name="A Test" />
<testcase classname="A TestList" name="Another Test" />
```

### LogFilePath

```
--logger:"junit;LogFilePath=<some-path>"
```

This can be a relative or absolute path to a directory or specific file. If no specific file name is given, then it will use a default file name. Be aware that this could cause issues if multiple test projects drop reports in the same directory with the default file name.

### Split On

Expecto introduced the ability to specify the delimiter for test list names, with the options of `/` and `.`. This argumente lets you specify _any_ string as the delimiter to use to split test list names.

### Keep Test Names Intact

Use Quotes `""` around any text you want to avoid splitting on. For example, if you're testing URL parsing, you may have a test name like `Parsing Tests.URL Parsing.Parses http://www.google.com`. If you split on `.` or `/` and use AllLists naming format, this test name will not split correctly. 

To avoid this, you can wrap text in quotes to escape being split. `Parsing Tests.URL Parsing.Parses "http://www.google.com"` using AllLists will split into `Parsing Tests.URL Parsing`, `Parses "http://www.google.com"`, as expected.

## Builds

GitHub Actions |
:---: |
[![GitHub Actions](https://github.com/BinaryDefense/BinaryDefense.Junit.Expecto.TestLogger/workflows/Build%20master/badge.svg)](https://github.com/BinaryDefense/BinaryDefense.Junit.Expecto.TestLogger/actions?query=branch%3Amaster) |
[![Build History](https://buildstats.info/github/chart/BinaryDefense/BinaryDefense.Junit.Expecto.TestLogger)](https://github.com/BinaryDefense/BinaryDefense.Junit.Expecto.TestLogger/actions?query=branch%3Amaster) |

## NuGet 

Package | Stable | Prerelease
--- | --- | ---
BinaryDefense.Junit.Expecto.TestLogger | [![NuGet Badge](https://buildstats.info/nuget/BinaryDefense.Junit.Expecto.TestLogger)](https://www.nuget.org/packages/BinaryDefense.Junit.Expecto.TestLogger/) | [![NuGet Badge](https://buildstats.info/nuget/BinaryDefense.Junit.Expecto.TestLogger?includePreReleases=true)](https://www.nuget.org/packages/BinaryDefense.Junit.Expecto.TestLogger/)

---

### Developing

Make sure the following **requirements** are installed on your system:

- [dotnet SDK](https://www.microsoft.com/net/download/core) 3.0 or higher

or

- [VSCode Dev Container](https://code.visualstudio.com/docs/remote/containers)


---

### Environment Variables

- `CONFIGURATION` will set the [configuration](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x#options) of the dotnet commands.  If not set, it will default to Release.
  - `CONFIGURATION=Debug ./build.sh` will result in `-c` additions to commands such as in `dotnet build -c Debug`
- `GITHUB_TOKEN` will be used to upload release notes and Nuget packages to GitHub.
  - Be sure to set this before releasing
- `DISABLE_COVERAGE` Will disable running code coverage metrics.  AltCover can have [severe performance degradation](https://github.com/SteveGilham/altcover/issues/57) so it's worth disabling when looking to do a quicker feedback loop.
  - `DISABLE_COVERAGE=1 ./build.sh`


---

### Building


```sh
> build.cmd <optional buildtarget> // on windows
$ ./build.sh  <optional buildtarget>// on unix
```

---

### Build Targets

- `Clean` - Cleans artifact and temp directories.
- `DotnetRestore` - Runs [dotnet restore](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-restore?tabs=netcore2x) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- [`DotnetBuild`](#Building) - Runs [dotnet build](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- `DotnetTest` - Runs [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=netcore21) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019).
- `ReportLocalTests` - Runs [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=netcore21) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019) with the flag enabled to generate a junit report inside the project file. This can be used to quickly check if the project still writes the correct reports.
- `AltCover` - Runs [dotnet test](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test?tabs=netcore21) on the [solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file?view=vs-2019) with flags for [altcover](https://github.com/SteveGilham/altcover) turned on. This step will fail if there is not enough test coverage.
- `GenerateCoverageReport` - Code coverage is run during `DotnetTest` and this generates a report via [ReportGenerator](https://github.com/danielpalme/ReportGenerator).
- `WatchTests` - Runs [dotnet watch](https://docs.microsoft.com/en-us/aspnet/core/tutorials/dotnet-watch?view=aspnetcore-3.0) with the test projects. Useful for rapid feedback loops.
- `GenerateAssemblyInfo` - Generates [AssemblyInfo](https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.applicationservices.assemblyinfo?view=netframework-4.8) for libraries.
- `DotnetPack` - Runs [dotnet pack](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-pack). This includes running [Source Link](https://github.com/dotnet/sourcelink).
- `SourceLinkTest` - Runs a Source Link test tool to verify Source Links were properly generated.
- `PublishToNuGet` - Publishes the NuGet packages generated in `DotnetPack` to NuGet via [paket push](https://fsprojects.github.io/Paket/paket-push.html).
- `GitRelease` - Creates a commit message with the [Release Notes](https://fake.build/apidocs/v5/fake-core-releasenotes.html) and a git tag via the version in the `Release Notes`.
- `GitHubRelease` - Publishes a [GitHub Release](https://help.github.com/en/articles/creating-releases) with the Release Notes and any NuGet packages.
- `FormatCode` - Runs [Fantomas](https://github.com/fsprojects/fantomas) on the solution file.
- `BuildDocs` - Generates Documentation from `docsSrc` and the [XML Documentation Comments](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/) from your libraries in `src`.
- `WatchDocs` - Generates documentation and starts a webserver locally.  It will rebuild and hot reload if it detects any changes made to `docsSrc` files, libraries in `src`, or the `docsTool` itself.
- `ReleaseDocs` - Will stage, commit, and push docs generated in the `BuildDocs` target.
- [`Release`](#Releasing) - Task that runs all release type tasks such as `PublishToNuGet`, `GitRelease`, `ReleaseDocs`, and `GitHubRelease`. Make sure to read [Releasing](#Releasing) to setup your environment correctly for releases.
---


### Releasing

- [Start a git repo with a remote](https://help.github.com/articles/adding-an-existing-project-to-github-using-the-command-line/)

```sh
git add .
git commit -m "Scaffold"
git remote add origin https://github.com/user/MyCoolNewLib.git
git push -u origin master
```

- [Create your NuGeT API key](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package#create-api-keys)
    - [Add your NuGet API key to paket](https://fsprojects.github.io/Paket/paket-config.html#Adding-a-NuGet-API-key)

    ```sh
    paket config add-token "https://www.nuget.org" 4003d786-cc37-4004-bfdf-c4f3e8ef9b3a
    ```

    - or set the environment variable `NUGET_TOKEN` to your key


- [Create a GitHub OAuth Token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/)
  - You can then set the environment variable `GITHUB_TOKEN` to upload release notes and artifacts to github
  - Otherwise it will fallback to username/password

- Then update the `CHANGELOG.md` with an "Unreleased" section containing release notes for this version, in [KeepAChangelog](https://keepachangelog.com/en/1.1.0/) format.

NOTE: Its highly recommend to add a link to the Pull Request next to the release note that it affects. The reason for this is when the `RELEASE` target is run, it will add these new notes into the body of git commit. GitHub will notice the links and will update the Pull Request with what commit referenced it saying ["added a commit that referenced this pull request"](https://github.com/TheAngryByrd/MiniScaffold/pull/179#ref-commit-837ad59). Since the build script automates the commit message, it will say "Bump Version to x.y.z". The benefit of this is when users goto a Pull Request, it will be clear when and which version those code changes released. Also when reading the `CHANGELOG`, if someone is curious about how or why those changes were made, they can easily discover the work and discussions.

Here's an example of adding an "Unreleased" section to a `CHANGELOG.md` with a `0.1.0` section already released.

```markdown
## [Unreleased]

### Added
- Does cool stuff!

### Fixed
- Fixes that silly oversight

## [0.1.0] - 2017-03-17
First release

### Added
- This release already has lots of features

[Unreleased]: https://github.com/user/MyCoolNewLib.git/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/user/MyCoolNewLib.git/releases/tag/v0.1.0
```

- You can then use the `Release` target, specifying the version number either in the `RELEASE_VERSION` environment
  variable, or else as a parameter after the target name.  This will:
  - update `CHANGELOG.md`, moving changes from the `Unreleased` section into a new `0.2.0` section
    - if there were any prerelease versions of 0.2.0 in the changelog, it will also collect their changes into the final 0.2.0 entry
  - make a commit bumping the version:  `Bump version to 0.2.0` and adds the new changelog section to the commit's body
  - publish the package to NuGet
  - push a git tag
  - create a GitHub release for that git tag

macOS/Linux Parameter:

```sh
./build.sh Release 0.2.0
```

macOS/Linux Environment Variable:

```sh
RELEASE_VERSION=0.2.0 ./build.sh Release
```


