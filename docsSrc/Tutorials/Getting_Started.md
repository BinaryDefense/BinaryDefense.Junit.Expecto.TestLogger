# Getting Started

First, make sure you are using Expecto for your unit tests.

Install the package:
```bash
paket install BinaryDefense.Junit.Expecto.TestLogger
```

And update your test project to use it as a reference. Then to run the test logger:

```bash
dotnet test --logger:"junit"
```

This will output an xml file in the same directory as the test project named `junit-test-results.xml`. This xml file should have the correct format for Gitlab to parse it into a test report.

## Here is the path to downloading

    [lang=bash]
    paket install BinaryDefense.Junit.Expecto.TestLogger


