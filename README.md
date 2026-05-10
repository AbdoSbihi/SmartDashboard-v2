Abdessamad Sbihi EXO4Z4 2025/2026/2 University of dunaújváros
SmartDashboard — F# & WebSharper
A live smart dashboard web application built with F# and WebSharper 

Try Live --> Live Demo --> https://smartdashboard20260504201519-h9emd4d8c7ejf7es.spaincentral-01.azurewebsites.net/

* Screenshots :
  
Weather widget : 

<img width="1919" height="962" alt="Weather" src="https://github.com/user-attachments/assets/c6923285-d95b-4d4f-b36d-078582544b3e" />

News Widget : 

<img width="1916" height="989" alt="news" src="https://github.com/user-attachments/assets/53ef3406-9ccd-4253-8870-77ee8ff8238b" />

Currency widget :

<img width="1911" height="979" alt="Currency" src="https://github.com/user-attachments/assets/122359e3-6b84-4629-bf35-2df40fc18a9f" />

Settings widget : 

<img width="1919" height="989" alt="Settings" src="https://github.com/user-attachments/assets/4f25d571-e6eb-4d88-8c0f-b2f8bfebcf6a" />



* Motivation :
  
This project demonstrates advanced F# concepts in a real-world web application aggregating live data from multiple external APIs. Key F# features used: 
- generic WidgetState<'T> discriminated union for independent widget loading states,
- record update syntax for immutable state transitions,
- parallel async workflows,
- and server-side HTTP proxying via [<Rpc>] functions to keep API keys secret.


* Features :
  
Weather — current conditions + 5-day forecast for any city 
News — top headlines across 6 categories 
Currency — live exchange rates for 10 currencies 
Settings — save preferred city, category, and currency per session


* Additional Features : (v2)
  
   * The dashboard includes four extra features :
      - A live clock displayed in the sidebar updates every second using a recursive async loop. 
      - A dark/light mode toggle switches the entire colour scheme instantly by setting a data-theme attribute on the body.
      - A temperature unit toggle switches all displayed temperatures between Celsius and Fahrenheit in real time , the conversion is reactive, meaning every temperature on screen re-renders the moment the unit changes.         - An auto-refresh timer counts down from 5 minutes in the sidebar footer and automatically re-fetches all three APIs when it reaches zero, implemented as a second recursive async loop running in parallel with the clock.



        <img width="1910" height="995" alt="1" src="https://github.com/user-attachments/assets/6cfb406a-f7df-4b5b-b893-b8a4a321c5cc" />


* Tech Stack :
  
Language: F# (.NET 10)
Frontend: WebSharper.UI 
Backend: ASP.NET Core + WebSharper RPC
Deployment: Azure App Service


* Project Structure
  
SmartDashboard/
├── Model.fs      
├── Server.fs     
├── Client.fs     
├── Site.fs       
├── Startup.fs    
└── wwwroot/     
