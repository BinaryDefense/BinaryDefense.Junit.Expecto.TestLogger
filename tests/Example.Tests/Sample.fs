module Tests

open Expecto

module Samples =

  [<Tests>]
  let tests =
    testList "samples" [
      testCase "universe exists (╭ರᴥ•́)" <| fun _ ->
        let subject = true
        Expect.isTrue subject "I compute, therefore I am."

      testCase "when true is not (should fail)" <| fun _ ->
        let subject = false
        Expect.isTrue subject "I should fail because the subject is false"

      testCase "I'm skipped (should skip)" <| fun _ ->
        Tests.skiptest "Yup, waiting for a sunny day..."

      testCase "I'm always fail (should fail)" <| fun _ ->
        Tests.failtest "This was expected..."

      test "I am (should fail)" {
        "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal true false
      }
    ]

module ListTests =
  let list1 = 
    testList "A list within a list" [
      testCase "should succeed" <| fun _ -> Expect.equal true true "should succeed"
    ]

  let list2 =
    testList "More tests within a list" [
      testCase "should succeed" <| fun _ -> Expect.equal true true "should succeed"
      testCase "should also succeed" <| fun _ -> Expect.equal true true "should succeed"
    ]
  
  [<Tests>]
  let tests =
    testList "test list tests" [
      list1
      list2
    ]
  