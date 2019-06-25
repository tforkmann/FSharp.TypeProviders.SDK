#if INTERACTIVE
#load "../src/ProvidedTypes.fsi" "../src/ProvidedTypes.fs" 
#load "../src/ProvidedTypesTesting.fs"

#else

module TPSDK.GeneratedOpTests
#endif

#if !NO_GENERATIVE

open System
open System.Reflection
open Microsoft.FSharp.Core.CompilerServices
open Xunit
open ProviderImplementation.ProvidedTypes
open ProviderImplementation.ProvidedTypesTesting
open Microsoft.FSharp.Quotations

#nowarn "760" // IDisposable needs new

let testCases() = 
    [("F# 3.1 Portable 259", "3.259.3.1", (fun _ ->  Targets.hasPortable259Assemblies()), Targets.Portable259FSharp31Refs)
     ("F# 4.0 Portable 259", "3.259.4.0", (fun _ ->  Targets.hasPortable259Assemblies() && Targets.supportsFSharp40()), Targets.Portable259FSharp40Refs)
     ("F# 3.1 .NET 4.5", "4.3.1.0", (fun _ ->  Targets.supportsFSharp31()), Targets.DotNet45FSharp31Refs)
     ("F# 4.0 .NET 4.5", "4.4.0.0", (fun _ ->  Targets.supportsFSharp40()), Targets.DotNet45FSharp40Refs)
     ("F# 4.1 .NET 4.5", "4.4.1.0", (fun _ ->  true), Targets.DotNet45FSharp41Refs)
     ("F# 4.1 .NET Standard 2.0", "4.4.1.0", (fun _ ->  true), Targets.DotNetStandard20FSharp41Refs)
     ("F# 4.1 .NET CoreApp 2.0", "4.4.1.0", (fun _ ->  true), Targets.DotNetCoreApp20FSharp41Refs) ]

let possibleVersions = 
    [ "3.259.3.1"
      "3.259.4.0"
      "4.3.1.0"
      "4.4.0.0"
      "4.4.1.0"
      "4.4.3.0"
      (typeof<list<int>>.Assembly.GetName().Version.ToString()) ]

let hostedTestCases() = 
    [("4.4.0.0", (fun _ ->  Targets.supportsFSharp40()), Targets.DotNet45FSharp40Refs) ]


let testProvidedAssembly exprs = 
    if Targets.supportsFSharp40() then
        let runtimeAssemblyRefs = Targets.DotNet45FSharp40Refs()
        let runtimeAssembly = runtimeAssemblyRefs.[0]
        let cfg = Testing.MakeSimulatedTypeProviderConfig (__SOURCE_DIRECTORY__, runtimeAssembly, runtimeAssemblyRefs) 
        let tp = TypeProviderForNamespaces(cfg) //:> TypeProviderForNamespaces
        let ns = "Tests"
        let tempAssembly = ProvidedAssembly()
        let container = ProvidedTypeDefinition(tempAssembly, ns, "Container", Some typeof<obj>, isErased = false)
        let mutable counter = 0
        
        let create (expr : Expr) =  
            counter <- counter + 1
            let name = sprintf "F%d" counter
            ProvidedMethod(name,[],expr.Type,invokeCode = (fun _args -> expr), isStatic = true)
            |> container.AddMember
            name
        let names = exprs |> List.map (fst >> create)
        do tempAssembly.AddTypes [container]
        do tp.AddNamespace(container.Namespace, [container])
        let providedNamespace = tp.Namespaces.[0] 
        let providedTypes  = providedNamespace.GetTypes()
        let providedType = providedTypes.[0] 
        let providedTypeDefinition = providedType :?> ProvidedTypeDefinition
        Assert.Equal("Container", providedTypeDefinition.Name)
        let test (container : Type) = 
            let call name = container.GetMethod(name).Invoke(null,[||])
            (names, exprs)
            ||> List.iter2 (fun name (_,f) -> f(call name))
        let assemContents = (tp :> ITypeProvider).GetGeneratedAssemblyContents(providedTypeDefinition.Assembly)
        let assembly = Assembly.Load assemContents
        assembly.ExportedTypes |> Seq.find (fun ty -> ty.Name = "Container") |> test

let runningOnMono = try Type.GetType("Mono.Runtime") <> null with _ -> false 

