module GeneratorTests

open Expecto

[<Tests>]
let tests =
  testList "generator" [
    test "I am (should fail)" {
      "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal true false
    }
  ]
