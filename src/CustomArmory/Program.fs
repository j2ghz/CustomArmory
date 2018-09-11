module CustomArmory.App

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Views

// ---------------------------------
// Web app
// ---------------------------------

let calendarHandler (server,realm,character) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! char =
                character
                |> BattleNetApi.character
            let model =
                char
                |> BattleNetApi.achievements
                |> Map.toSeq
                |> Seq.map (fun (id,time) -> time, GiraffeViewEngine.a [ GiraffeViewEngine.Attributes._href (sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id character time ) ] [])
                |> Seq.groupBy (fun (timestamp,_) -> (DateTimeOffset.FromUnixTimeMilliseconds timestamp).Date)
                |> Seq.sortBy fst
            let view      = Views.calendar model
            return! htmlView view next ctx
        }

let storylinesHandler (server,realm,character) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! charData =
                BattleNetApi.characterUrl "" server realm character
                |> BattleNetApi.character
            let storylineData = Storylines.fromData charData
            let model =
                StorylineData.storylines
                |> List.map storylineData
            let view = Views.storylines model
            return! htmlView view next ctx
        }

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> redirectTo false "/eu/chamber-of-aspects/Kosiilspaan/storylines"
                subRoutef "/%s/%s/%s" (fun src ->
                    choose [
                        route "/calendar" >=> calendarHandler src
                        route "/storylines" >=> storylinesHandler src
                    ]
                )                
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