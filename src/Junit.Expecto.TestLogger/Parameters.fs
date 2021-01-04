namespace Junit.Expecto.TestLogger

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

  /// If the user just does "logger:junit", this will be the file name
  [<Literal>]
  let DefaultFileName = "junit-test-results.xml"

  [<Literal>]
  let NameFormat = "NameFormat"

  [<Literal>]
  let RootList = "RootList"
  [<Literal>]
  let RootListLI = "rootlist"
  [<Literal>]
  let AllLists = "AllLists"
  [<Literal>]
  let AllListsLI = "alllists"


type NameFormat =
/// The root list name will appear as the classname. The name will contain the rest of the lists and the test case name. 
/// This is the default behavior.
| RootList
/// The classname will contain all the list names. The name will contain only the test case name.
| AllLists

type Parameters = {
  /// The raw user input parameters
  InputParameters : Map<string, string>

  /// The absolute path to write the report to.
  OutputFilePath : string

  /// The format to use to name the classname and name fields of a testcase in the XML.
  NameFormat : NameFormat

  /// Any unrecognized parameters from the user input
  UnrecognizedParameters : (string * string) list
} with
    static member Empty() = {
      InputParameters = Map []
      OutputFilePath = ""
      UnrecognizedParameters = []
      NameFormat = NameFormat.RootList
    }

    member this.TryGetInput (key : string) =
      match this.InputParameters.TryGetValue(key.ToLowerInvariant()) with
      | true, value -> Some value
      | false, _ -> None

module Parameters =

  /// Given a possible user-entered value, construct a full file path.
  let buildFilePath (filePathOption: string option) =
    let filePathOption = 
      filePathOption
      |> Option.map (fun x -> if (x.EndsWith ".xml" |> not) then x + Constants.DefaultFileName else x)
      |> Option.map System.IO.Path.GetFullPath

    match filePathOption with
    | Some x -> x
    | None -> System.IO.Path.GetFullPath("./" + Constants.DefaultFileName)

  let getNameFormat (s : string) =
    match s.ToLowerInvariant() with
    | Constants.RootListLI -> NameFormat.RootList
    | Constants.AllListsLI -> NameFormat.AllLists
    | _ -> NameFormat.RootList

  let parseParameters (inputParameters : Parameters) =
    let tryGetValue (key : string) (map : Map<string, string>) =
      match map.TryGetValue (key.ToLowerInvariant()) with
      | true, value -> Some value
      | false, _ -> None

    let tryGet key = tryGetValue key inputParameters.InputParameters
    let filePath = tryGet Constants.LogFilePath |> buildFilePath
    let nameformat = tryGet Constants.NameFormat |> Option.defaultValue "" |> getNameFormat

    { inputParameters with
        OutputFilePath = filePath
        NameFormat = nameformat
    }