namespace Microsoft.VisualStudio.TestPlatform.Extension.Junit.Expecto.TestLogger

open System
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

type Report = {
  Tests : TestResult list
} with
    static member Empty() = {
      Tests = []
    }

module Constants =
  
  /// The path and file relative to the test project.
  [<Literal>]
  let LogFilePath = "LogFilePath"

  /// <summary>
  /// Uri used to uniquely identify the logger.
  /// </summary>
  [<Literal>]
  let ExtensionUri = "logger://Microsoft/TestPlatform/JunitExpectoLogger/v1"

  /// <summary>
  /// User friendly name to uniquely identify the console logger
  /// </summary>
  [<Literal>]
  let FriendlyName = "junit"

  /// If the user just does "logger:junit", this will be the file name
  [<Literal>]
  let DefaultFileName = "test-results.xml"

type Parameters = {
  /// The raw user input parameters
  InputParameters : Map<string, string>

  // /// The directory the output file will go to.
  // OutputDir : string

  // // The name of the file to write the report to.
  // FileName : string

  /// The absolute path to write the report to.
  OutputFilePath : string

  /// Any unrecognized parameters from the user input
  UnrecognizedParameters : (string * string) list
} with
    static member Empty() = {
      InputParameters = Map []
      // OutputDir = ""
      // FileName = ""
      OutputFilePath = ""
      UnrecognizedParameters = []
    }

    member this.TryGetInput (key : string) =
      match this.InputParameters.TryGetValue(key.ToLowerInvariant()) with
      | true, value -> Some value
      | false, _ -> None

module Parameters =
  let tryGetValue (key : string) (map : Map<string, string>) =
    match map.TryGetValue (key.ToLowerInvariant()) with
    | true, value -> Some value
    | false, _ -> None

  /// Given a possible user-entered value, construct a full file path.
  let buildFilePath (filePathOption: string option) =
    let filePathOption = 
      filePathOption
      |> Option.map (fun x -> if (x.EndsWith ".xml" |> not) then x + Constants.DefaultFileName else x)
      |> Option.map System.IO.Path.GetFullPath

    match filePathOption with
    | Some x -> x
    | None -> System.IO.Path.GetFullPath("./" + Constants.DefaultFileName)

  let parseParameters (inputParameters : Parameters) =
    let tryGet key = tryGetValue key inputParameters.InputParameters
    let filePath = 
      tryGet Constants.LogFilePath 
      |> buildFilePath

    { inputParameters with
        OutputFilePath = filePath
    }


module XmlBuilder =
  open System.Xml
  open System.Xml.Linq

  let inline private xAttr name data = XAttribute(XName.Get name, data)

  let inline private xProperty (name: string) value =
    let value' = if isNull value then "no value" else value
    XElement(
      XName.Get "property", 
      [|
        xAttr "name" name
        xAttr "value" value'
      |]
    ) :> XObject

  let private writeParameterProperties (args : Map<string, string>) =
    args
    |> Map.toSeq
    |> Seq.map (fun (x, y) -> xProperty x y)

  let buildProperties (args: Map<string, string>) =
    XElement(
      XName.Get "properties", [|
        xProperty "clr-version" (Environment.Version.ToString())
        xProperty "os-version" Environment.OSVersion.VersionString
        xProperty "platform" (Environment.OSVersion.Platform.ToString())
        xProperty "cwd" Environment.CurrentDirectory
        xProperty "machine-name" Environment.MachineName
        xProperty "user" Environment.UserName
        xProperty "user-domain" Environment.UserDomainName
        yield! writeParameterProperties args
      |]
    )

  let buildTestCase (test : TestResult) =
    let content: XObject[] =
      let makeMessageNode messageType (message: string) =
        XElement(
          XName.Get messageType,
          xAttr "message" message
        )
      match test.Outcome with
      | TestOutcome.None -> [||]
      | TestOutcome.Passed -> [||]
      | TestOutcome.Failed -> 
          let msg = test.ErrorMessage
          let msgBlock = test.ErrorStackTrace
          [|
            XElement(
              XName.Get "failure", [|
                xAttr "message" msg :> XObject
                XText $"{msg}\n{msgBlock}" :> XObject
              |]
            )
          |]
      | TestOutcome.Skipped -> [|
            XElement(
              XName.Get "skipped"
            )
        |]
      | TestOutcome.NotFound -> [||]
    
    XElement(XName.Get "testcase",
        [|
          // yield (xAttr "classname" className) :> XObject
          yield (xAttr "name" test.DisplayName) :> XObject
          yield (xAttr "time" test.Duration.TotalSeconds) :> XObject
          yield! content
        |]) :> XObject

  let buildSuite (reports : TestResult array) =
    XElement(XName.Get "testsuite",
      [|
        // yield (xAttr "id" assemblyName) :> XObject
        // yield (xAttr "name" assemblyName) :> XObject
        // yield (xAttr "package" assemblyName) :> XObject
        yield (xAttr "timestamp" (DateTime.UtcNow.ToString())) :> XObject
        yield (xAttr "tests" (Seq.length reports)) :> XObject
        // yield (xAttr "skipped" (Seq.length summary.ignored)) :> XObject
        // yield (xAttr "failures" (Seq.length summary.failed)) :> XObject
        // yield (xAttr "errors" (Seq.length summary.errored)) :> XObject
        // yield (xAttr "time" (time summary.duration.TotalSeconds)) :> XObject
        yield (xAttr "hostname" Environment.UserDomainName) :> XObject
        //yield properties :> XObject
        yield! (reports |> Seq.map buildTestCase)
      |])

  let buildDocument (args: Map<string, string>) (testSuite : XElement) =
    let properties = buildProperties args
    let emptyTestSuites =
      XElement(
        XName.Get "testsuites", [|
          properties,
          XElement(
            XName.Get "testsuite", [|
              testSuite
            |]
          )
        |]
      )
    let doc = XDocument(emptyTestSuites)
    doc


