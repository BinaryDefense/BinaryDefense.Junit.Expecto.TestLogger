namespace Junit.Expecto.TestLogger.Xml

open System
open System.Xml
open System.Xml.Linq
open Junit.Expecto.TestLogger
open Junit.Expecto.TestLogger.TestReportBuilder
open Junit.Expecto.TestLogger.Parameters

module XmlBuilder =
  open Microsoft.VisualStudio.TestPlatform.ObjectModel
  open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
  open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

  let strMaybe (s : string) = if isNull s then "" else s

  let purgeString (s : string) =
    if isNull s || s = "" then 
      ""
    else
      let sb = System.Text.StringBuilder()
      s.ToCharArray()
      |> Seq.iter (fun c ->
        // only permit valid xml characters
        if XmlConvert.IsXmlChar c then 
          sb.Append c |> ignore
        else 
          ()
      )
      sb.ToString()

  let inline private xAttr name (value : string) = XAttribute(XName.Get name, (purgeString value))

  let inline xAttrMaybe k v =
    // returning an array requires `yield!` but no fiddling with Option
    if isNull v then
      [||]
    else
      [|
        xAttr k v :> XObject
      |]

  let inline private xProperty (name: string) (value : string) =
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
        yield xProperty "clr-version" (Environment.Version.ToString())
        yield xProperty "os-version" Environment.OSVersion.VersionString
        yield xProperty "platform" (Environment.OSVersion.Platform.ToString())
        yield xProperty "cwd" Environment.CurrentDirectory
        yield xProperty "machine-name" Environment.MachineName
        yield xProperty "user" Environment.UserName
        yield xProperty "user-domain" Environment.UserDomainName
        yield! writeParameterProperties args
      |]
    )

  let buildTestCase (test : TestReport) =
    let content: XObject[] =
      let makeMessageNode messageType (message: string) =
        XElement(
          XName.Get messageType,
          xAttr "message" message
        )
      match test.TestOutcome with
      | Passed -> [||]
      | Failed (msg, block) -> 
          let msg' = msg |> strMaybe |> purgeString
          let block' = block |> strMaybe |> purgeString
          [|
            XElement(
              XName.Get "failure", [|
                xAttr "message" msg' :> XObject
                xAttr "type" "failure" :> XObject
                XText $"{block'}" :> XObject
              |]
            )
          |]
      | Skipped msg -> [|
        XElement(
              XName.Get "skipped", [|
                xAttr "message" msg :> XObject
              |]
            )
        |]
      | NoOutcome -> [|
        XElement(
              XName.Get "unknown", [|
                xAttr "message" "Unknown test result" :> XObject
              |]
            )
        |]
    XElement(XName.Get "testcase",
      [|
        yield! xAttrMaybe "classname" test.ClassName
        yield! xAttrMaybe "name" test.TestName
        yield (xAttr "time" (test.Duration.TotalSeconds.ToString())) :> XObject
        yield! content
      |])

  let buildSuite (parameters: Parameters) (reports : TestReport array) =
    XElement(XName.Get "testsuite",
      [|
        // yield (xAttr "id" assemblyName) :> XObject
        // yield (xAttr "name" assemblyName) :> XObject
        // yield (xAttr "package" assemblyName) :> XObject
        yield (xAttr "timestamp" (DateTime.UtcNow.ToString())) :> XObject
        yield (xAttr "tests" ((Seq.length reports).ToString())) :> XObject
        // yield (xAttr "skipped" (Seq.length summary.ignored)) :> XObject
        // yield (xAttr "failures" (Seq.length summary.failed)) :> XObject
        // yield (xAttr "errors" (Seq.length summary.errored)) :> XObject
        // yield (xAttr "time" (time summary.duration.TotalSeconds)) :> XObject
        yield (xAttr "hostname" Environment.UserDomainName) :> XObject
        //yield properties :> XObject
        yield! reports |> Seq.map (fun t -> buildTestCase t :> XObject)
      |])

  let buildDocument (parameters: Parameters) (testSuite : XElement) =
    let properties = buildProperties parameters.InputParameters
    let testSuites =
      XElement(
        XName.Get "testsuites", [|
          properties
          XElement(
            XName.Get "testsuite", [|
              testSuite
            |]
          )
        |]
      )
    let doc = XDocument(testSuites)
    doc


module XmlWriter =
  open System
  open System.Globalization
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
    resultsFileMessage
