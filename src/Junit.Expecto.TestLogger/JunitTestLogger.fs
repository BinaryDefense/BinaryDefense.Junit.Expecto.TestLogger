namespace Microsoft.VisualStudio.TestPlatform.Extension.Junit.Expecto.TestLogger

open System
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

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

type Parameters = {
  /// The raw user input parameters
  InputParameters : Map<string, string>

  /// The directory the output file will go to.
  OutputDir : string

  // The name of the file to write the report to.
  FileName : string

  /// The absolute path to write the report to.
  OutputFilePath : string

  /// Any unrecognized parameters from the user input
  UnrecognizedParameters : (string * string) list
} with
    static member Empty() = {
      InputParameters = Map []
      OutputDir = ""
      FileName = ""
      OutputFilePath = ""
      UnrecognizedParameters = []
    }

module Parameters =

  let tryGetValue (key : string) (map : Map<string, string>) =
    match map.TryGetValue (key.ToLowerInvariant()) with
    | true, value -> Some value
    | false, _ -> None

  let parseParameters (inputParameters : Parameters) =
    let tryGet key = tryGetValue key inputParameters.InputParameters
    let filePath = tryGet Constants.LogFilePath

    { inputParameters with
        OutputFilePath = Option.defaultValue "./" filePath
    }

module XmlBuilder =
  open System.Xml
  open System.Xml.Linq

  let inline private xAttr name data = XAttribute(XName.Get name, data)

  let inline private xProperty (name: string) value =
    XElement(XName.Get "property", [|
      xAttr "name" name
      xAttr "value" value
    |]) :> XObject

  // let aggregateTestSuites suites = []

  let private writeParameterProperties (args : Map<string, string>) =
    args
    |> Map.toSeq
    |> Seq.map (fun (x, y) -> xProperty x y)

  let buildProperties (args: Map<string, string>) =
    XElement(
      XName.Get "properties", [|
        xProperty "clr-version" Environment.Version
        xProperty "os-version" Environment.OSVersion.VersionString
        xProperty "platform" Environment.OSVersion.Platform
        xProperty "cwd" Environment.CurrentDirectory
        xProperty "machine-name" Environment.MachineName
        xProperty "user" Environment.UserName
        xProperty "user-domain" Environment.UserDomainName
        yield! writeParameterProperties args
      |]
    )

  let buildDocument (args: Map<string, string>) (testSuiteList : XElement list) =
    let properties = buildProperties args
    let emptyTestSuites =
      XElement(
        XName.Get "testsuites", [|
          properties,
          XElement(
            XName.Get "testsuite", [||]
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

  let mutable _parameters = Parameters.Empty()
  member this.Parameters with
    get() = _parameters
    and set(value) = _parameters <- value

  member internal this.TestRunCompleteHandler(e : TestRunCompleteEventArgs) =
    let doc = XmlBuilder.buildDocument this.Parameters.InputParameters []
    XmlWriter.writeXmlFile this.Parameters doc
    ()

  member internal this.InitializeImpl (events: TestLoggerEvents) (outputPath : string) =
    //events.TestRunMessage += this.TestMessageHandler
    events.TestRunComplete.Add(this.TestRunCompleteHandler)
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
      let outputFilePath = System.IO.Path.Combine(testResultsDirPath, "test-results.xml") |> System.IO.Path.GetFullPath
      this.Parameters <- { this.Parameters with InputParameters = paramsMap; OutputFilePath = outputFilePath }
      ()

    /// <summary>
    /// Initializes the Test Logger with given parameters.
    /// </summary>
    /// <param name="events">Events that can be registered for.</param>
    /// <param name="parameters">Collection of parameters</param>
    member this.Initialize(events : TestLoggerEvents, parameters : System.Collections.Generic.Dictionary<string, string>) =
      Console.WriteLine($"Initializing Junit Expecto logger with multiple parameters.")
      let paramsMap =
        (parameters :> seq<_>)
        |> Seq.map (|KeyValue|)
        |> Seq.map (fun (x, y) -> (x.ToLowerInvariant(), y))
        |> Map.ofSeq
      this.Parameters <- { this.Parameters with InputParameters = paramsMap}
      ()

