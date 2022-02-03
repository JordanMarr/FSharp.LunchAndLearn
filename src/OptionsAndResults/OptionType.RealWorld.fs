/// FEATURE: Calculate a price for a given order Id with a discount code string.
/// FEATURE: Format an optional delivery date for a report.
module OptionType.``Real World Example - Order System``

open System

type Order = {
    Id: int
    Price: decimal
    DateDelivered: DateTimeOffset option // <-- Explicitly represents a nullable db column as an `option`
}

and Discount = 
    | PercentageOff of int
    | AmountOff of decimal

let fakeOrdersTable = 
    [
        { Id = 1; Price = 100M; DateDelivered = None }
        { Id = 2; Price = 100M; DateDelivered = Some (DateTimeOffset.Now.AddDays 7) }
        { Id = 3; Price = 100M; DateDelivered = None }
    ]
    |> List.map (fun order -> order.Id, order)
    |> Map.ofList

/// Gets an order by its Id (if it exists in the database).
/// It is common to represent single record queries with an `option` type.
let getOrder (orderId: int) = 
    async { 
        // Simulate database stuff...
        return fakeOrdersTable.TryFind orderId
    }

/// Checks code for a valid discount (if any).
/// Returns an option type that must be explicitly "unpacked" before it can be used.
let checkDiscountCode (code: string) =
    async {
        // Simulate database stuff...
        return 
            match code with
            | "SAVE-5-PERCENT" -> Some (PercentageOff 5)
            | "2-DOLLARS-OFF" -> Some (AmountOff 2.00M)
            | _ -> None
    }

let applyDiscount discount price = 
    match discount with
    | PercentageOff percent -> price - (price * (decimal percent / 100M) )
    | AmountOff amt -> price - amt

/// 1) Gets an order from database
/// 2) Applies discount code if valid
/// 3) Returns final price
let calculatePrice (orderId: int, code: string) = 
    async {
        let! orderMaybe = getOrder orderId
        let! discountMaybe = checkDiscountCode code

        return 
            match orderMaybe, discountMaybe with
            | Some order, Some discount -> order.Price |> applyDiscount discount 
            | Some order, None -> order.Price
            | None, _ -> failwith $"No order found with Id = {orderId}."
    }

let formatDeliveryDate (order: Order) = 
    match order.DateDelivered with
    | Some date -> $"Delivered on {date.LocalDateTime.ToShortDateString()}."
    | None -> "Not delivered yet."


open NUnit.Framework

[<Test>]
let ``Order Not Found in Database`` () = 
    async {
        try
            let! price = calculatePrice (50, "SAVE-5-PERCENT")
            Assert.Fail("Should have thrown exception.")
        with ex ->
            Assert.Pass("'Order Not Found' exception was triggered as expected.")
    }

[<Test>]
let ``Order with Valid Discount Code`` () = 
    async {
        let! price = calculatePrice (1, "SAVE-5-PERCENT")
        Assert.AreEqual(95M, price)
    }

[<Test>]
let ``Order with Invalid Discount Code`` () = 
    async {
        let! price = calculatePrice (1, "FREE-STUFF!!")
        Assert.AreEqual(100M, price)
    }
