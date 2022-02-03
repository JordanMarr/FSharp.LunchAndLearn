/// F# Option type *explicitly* types that are optional.
namespace OptionType

open System

module ``Fundamentals - Option Module`` = 

    let number : int option = Some 3

    // Pattern match is most common way to unwrap Option type:
    match number with
    | Some n -> printfn $"Number: {n}"
    | None -> printfn "No number"

    // But sometimes you may prefer to use functions on the Option module:
    number
    |> Option.iter (printfn "Number: %i")

    let transform1 = 
        number 
        |> Option.map (fun n -> n + 5)
    
    let square n = n * n
    
    let transform2 = 
        number
        |> Option.map (fun n -> n + 2)
        |> Option.map square

    let transform3 = 
        number
        |> Option.map string

        
    // Supply a defaultValue to "unwrap" a value from an Option
    let transform4 = 
        number 
        |> Option.defaultValue 0

    // Mapping if some and also providing a default to unwrap a value regardless
    let transform5 =
        number
        |> Option.map string
        |> Option.defaultValue "---"

    // Since `defaultValue` is always initialized, 
    // use defaultWith for operation that should only initialize when value is `None`.
    // (useful for expensive operations)
    let transform6 = 
        number
        |> Option.map string
        |> Option.defaultWith (fun () -> "---")

    // Use `iter` to perform an operation that return `unit` (void) when value is `Some`.
    number
    |> Option.map string
    |> Option.iter (printfn "Number: %s") // <-- use iter when returning unit

    // Always use option types in your domain models!
    // Option types make F# explicit and prevent null reference exceptions!
    type Person = {
        FName: string
        LName: string
        MI: string option
        DoB: DateTime option
    }

    let createPerson (fname: string, lname: string, mi: string, dob: DateTime Nullable) = 
        { FName = fname
        ; LName = lname
        ; MI = mi |> Option.ofObj
        ; DoB = dob |> Option.ofNullable }


    module Crash = 

        type CrashForm = {
            Id: int
            Vehicle1: Vehicle
            Vehicle2: Vehicle option
        }
        and Vehicle = {
            Id: int
        }

        let saveVehicle (vehicle: Vehicle) = 
            printfn $"Saving Vehicle: {vehicle}"

        let saveCrash (crash: CrashForm) = 
            crash.Vehicle1 |> saveVehicle
            crash.Vehicle2 |> Option.iter saveVehicle
   