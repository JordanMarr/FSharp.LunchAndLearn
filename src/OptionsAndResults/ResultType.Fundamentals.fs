namespace ResultType

/// Used to return different info in success or fail state.
module ``Fundamentals - Business Rules and Validation`` = 
    open System

    type NewTimeEntry = {
        User: string
        Hours: decimal
        Date: DateTimeOffset
    }

    let saveTimeEntryApi (entry: NewTimeEntry) = 
        // Business rule: 
        // Cannot save time entries in the future
        if entry.Date <= DateTimeOffset.Now then
            printfn $"Saving entry to DB: {entry}." 
            Ok ()
        else
            Error "Can't save future time entries."

    // Valid entry
    let result1 = 
        { User = "jmarr@microdesk.com"; Hours = 8.5M; Date = DateTimeOffset.Now }
        |> saveTimeEntryApi

    match result1 with 
    | Ok () -> printfn "Entry saved."
    | Error err -> printfn $"Cannot save entry: {err}"


    // Invalid Entry
    let result2 = 
        { User = "jmarr@microdesk.com"; Hours = 8.5M; Date = DateTimeOffset.Now.AddDays 7 }
        |> saveTimeEntryApi

    match result2 with 
    | Ok () -> printfn "Entry saved."
    | Error err -> printfn $"Error: {err}"


module ``Fundamentals - Known or Expected Errors`` = 
    
    let divide (dividend: float, divisor: float) = 
        if divisor <> 0
        then Ok (dividend / divisor)
        else Error "Cannot divide by zero."

    // Divide by Non-Zero
    let result = divide (6, 2)
    match result with
    | Ok quotient -> printfn $"Quotient: {quotient}"
    | Error err -> printfn $"Error: {err}"

    // Divide by Zero
    match divide (5, 0) with
    | Ok quotient -> printfn $"Quotient: {quotient}"
    | Error err -> printfn $"Error: {err}"


    let square (n: float) = n * n

    /// Use the Result.map to handle only success result in a pipeline.
    let divideAndSquareResult1 = 
        divide (6, 2)
        |> Result.map (fun quotient -> square quotient)
        |> Result.map (fun squared -> int squared)

    /// Same thing but removed lambdas
    let divideAndSquareResult2 = 
        divide (6, 2)
        |> Result.map square
        |> Result.map int
        |> Result.map string

    /// Invalid result
    let divideAndSquareResult3 = 
        divide (6, 0)
        |> Result.map square
        |> Result.map int
