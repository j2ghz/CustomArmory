module CustomArmory.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Views
open Giraffe
open FSharp.Data.Runtime

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }


// ---------------------------------
// Web app
// ---------------------------------

let indexHandler =
    let model     = Data.categories
    let view      = Views.index model (Data.character "Kosiilspaan")
    htmlView view

let calendarHandler =
    let char =
        "Kosiilspaan"
        |> Data.character
    let achievements =
        char
        |> Data.completedAchievements
        |> Seq.map (fun (id,time) -> time, Giraffe.GiraffeViewEngine.a [ Giraffe.GiraffeViewEngine.Attributes._href (sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id "Kosiilspaan" time ) ] [])
    let criteria =
        char
        |> Data.criteriaDate
        |> Seq.map (fun (id,time) -> time, Giraffe.GiraffeViewEngine.p [] [string id |> GiraffeViewEngine.encodedText])
    let model =
        achievements
        |> Seq.groupBy (fun (timestamp,_) -> (DateTimeOffset.FromUnixTimeMilliseconds timestamp).Date)
        |> Seq.sortBy fst
    let view      = Views.calendar model
    htmlView view

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler
                route "/calendar" >=> calendarHandler
            ]
        setStatusCode 404 >=> text "Not Found" ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IHostingEnvironment>()
    (match env.IsDevelopment() with
    | true  -> app.UseDeveloperExceptionPage()
    | false -> app.UseGiraffeErrorHandler errorHandler)
        .UseHttpsRedirection()
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    services.AddCors()    |> ignore
    services.AddGiraffe() |> ignore

let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder(*.AddFilter(filter)*).AddConsole().AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .UseApplicationInsights()
        .Build()
        .Run()
    0