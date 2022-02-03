/// Implements the reservation feature.
module FinalProject.Reservations

open System
open FsToolkit.ErrorHandling

/// Describes a reservation table record.
type Reservation = {
    Email: string
    Date: DateTime
    Location: string
}

/// API access token returned by 3rd party vacation rental API.
type ApiToken = {
    AccessToken: string
    Expiration: DateTimeOffset
}

/// Result returned by 3rd party vacation rental API.
type Confirmation = {
    ConfirmationNumber: int
}

/// Categorizes all possible error types (this is required for the FsToolkit.ErrorHandling result workflow)
type ReservationError = 
    | TokenNotAvailable
    | ValidationErrors of errors: string list
    | GeneralError of exMsg: string

/// Functions that get or put data required for the feature (non-pure).
module IO = 
    /// Tries to find a reservation in the database.
    let getReservationsForDate (date: DateTime) = 
        async {
            let fakeReservationsTable = [
                let today = DateTime.Today

                for days in [7..14] do
                    { Email = "jmarr@microdesk.com"; Date = today.AddDays days; Location = "Dune Our Thing" }
                    { Email = "bouellette@microdesk.com"; Date = today.AddDays days; Location = "Porpoise of Life" }
            ]
            
            return fakeReservationsTable |> List.filter (fun r -> r.Date = date)
        }
            
    /// Tries to get the token from cache (if user has already logged in).
    let tryGetRentalApiToken () = 
        async {  
            return Some { AccessToken ="eydaslkjf"; Expiration = DateTimeOffset.Now.AddMinutes 30 }
        }

    let reserveRentalProperty (token: string, reservation: Reservation) = 
        async {
            try
                // Make REST call to 3rd party API
                return Ok { Confirmation.ConfirmationNumber = 123 }
            with 
            | :? System.UnauthorizedAccessException ->
                return Error "Rental property reservation failed -- token expired -- please login again."
            | ex ->
                // log ex
                //logError ex.Message
                return Error "An unexpected error occurred while confirming rental with 3rd party API."
        }

    /// Saves a reservation record to the database.
    let saveReservation (reservation: Reservation, confirmation: Confirmation) = 
        async {
            // Fake save to database (just returns "unit")...
            return ()
        }

/// Core business logic (should be pure functions).
/// "Core" = easily unit testable
module Core = 
    
    let createReservationIfValid (email: string, date: DateTime, location: string, existingReservations: Reservation list) = 
        let atLeast7DaysOut = 
            let daysOut = (date - DateTime.Today).TotalDays
            if daysOut >= 7
            then Ok ()
            else Error "Must be at least 7 days out"

        let notReservedByOther = 
            if existingReservations |> List.exists (fun r -> r.Email <> email && r.Date = date && r.Location = location)
            then Error $"'{location}' has already been reserved by someone else."
            else Ok ()

        let notReservedByUser = 
            if existingReservations |> List.exists (fun r -> r.Email = email && r.Date = date && r.Location = location)
            then Error $"'{location}' has already been reserved by you."
            else Ok ()

        validation {
            let! _ = atLeast7DaysOut
            and! _ = notReservedByOther
            and! _ = notReservedByUser
            
            return { Email = email; Date = date; Location = location }
        }

/// Log and map to ReservationError.
let handleException (ex: exn) = 
    match ex with
    | :? UnauthorizedAccessException -> 
        TokenNotAvailable
    | ex -> 
        printfn $"Error: {ex.Message}" // Log exception
        GeneralError "An unexpected error occurred"

