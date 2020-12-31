namespace Expecto.Junit.TestLogger

open System
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

type Parameters = {
  /// The raw user input parameters
  InputParameters : Map<string, string>

  /// The file path to write the report to
  OutputFilePath : string

  /// Any unrecognized parameters from the user input
  UnrecognizedParameters : (string * string) list
} with
    static member Empty() = {
      InputParameters = Map []
      OutputFilePath = "./junit-report.xml"
      UnrecognizedParameters = []
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

  let writeProperties (args: Map<string, string>) =
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
    let properties = writeProperties args
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

  let writeXml (parameters: Parameters) (doc: XDocument) =
    //used to create directory if needed
    // let loggerFileDirPath = Path.GetDirectoryName(".")

    let writerSettings = XmlWriterSettings()
    writerSettings.Encoding <- System.Text.UTF8Encoding()
    writerSettings.Indent <- true
    

    use file = File.Create(parameters.OutputFilePath)
    use writer = XmlWriter.Create(file, writerSettings)
    doc.Save(writer)

    let resultsFileMessage = String.Format(CultureInfo.CurrentCulture, "JunitXML Logger - Results File: {0}", parameters.OutputFilePath)
    Console.WriteLine(Environment.NewLine + resultsFileMessage)

type JunitTestLogger() =
  
  /// <summary>
  /// Uri used to uniquely identify the logger.
  /// </summary>
  [<Literal>]
  let ExtensionUri = "logger://Microsoft/TestPlatform/JUnitXmlLogger/v1"

  /// <summary>
  /// User friendly name to uniquely identify the console logger
  /// </summary>
  [<Literal>]
  let FriendlyName = "junit"

  let mutable args : Map<string, string> = Map<_,_> []

  let mutable _parameters = Parameters.Empty()
  member this.Parameters with
    get() = _parameters
    and set(value) = _parameters <- value

  member internal this.TestRunCompleteHandler(sender, e : TestRunCompleteEventArgs) =
    let doc = XmlBuilder.buildDocument this.Parameters.InputParameters []
    XmlWriter.writeXml this.Parameters doc
    ()

  interface ITestLoggerWithParameters with

    /// <summary>
    /// Initializes the Test Logger with given parameters.
    /// </summary>
    /// <param name="events">Events that can be registered for.</param>
    /// <param name="parameters">Collection of parameters</param>
    member this.Initialize(events : TestLoggerEvents, testResultsDirPath : string) =
      ()

    /// <summary>
    /// Initializes the Test Logger with given parameters.
    /// </summary>
    /// <param name="events">Events that can be registered for.</param>
    /// <param name="parameters">Collection of parameters</param>
    member this.Initialize(events : TestLoggerEvents, parameters : System.Collections.Generic.Dictionary<string, string>) =
      let paramsMap = 
        (parameters :> seq<_>)
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq
      args <- paramsMap
      this.Parameters <- { this.Parameters with InputParameters = paramsMap}
      ()

