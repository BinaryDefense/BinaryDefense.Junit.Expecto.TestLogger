namespace BinaryDefense.Junit.Expecto.TestLogger

open BinaryDefense.Junit.Expecto.TestLogger
open BinaryDefense.Junit.Expecto.TestLogger.TestReportBuilder
open BinaryDefense.Junit.Expecto.TestLogger.Parameters
open BinaryDefense.Junit.Expecto.TestLogger.Xml
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging
open System

[<FriendlyName(Constants.FriendlyName)>]
[<ExtensionUri(Constants.ExtensionUri)>]
type JunitTestLogger() =

  let mutable testResults = ResizeArray<TestResult>()

  let mutable _parameters = Parameters.Empty()
  member this.Parameters 
    with get() = _parameters
    and set(value) = _parameters <- value

  member internal this.TestRunMessageHandler(e : TestRunMessageEventArgs) =
    ()

  member internal this.TestRunStartHandler(e: TestRunStartEventArgs) =
    ()

  member internal this.TestResultHandler(e : TestResultEventArgs) =
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
      if Array.isEmpty reports |> not then
        _parameters <- TestReportBuilder.replaceAssemblyName this.Parameters (Array.head reports)

      let suites = XmlBuilder.buildSuite (this.Parameters) reports
      let doc = XmlBuilder.buildDocument this.Parameters suites
      printfn "\n%s" (XmlWriter.writeXmlFile this.Parameters doc)
    with
    | ex ->
      printfn $"Error writing junit report: {ex.Message}\n{ex.StackTrace}"
    ()

  member internal this.InitializeImpl (events: TestLoggerEvents) =
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
      printfn $"Initializing Junit Expecto logger with the test result dir path of {testResultsDirPath}."
      let paramsMap =
        Map.ofSeq [Constants.LogFilePath, testResultsDirPath]
      this.Parameters <- { this.Parameters with InputParameters = paramsMap }
      this.InitializeImpl events
      ()

    /// <summary>
    /// Initializes the Test Logger with given parameters.
    /// </summary>
    /// <param name="events">Events that can be registered for.</param>
    /// <param name="parameters">Collection of parameters</param>
    member this.Initialize(events : TestLoggerEvents, parameters : System.Collections.Generic.Dictionary<string, string>) =
      printfn $"Initializing Junit Expecto logger with multiple parameters."
      let paramsList = parameters :> seq<_> |> Seq.map (|KeyValue|)

      paramsList
      |> Seq.iter (fun (k, v) -> printfn $"parameter: ({k}, {v})")

      let paramsMap =
        paramsList
        |> Seq.map (fun (x, y) -> (x.ToLowerInvariant(), y))
        |> Map.ofSeq

      this.Parameters <- { this.Parameters with InputParameters = paramsMap}

      this.InitializeImpl events
      ()

