namespace BinaryDefense.Junit.Expecto.TestLogger

open System

module TestNameParsing =

  type TextBlock =
    | NoBlock
    | Regular of str : string
    | Quote of str : string
      with
        member this.AppendChar (x : char) =
          match this with
          | NoBlock -> Regular (string x)
          | Regular value -> Regular (value + (string x))
          | Quote value -> Quote (value + (string x))

        override this.ToString() =
                    match this with
                    | NoBlock -> ""
                    | Regular v -> v
                    | Quote v -> v

  type Block =
  | Text of textBlock : TextBlock
  | Split
    // re-add the splitter character
    with member this.toStr (splitter : string) =
                  match this with
                  | Text tb -> string tb
                  | Split -> splitter

  let rec readUntilMatch (splitOn : string) (chars : char list) (accChars : string) =
    if accChars = splitOn then
      true, accChars
    elif splitOn.Contains(accChars) then
      match chars with
      | [] -> false, ""
      | x :: xs -> readUntilMatch splitOn xs (accChars + (string x))
    else
      false, ""

  let parseQuoteWithMatch (current : TextBlock) =
    //if we've found a quote that has a quote ahead...
    match current with
    // and there is no block being built, start a quote block, and continue
    | NoBlock ->
      Quote "\"", None
    //and we're in a regular block, close the regular block, create a quote block, and continue
    | Regular value ->
      Quote "\"", Some (Text current)
    //and we're in a quote block, close the quote block and continue
    | Quote value ->
      let closed = current.AppendChar '\"'
      let block = (Text closed)
      NoBlock, Some block


  let parseQuoteWithoutMatch (current : TextBlock) =
    //we got a quote but the lookahead is false. If we're in a quote, close it; if not, proceed as normal
    match current with 
    | Quote value ->
      //let closed = current.AppendChar '\"'
      let closed = Regular (value + "\"")
      let block = Text closed
      let next = NoBlock
      next, Some block
    | NoBlock ->
      let next = Regular "\""
      next, None
    | Regular value ->
      let next = current.AppendChar '\"'
      next, None


  let parseSplitOnCharacter splitOn (current : TextBlock) currentChar remainingChars =
    //if we've found a split character...
    match current with
    | Quote _ -> 
      //and we're in a quote; add the split character to the quote & continue
      let next = current.AppendChar currentChar
      next, remainingChars, []
    | NoBlock ->
      //and we're not in a block, read ahead to see if we fully match our split string
      match readUntilMatch splitOn remainingChars (string currentChar) with
      | true, acc -> 
        //we have a match, so create a split and continue
        let length = acc.Length
        
        //let nextChars =  if length = 1 then List.skip (1) remainingChars else List.skip (length - 1) remainingChars
        let nextChars =List.skip (acc.Length - 1) remainingChars
        NoBlock, nextChars, [ Split ]
      | false, _ ->
        //not a match, add this character to a block and continue
        let next = Regular (string currentChar)
        next, remainingChars, []
    | Regular value ->
      //we're in a block but we may be entering a split. Check for a split, and if so, split; then continue
      match readUntilMatch splitOn remainingChars (string currentChar) with
      | true, acc ->
        let b = Text current
        let nextChars = List.skip (acc.Length - 1) remainingChars
        NoBlock, nextChars, [ Split; b ]
      | false, _ ->
        //false alarm, add this character to the block and continue
        let next = current.AppendChar currentChar
        next, remainingChars, []


  let parseTestNameToBlocks (splitOn : string) (str : string) : Block list =
    let lookAheadFor (target : string) (chars : char list) =
      let sb = Text.StringBuilder()
      chars |> List.iter (fun c -> sb.Append(c) |> ignore)
      let str = string sb
      str.Contains target

    let rec traverse (current : TextBlock) (chars : char list) (blocks : Block list) =
      match chars with
      | [] ->
        match current with
        | NoBlock -> blocks
        | Regular _ -> (Text current) :: blocks
        //string ended while we're in a quote block; turn the quote block into a Regular and return
        | Quote value -> (Regular value |> Text) :: blocks
      
      | '\"' :: xs when (not (List.isEmpty xs)) && lookAheadFor "\"" (xs) ->
        let next, block = parseQuoteWithMatch current
        match block with
        | Some b ->
          traverse next xs (b :: blocks)
        | None ->
          traverse next xs blocks
      
      | '\"' :: xs -> 
        let next, block = parseQuoteWithoutMatch current
        match block with
        | Some b ->
          traverse next xs (b :: blocks)
        | None ->
          traverse next xs blocks

      | x :: xs when splitOn.Contains(string x) ->
        let next, remainingChars, newBlocks = parseSplitOnCharacter splitOn current x xs
        traverse next remainingChars ( newBlocks @ blocks )

      | x :: xs ->
        let block = current.AppendChar x
        traverse block xs blocks

    let chars = str.ToCharArray() |> List.ofArray
    traverse NoBlock chars [] |> List.rev


  let splitClassName (splitOn : string) (nf : NameFormat) (str : string) : (string * string) =
    let blocks = parseTestNameToBlocks splitOn str

    let trimSplits (blocks: Block list) =
      let trimSplit bl =
        match bl with
        | Split :: xs -> xs
        | x :: xs -> bl
        | [] -> []
      blocks
      |> trimSplit
      |> List.rev
      |> trimSplit
      |> List.rev

    let getSlice start last (list: 'a list) =
      list.GetSlice(Some start, Some last)

    let writeList (bl : Block list) =
      bl
      |> trimSplits
      |> List.map (fun x -> x.toStr splitOn)
      |> String.concat ""

    match nf with
    | NameFormat.AllLists ->
      if blocks.Length > 3 then
        // if we have more than 3 blocks, we need to traverse the list, 
        // build the list of blocks for the class name, and grab the last block for the test name
        let rec buildNames (acc: Block list) (rem: Block list) =
          match rem with
          | [] -> 
            failwith "Traversed entire block list when building classname, name for NameFormat AllLists, but should have constructed names before this point."
          | [ x ] -> acc |> List.rev |> writeList, (x.toStr splitOn)
          | x :: [ Split ] -> acc |> List.rev |> writeList, (x.toStr splitOn)
          | x :: xs -> buildNames (x :: acc) xs

        blocks |> trimSplits |> buildNames []
      elif blocks.Length = 3 then
        // 3 blocks = head / split / tail
        let blocks' = trimSplits blocks
        let classname = blocks'.Head
        let name = blocks' |> List.last
        (classname.toStr splitOn), (name.toStr splitOn)
      else
        str, str
    | NameFormat.RootList ->
      if blocks.Length >= 2 then
        //all elements go in the name except the first one
        let classname = blocks.Head
        let name = blocks |> List.tail |> writeList
        (classname.toStr splitOn), name
      else
        str, str

module TestReportBuilder =
  open BinaryDefense.Junit.Expecto.TestLogger.Parameters
  open Microsoft.VisualStudio.TestPlatform.ObjectModel
  open Microsoft.VisualStudio.TestPlatform.ObjectModel.Client
  open Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging

  type TestOutcome =
  | Passed
  | Failed of msgSummary : string * msg : string
  /// error tests are returned the same as failed tests.
  //| Errored of msgSummary : string * msg : string
  | Skipped of msg : string
  /// Covers unusual enum results that may be returned from the tests
  | NoOutcome

  type TestReport = {
    TestOutcome : TestOutcome
    TestName : string
    ClassName : string
    /// The timespan for how long it took this test to run.
    Duration : TimeSpan
    /// The source assembly for this test. "/some/path/to/Tests.dll"
    Source : string
    /// The file path for this test. "/some/path/to/tests/MyTestFile.fs"
    CodeFilePath : string
  } with
      static member Empty() = {
        TestOutcome = Passed
        TestName = ""
        ClassName = ""
        Duration = TimeSpan()
        Source = ""
        CodeFilePath = ""
      }

  let ifNullThen fallback s = if isNull s then fallback else s

  let split (splitter : string) (source: string) = source.Split(splitter)


  let parseTestOutcome (test : TestResult) =
    let tryFirstTestLine() =
      let split = test.ErrorMessage |> ifNullThen "" |> split "\n"
      //the first line of the Expecto error message is always a blank line. the 2nd line contains the unit test message (ie, the "should have [...]")
      //the remaining lines are the full error message, like `expected` and the stack trace.
      if split.Length >= 2 then split.[1] else ""
    
    let tryFirstMessage() =
      match Seq.tryHead test.Messages with
      | Some x -> x.Text
      | None -> ""
    match test.Outcome with
    | Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Passed -> TestOutcome.Passed
    | Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Failed -> TestOutcome.Failed (tryFirstTestLine(), test.ErrorMessage)
    | Microsoft.VisualStudio.TestPlatform.ObjectModel.TestOutcome.Skipped -> TestOutcome.Skipped (tryFirstMessage())
    // covers cases of None & NotFound, plus any other enum values that shouldn't happen.
    | _ -> TestOutcome.NoOutcome

  let replaceAssemblyName (parameters : Parameters) (test: TestReport) =
    if parameters.OutputFilePath.Contains("{assembly}") then
      let assembly = test.Source.Substring(test.Source.LastIndexOf("/") + 1).Replace(".dll", "")
      { parameters with 
          OutputFilePath = parameters.OutputFilePath.Replace("{assembly}", assembly)
      }
    else
      parameters

  let buildTestReport (parameters: Parameters) (test : TestResult) =
    let outcome = parseTestOutcome test
    let classname, name = test.TestCase.DisplayName |> ifNullThen "" |> TestNameParsing.splitClassName parameters.SplitOn parameters.NameFormat
    { TestReport.Empty() with
        ClassName = classname
        TestName = name
        TestOutcome = outcome
        Duration = test.Duration
        Source = test.TestCase.Source |> ifNullThen ""
        CodeFilePath = test.TestCase.CodeFilePath |> ifNullThen ""
    }