let check (e : Expr<'a>) expected = 
    e.Raw, fun o -> 
        let actual = Assert.IsType<'a>(o)
        Assert.True((expected = actual), sprintf "Expected %A got %A. (%A)" expected actual e)

let checkExpr (e : Expr<'a>) = 
    let expected = FSharp.Linq.RuntimeHelpers.LeafExpressionConverter.EvaluateQuotation(e) :?> 'a
    check e expected


[<Fact>]
let ``sub execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ 50 - 30 @>
            checkExpr <@ 50.0 - 30.0 @>
            checkExpr <@ 50.f - 30.f @>
            checkExpr <@ 50L - 30L @>
            checkExpr <@ 50UL - 30UL @>
            checkExpr <@ 50l - 30l @>
            checkExpr <@ 50s - 30s @>
            checkExpr <@ 50us - 30us @>
            check <@ 50y - 30y @> 20y
            check <@ 50uy - 30uy @> 20uy
            checkExpr <@ 50m - 30m @>
            checkExpr <@ TimeSpan.FromMinutes 50.0 - TimeSpan.FromMinutes 30.0 @>
        ]

[<Fact>]
let ``add execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ 50 + 30 @>
            checkExpr <@ 50.0 + 30.0 @>
            checkExpr <@ 50.f + 30.f @>
            checkExpr <@ 50L + 30L @>
            checkExpr <@ 50UL + 30UL @>
            checkExpr <@ 50l + 30l @>
            checkExpr <@ 50s + 30s @>
            checkExpr <@ 50us + 30us @>
            check <@ 50y + 30y @> 80y
            check <@ 50uy + 30uy @> 80uy
            checkExpr <@ 50m + 30m @>
            checkExpr <@ TimeSpan.FromMinutes 50.0 + TimeSpan.FromMinutes 30.0 @>
        ]        
        
[<Fact>]
let ``mul execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ 50 * 30 @>
            checkExpr <@ 50.0 * 30.0 @>
            checkExpr <@ 50.f * 30.f @>
            checkExpr <@ 50L * 30L @>
            checkExpr <@ 50UL * 30UL @>
            checkExpr <@ 50l * 30l @>
            checkExpr <@ 50s * 30s @>
            checkExpr <@ 50us * 30us @>
            check <@ 5y * 3y @> 15y
            check <@ 5uy * 3uy @> 15uy
            checkExpr <@ 50m * 30m @>
            //TODO: mul method test
        ]         
        
[<Fact>]
let ``div execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ 50 / 30 @>
            checkExpr <@ 50.0 / 30.0 @>
            checkExpr <@ 50.f / 30.f @>
            checkExpr <@ 50L / 30L @>
            checkExpr <@ 50UL / 30UL @>
            checkExpr <@ 50l / 30l @>
            checkExpr <@ 50s / 30s @>
            checkExpr <@ 50us / 30us @>
            checkExpr <@ 50 / 10 @>
            checkExpr <@ 50.0 / 10.0 @>
            checkExpr <@ 50.f / 10.f @>
            checkExpr <@ 50L / 30L @>
            checkExpr <@ 50UL / 10UL @>
            checkExpr <@ 50l / 10l @>
            checkExpr <@ 50s / 10s @>
            checkExpr <@ 50us / 10us @>
            check <@ 50y / 10y @> 5y
            check <@ 50uy / 10uy @> 5uy
            checkExpr <@ 50m / 30m @>
            //TODO: mul method test
        ]        

[<Fact>]
let ``neg execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ (~-) 50 @>
            checkExpr <@ (~-) 30.0 @>
            checkExpr <@ (~-) 30.f @>
            checkExpr <@ (~-) 30L @>
            checkExpr <@ (~-) 30l @>
            checkExpr <@ (~-) 30s @>
            check <@ (~-) 30y @> -30y
            checkExpr <@ (~-) 30m @>
            checkExpr <@ (~-) (TimeSpan.FromMinutes 50.0) @>
        ]     

[<Fact>]
let ``pos execute correctly``() =
    testProvidedAssembly 
        [
            check <@ (~+) 50 @> 50
            check <@ (~+) 30.0 @> 30.0
            check <@ (~+) 30.f @> 30.f
            check <@ (~+) 30L @> 30L
            check <@ (~+) 30l @> 30l
            check <@ (~+) 30s @> 30s
            check <@ (~+) 30y @> 30y
            check <@ (~+) 30m @> 30m
            check <@ (~+) (TimeSpan.FromMinutes 50.0) @> (TimeSpan.FromMinutes 50.0)
        ]     


