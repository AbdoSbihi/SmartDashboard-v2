module SmartDashboard.Startup

open System
open System.Diagnostics
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open WebSharper.AspNetCore

type Startup(configuration: IConfiguration) =

    member _.ConfigureServices(services: IServiceCollection) =
        Server.configure configuration
        services
            .AddSitelet(Site.Main)
            .AddWebSharper()
        |> ignore
        services.AddRouting() |> ignore

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
        let df = DefaultFilesOptions()
        df.DefaultFileNames.Clear()
        df.DefaultFileNames.Add("Main.html")
        app.UseDefaultFiles(df) |> ignore
        app.UseStaticFiles()    |> ignore
        app.UseWebSharper()     |> ignore

let openBrowser (url: string) =
    try
        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            Process.Start(ProcessStartInfo(url, UseShellExecute = true)) |> ignore
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            Process.Start("open", url) |> ignore
        else
            Process.Start("xdg-open", url) |> ignore
    with ex ->
        printfn "Could not open browser: %s" ex.Message

[<EntryPoint>]
let main argv =
    let port = Environment.GetEnvironmentVariable("PORT")
    let url  =
        if port <> null then "http://0.0.0.0:" + port
        else "http://localhost:5000"
    let host =
        Host.CreateDefaultBuilder(argv)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder.UseStartup<Startup>() |> ignore
                webBuilder.UseUrls(url)           |> ignore)
            .Build()
    host.Start()
    if port = null then
        Threading.Thread.Sleep(500)
        openBrowser url
    printfn "SmartDashboard running at %s" url
    host.WaitForShutdown()
    0