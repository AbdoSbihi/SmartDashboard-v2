namespace SmartDashboard

open System
open System.Net.Http
open System.Text.Json
open WebSharper
open Microsoft.Extensions.Configuration

module private Http =
    let client = new HttpClient()
    client.Timeout <- TimeSpan.FromSeconds(10.0)
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SmartDashboard/1.0")

    let getJson (url: string) =
        async {
            let! resp = client.GetAsync(url) |> Async.AwaitTask
            let! body = resp.Content.ReadAsStringAsync() |> Async.AwaitTask
            if not resp.IsSuccessStatusCode then
                let code = int resp.StatusCode
                failwithf "HTTP %d: %s" code body
            return JsonDocument.Parse(body)
        }

module private Config =
    let mutable private cfg : IConfiguration = null
    let init (c: IConfiguration) = cfg <- c
    let get (key: string) =
        if cfg = null then
            failwith "Configuration not initialized. Call Server.configure first."
        let v = cfg.[key]
        if String.IsNullOrEmpty(v) then
            failwithf "Missing config key '%s' in appsettings.json." key
        v

module Server =

    let private tryGet (el: JsonElement) (key: string) : JsonElement option =
        let mutable tmp = Unchecked.defaultof<JsonElement>
        if el.TryGetProperty(key, &tmp) then Some tmp
        else None

    let private str (el: JsonElement) (key: string) : string =
        match tryGet el key with
        | Some v ->
            let s = v.GetString()
            if s = null then "" else s
        | None -> ""

    let private flt (el: JsonElement) (key: string) : float =
        match tryGet el key with
        | Some v -> v.GetDouble()
        | None   -> 0.0

    let private intv (el: JsonElement) (key: string) : int =
        match tryGet el key with
        | Some v -> v.GetInt32()
        | None   -> 0

    // ── GetWeather 
    [<Rpc>]
    let GetWeather (city: string) : Async<Result<WeatherData, string>> =
        async {
            try
                let key = Config.get "OpenWeatherKey"
                let url = sprintf
                            "https://api.openweathermap.org/data/2.5/weather?q=%s&appid=%s&units=metric"
                            (Uri.EscapeDataString(city)) key
                let! doc  = Http.getJson url
                let root  = doc.RootElement
                let cod =
                    match tryGet root "cod" with
                    | Some v -> v.ToString()
                    | None   -> "200"
                if cod <> "200" then
                    return Error (sprintf "City '%s' not found." city)
                else
                    let main    = root.GetProperty("main")
                    let wind    = root.GetProperty("wind")
                    let weather = root.GetProperty("weather").[0]
                    let sys     = root.GetProperty("sys")
                    return Ok {
                        City      = str root "name"
                        Country   = str sys  "country"
                        TempC     = flt main "temp"
                        FeelsLike = flt main "feels_like"
                        Humidity  = intv main "humidity"
                        WindSpeed = flt wind "speed"
                        Condition = {
                            Main        = str weather "main"
                            Description = str weather "description"
                            Icon        = str weather "icon"
                        }
                        FetchedAt = DateTime.UtcNow
                    }
            with ex ->
                return Error (sprintf "Weather: %s" ex.Message)
        }

    // ── GetForecast 
    [<Rpc>]
    let GetForecast (city: string) : Async<Result<ForecastDay list, string>> =
        async {
            try
                let key = Config.get "OpenWeatherKey"
                let url = sprintf
                            "https://api.openweathermap.org/data/2.5/forecast?q=%s&appid=%s&units=metric&cnt=40"
                            (Uri.EscapeDataString(city)) key
                let! doc   = Http.getJson url
                let root   = doc.RootElement
                let items  = root.GetProperty("list")
                let days =
                    [ for i in 0 .. items.GetArrayLength() - 1 do
                        let item = items.[i]
                        let dt   = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).DateTime
                        let main = item.GetProperty("main")
                        let w    = item.GetProperty("weather").[0]
                        let tmin = flt main "temp_min"
                        let tmax = flt main "temp_max"
                        let icon = str w "icon"
                        let desc = str w "description"
                        yield (dt.Date, tmin, tmax, icon, desc) ]
                    |> List.groupBy (fun (d,_,_,_,_) -> d)
                    |> List.truncate 5
                    |> List.map (fun (date, entries) ->
                        let minT = entries |> List.map (fun (_,mn,_,_,_) -> mn) |> List.min
                        let maxT = entries |> List.map (fun (_,_,mx,_,_) -> mx) |> List.max
                        let (_,_,_,icon,desc) = entries.[entries.Length / 2]
                        { Date    = date.ToString("ddd dd")
                          TempMin = minT
                          TempMax = maxT
                          Icon    = icon
                          Desc    = desc })
                return Ok days
            with ex ->
                return Error (sprintf "Forecast: %s" ex.Message)
        }

    // ── GetNews 
    [<Rpc>]
    let GetNews (category: string) : Async<Result<NewsArticle list, string>> =
        async {
            try
                let key = Config.get "NewsApiKey"
                // Try top-headlines first
                let url = sprintf
                            "https://newsapi.org/v2/top-headlines?category=%s&language=en&pageSize=9&apiKey=%s"
                            category key
                let! doc    = Http.getJson url
                let root    = doc.RootElement
                let status  = str root "status"
                if status <> "ok" then
                    let msg = str root "message"
                    return Error (sprintf "NewsAPI: %s" msg)
                else
                    let articles = root.GetProperty("articles")
                    let result =
                        [ for i in 0 .. articles.GetArrayLength() - 1 do
                            let a = articles.[i]
                            let src =
                                match tryGet a "source" with
                                | Some s -> str s "name"
                                | None   -> ""
                            let imgUrl =
                                match tryGet a "urlToImage" with
                                | Some v ->
                                    let s = v.GetString()
                                    if s = null then "" else s
                                | None -> ""
                            let desc =
                                match tryGet a "description" with
                                | Some v ->
                                    let s = v.GetString()
                                    if s = null then "" else s
                                | None -> ""
                            let title = str a "title"
                            if title <> "" && title <> "[Removed]" then
                                yield {
                                    Title       = title
                                    Description = desc
                                    Url         = str a "url"
                                    Source      = src
                                    PublishedAt = str a "publishedAt"
                                    ImageUrl    = imgUrl
                                } ]
                    return Ok result
            with ex ->
                let msg = ex.Message
                if msg.Contains("426") || msg.Contains("400") then
                    return Error "NewsAPI free tier does not support localhost. Deploy the app or upgrade your NewsAPI plan."
                else
                    return Error (sprintf "News: %s" msg)
        }

    // ── GetCurrencyRates 
    [<Rpc>]
    let GetCurrencyRates (baseCurrency: string) : Async<Result<CurrencyRates, string>> =
        async {
            try
                let symbols = Currency.codes |> List.filter (fun c -> c <> baseCurrency) |> String.concat ","
                let url = sprintf
                            "https://api.frankfurter.app/latest?from=%s&to=%s"
                            baseCurrency symbols
                let! doc    = Http.getJson url
                let root    = doc.RootElement
                let ratesEl = root.GetProperty("rates")
                let date    = str root "date"
                let rates =
                    Currency.supported
                    |> List.choose (fun (code, name, flag) ->
                        if code = baseCurrency then
                            Some { Code = code; Name = name; Rate = 1.0; Flag = flag }
                        else
                            let mutable tmp = Unchecked.defaultof<JsonElement>
                            if ratesEl.TryGetProperty(code, &tmp) then
                                Some { Code = code; Name = name; Rate = tmp.GetDouble(); Flag = flag }
                            else None)
                return Ok {
                    Base      = baseCurrency
                    Rates     = rates
                    UpdatedAt = date
                }
            with ex ->
                return Error (sprintf "Currency: %s" ex.Message)
        }

    let mutable private savedConfig = DashboardConfig.defaultConfig

    [<Rpc>]
    let SaveConfig (cfg: DashboardConfig) : Async<unit> =
        async { savedConfig <- cfg }

    [<Rpc>]
    let GetConfig () : Async<DashboardConfig> =
        async { return savedConfig }

    let configure (cfg: IConfiguration) =
        Config.init cfg