[<Fact>]
let ``rem execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ 50 % 30 @>
            checkExpr <@ 50.0 % 30.0 @>
            checkExpr <@ 50.f % 30.f @>
            checkExpr <@ 50L % 30L @>
            checkExpr <@ 50UL % 30UL @>
            checkExpr <@ 50l % 30l @>
            checkExpr <@ 50s % 30s @>
            checkExpr <@ 50us % 30us @>
            check <@ 50y % 30y @> 20y
            check <@ 50uy % 30uy @> 20uy
            checkExpr <@ 50m % 30m @>
            //TODO: rem method test
        ]


[<Fact>]
let ``shl execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ 2 <<< 3 @>
            checkExpr <@ 2L <<< 3 @>
            checkExpr <@ 5UL <<< 3 @>
            checkExpr <@ 5l <<< 3 @>
            checkExpr <@ 5s <<< 3 @>
            checkExpr <@ 5us <<< 3 @>
            checkExpr <@ 5y <<< 3 @>
            checkExpr <@ 5uy <<< 3 @>
            checkExpr <@ 2 <<< 1 @>
            checkExpr <@ 2L <<< 1 @>
            checkExpr <@ 5UL <<< 1 @>
            checkExpr <@ 5l <<< 1 @>
            checkExpr <@ 5s <<< 1 @>
            checkExpr <@ 5us <<< 1 @>
            checkExpr <@ 5y <<< 1 @>
            checkExpr <@ 5uy <<< 1 @>
        ]

[<Fact>]
let ``shr execute correctly``() =
    testProvidedAssembly 
        [
            checkExpr <@ 2 >>> 3 @>
            checkExpr <@ 2L >>> 3 @>
            checkExpr <@ 5UL >>> 3 @>
            checkExpr <@ 5l >>> 3 @>
            checkExpr <@ 5s >>> 3 @>
            checkExpr <@ 5us >>> 3 @>
            checkExpr <@ 5y >>> 3 @>
            checkExpr <@ 5uy >>> 3 @>
            checkExpr <@ 2 >>> 1 @>
            checkExpr <@ 2L >>> 1 @>
            checkExpr <@ 5UL >>> 1 @>
            checkExpr <@ 5l >>> 1 @>
            checkExpr <@ 5s >>> 1 @>
            checkExpr <@ 5us >>> 1 @>
            checkExpr <@ 5y >>> 1 @>
            checkExpr <@ 5uy >>> 1 @>
        ]


[<Fact>]
let ``int execute correctly``() =
    testProvidedAssembly 
        [
            check <@ int "50" @> 50
            checkExpr <@ int 50 @>
            checkExpr <@ int 50.0 @>
            checkExpr <@ int 50.f @>
            checkExpr <@ int 50UL @>
            checkExpr <@ int 50l @>
            checkExpr <@ int 50s @>
            checkExpr <@ int 50us @>
            checkExpr <@ int 50y @>
            checkExpr <@ int 50uy @> 
            checkExpr <@ int 50m @>
            checkExpr <@ int 50I @>
        ]
    
[<Fact>]
let ``int32 execute correctly``() =
    testProvidedAssembly 
        [
            check <@ int32 "50" @> 50
            checkExpr <@ int32 50 @>
            checkExpr <@ int32 50.0 @>
            checkExpr <@ int32 50.f @>
            checkExpr <@ int32 50UL @>
            checkExpr <@ int32 50l @>
            checkExpr <@ int32 50s @>
            checkExpr <@ int32 50us @>
            checkExpr <@ int32 50y @>
            checkExpr <@ int32 50uy @> 
            checkExpr <@ int32 50m @>
            checkExpr <@ int32 50I @>
        ]


[<Fact>]
let ``int64 execute correctly``() =
    testProvidedAssembly 
        [
            check <@ int64 "50" @> 50L
            checkExpr <@ int64 50 @>
            checkExpr <@ int64 50.0 @>
            checkExpr <@ int64 50.f @>
            checkExpr <@ int64 50UL @>
            checkExpr <@ int64 50l @>
            checkExpr <@ int64 50s @>
            checkExpr <@ int64 50us @>
            checkExpr <@ int64 50y @>
            checkExpr <@ int64 50uy @> 
            checkExpr <@ int64 50m @>
            checkExpr <@ int64 50I @>
        ]

