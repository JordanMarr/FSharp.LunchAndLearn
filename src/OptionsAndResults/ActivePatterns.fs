/// Active patterns can be used as smart "parsers" that can recognize patterns and pre-format the data for you.
/// They make F# Pattern Matching infinitely customizable for your domain.
namespace ActivePatterns

/// Multiple choice Active Pattern provides a finite list of results in the form of a Discriminated Union.
module ``Multiple Choice - HTML Color Parser`` = 
    open System
    open System.Drawing

    /// An active pattern that parses an html background color string.
    let (|RGBColor|NamedColor|NoColor|) (color: string) = 
        let color = color.Replace(" !important", "")
        if color.StartsWith("rgb(") then
            let rgb = 
                color.Split([| "rgb("; ", "; ")" |], StringSplitOptions.RemoveEmptyEntries) 
                |> Array.toList
                |> List.map int
            match rgb with
            | [r;g;b] -> RGBColor (r,g,b)
            | _ -> NoColor
        else 
            NamedColor color

    let color1 = 
        match "rgb(20, 5, 255)" with
        | RGBColor (r,g,b) -> Color.FromArgb(r,g,b)
        | NamedColor name -> Color.FromName(name)
        | NoColor -> Color.White


module ``Multiple Choice - Level Parser`` =
    open System
    
    /// Parses a level description.
    let (|Basement|Roof|Floor|InvalidLevel|) (levelDescription: string) = 
        let ucaseDesc = (string levelDescription).ToUpper()
        if ucaseDesc.StartsWith("LEVEL: ") then 
            let floorValue = ucaseDesc.Replace("LEVEL: ", "")
            match Int32.TryParse(floorValue) with
            | true, number -> Floor number
            | false, _ -> 
                match floorValue with
                | "BASEMENT" -> Basement
                | "ROOF" -> Roof
                | _ -> InvalidLevel
        else
            InvalidLevel


    let transformFloorText (levelDescriptor: string) = 
        match levelDescriptor with
        | Floor number -> $"The floor number is: {number}"
        | Basement -> "This is the basement"
        | Roof -> "This is the roof"
        | InvalidLevel -> $"Invalid level descriptor: {levelDescriptor}"

    let f1 = transformFloorText "LEVEL: 1"
    let f2 = transformFloorText "LEVEL: 20"
    let f3 = transformFloorText "LEVEL: Basement"
    let f4 = transformFloorText "LEVEL: Roof"


/// Partial patterns can be "sprinkled in" with other pattern matches.
/// This makes them very powerful as they are more "general purpose" and reusable.
module ``Partial Patterns - IsNullOrWhiteSpace parser`` =
    open System

    let (|NullOrWhiteSpace|_|) (str: string) = 
        if String.IsNullOrWhiteSpace str
        then Some NullOrWhiteSpace
        else None

    let transformPositionCode (positionCode: string) = 
        match positionCode with
        | NullOrWhiteSpace -> "---"
        | "1" -> "First Position"
        | "2" -> "Second Position"
        | other -> $"Unrecognized Position: {other}"

    let p0 = transformPositionCode null
    let p1 = transformPositionCode "1"
    let p2 = transformPositionCode "2"
    let p3 = transformPositionCode "side"



