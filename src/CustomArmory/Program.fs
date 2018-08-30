module CustomArmory.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open GiraffeViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "CustomArmory" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
                script [] [ rawText "var whTooltips = {colorLinks: true, iconizeLinks: true, renameLinks: false, iconSize: 'large'};" ]
                script [ _async; _src "https://wow.zamimg.com/widgets/power.js" ] []
            ]
            body [] content
        ]

    let achievementLink' (id,crs) =
        let earned = Map.tryFind id Data.completedAchievements
        a [ _href (sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id (if earned.IsSome then "Kosiilspaan" else "") (Option.defaultValue 0L earned) );  _rel (Data.filterCriteria crs); _class (if Map.containsKey id Data.completedAchievements then "" else "missing") ] []

    let achievementLink2 (a:Data.AllAchievements.Achievement2) =
        achievementLink' (a.Id,a.Criteria |> Array.map (fun c -> c.Id))

    let achievementLink3 (a:Data.AllAchievements.Achievement3) =
        achievementLink' (a.Id,a.Criteria |> Array.map (fun c -> c.Id))

    let index (model : Data.AllAchievements.Achievement[]) =
        [ol [] [yield! model |> Array.map (fun c ->
            li [] [
                h2 [] [ encodedText c.Name ]
                div [] [yield! c.Achievements |> Array.map achievementLink2 ]
                ol [] [yield! c.Categories |> Array.map (fun cat -> li [] [
                    h3 [] [encodedText cat.Name]
                    div [] [yield! cat.Achievements |> Array.map achievementLink3 ]
                ])]
            ])]]
        |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    let model     = Data.categories
    let view      = Views.index model
    htmlView view

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "world"
                routef "/hello/%s" indexHandler
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