[<Fact>]
let ``int16 execute correctly``() =
    testProvidedAssembly 
        [
            check <@ int16 "50" @> 50s
            checkExpr <@ int16 50 @>
            checkExpr <@ int16 50.0 @>
            checkExpr <@ int16 50.f @>
            checkExpr <@ int16 50UL @>
            checkExpr <@ int16 50l @>
            checkExpr <@ int16 50s @>
            checkExpr <@ int16 50us @>
            checkExpr <@ int16 50y @>
            checkExpr <@ int16 50uy @> 
            checkExpr <@ int16 50m @>
            checkExpr <@ int16 50I @>
        ]


[<Fact>]
let ``uint32 execute correctly``() =
    testProvidedAssembly 
        [
            check <@ uint32 "50" @> 50u
            checkExpr <@ uint32 50 @>
            checkExpr <@ uint32 50.0 @>
            checkExpr <@ uint32 50.f @>
            checkExpr <@ uint32 50UL @>
            checkExpr <@ uint32 50l @>
            checkExpr <@ uint32 50s @>
            checkExpr <@ uint32 50us @>
            checkExpr <@ uint32 50y @>
            checkExpr <@ uint32 50uy @> 
            checkExpr <@ uint32 50m @>
            checkExpr <@ uint32 50I @>
        ]


[<Fact>]
let ``uint64 execute correctly``() =
    testProvidedAssembly 
        [
            check <@ uint64 "50" @> 50UL
            checkExpr <@ uint64 50 @>
            checkExpr <@ uint64 50.0 @>
            checkExpr <@ uint64 50.f @>
            checkExpr <@ uint64 50UL @>
            checkExpr <@ uint64 50l @>
            checkExpr <@ uint64 50s @>
            checkExpr <@ uint64 50us @>
            checkExpr <@ uint64 50y @>
            checkExpr <@ uint64 50uy @> 
            checkExpr <@ uint64 50m @>
            checkExpr <@ uint64 50I @>
        ]

[<Fact>]
let ``uint16 execute correctly``() =
    testProvidedAssembly 
        [
            check <@ uint16 "50" @> 50us
            checkExpr <@ uint16 50 @>
            checkExpr <@ uint16 50.0 @>
            checkExpr <@ uint16 50.f @>
            checkExpr <@ uint16 50UL @>
            checkExpr <@ uint16 50l @>
            checkExpr <@ uint16 50s @>
            checkExpr <@ uint16 50us @>
            checkExpr <@ uint16 50y @>
            checkExpr <@ uint16 50uy @> 
            checkExpr <@ uint16 50m @>
            checkExpr <@ uint16 50I @>
        ]

[<Fact>]
let ``byte execute correctly``() =
    testProvidedAssembly 
        [
            check <@ byte "50" @> 50uy
            checkExpr <@ byte 50 @>
            checkExpr <@ byte 50.0 @>
            checkExpr <@ byte 50.f @>
            checkExpr <@ byte 50UL @>
            checkExpr <@ byte 50l @>
            checkExpr <@ byte 50s @>
            checkExpr <@ byte 50us @>
            checkExpr <@ byte 50y @>
            checkExpr <@ byte 50uy @> 
            checkExpr <@ byte 50m @>
            checkExpr <@ byte 50I @>
        ]


[<Fact>]
let ``sbyte execute correctly``() =
    testProvidedAssembly 
        [
            check <@ sbyte "50" @> 50y
            checkExpr <@ sbyte 50 @>
            checkExpr <@ sbyte 50.0 @>
            checkExpr <@ sbyte 50.f @>
            checkExpr <@ sbyte 50UL @>
            checkExpr <@ sbyte 50l @>
            checkExpr <@ sbyte 50s @>
            checkExpr <@ sbyte 50us @>
            checkExpr <@ sbyte 50y @>
            checkExpr <@ sbyte 50uy @> 
            checkExpr <@ sbyte 50m @>
            checkExpr <@ sbyte 50I @>
        ]

#endif