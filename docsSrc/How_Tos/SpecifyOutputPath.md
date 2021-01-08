# How To Specify File Name Or Path

The logger accepts a parameter of `LogFilePath`, which can be either a directory or a file name. It will expect a file name to end in `.xml` and, if not, will append the default file name (`junit-test-results.xml`) to the path.

Pass in the argument like this:
```bash
dotnet test --logger:"junit;LogFilePath=./my/directory"
```

Note that you need to specify the local directory via `./` or it will assume it's an absolute path. You do not need to end your directory with a `/`; since this path does not end in `.xml`, it will create the report in that directory with the default file name.

Specify a file with:
```bash
dotnet test --logger:"junit;LogFilePath=../../myDirectory/junit-report.xml"
```

