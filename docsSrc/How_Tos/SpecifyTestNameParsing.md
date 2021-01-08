# How To Specify Test Name Parsing

There are 2 options for parsing test names:
1. The delimiter used to split test list names
2. Putting the test list names in the `classname` or the `name` of the testcase.

## Split On

Expecto uses `.` by default to split test list names:

`My Test List.My Nested Test List.My Test Case`


But it can be configured to use `/`. By default, the test logger will split that test name on `.`, but you can specify any string to be the delimiter. 

Specify the delimiter using `SplitOn`:

    [lang=bash]
    dotnet test --logger:"junit;SplitOn=."


## Name Format

Junit reports have 2 fields in `testcase`:

    [lang=xml]
    <testcase classname="Test Result Builder tests.Split Class Name tests" name="AllLists-No Nesting" time="0.028" />


`classname` will populate the `Suite` column in Gitlab's test reports, while `name` will be the test name. When the test is not in a list, both values will be the name of the test. When a test is contained in a single list, then the classname will be the list name and the name will be the test name. 

When a test is in multiple lists, like this:

    [lang=fsharp]
    open Expecto
    let tests =
        testList "Root List" [
            testList "More tests" [
                testCase "A test" <| fun _ -> ()
            ]
            testCase "Another test" <| fun _ -> ()
        ]


The logger will need to decide where to put `More tests` - is that part of the test name, or part of the suite name? This is configurable by the `NameFormat` argument, with options of `RootList` or `AllLists`.

`RootList` will put only the root list as the Suite name:

    [lang=xml]
    <testcase classname="Root List" name="More Tests.A test" time="0.028" />


`AllLists` will put all lists in the Suite name:

    [lang=xml]
    <testcase classname="Root List.More Tests" name="A test" time="0.028" />

