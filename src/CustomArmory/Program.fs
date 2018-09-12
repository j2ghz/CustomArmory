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
open Microsoft.Extensions.Configuration

let config (ctx : HttpContext) key = ctx.GetService<IConfiguration>().[key]


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
                BattleNetApi.characterUrl (config ctx "BattleNetApiKey") server realm character
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
    builder.AddConsole().AddDebug() |> ignore

let configureAppConfiguration (args:string[]) (context: WebHostBuilderContext) (config: IConfigurationBuilder) =  
    config
        .AddJsonFile("appsettings.json",false)
        .AddJsonFile(sprintf "appsettings.%s.json" context.HostingEnvironment.EnvironmentName ,true)
        .AddJsonFile("appsettings.secrets.json",true)
        .AddEnvironmentVariables()
        //.AddUserSecrets() https://github.com/Microsoft/visualfsharp/issues/5549#issuecomment-417804392 Wait for release 2.2
        .AddCommandLine(args)
        |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .ConfigureAppConfiguration(configureAppConfiguration args)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .UseApplicationInsights()
        .Build()
        .Run()
    0