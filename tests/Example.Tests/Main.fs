module Example.Tests
open Expecto

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [||] argv
