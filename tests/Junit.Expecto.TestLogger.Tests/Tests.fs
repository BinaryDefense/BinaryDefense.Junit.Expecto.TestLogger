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

