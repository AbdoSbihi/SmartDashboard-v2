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

    // ── GLOBAL STATE 
    let state       : Var<AppState> = Var.Create AppState.init
    let cityInput   : Var<string>   = Var.Create "Budapest"
    let amountInput : Var<string>   = Var.Create "1"

    // ── HELPERS 
    let getElementValue (el: Dom.Element) : string =
        JS.Get<string> "value" el

    let setWeather  w = state.Value <- { state.Value with Weather  = w }
    let setForecast f = state.Value <- { state.Value with Forecast = f }
    let setNews     n = state.Value <- { state.Value with News     = n }
    let setCurrency c = state.Value <- { state.Value with Currency = c }

    let weatherIcon (code: string) : string =
        sprintf "https://openweathermap.org/img/wn/%s@2x.png" code

    let fmtTemp (t: float) : string = sprintf "%.1f°C" t

    // ── LOAD FUNCTIONS 
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

    // ── WIDGET SHELL 
    let widgetCard (title: string) (icon: string) (onRefresh: unit -> unit) (content: Doc) : Doc =
        div [attr.``class`` "widget-card"] [
            div [attr.``class`` "widget-header"] [
                div [attr.``class`` "widget-title"] [
                    span [attr.``class`` "widget-icon"] [text icon]
                    span [] [text title]
                ]
                button [
                    attr.``class`` "btn-refresh"
                    on.click (fun _ _ -> onRefresh ())
                ] [text "↻"]
            ]
            div [attr.``class`` "widget-body"] [content]
        ]

    let renderWidgetState (ws: WidgetState<'T>) (render: 'T -> Doc) : Doc =
        match ws with
        | Idle     ->
            div [attr.``class`` "widget-idle"] [text "Click ↻ to load"] :> Doc
        | Fetching ->
            div [attr.``class`` "widget-loading"] [
                div [attr.``class`` "spinner"] []
                span [] [text "Loading…"]
            ] :> Doc
        | Failed e ->
            div [attr.``class`` "widget-error"] [text (sprintf "Error: %s" e)] :> Doc
        | Loaded d ->
            render d

    // ── WEATHER WIDGET 
    let weatherContent (data: WeatherData) : Doc =
        let temp     : float = data.TempC
        let feels    : float = data.FeelsLike
        let wind     : float = data.WindSpeed
        let humidity : int   = data.Humidity
        div [attr.``class`` "weather-main"] [
            div [attr.``class`` "weather-top"] [
                img [
                    attr.src (weatherIcon data.Condition.Icon)
                    attr.``class`` "weather-icon-img"
                    attr.alt data.Condition.Description
                ]
                div [] [
                    div [attr.``class`` "weather-temp"] [text (fmtTemp temp)]
                    div [attr.``class`` "weather-city"] [text (sprintf "%s, %s" data.City data.Country)]
                    div [attr.``class`` "weather-desc"] [text data.Condition.Description]
                ]
            ]
            div [attr.``class`` "weather-details"] [
                div [attr.``class`` "weather-detail"] [
                    span [attr.``class`` "detail-label"] [text "Feels like"]
                    span [attr.``class`` "detail-value"] [text (fmtTemp feels)]
                ]
                div [attr.``class`` "weather-detail"] [
                    span [attr.``class`` "detail-label"] [text "Humidity"]
                    span [attr.``class`` "detail-value"] [text (sprintf "%d%%" humidity)]
                ]
                div [attr.``class`` "weather-detail"] [
                    span [attr.``class`` "detail-label"] [text "Wind"]
                    span [attr.``class`` "detail-value"] [text (sprintf "%.1f m/s" wind)]
                ]
            ]
        ]

    let forecastContent (days: ForecastDay list) : Doc =
        div [attr.``class`` "forecast-row"] (
            days |> List.map (fun d ->
                let tmax : float = d.TempMax
                let tmin : float = d.TempMin
                div [attr.``class`` "forecast-day"] [
                    div [attr.``class`` "forecast-date"] [text d.Date]
                    img [
                        attr.src (weatherIcon d.Icon)
                        attr.``class`` "forecast-icon"
                        attr.alt d.Desc
                    ]
                    div [attr.``class`` "forecast-temps"] [
                        span [attr.``class`` "temp-max"] [text (fmtTemp tmax)]
                        span [attr.``class`` "temp-min"] [text (fmtTemp tmin)]
                    ]
                ] :> Doc
            )
        )

    let weatherWidget () : Doc =
        let searchBar =
            div [attr.``class`` "search-bar"] [
                Doc.InputType.Text
                    [attr.``class`` "search-input"; attr.placeholder "Search city…"]
                    cityInput
                button [
                    attr.``class`` "btn-search"
                    on.click (fun _ _ ->
                        let city = cityInput.Value.Trim()
                        if city <> "" then
                            state.Value <- { state.Value with City = city }
                            loadWeather city)
                ] [text "Search"]
            ]
        div [] [
            searchBar
            state.View |> Doc.BindView (fun s ->
                let weatherDoc  = renderWidgetState s.Weather weatherContent
                let forecastDoc =
                    match s.Forecast with
                    | Loaded days -> forecastContent days
                    | _           -> Doc.Empty
                Doc.Concat [weatherDoc; forecastDoc])
        ]

    // ── NEWS WIDGET 
    let newsArticle (a: NewsArticle) : Doc =
        Doc.Element "a" [
            attr.href      a.Url
            attr.target    "_blank"
            attr.rel       "noopener noreferrer"
            attr.``class`` "news-card"
        ] [
            (if a.ImageUrl <> "" then
                img [attr.src a.ImageUrl; attr.``class`` "news-img"; attr.alt a.Title] :> Doc
             else Doc.Empty)
            div [attr.``class`` "news-content"] [
                div [attr.``class`` "news-source"] [text a.Source]
                div [attr.``class`` "news-title"]  [text a.Title]
                div [attr.``class`` "news-desc"]   [text a.Description]
            ] :> Doc
        ] :> Doc

    let newsWidget () : Doc =
        let tabDocs =
            NewsCategory.all |> List.map (fun cat ->
                state.View |> Doc.BindView (fun s ->
                    let isActive = s.NewsCategory = cat
                    button [
                        attr.``class`` (if isActive then "tab-btn tab-active" else "tab-btn")
                        on.click (fun _ _ ->
                            state.Value <- { state.Value with NewsCategory = cat }
                            loadNews cat)
                    ] [text cat.Label] :> Doc))
        let tabs = div [attr.``class`` "news-tabs"] [Doc.Concat tabDocs]
        div [] [
            tabs
            state.View |> Doc.BindView (fun s ->
                renderWidgetState s.News (fun articles ->
                    div [attr.``class`` "news-grid"] [
                        Doc.Concat (articles |> List.map newsArticle)
                    ]))
        ]

    // ── CURRENCY WIDGET 
    let currencyWidget () : Doc =
        let baseSelector =
            div [attr.``class`` "currency-controls"] [
                div [attr.``class`` "form-field"] [
                    label [] [text "Base Currency"]
                    Doc.Element "select" [
                        attr.``class`` "form-select"
                        on.change (fun el _ ->
                            let code = getElementValue el
                            state.Value <- { state.Value with BaseCurrency = code }
                            loadCurrency code)
                    ] [
                        Doc.Concat (
                            Currency.supported |> List.map (fun (code, name, flag) ->
                                let isSelected = code = state.Value.BaseCurrency
                                let attrs =
                                    if isSelected
                                    then [attr.value code; attr.selected "selected"]
                                    else [attr.value code]
                                Doc.Element "option" attrs
                                    [Doc.TextNode (sprintf "%s %s - %s" flag code name)] :> Doc))
                    ]
                ]
                div [attr.``class`` "form-field"] [
                    label [] [text "Amount"]
                    Doc.InputType.Text
                        [attr.``class`` "form-input"; attr.placeholder "1.00"]
                        amountInput
                ]
            ]

        let ratesTable (rates: CurrencyRates) : Doc =
            let amount =
                match Double.TryParse(amountInput.Value) with
                | true, v -> v
                | _       -> 1.0
            div [] [
                div [attr.``class`` "currency-updated"] [
                    text (sprintf "Updated: %s" rates.UpdatedAt)
                ]
                Doc.Element "table" [attr.``class`` "rates-table"] [
                    Doc.Element "thead" [] [
                        Doc.Element "tr" [] [
                            Doc.Concat [
                                Doc.Element "th" [] [Doc.TextNode "Currency"]
                                Doc.Element "th" [] [Doc.TextNode "Rate"]
                                Doc.Element "th" [] [Doc.TextNode "Converted"]
                            ]
                        ]
                    ]
                    Doc.Element "tbody" [] [
                        Doc.Concat (
                            rates.Rates
                            |> List.filter (fun r -> r.Code <> rates.Base)
                            |> List.map (fun r ->
                                let rate      : float = r.Rate
                                let converted : float = Math.Round(amount * rate, 2)
                                Doc.Element "tr" [] [
                                    Doc.Concat [
                                        Doc.Element "td" [] [
                                            Doc.TextNode (sprintf "%s %s" r.Flag r.Code)]
                                        Doc.Element "td" [attr.``class`` "rate-val"] [
                                            Doc.TextNode (sprintf "%.4f" rate)]
                                        Doc.Element "td" [attr.``class`` "rate-converted"] [
                                            Doc.TextNode (sprintf "%.2f %s" converted r.Code)]
                                    ]
                                ] :> Doc))
                    ]
                ]
            ]

        div [] [
            baseSelector
            View.Map2
                (fun s (amt: string) -> (s, amt))
                state.View
                amountInput.View
            |> Doc.BindView (fun (s, _) ->
                renderWidgetState s.Currency ratesTable)
        ]

    // ── SETTINGS WIDGET 
    let settingsWidget () : Doc =
        let saved = Var.Create ""
        div [attr.``class`` "settings-content"] [
            p [attr.``class`` "settings-desc"] [
                text "Your current dashboard preferences."
            ]
            state.View |> Doc.BindView (fun s ->
                div [attr.``class`` "settings-grid"] [
                    div [attr.``class`` "settings-row"] [
                        span [attr.``class`` "settings-label"] [text "Default City"]
                        span [attr.``class`` "settings-value"] [text s.City]
                    ]
                    div [attr.``class`` "settings-row"] [
                        span [attr.``class`` "settings-label"] [text "News Category"]
                        span [attr.``class`` "settings-value"] [text s.NewsCategory.Label]
                    ]
                    div [attr.``class`` "settings-row"] [
                        span [attr.``class`` "settings-label"] [text "Base Currency"]
                        span [attr.``class`` "settings-value"] [text s.BaseCurrency]
                    ]
                ])
            saved.View |> Doc.BindView (fun msg ->
                if msg = "" then Doc.Empty
                else div [attr.``class`` "settings-saved"] [text msg] :> Doc)
            button [
                attr.``class`` "btn-save"
                on.click (fun _ _ ->
                    let s = state.Value
                    let cfg = {
                        DefaultCity     = s.City
                        DefaultCategory = s.NewsCategory.ApiValue
                        DefaultCurrency = s.BaseCurrency
                    }
                    async {
                        do! Server.SaveConfig cfg
                        saved.Value <- "Preferences saved!"
                        do! Async.Sleep 2000
                        saved.Value <- ""
                    } |> Async.Start)
            ] [text "Save Preferences"]
        ]

    // ── TAB TYPE 
    type DashTab = WeatherTab | NewsTab | CurrencyTab | SettingsTab

    // ── MAIN APP 
    let renderApp () : Doc =
        let activeTab : Var<DashTab> = Var.Create WeatherTab

        let navItem (tab: DashTab) (icon: string) (label: string) : Doc =
            activeTab.View |> Doc.BindView (fun current ->
                button [
                    attr.``class`` (if current = tab then "nav-btn nav-active" else "nav-btn")
                    on.click (fun _ _ -> activeTab.Value <- tab)
                ] [
                    span [attr.``class`` "nav-icon"]  [text icon]
                    span [attr.``class`` "nav-label"] [text label]
                ] :> Doc)

        let mainContent : Doc =
            activeTab.View |> Doc.BindView (fun tab ->
                match tab with
                | WeatherTab  ->
                    widgetCard "Weather & Forecast" "🌤"
                        (fun () -> loadWeather state.Value.City)
                        (weatherWidget ())
                | NewsTab     ->
                    widgetCard "Top Headlines" "📰"
                        (fun () -> loadNews state.Value.NewsCategory)
                        (newsWidget ())
                | CurrencyTab ->
                    widgetCard "Currency Exchange" "💱"
                        (fun () -> loadCurrency state.Value.BaseCurrency)
                        (currencyWidget ())
                | SettingsTab ->
                    widgetCard "Settings" "⚙"
                        (fun () -> ())
                        (settingsWidget ()))

        div [attr.``class`` "app-shell"] [
            nav [attr.``class`` "sidebar"] [
                div [attr.``class`` "sidebar-brand"] [
                    div [attr.``class`` "brand-title"] [text "SmartDash"]
                    div [attr.``class`` "brand-sub"]   [text "Live Dashboard"]
                ]
                div [attr.``class`` "nav-items"] [
                    navItem WeatherTab  "🌤" "Weather"
                    navItem NewsTab     "📰" "News"
                    navItem CurrencyTab "💱" "Currency"
                    navItem SettingsTab "⚙"  "Settings"
                ]
            ]
            Doc.Element "main" [attr.``class`` "main-content"] [
                mainContent
            ]
        ]

    [<SPAEntryPoint>]
    let Main () =
        loadAll ()
        renderApp () |> Doc.RunById "main"