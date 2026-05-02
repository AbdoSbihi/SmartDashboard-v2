namespace SmartDashboard

open System
open WebSharper

// ── WEATHER 
[<JavaScript>]
type WeatherCondition = {
    Main        : string
    Description : string
    Icon        : string
}

[<JavaScript>]
type WeatherData = {
    City      : string
    Country   : string
    TempC     : float
    FeelsLike : float
    Humidity  : int
    WindSpeed : float
    Condition : WeatherCondition
    FetchedAt : DateTime
}

[<JavaScript>]
type ForecastDay = {
    Date    : string
    TempMin : float
    TempMax : float
    Icon    : string
    Desc    : string
}

// ── NEWS 
[<JavaScript>]
type NewsArticle = {
    Title       : string
    Description : string
    Url         : string
    Source      : string
    PublishedAt : string
    ImageUrl    : string
}

[<JavaScript>]
type NewsCategory =
    | General
    | Technology
    | Science
    | Sports
    | Business
    | Health

    member this.Label =
        match this with
        | General    -> "General"
        | Technology -> "Technology"
        | Science    -> "Science"
        | Sports     -> "Sports"
        | Business   -> "Business"
        | Health     -> "Health"

    member this.ApiValue =
        match this with
        | General    -> "general"
        | Technology -> "technology"
        | Science    -> "science"
        | Sports     -> "sports"
        | Business   -> "business"
        | Health     -> "health"

[<JavaScript>]
module NewsCategory =
    let all = [ General; Technology; Science; Sports; Business; Health ]

// ── CURRENCY 
[<JavaScript>]
type CurrencyRate = {
    Code : string
    Name : string
    Rate : float
    Flag : string
}

[<JavaScript>]
type CurrencyRates = {
    Base      : string
    Rates     : CurrencyRate list
    UpdatedAt : string
}

[<JavaScript>]
module Currency =
    let supported = [
        ("USD", "US Dollar",         "🇺🇸")
        ("EUR", "Euro",              "🇪🇺")
        ("GBP", "British Pound",     "🇬🇧")
        ("JPY", "Japanese Yen",      "🇯🇵")
        ("HUF", "Hungarian Forint",  "🇭🇺")
        ("CHF", "Swiss Franc",       "🇨🇭")
        ("CAD", "Canadian Dollar",   "🇨🇦")
        ("AUD", "Australian Dollar", "🇦🇺")
        ("CNY", "Chinese Yuan",      "🇨🇳")
        ("MAD", "Moroccan Dirham",   "🇲🇦")
    ]
    let codes = supported |> List.map (fun (c,_,_) -> c)

// ── WIDGET STATE 
[<JavaScript>]
type WidgetState<'T> =
    | Idle
    | Fetching
    | Loaded of 'T
    | Failed of string

// ── APP STATE ─────────────────────────────────────────────────
[<JavaScript>]
type AppState = {
    Weather      : WidgetState<WeatherData>
    Forecast     : WidgetState<ForecastDay list>
    News         : WidgetState<NewsArticle list>
    Currency     : WidgetState<CurrencyRates>
    City         : string
    NewsCategory : NewsCategory
    BaseCurrency : string
    Amount       : float
}

[<JavaScript>]
module AppState =
    let init = {
        Weather      = Idle
        Forecast     = Idle
        News         = Idle
        Currency     = Idle
        City         = "Budapest"
        NewsCategory = Technology
        BaseCurrency = "USD"
        Amount       = 1.0
    }

// ── DASHBOARD CONFIG 
[<JavaScript>]
type DashboardConfig = {
    DefaultCity     : string
    DefaultCategory : string
    DefaultCurrency : string
}

[<JavaScript>]
module DashboardConfig =
    let defaultConfig = {
        DefaultCity     = "Budapest"
        DefaultCategory = "technology"
        DefaultCurrency = "USD"
    }