# Metal Price Console v3.0 beta

## **<span style="color: red;">Important Note:</span>**


### *This branch is ongoing development and may NOT function*

#### *This version in the development branch contains code that includes the --palladium and --platinum switches to retrieve prices for those metals.*

#### *However, for some reason, the third-party api does not appear to return data when a date is supplied, so only the latest price information is retrieved.*

#### *Date is NOT used in the code currently, thus, **DO NOT USE** the history command with the --palladium and --platinum switch and note that the --date switch for price command is ignored.*

I have emailed the developers in hopes to get feedback on this issue, as all four metals should function the same, based on their documentation, but even their documentation page behaves in the same manner.

#### Update 8/23/2023 Got a response from one of the developers, and they confirmed that historical data is not currently supported in these two metals, but they are looking to add support for it. I will leave the supporting code in, and re-enable the date support when it's available.

## Table of Contents

- [About](#about)
- [Status](#status)
- [Getting Started](#getting_started)
- [Prerequisites](#prerequisites)
- [Installing](#installing)
- [Usage](#usage)

## About

Simple console application utility that calls a third party web api to retrieve Gold or Silver prices and stores the data in a database.

This application uses the following open source libraries.

-- [PublicHoliday nuget package](https://github.com/martinjw/Holiday/")

-- [Spectre Console and Spectre Console Cli](https://spectreconsole.net/)

## Status

.NET 6
[![build](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet6.yml/badge.svg?branch=main)](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet6.yml)

.NET 7
[![build](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet7.yml/badge.svg?branch=main)](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet7.yml)

.NET 8 Preview 7
[![build](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet8.yml/badge.svg?branch=main)](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet8.yml)

<a href="https://www.buymeacoffee.com/dotnetdev" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## Getting Started <a name = "getting_started"></a>

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

## Prerequisites

1. .NET 6, .NET 7 or .NET 8 Preview.
2. Account with [GoldApi.io](https://www.goldapi.io/) Free account gives you 100 free (was 300 but they changed this in May 2023) api calls per month.
3. MariaDB (or MySQL).
4. Configure appsettings **(Token) (DefaultDB) keys**
5. Set MonthlyAllowance if your account has a difference allowance amount.

**Note: Setting this above your allowance will only make the API calls fail once you hit your limit.**

```json
{
  "ApiServer": {
    "Token": "",
    "CacheFile": "MetalPrice.cache",
    "BaseURL": "https://www.goldapi.io/api/",
    "Gold": "XAU/",
    "Palladium": "XPD/",
    "Platinum": "XPT/",
    "Silver":  "XAG/",
    "Currency":  "USD/",
    "MonthlyAllowance": "100",
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

## Installing

Add your private settings for Token (ApiKey) and Database connection string or add them to appsettings.json above.

```bash
dotnet user-secrets init
dotnet user-secrets set "ApiServer:Token" "<YourApiKey>"
dotnet user-secrets set "ConnectionStrings:DefaultDB" "<YourDatabaseConnectionString>"
dotnet user-secrets list
```

Create the tables and stored procedures used by this utility.

To create a fresh install, run the MetalRates-table.sql script in the db directory.

MetalRates-table.sql to create the table.
usp_AddMetalPrice.sql to create the stored procedure.

> *You do not need to run the MetalRates-table script if you do the restore below.

If you would like the full history (Back to Dec 6th, 2018) of Metal Prices database, which will save you time and API calls if you want historical data, then restore the database in the db directory called MetalPrices.sql.gz.

```bash
gzip -d MetalPrices.sql.gz
mysql -u <your username> -p <Your Target Database> < MetalPrices.sql
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

## Usage

Commands

acct

- Gets details of your account (like sample above) at the third party web service.

status

- Gets the status of the third party web service, which is true (1) or false (0) for online.

restore
- Restores existing cache file to the database.

  ###### The cache file is created automatically when the call to the database fails during normal processing. The restore process is manual and you have to use the restore command.

price

- Gets yesterday's (Prices are available for previous days close) Gold rate and saves to the configured database if passing --save

price --date YYYY-MM-DD

- Gets a specific date instead of yesterday's (default) close price.

history --start YYYY-MM-DD --end YYYY-MM-DD

- Gets prices from the start date specified to the end date. It will skip weekends and holidays and the current date to avoid unnecessary api calls.

### Switches

- --save writes the price data to the database on commands price and backtrack.

- --fake will load sample data, instead of calling the WebApi.

- --gold Retrieves gold prices. This is the default and never needs to be used.

- --silver Retrieves silver prices.

- --palladium Retrieves palladium prices.

- --platinum Retrieves platinum prices.

- --currency EUR Specifies the base currency, which the default is USD. This can also just be set in the appsettings.json file.

Example:

```bash
history --start 2023-07-31 --end 2023-06-21  Will get the gold rates from July 31st, 2023 to June 21st, 2023.

history --start 2023-07-31 --end 2023-06-21 --save  Will get the gold rates from July 31st, 2023 to June 21st, 2023 and save to the database.
```

#### Only U.S. Non-Holiday Week Days Are Processed

Passing --debug will output configuration data. If you pass --debug --hidden, it will also output  private configuration data (eg: DB connection string, token) even when not put into appsettings.json but in user-secrets instead.

Run the application with no commands (dotnet run) and you will get the following usage screen.

```bash
╭──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╮
│                                              Gold ⛏  Price Console v2.5                                             │
│                                               Written By Scott Glasgow                                               │
╰──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╯
╭──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╮
│                                 Cache file exists. Use restore to load to database.                                  │
╰──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────╯
USAGE:
    metalPriceConsole.dll [OPTIONS] <COMMAND>

EXAMPLES:
    dotnet run  history --start YYYY-MM-DD --end YYYY-MM-DD --silver --currency USD --fake --save --debug --hidden
    dotnet run  price --date YYYY-MM-DD --silver --currency USD --fake --save --debug --hidden
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
