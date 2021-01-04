namespace Junit.Expecto.TestLogger

module TestReportBuilder =
  open Junit.Expecto.TestLogger.Parameters
  open Microsoft.VisualStudio.TestPlatform.ObjectModel
  open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
  open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

  type TestOutcome =
  | Passed
  | Failed of msgSummary : string * msg : string
  /// error tests are returned the same as failed tests.
  //| Errored of msgSummary : string * msg : string
  | Skipped of msg : string
  /// Covers unusual enum results that may be returned from the tests
  | NoOutcome

  type TestReport = {
    TestOutcome : TestOutcome
    TestName : string
    ClassName : string
    /// The timespan for how long it took this test to run.
    Duration : System.TimeSpan
    /// The source assembly for this test. "/some/path/to/Tests.dll"
    Source : string
    /// The file path for this test. "/some/path/to/tests/MyTestFile.fs"
    CodeFilePath : string
  } with
      static member Empty() = {
        TestOutcome = Passed
        TestName = ""
        ClassName = ""
        Duration = System.TimeSpan()
        Source = ""
        CodeFilePath = ""
      }

  /// Splits a test into classname, name
  let splitClassName (nf : NameFormat) (name : string) =
    let firstSlash = name.IndexOf("/")
    let lastSlash = name.LastIndexOf("/")
    let maybeSplit i =
      if i > 0 then
        name.Substring(0, i), name.Substring(i + 1)
      else 
        name, name

    match nf with
    | NameFormat.RootList -> maybeSplit firstSlash
    | NameFormat.AllLists -> maybeSplit lastSlash
    // | TestCase -> maybeSplit lastSlash
    // | List -> "", name
    // | NoList ->
    //   let name = maybeSplit lastSlash |> snd
    //   "", name

  let parseTestOutcome (test : TestResult) =
    let tryFirstTestLine() =
      let split = test.ErrorMessage.Split("\n")
      //the first line of the message is always a blank line. the 2nd line contains the unit test message
      if split.Length >= 2 then split.[1] else ""
    
    let tryFirstMessage() =
      match Seq.tryHead test.Messages with
      | Some x -> x.Text
      | None -> ""
    match test.Outcome with
    | Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed -> TestOutcome.Passed
    | Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed -> TestOutcome.Failed (tryFirstTestLine(), test.ErrorMessage)
    | Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped -> TestOutcome.Skipped (tryFirstMessage())
    // covers cases of None & NotFound, plus any other enum values that shouldn't happen.
    | _ -> TestOutcome.NoOutcome


  let buildTestReport (parameters: Parameters) (test : TestResult) =
    let outcome = parseTestOutcome test
    let classname, name = splitClassName parameters.NameFormat test.TestCase.DisplayName
    { TestReport.Empty() with
        ClassName = classname
        TestName = name
        TestOutcome = outcome
        Duration = test.Duration
        Source = test.TestCase.Source
        CodeFilePath = test.TestCase.CodeFilePath
    }