/// Feature request handler:
/// F# pattern matching forces us to explicitly handle both happy path and all error conditions.
/// This forces our code to be robust and correct, although it can become complex.
let handleReservationRequest1 (email: string, date: DateTime, location: string) =
    async {
        try
            let! existing = IO.getReservationsForDate date

            let reservationResult = Core.createReservationIfValid (email, date, location, existing)

            // Explicit error handling is robust, but can quickly become complex:
            match reservationResult with
            | Ok reservation -> 
                let! tokenMaybe = IO.tryGetRentalApiToken ()
                match tokenMaybe with
                | Some token -> 
                    let! rentalPropertyResult = IO.reserveRentalProperty (token.AccessToken, reservation)
                    match rentalPropertyResult with
                    | Ok confirmation -> 
                        do! IO.saveReservation (reservation, confirmation)
                        return Ok confirmation

                    | Error err -> 
                        return Error (ReservationError.GeneralError err)

                | None ->
                    return Error ReservationError.TokenNotAvailable

            | Error errors -> 
                return Error (ReservationError.ValidationErrors errors)
        with ex ->
            return Error (handleException ex)
    } 

/// Feature request handler: (using FsToolkit.ErrorHandling)
/// FsToolkit.ErrorHandling gives lets us code for the "happy path" while still handling all error conditions.
/// This allows us to write code that looks like a "naive example", but is actually production-ready!!
let handleReservationRequest2 (email: string, date: DateTime, location: string) =
    asyncResult {
        let! existing = IO.getReservationsForDate date
        let! reservation = Core.createReservationIfValid (email, date, location, existing) |> Result.mapError ValidationErrors
        let! token = IO.tryGetRentalApiToken () |> AsyncResult.requireSome TokenNotAvailable
        let! confirmation = IO.reserveRentalProperty (token.AccessToken, reservation) |> AsyncResult.mapError GeneralError
        do! IO.saveReservation (reservation, confirmation)
        return confirmation
    } 
    |> AsyncResult.catch handleException



open NUnit.Framework
        
// UNIT TESTS (CORE)

[<Test>]
let ``Test Validation`` () = 
    let today = DateTime.Today
    let fakeReservations = [
        for days in [7..14] do
            { Email = "jmarr@microdesk.com"; Date = today.AddDays days; Location = "Dune Our Thing" }
            { Email = "bouellette@microdesk.com"; Date = today.AddDays days; Location = "Porpoise of Life" }
    ]
    let reserveDate = today.AddDays 30
    let resp = Core.createReservationIfValid ("jmarr@microdesk.com", reserveDate, "Dune Our Thing", fakeReservations)
    match resp with
    | Ok reservation -> Assert.AreEqual({ Email = "jmarr@microdesk.com"; Date = reserveDate; Location = "Dune Our Thing" }, reservation)
    | Error errors -> failwith $"Expected no validation errors, but got '{errors}'"
    

// INTEGRATION TESTS

let printReservationResponse response = 
    match response with
    | Ok confirmation -> printfn $"Your reservation is booked! Confirmation #: {confirmation.ConfirmationNumber}"
    | Error (ReservationError.TokenNotAvailable) -> printfn "You are not logged into the reservations portal."
    | Error (ReservationError.ValidationErrors errors) -> printfn $"One or more validation issues must be resolved: {errors}"
    | Error (ReservationError.GeneralError exMsg) -> printfn $"An unexpected error ocurred: {exMsg}"
    
[<Test>]
let ``Validation Error: must be at least 7 days out`` () = 
    async {
        let! response = handleReservationRequest1 ("jmarr@microdesk.com", DateTime.Today, "Dune Our Thing")
        printReservationResponse response
    }
    
[<Test>]
let ``Validation Error: already reserved by you`` () = 
    async {
        let! response = handleReservationRequest1 ("jmarr@microdesk.com", DateTime.Today.AddDays 7, "Dune Our Thing")
        printReservationResponse response
    }
    
[<Test>]
let ``Validation Error: Already reserved by someone else`` () = 
    async {
        let! response = handleReservationRequest1 ("jmarr@microdesk.com", DateTime.Today.AddDays 7, "Porpoise of Life")
        printReservationResponse response
    }
    
[<Test>]
let ``Valid Reservation`` () = 
    async {
        let! response = handleReservationRequest1 ("jmarr@microdesk.com", DateTime.Today.AddDays 30, "Dune Our Thing")
        printReservationResponse response
    }
    

