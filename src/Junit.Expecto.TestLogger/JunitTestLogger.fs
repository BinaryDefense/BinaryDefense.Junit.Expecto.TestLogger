namespace Microsoft.VisualStudio.TestPlatform.Extension.Junit.Expecto.TestLogger

open Junit.Expecto.TestLogger
open Junit.Expecto.TestLogger.TestReportBuilder
open Junit.Expecto.TestLogger.Parameters
open Junit.Expecto.TestLogger.Xml
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open System

[<FriendlyName(Constants.FriendlyName)>]
[<ExtensionUri(Constants.ExtensionUri)>]
type JunitTestLogger() =
  let printTestCase (tc : TestCase) =
    //let filterManaged (l : TestProperty seq) = Seq.filter (fun x -> x.Contains("Managed")) l
    let printTestProps (tp : TestProperty seq) =
      tp |> Seq.iter (fun x -> 
        printfn "TestProperty: %s\n  %A" (string x) (x)
      )
    // printfn
    //   "  Properties: %A\nFQN: %A\n  DisplayName: %A\n  Uri: %A\n  Source: %A\n  Code File Path: %A\n  LineNumber: %A\n"
    //   (tc.Properties :> seq<_>)
    //   tc.FullyQualifiedName
    //   tc.DisplayName
    //   (string tc.ExecutorUri)
    //   tc.Source
    //   tc.CodeFilePath
    //   tc.LineNumber
    printTestProps tc.Properties
    ()

  let mutable testResults = ResizeArray<TestResult>()
  let mutable testReports = ResizeArray<Junit.Expecto.TestLogger.TestReportBuilder.TestReport>()

  let mutable _parameters = Parameters.Empty()
  member this.Parameters 
    with get() = _parameters
    and set(value) = _parameters <- value

  member internal this.TestRunMessageHandler(e : TestRunMessageEventArgs) =
    ()

  member internal this.TestRunStartHandler(e: TestRunStartEventArgs) =
    ()

  member internal this.TestResultHandler(e : TestResultEventArgs) =
    // printfn 
    //   "Test result:\n  displayName: %s\n  outcome: %s\n  result messages: %A\n  Error msg: %A\n  FirstLine: %A\n" 
    //   e.Result.DisplayName 
    //   (string e.Result.Outcome) 
    //   (e.Result.Messages)
    //   (e.Result.ErrorMessage)
    //   (e.Result.ErrorMessage.Split("\n").[1])
    // printTestCase e.Result.TestCase
    testResults.Add(e.Result)
    ()

  member internal this.TestRunCompleteHandler(e : TestRunCompleteEventArgs) =
    try
      let reports : TestReport array = 
        if (isNull testResults) || testResults.Count = 0 then 
          printfn "Report list contains 0 tests results."
          [||]
        else
          testResults.ToArray() |> Array.map (buildTestReport this.Parameters)
      let suites = XmlBuilder.buildSuite (this.Parameters) reports
      let doc = XmlBuilder.buildDocument this.Parameters suites
      printfn "\n%s" (XmlWriter.writeXmlFile this.Parameters doc)
    with
    | ex ->
      Console.WriteLine($"Error writing junit report: {ex.Message}\n{ex.StackTrace}")
    ()

  member internal this.InitializeImpl (events: TestLoggerEvents) (outputPath : string) =
    events.TestRunComplete.Add(this.TestRunCompleteHandler)
    events.TestResult.Add(this.TestResultHandler)
    events.TestRunMessage.Add(this.TestRunMessageHandler)
    events.TestRunStart.Add(this.TestRunStartHandler)
    this.Parameters <- Parameters.parseParameters this.Parameters
    ()

  interface ITestLoggerWithParameters with

    /// <summary>
    /// Initializes the Test Logger with given parameters.
    /// </summary>
    /// <param name="events">Events that can be registered for.</param>
    /// <param name="testResultsDirPath">The path to the directory to output the test results.</param>
    member this.Initialize(events : TestLoggerEvents, testResultsDirPath : string) =
      Console.WriteLine($"Initializing Junit Expecto logger with the test result dir path of {testResultsDirPath}.")
      let paramsMap =
        Map.ofSeq ["TestResultsDirPath", testResultsDirPath]
      let outputFilePath = System.IO.Path.Combine(testResultsDirPath, Constants.DefaultFileName) |> System.IO.Path.GetFullPath
      this.Parameters <- { this.Parameters with InputParameters = paramsMap; OutputFilePath = outputFilePath }
      ()

    /// <summary>
    /// Initializes the Test Logger with given parameters.
    /// </summary>
    /// <param name="events">Events that can be registered for.</param>
    /// <param name="parameters">Collection of parameters</param>
    member this.Initialize(events : TestLoggerEvents, parameters : System.Collections.Generic.Dictionary<string, string>) =
      Console.WriteLine($"Initializing Junit Expecto logger with multiple parameters.")
      let paramsList = parameters :> seq<_> |> Seq.map (|KeyValue|)

      printf "\n"
      paramsList
      |> Seq.iter (fun (k, v) -> Console.WriteLine($"parameter: ({k}, {v})\n"))

      let paramsMap =
        paramsList
        |> Seq.map (fun (x, y) -> (x.ToLowerInvariant(), y))
        |> Map.ofSeq
      this.Parameters <- { this.Parameters with InputParameters = paramsMap}

      this.InitializeImpl events ""
      ()

