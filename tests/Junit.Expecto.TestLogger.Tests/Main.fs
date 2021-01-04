module ExpectoTemplate

open Expecto

// let tests =
//   testList "all tests" [
//     JUnit.Expecto.Tests.XmlBuilderTests.tests
//     JUnit.Expecto.Tests.XmlWriterTests.tests
//     JUnit.Expecto.Tests.ParametersTests.tests
//     JUnit.Expecto.Tests.TestResultsTests.tests
//   ]

[<EntryPoint>]
let main argv = 
  //Tests.runTestsInAssembly defaultConfig argv
  Tests.runTestsInAssemblyWithCLIArgs [||] argv
  //Tests.runTestsWithCLIArgs [||] argv tests
