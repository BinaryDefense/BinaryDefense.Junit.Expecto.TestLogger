module JUnit.Expecto.Tests

open System
open Expecto
open Junit.Expecto.TestLogger
open Junit.Expecto.TestLogger.Xml
open Microsoft.VisualStudio.TestPlatform.Extension.Junit.Expecto.TestLogger
open Microsoft.VisualStudio.TestPlatform.ObjectModel
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

let force r =
  match r with
  | Ok x -> x
  | Error e -> failwithf "%A" e

let forceO o =
    match o with
    | Some x -> x
    | None -> failwithf "Expected to find value in Option, but found None."

let parameters = Parameters.Empty()

module XmlBuilderTests =
    open System.Xml.Linq

    let buildPropertiesTests =
        testList "buildProperties tests" [
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

    let buildSuiteTests =
        testList "buildSuite tests" [
            testCase "build suite does not error when given an empty list" <| fun _ -> 
                let (results : TestReportBuilder.TestReport array) = [||]
                let output = XmlBuilder.buildSuite parameters results
                Expect.isNotNull output "Should not return null, ever."
                let innerList = output.Attributes() :> seq<_>
                Expect.isGreaterThan (Seq.length innerList) 0 "Should have more than 0 attributes."
            testCase "Build suite returns structure when given a single argument" <| fun _ ->
                let (results : TestReportBuilder.TestReport array) = [| TestReportBuilder.TestReport.Empty() |]
                let output = XmlBuilder.buildSuite parameters results
                Expect.isNotNull output "Should not return null, ever."
                let innerList = output.Attributes() :> seq<_>
                Expect.isGreaterThan (Seq.length innerList) 0 "Should have more than 0 attributes"
                let children = output.Elements() :> seq<_>
                Expect.isGreaterThan (Seq.length children) 0 "Should have more than 0 children"
        ]

    [<Tests>]
    let tests =
        testList "XmlBuilder tests" [
            yield buildPropertiesTests
            yield buildSuiteTests
        ]

module XmlWriterTests =
    open System.Xml.Linq
    open System.IO

    [<Literal>]
    let FileName = "test-report.xml"
    
    [<Literal>]
    let RelativeDirName = "test-reports"

    let doc name = XDocument(XElement(XName.Get "test-document", [
        XAttribute(XName.Get "testname", name)
    ]))

    let docWithTest() =
        let test = TestReportBuilder.TestReport.Empty()
        let suite = XmlBuilder.buildSuite parameters [| test |]
        let doc = XmlBuilder.buildDocument parameters suite
        doc

    let testCleanup() =
        if Directory.Exists(RelativeDirName) then Directory.Delete(RelativeDirName, true)
        if File.Exists(FileName) then File.Delete(FileName)

    let path (vals : string []) = Path.GetFullPath(Path.Combine(vals))
    
    let readFile path = seq {
        use sr = File.OpenText(path)
        while not sr.EndOfStream do
            yield sr.ReadLine()
    }


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
                XmlWriter.writeXmlFile parameters (doc "Writes a file" ) |> ignore
                let isFileThere = File.Exists(path)
                Expect.isTrue isFileThere "Should have written a file in current directory"
            yield! testWithCleanup "Writes a directory" <| fun _ ->
                let path = path [| RelativeDirName; FileName |]
                let parameters = { parameters with OutputFilePath = path }
                XmlWriter.writeXmlFile parameters (doc "Writes a directory") |> ignore
                let isFileThere = File.Exists(path)
                Expect.isTrue isFileThere "Should have written a file in relative directory"
            yield! testWithCleanup "Does not write &lt; (an escaped < symbol)" <| fun _ ->
                let path = path([|FileName|])
                let parameters = { parameters with OutputFilePath = path }
                XmlWriter.writeXmlFile parameters (docWithTest()) |> ignore
                let fileLines = readFile path
                fileLines |> Seq.iter (fun line ->
                    let contains = line.Contains("%lt;")
                    Expect.isFalse contains "File printed &lt; instead of <"
                )
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
                let input = parameters.TryGetInput (Constants.LogFilePath.ToLowerInvariant())
                let output = Parameters.buildFilePath input
                Expect.isNotEmpty output "Should not return empty string for path"
            testCase "Parameters with no path key returns the default file name" <| fun _ ->
                let input = parameters.TryGetInput (Constants.LogFilePath.ToLowerInvariant())
                let output = Parameters.buildFilePath input
                let index = output.IndexOf(Constants.DefaultFileName)
                Expect.isGreaterThan index 0 "Should contain the default file name"
        ]

module TestResultsTests =
    open Junit.Expecto.TestLogger

    type Nesting =
    | NoNesting
    | SomeNesting
    | LotsOfNesting
        with
            member this.value =
                match this with
                | NoNesting -> "No Nesting"
                | SomeNesting -> "Some Nesting/A Test"
                | LotsOfNesting -> "More Nesting/Lots of Nesting/So much Nesting/A Test"
            member this.name =
                match this with
                | NoNesting -> "No Nesting"
                | SomeNesting -> "Some Nesting"
                | LotsOfNesting -> "Lots of Nesting"

    let noNesting = "No Nesting"
    let someNesting = "Some Nesting/A Test"
    let lotsOfNesting = "More Nesting/Lots of Nesting/So much Nesting/A Test"

    let buildTestCase (nesting : Nesting) (nameFormat : NameFormat) (classname : string) (name : string) =
        testCase (sprintf "%s-%s" (string nameFormat) (nesting.name)) <| fun _ ->
            let classnameR, nameR = TestReportBuilder.splitClassName nameFormat nesting.value
            Expect.equal classnameR classname "Should build the expected class name"
            Expect.equal nameR name "Should build the expected test name"

    let splitClassNameTests =
        testList "Split Class Name tests" [
            buildTestCase Nesting.NoNesting NameFormat.RootList "" NoNesting.value
            buildTestCase Nesting.NoNesting NameFormat.AllLists NoNesting.value NoNesting.value
            buildTestCase Nesting.SomeNesting NameFormat.RootList "Some Nesting" "A Test"
            buildTestCase Nesting.SomeNesting NameFormat.AllLists "Some Nesting" "A Test"
            buildTestCase Nesting.LotsOfNesting NameFormat.RootList "More Nesting" "Lots of Nesting/So much NestingA Test"
            buildTestCase Nesting.LotsOfNesting NameFormat.AllLists "More Nesting/Lots of Nesting/So much Nesting" "A Test"
        ]

    [<Tests>]
    let tests =
        testList "Test Result Builder tests" [
            yield splitClassNameTests
        ]