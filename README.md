# Gold Price Console v2.0.0

## About <a name = "about"></a>

Simple console application utility that calls a third party web api to retrieve Gold prices and stores the data in a database. 

This application uses PublicHoliday nuget package (Copyright (C) 2013 Martin Willey) which the source code and license is available at <a href="https://github.com/martinjw/Holiday/" target="_blank">Martin Willey's Github</a>. 

## Status

.NET 6
[![build](https://github.com/dotnetdeveloperaz/GoldPriceConsole/actions/workflows/dotnet6.yml/badge.svg?branch=main)](https://github.com/dotnetdeveloperaz/GoldPriceConsole/actions/workflows/dotnet6.yml)

.NET 7
[![build](https://github.com/dotnetdeveloperaz/GoldPriceConsole/actions/workflows/dotnet7.yml/badge.svg?branch=main)](https://github.com/dotnetdeveloperaz/GoldPriceConsole/actions/workflows/dotnet7.yml)

.NET 8 Preview 3
[![build](https://github.com/dotnetdeveloperaz/GoldPriceConsole/actions/workflows/dotnet8.yml/badge.svg?branch=main)](https://github.com/dotnetdeveloperaz/GoldPriceConsole/actions/workflows/dotnet8.yml)

## Buy Me A Coffee
<a href="https://www.buymeacoffee.com/dotnetdev" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## Table of Contents

- [About](#about)
- [Getting Started](#getting_started)
- [Prerequisites](#prerequisites)
- [Installing](#installing)
- [Usage](#usage)

## Getting Started <a name = "getting_started"></a>

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. 

### Prerequisites <a name = "prerequisites"></a>

1. .NET 6, .NET 7 or .NET 8 Preview.
2. Account with [GoldApi.io](https://www.goldapi.io/) Free account gives you 300 api calls per month.
3. MariaDB (or MySQL).
4. Configure appsettings **(Token) (DefaultDB) keys**
5. Set MonthlyAllowance if your account has a difference allowance amount.

**Note: Setting this above your allowance will only make the API calls fail once you hit your limit.**

```
{
  "ApiServer": {
    "Token": "",
    "BaseURL": "https://www.goldapi.io/api/",
    "DefaultMetal": "XAU/USD/",
    "MonthlyAllowance": "300",
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultDB": ""
  },
  "AllowedHosts": "*"
}
```

### Installing <a name = "installing"></a>

Add your private settings for Token (ApiKey) and Database connection string or add them to appsettings.json above.

```bash
dotnet user-secrets init
dotnet user-secrets set "ApiServer:Token" "<YourApiKey>"
dotnet user-secrets set "ConnectionStrings:DefaultDB" "<YourDatabaseConnectionString>"
dotnet user-secrets list
```

Create the tables and stored procedures used by this utility.

To create a fresh install, run the GoldRates-table.sql script in the db directory.

GoldRates-table.sql to create the table.
usp_AddGoldPrice.sql to create the stored procedure.

> *You do not need to run the GoldRates-table script if you do the restore below. 

If you would like the full history (Back to Dec 6th, 2018) of Gold Price, which will save you time and API calls if you want historical data, then restore the database in the db directory called GoldPrices.sql.gz.

```bash
gzip -d GoldPrices.sql.gz
mysql -u <your username> -p <Your Target Database> < GoldPrices.sql
```

Build the project by running the following in the project folder.
```bash 
dotnet build
``` 
To run a simple test, run the following.
```bash 
dotnet run acct
```
You should see something similar to:
```bash
  ⏳ Start Processing Get Account Information...                                                    
  ✔ Finished Getting Account Information...                                                         
      Requests Today: 9 Requests Yesterday: 0                                                       
      Requests This Month: 20 Requests Last Month: 46                                               
  ✔ Remaining WebAPI Requests: 280    
```

## Usage <a name = "usage"></a>
Commands

acct
- Gets details of your account (like sample above) at the third party web service.

status 
- Gets the status of the third party web service, which is true (1) or false (0) for online.

price
- Gets yesterday's (Prices are available for previous days close) Gold rate (and saves to the configured database if passing --save

price --date YYYY-MM-DD 
- Gets a specific date instead of yesterday's (default) close price.

backtrack --start <YYYY-MM-DD> --end <YYYY-MM-DD> 
- Gets Gold prices from the start date specified to the end date. It will skip weekends and holidays and the current date to avoid unecessary api calls.

** Appending --save will write the price data to the database on commands price and backtrack.**

restore
-- Restores existing cache file to the database.

Example:
```
backtrack --start 2023-07-31 --end 2023-06-21  Will get the gold rates from July 31st, 2023 to June 21st, 2023.

backtrack --start 2023-07-31 --end 2023-06-21 --save  Will get the gold rates from July 31st, 2023 to June 21st, 2023 and save to the database.
```

*Only U.S. Non-Holiday Week Days Are Processed*

Passing --debug will output configuration data. If you pass --debug --hidden, it will also output  private configuration data (eg: DB connection string, token) even when not put into appsettings.json but in user-secrets instead.

Run the application with no commands (dotnet run) and you will get the following usage screen.
```bash
╭──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╮
│                                              Gold ⛏  Price Console v2.1                                             │
│                                               Written By Scott Glasgow                                               │
╰──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╯
╭──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╮
│                                 Cache file exists. Use restore to load to database.                                  │
╰──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╯
USAGE:
    GoldPriceConsole.dll [OPTIONS] <COMMAND>

EXAMPLES:
    dotnet run  history --start YYYY-MM-DD --end YYYY-MM-DD --debug --hidden
    dotnet run  price --date YYYY-MM-DD --save --debug --hidden
    dotnet run  acct --debug --hidden
    dotnet run  status --debug --hidden
    dotnet run restore --debug --hidden

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information

COMMANDS:
    account    Retrieves account information
    history    Retrieves historical gold prices. Use --save to save to the database.
               Weekends and holidays are skipped because markets are closed
    price      Retrieves the current gold price. Use --save to save to database. Weekends and holidays are skipped
    acct       Retrieves Account Statistics
    restore    Restores cache file
    status     Retrieves WebApi Status
```
