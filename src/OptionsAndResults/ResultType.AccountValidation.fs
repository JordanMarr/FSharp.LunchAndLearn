/// Shows how to use the built-in F# "Result" type along with the
/// "FsToolkit.ErrorHandling" validation computation expression.
module ResultType.``Real World Example - Account Validation``

open System
open FsToolkit.ErrorHandling

type Account = { 
    Username: string
}

type CreateAcountRequest = {
    Username: string
    Password: string
}

module ValidationRules = 
    let notNullOrWS str = if String.IsNullOrWhiteSpace str then Error "Is null or empty" else Ok str
    let minLen (min: int) (str: string) = if str.Length >= min then Ok str else Error $"Must have minimum length of {min}"
    let maxLen (max: int) (str: string) = if str.Length <= max then Ok str else Error $"Must have maximum length of {max}"
    let containsSpecialChar (str: string) = 
        let specialCharacters = ['!'; '@'; '#'; '$'; '%']
        if specialCharacters |> List.exists str.Contains
        then Ok str
        else Error $"Must contain at least one of the following special characters: %A{specialCharacters}"

let createAccountIfValid (username: string, password: string) = 
    validation {
        let! _ = username |> ValidationRules.notNullOrWS 
        and! _ = password |> ValidationRules.minLen 8 
        and! _ = password |> ValidationRules.maxLen 50 
        and! _ = password |> ValidationRules.containsSpecialChar
        return { Account.Username = username }
    }
    
open NUnit.Framework
    
[<Test>]
let ``All rules should fail`` () =
    createAccountIfValid ("", "pass123")
    |> (printfn "%A")
    
[<Test>]
let ``All rules should pass`` () =
    createAccountIfValid ("jordan", "Better#Password")
    |> (printfn "%A")
        
    