module XmlWriter =
  open System
  open System.Globalization
  open System.Xml
  open System.Xml.Linq
  open System.IO

  let private checkDir (path : string) =
    //create directory if it doesn't exist
    let dirPath = Path.GetDirectoryName(path)
    match Directory.Exists dirPath with
    | false -> Directory.CreateDirectory(dirPath)
    | true -> DirectoryInfo dirPath

  let writeXmlFile (parameters: Parameters) (doc: XDocument) =
    let writerSettings = XmlWriterSettings()
    writerSettings.Encoding <- System.Text.UTF8Encoding()
    writerSettings.Indent <- true

    let path = parameters.OutputFilePath
    checkDir path |> ignore

    use file = File.Create(path)
    use writer = XmlWriter.Create(file, writerSettings)
    doc.Save(writer)

    let resultsFileMessage = String.Format(CultureInfo.CurrentCulture, "JunitXML Logger - Results File: {0}", parameters.OutputFilePath)
    Console.WriteLine(Environment.NewLine + resultsFileMessage)

[<FriendlyName(Constants.FriendlyName)>]
[<ExtensionUri(Constants.ExtensionUri)>]
type JunitTestLogger() =
  
  let mutable report = Report.Empty()
  let mutable reportList = ResizeArray<TestResult>()

  let mutable _parameters = Parameters.Empty()
  member this.Parameters with
      get() = _parameters
      and set(value) = _parameters <- value

  member internal this.TestRunMessageHandler(e : TestRunMessageEventArgs) =
    Console.WriteLine($"Got test run event of {e.ToString()}")
    ()

  member internal this.TestRunStartHandler(e: TestRunStartEventArgs) =
    Console.WriteLine($"Got test run start event: {e.ToString()}")
    ()

  member internal this.TestResultHandler(e : TestResultEventArgs) =
    //Console.WriteLine($"Got test result event of {e.ToString()}")
    reportList.Add(e.Result)
    ()

  member internal this.TestRunCompleteHandler(e : TestRunCompleteEventArgs) =
    try
      if (isNull reportList) || reportList.Count = 0 then printfn "Report list contains 0 tests results."
      let suites = XmlBuilder.buildSuite (reportList.ToArray())
      let doc = XmlBuilder.buildDocument this.Parameters.InputParameters suites
      XmlWriter.writeXmlFile this.Parameters doc
    with
    | ex ->
      Console.WriteLine($"Error writing junit report: {ex.Message}\n{ex.StackTrace}")
    ()

  member internal this.InitializeImpl (events: TestLoggerEvents) (outputPath : string) =
    //events.TestRunMessage += this.TestMessageHandler
    events.TestRunComplete.Add(this.TestRunCompleteHandler)
    events.TestResult.Add(this.TestResultHandler)
    events.TestRunMessage.Add(this.TestRunMessageHandler)
    events.TestRunStart.Add(this.TestRunStartHandler)
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

