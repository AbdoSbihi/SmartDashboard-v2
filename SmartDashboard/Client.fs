namespace SmartDashboard

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open WebSharper.UI.Notation

[<JavaScript>]
module Client =

    // ── STATE ────────────────────────────────────────────────
    let state       : Var<AppState> = Var.Create AppState.init
    let cityInput   : Var<string>   = Var.Create "Budapest"
    let amountInput : Var<string>   = Var.Create "1"

    // ── HELPERS ──────────────────────────────────────────────
    let getElementValue (el: Dom.Element) : string =
        JS.Get<string> "value" el

    let setWeather  w = state.Value <- { state.Value with Weather  = w }
    let setForecast f = state.Value <- { state.Value with Forecast = f }
    let setNews     n = state.Value <- { state.Value with News     = n }
    let setCurrency c = state.Value <- { state.Value with Currency = c }

    // Using string concat instead of format to avoid float/int tuple confusion
    let weatherIcon (code: string) : string =
        "https://openweathermap.org/img/wn/" + code + "@2x.png"

    // Explicit float param + string concat avoids format string type errors
    let fmtTemp (t: float) : string =
        let rounded = Math.Round(t, 1)
        string rounded + "°C"

    // ── ELEMENT HELPERS ──────────────────────────────────────
    // These wrap Doc.Element to guarantee Doc return type everywhere
    let el (tag: string) (cls: string) (children: Doc list) : Doc =
        Doc.Element tag [attr.``class`` cls] children

    let elA (tag: string) (attrs: Attr list) (children: Doc list) : Doc =
        Doc.Element tag attrs children

    let txt (s: string) : Doc =
        Doc.TextNode s

    // ── LOAD FUNCTIONS ───────────────────────────────────────
    let loadWeather (city: string) =
        setWeather Fetching
        setForecast Fetching
        async {
            let! wr = Server.GetWeather city
            match wr with
            | Ok data ->
                setWeather (Loaded data)
                let! fr = Server.GetForecast city
                match fr with
                | Ok days -> setForecast (Loaded days)
                | Error e -> setForecast (Failed e)
            | Error e ->
                setWeather  (Failed e)
                setForecast (Failed e)
        } |> Async.Start

    let loadNews (cat: NewsCategory) =
        setNews Fetching
        async {
            let! result = Server.GetNews cat.ApiValue
            match result with
            | Ok articles -> setNews (Loaded articles)
            | Error e     -> setNews (Failed e)
        } |> Async.Start

    let loadCurrency (base': string) =
        setCurrency Fetching
        async {
            let! result = Server.GetCurrencyRates base'
            match result with
            | Ok rates -> setCurrency (Loaded rates)
            | Error e  -> setCurrency (Failed e)
        } |> Async.Start

    let loadAll () =
        let s = state.Value
        loadWeather  s.City
        loadNews     s.NewsCategory
        loadCurrency s.BaseCurrency

    // ── WIDGET CARD ──────────────────────────────────────────
    let widgetCard (title: string) (icon: string) (onRefresh: unit -> unit) (content: Doc) : Doc =
        el "div" "widget-card" [
            el "div" "widget-header" [
                el "div" "widget-title" [
                    el "span" "widget-icon" [txt icon]
                    txt title
                ]
                elA "button" [
                    attr.``class`` "btn-refresh"
                    on.click (fun _ _ -> onRefresh ())
                ] [txt "↻"]
            ]
            el "div" "widget-body" [content]
        ]

    // ── WIDGET STATE ─────────────────────────────────────────
    let renderWidgetState (ws: WidgetState<'T>) (render: 'T -> Doc) : Doc =
        match ws with
        | Idle     -> el "div" "widget-idle"    [txt "Click ↻ to load"]
        | Fetching -> el "div" "widget-loading" [el "div" "spinner" []; txt "Loading…"]
        | Failed e -> el "div" "widget-error"   [txt ("Error: " + e)]
        | Loaded d -> render d

    // ── WEATHER ──────────────────────────────────────────────
    let weatherContent (data: WeatherData) : Doc =
        let temp     : float = data.TempC
        let feels    : float = data.FeelsLike
        let wind     : float = data.WindSpeed
        let humidity : int   = data.Humidity
        let humStr = string humidity + "%"
        let windStr = string (Math.Round(wind, 1)) + " m/s"
        el "div" "weather-main" [
            el "div" "weather-top" [
                elA "img" [
                    attr.src (weatherIcon data.Condition.Icon)
                    attr.``class`` "weather-icon-img"
                    attr.alt data.Condition.Description
                ] []
                el "div" "weather-info" [
                    el "div" "weather-temp" [txt (fmtTemp temp)]
                    el "div" "weather-city" [txt (data.City + ", " + data.Country)]
                    el "div" "weather-desc" [txt data.Condition.Description]
                ]
            ]
            el "div" "weather-details" [
                el "div" "weather-detail" [el "span" "detail-label" [txt "Feels like"]; el "span" "detail-value" [txt (fmtTemp feels)]]
                el "div" "weather-detail" [el "span" "detail-label" [txt "Humidity"];   el "span" "detail-value" [txt humStr]]
                el "div" "weather-detail" [el "span" "detail-label" [txt "Wind"];       el "span" "detail-value" [txt windStr]]
            ]
        ]

    let forecastContent (days: ForecastDay list) : Doc =
        let dayDocs : Doc list =
            days |> List.map (fun d ->
                let tmax : float = d.TempMax
                let tmin : float = d.TempMin
                el "div" "forecast-day" [
                    el "div" "forecast-date" [txt d.Date]
                    elA "img" [attr.src (weatherIcon d.Icon); attr.``class`` "forecast-icon"; attr.alt d.Desc] []
                    el "div" "forecast-temps" [
                        el "span" "temp-max" [txt (fmtTemp tmax)]
                        el "span" "temp-min" [txt (fmtTemp tmin)]
                    ]
                ])
        el "div" "forecast-row" dayDocs

    let weatherWidget () : Doc =
        let searchBar =
            el "div" "search-bar" [
                Doc.InputType.Text [attr.``class`` "search-input"; attr.placeholder "Search city…"] cityInput
                elA "button" [
                    attr.``class`` "btn-search"
                    on.click (fun _ _ ->
                        let city = cityInput.Value.Trim()
                        if city <> "" then
                            state.Value <- { state.Value with City = city }
                            loadWeather city)
                ] [txt "Search"]
            ]
        el "div" "weather-widget" [
            searchBar
            state.View |> Doc.BindView (fun s ->
                let wDoc = renderWidgetState s.Weather weatherContent
                let fDoc =
                    match s.Forecast with
                    | Loaded days -> forecastContent days
                    | _           -> Doc.Empty
                el "div" "weather-results" [wDoc; fDoc])
        ]

    // ── NEWS ─────────────────────────────────────────────────
    let newsArticle (a: NewsArticle) : Doc =
        let imgDoc : Doc =
            if a.ImageUrl <> "" then
                elA "img" [attr.src a.ImageUrl; attr.``class`` "news-img"; attr.alt a.Title] []
            else Doc.Empty
        elA "a" [
            attr.href a.Url; attr.target "_blank"
            attr.rel "noopener noreferrer"; attr.``class`` "news-card"
        ] [
            imgDoc
            el "div" "news-content" [
                el "div" "news-source" [txt a.Source]
                el "div" "news-title"  [txt a.Title]
                el "div" "news-desc"   [txt a.Description]
            ]
        ]

    let newsWidget () : Doc =
        let tabDocs : Doc list =
            NewsCategory.all |> List.map (fun cat ->
                state.View |> Doc.BindView (fun s ->
                    let cls = if s.NewsCategory = cat then "tab-btn tab-active" else "tab-btn"
                    elA "button" [
                        attr.``class`` cls
                        on.click (fun _ _ ->
                            state.Value <- { state.Value with NewsCategory = cat }
                            loadNews cat)
                    ] [txt cat.Label]))
        el "div" "news-widget" [
            el "div" "news-tabs" tabDocs
            state.View |> Doc.BindView (fun s ->
                renderWidgetState s.News (fun articles ->
                    el "div" "news-grid" (articles |> List.map newsArticle)))
        ]

    // ── CURRENCY ─────────────────────────────────────────────
    let currencyWidget () : Doc =
        let optDocs : Doc list =
            Currency.supported |> List.map (fun (code, name, flag) ->
                let attrs =
                    if code = state.Value.BaseCurrency
                    then [attr.value code; attr.selected "selected"]
                    else [attr.value code]
                elA "option" attrs [txt (flag + " " + code + " - " + name)])

        let baseSelector =
            el "div" "currency-controls" [
                el "div" "form-field" [
                    elA "label" [] [txt "Base Currency"]
                    elA "select" [
                        attr.``class`` "form-select"
                        on.change (fun el _ ->
                            let code = getElementValue el
                            state.Value <- { state.Value with BaseCurrency = code }
                            loadCurrency code)
                    ] optDocs
                ]
                el "div" "form-field" [
                    elA "label" [] [txt "Amount"]
                    Doc.InputType.Text [attr.``class`` "form-input"; attr.placeholder "1.00"] amountInput
                ]
            ]

        let ratesTable (rates: CurrencyRates) : Doc =
            let amountVal =
                match Double.TryParse(amountInput.Value) with
                | true, v -> v
                | _       -> 1.0
            let rowDocs : Doc list =
                rates.Rates
                |> List.filter (fun r -> r.Code <> rates.Base)
                |> List.map (fun r ->
                    let rate : float = r.Rate
                    let prod : float = amountVal * rate
                    // FIX: bind Math.Round result to avoid (float * int) tuple
                    let conv : float = Math.Round(prod, 2)
                    elA "tr" [] [
                        elA "td" [] [txt (r.Flag + " " + r.Code)]
                        elA "td" [attr.``class`` "rate-val"]       [txt (sprintf "%.4f" rate)]
                        elA "td" [attr.``class`` "rate-converted"] [txt (sprintf "%.2f" conv + " " + r.Code)]
                    ])
            el "div" "rates-wrap" [
                el "div" "currency-updated" [txt ("Updated: " + rates.UpdatedAt)]
                elA "table" [attr.``class`` "rates-table"] [
                    elA "thead" [] [
                        elA "tr" [] [
                            elA "th" [] [txt "Currency"]
                            elA "th" [] [txt "Rate"]
                            elA "th" [] [txt "Converted"]
                        ]
                    ]
                    elA "tbody" [] rowDocs
                ]
            ]

        el "div" "currency-widget" [
            baseSelector
            View.Map2
                (fun (s: AppState) (amt: string) -> (s, amt))
                state.View amountInput.View
            |> Doc.BindView (fun (s, _) ->
                renderWidgetState s.Currency ratesTable)
        ]

    // ── SETTINGS ─────────────────────────────────────────────
    let settingsWidget () : Doc =
        let saved = Var.Create ""
        el "div" "settings-content" [
            elA "p" [attr.``class`` "settings-desc"] [txt "Your current dashboard preferences."]
            state.View |> Doc.BindView (fun s ->
                el "div" "settings-grid" [
                    el "div" "settings-row" [el "span" "settings-label" [txt "Default City"];     el "span" "settings-value" [txt s.City]]
                    el "div" "settings-row" [el "span" "settings-label" [txt "News Category"];    el "span" "settings-value" [txt s.NewsCategory.Label]]
                    el "div" "settings-row" [el "span" "settings-label" [txt "Base Currency"];    el "span" "settings-value" [txt s.BaseCurrency]]
                ])
            saved.View |> Doc.BindView (fun msg ->
                if msg = "" then Doc.Empty
                else el "div" "settings-saved" [txt msg])
            elA "button" [
                attr.``class`` "btn-save"
                on.click (fun _ _ ->
                    let s = state.Value
                    let cfg = { DefaultCity = s.City; DefaultCategory = s.NewsCategory.ApiValue; DefaultCurrency = s.BaseCurrency }
                    async {
                        do! Server.SaveConfig cfg
                        saved.Value <- "Preferences saved!"
                        do! Async.Sleep 2000
                        saved.Value <- ""
                    } |> Async.Start)
            ] [txt "Save Preferences"]
        ]

    // ── TAB TYPE ─────────────────────────────────────────────
    type DashTab = WeatherTab | NewsTab | CurrencyTab | SettingsTab

    // ── RENDER APP ───────────────────────────────────────────
    let renderApp () : Doc =
        let activeTab : Var<DashTab> = Var.Create WeatherTab

        let navItem (tab: DashTab) (icon: string) (label: string) : Doc =
            activeTab.View |> Doc.BindView (fun current ->
                elA "button" [
                    attr.``class`` (if current = tab then "nav-btn nav-active" else "nav-btn")
                    on.click (fun _ _ -> activeTab.Value <- tab)
                ] [
                    el "span" "nav-icon"  [txt icon]
                    el "span" "nav-label" [txt label]
                ])

        let mainContent : Doc =
            activeTab.View |> Doc.BindView (fun tab ->
                match tab with
                | WeatherTab  -> widgetCard "Weather & Forecast" "🌤" (fun () -> loadWeather state.Value.City)        (weatherWidget ())
                | NewsTab     -> widgetCard "Top Headlines"      "📰" (fun () -> loadNews state.Value.NewsCategory)   (newsWidget ())
                | CurrencyTab -> widgetCard "Currency Exchange"  "💱" (fun () -> loadCurrency state.Value.BaseCurrency)(currencyWidget ())
                | SettingsTab -> widgetCard "Settings"           "⚙"  (fun () -> ())                                  (settingsWidget ()))

        el "div" "app-shell" [
            elA "nav" [attr.``class`` "sidebar"] [
                el "div" "sidebar-brand" [
                    el "div" "brand-title" [txt "SmartDash"]
                    el "div" "brand-sub"   [txt "Live Dashboard"]
                ]
                el "div" "nav-items" [
                    navItem WeatherTab  "🌤" "Weather"
                    navItem NewsTab     "📰" "News"
                    navItem CurrencyTab "💱" "Currency"
                    navItem SettingsTab "⚙"  "Settings"
                ]
            ]
            Doc.Element "main" [attr.``class`` "main-content"] [mainContent]
        ]

    [<SPAEntryPoint>]
    let Main () =
        loadAll ()
        renderApp () |> Doc.RunById "main"