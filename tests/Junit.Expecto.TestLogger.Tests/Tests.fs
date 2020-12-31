module Tests

open System
open Expecto
open Microsoft.VisualStudio.TestPlatform.Extension.Junit.Expecto.TestLogger

let force r =
  match r with
  | Ok x -> x
  | Error e -> failwithf "%A" e

let forceO o =
    match o with
    | Some x -> x
    | None -> failwithf "Expected to find value in Option, but found None."

module XmlBuilderTests =
    open System.Xml.Linq

    [<Tests>]
    let tests =
        testList "XmlBuilder tests" [
            testCase "buildProperties writes at least 1 property" <| fun _ ->
                let args = Map ["hello", "world"]
                let output = XmlBuilder.buildProperties args
                Expect.equal (output.HasElements) true "properties with args did not create child element"
            testCase "buildProperties writes passed arg" <| fun _ ->
                let args = Map ["hello", "world"]
                let output = XmlBuilder.buildProperties args
                let elementFound = 
                    output.DescendantsAndSelf(XName.Get "property")
                    |> Seq.cast<XElement>
                    |> Seq.tryFind (fun x -> x.Attribute(XName.Get "name").Value = "hello")
                Expect.isSome elementFound "Did not find expected element from args in properties list."
            testCase "buildProperties writes correct value for arg" <| fun _ ->
                let args = Map ["hello", "world"]
                let output = XmlBuilder.buildProperties args
                let elementFound =
                    output.DescendantsAndSelf(XName.Get "property")
                    |> Seq.cast<XElement>
                    |> Seq.tryFind (fun x -> x.Attribute(XName.Get "name").Value = "hello")
                    |> forceO
                let attributeValue = elementFound.Attribute(XName.Get "value").Value
                Expect.equal attributeValue "world" $"Did not find expected value in written property. Element was {elementFound.ToString()}"
        ]

module XmlWriterTests =
    open System.Xml.Linq
    open System.IO

    [<Literal>]
    let FileName = "test-report.xml"
    
    [<Literal>]
    let RelativeDirName = "test-reports"

    let parameters = Parameters.Empty()

    let doc name = XDocument(XElement(XName.Get "test-document", [
        XAttribute(XName.Get "testname", name)
    ]))

    let testCleanup() =
        if Directory.Exists(RelativeDirName) then Directory.Delete(RelativeDirName, true)
        if File.Exists(FileName) then File.Delete(FileName)

    let path (vals : string []) = Path.GetFullPath(Path.Combine(vals))


    [<Tests>]
    let tests =
        let testWithCleanup name test = 
            [
                testCase name test
                testCase $"{name}-cleanup" <| fun _ -> testCleanup()
            ]

        testSequenced <| testList "XmlWriter tests" [
            yield! testWithCleanup "Writes a file" <| fun _ ->
                let path = path([|FileName|])
                let parameters = { parameters with OutputFilePath = path }
                XmlWriter.writeXmlFile parameters (doc "Writes a file" )
                let isFileThere = File.Exists(path)
                Expect.isTrue isFileThere "Should have written a file in current directory"
            yield! testWithCleanup "Writes a directory" <| fun _ ->
                let path = path [| RelativeDirName; FileName |]
                let parameters = { parameters with OutputFilePath = path }
                XmlWriter.writeXmlFile parameters (doc "Writes a directory")
                let isFileThere = File.Exists(path)
                Expect.isTrue isFileThere "Should have written a file in relative directory"
        ]

module ParametersTests =

    [<Tests>]
    let tests =
        testList "Parameter parsing and building" [
            testCase "build file path with None returns a string" <| fun _ ->
                let output = Parameters.buildFilePath None
                Expect.isNotEmpty output "Should not return empty string for path"
            testCase "build file path with None returns an xml file" <| fun _ ->
                let output = Parameters.buildFilePath None
                Expect.stringEnds output ".xml" "Should have returned a path to an xml file"
            testCase "Build file path with a path and no file returns an xml file" <| fun _ ->
                let output = Parameters.buildFilePath (Some "./hello")
                Expect.stringEnds output ".xml" "Should have returned a path to an xml file"
            testCase "Build file path with a path and no file does not stack forward slashes" <| fun _ ->
                let output = Parameters.buildFilePath (Some "./hello")
                let doubleslash = output.IndexOf("//")
                Expect.equal doubleslash -1 $"Should not find a double slash in the path {output}"
            testCase "Build file path with a path and a file does not double up xml" <| fun _ ->
                let output = Parameters.buildFilePath (Some "./hello/world.xml")
                let firstindex = output.IndexOf(".xml")
                let lastindex = output.LastIndexOf(".xml")
                Expect.equal firstindex lastindex $"Should not find two .xml in output {output}"

            testCase "Parameters with no path key returns a path" <| fun _ ->
                let parameters = Parameters.Empty()
                let input = parameters.TryGetInput (Constants.LogFilePath.ToLowerInvariant())
                let output = Parameters.buildFilePath input
                Expect.isNotEmpty output "Should not return empty string for path"
            testCase "Parameters with no path key returns the default file name" <| fun _ ->
                let parameters = Parameters.Empty()
                let input = parameters.TryGetInput (Constants.LogFilePath.ToLowerInvariant())
                let output = Parameters.buildFilePath input
                let index = output.IndexOf(Constants.DefaultFileName)
                Expect.isGreaterThan index 0 "Should contain the default file name"
        ]