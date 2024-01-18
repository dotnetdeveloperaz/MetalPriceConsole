# Metal Price Console v3.0

## **<span style="color: red;">Important Note:</span>**

#### *This version contains code that includes the --palladium and --platinum switches to retrieve prices for those metals.*

#### *However, for some reason, the third-party api does not appear to return data when a date is supplied, so only the latest price information is retrieved.*

#### *Note that the --start and --end date switches are ignored.*

#### I have emailed the developers in hopes to get feedback on this issue, as all four metals should function the same, based on their documentation, but even their documentation page behaves in the same manner.

#### Update 8/23/2023 Got a response from one of the developers, and they confirmed that historical data is not currently supported in these two metals, but they are looking to add support for it. I will re-add support for specifying dates once they support it.

#### As of Jan 1, 2024, Palladium and Platinum do NOT support historical data still. The current code does NOT account for this, as this went through a rewrite during New Years Eve.

## Table of Contents

- [About](#about)
- [Status](#status)
- [Getting Started](#getting_started)
- [Prerequisites](#prerequisites)
- [Installing](#installing)
- [Usage](#usage)

## About

Simple console application utility that calls a third party web api to retrieve Gold, Silver, Palladium, or Platinum prices and can also store the data in a database or can be stored in a json file using the --cache switch.

This application uses the following open source libraries.

-- [PublicHoliday nuget package](https://github.com/martinjw/Holiday/")

-- [Spectre Console and Spectre Console Cli](https://spectreconsole.net/)

## Status

.NET 8
[![build](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet8.yml/badge.svg?branch=main)](https://github.com/dotnetdeveloperaz/metalPriceConsole/actions/workflows/dotnet8.yml)

<a href="https://www.buymeacoffee.com/dotnetdev" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## Getting Started <a name = "getting_started"></a>

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes.

## Prerequisites

1. .NET 6, .NET 7 or .NET 8.
2. Account with [GoldApi.io](https://www.goldapi.io/) Free account gives you 100 free (was 300 but they changed this in May 2023) api calls per month.
3. MariaDB (or MySQL) if using the --save switch. This is optional.
4. Configure appsettings or user-secrets **(Token) (DefaultDB) keys**
5. Set MonthlyAllowance if your account has a difference allowance amount.

**Note: Setting this above your allowance will only make the API calls fail once you hit your limit.**

```json
"ApiServer": {
    "Token": "",
    "CacheFile": "MetalPrice.cache",
    "BaseURL": "https://www.goldapi.io/api/",
    "Gold": "XAU",
    "Silver": "XAG",
    "Palladium": "XPD",
    "Platinum":  "XPT",
    "Currency": "USD",
    "MonthlyAllowance": "100"
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
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                               Metal Price Console v3.0                                               │
│                                               Written By Scott Glasgow                                               │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                                     Requests Today 14                                                │
│                                                 Requests Yesterday 2                                                 │
│                                                Requests This Month 27                                                │
│                                                Requests Last Month 54                                                │
│                                            Remaining WebAPI Requests: 73                                             │
├──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                       Finished Retrieving Account Details From https://www.goldapi.io/api/stat                       │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

## Usage

Commands

acct or account

- Gets details of your account (like sample above) at the third party web service.

status

- Gets the status of the third party web service, which is true (1) or false (0) for online.

restore
- Restores existing cache file to the database.

  ###### The cache file is created automatically when the call to the database fails during normal processing. The restore process is manual and you have to use the restore command.

price 

- Gets yesterday's (Prices are available for previous days close) Gold rate and saves to the configured database if passing --save or to a json file is passing --cache.

price --start YYYY-MM-DD

- Gets a specific date instead of yesterday's (default) close price.

price --start YYYY-MM-DD --end YYYY-MM-DD

- Gets prices from the start date specified to the end date. It will skip weekends and holidays and the current date to avoid unnecessary api calls.

view
- Works like price, except that it will display data from either the cache file (when using --cache) or the database if the --cache switch is not used.
- all switches work with exception of --save --fake --token
- Dates are NOT checked for holiday or weekends since there would never be data for those dates stored in the cache or database.
- if no metal switch is used (eg: --silver) the gold is the default just like when using the price command.
view --start YYYY-MM-DD --end YYYY-MM-DD --silver

### Switches

- --save writes the price data to the database on commands price and backtrack.

- --cache writes the data to a json file which later can be restored to the configured database, except when using the view command, which then will display data from the cache file.

- --fake will load sample data, instead of calling the WebApi. This is available for price, history and acct commands.

- --gold Retrieves gold prices. This is the default and never needs to be used.

- --silver Retrieves silver prices.

- --palladium Retrieves palladium prices.

- --platinum Retrieves platinum prices.

- --currency EUR Specifies the base currency, in ISO 4217 format,  which the default is USD. This can also just be set in the appsettings.json file. See goldapi.io for the supported base currencies. List below as of Jan, 1, 2024.

- --token <YourApiToken> This will override the token in appsettings.json or in user-secrets.

Example:

```bash
price --start 2023-07-31 --end 2023-06-21  Will get the gold rates from July 31st, 2023 to June 21st, 2023 skipping holidays and weekends.

price --start 2023-07-31 --end 2023-06-21 --save  Will get the gold rates from July 31st, 2023 to June 21st, 2023 and save to the database.
```

#### Base Currencies Supported
``` text
USD - United States Dollar
EUR - European Euro
CAD - Canadian Dollar
GBP - British Pound
JPY - Japanese Yen
AED - E.A.U. Durham
AUD - Australian Dollar
BTC - Bitcoin
CHF - Swiss Franc
CNY - Chinese/Yuan Renminbi
CZK - Czech Krona
EGP - Egyptian Pound
HKD - Hong Kong Dollar
INR - Indian Rupee
JOD - Jordanian Dinar
KRW - South Korean Won
KWD - Kuwaiti Dinar
MXN - Mexican Peso
MYR - Malaysian Ringgit
OMR - Omani Rial
PLN - Polish Zloty
RUB - Russian Ruble
SAR - Saudi Riyal
SGD - Singapore Dollar
THB - Thai Baht
XAG - Gold/Silver Ratio
ZAR - South African Rand
```

#### Only U.S. Non-Holiday Week Days Are Processed

Passing --debug will output configuration data. If you pass --debug --hidden, it will also output private configuration data (eg: DB connection string, token) even when not put into appsettings.json but in user-secrets instead.

Run the application with no commands (dotnet run) and you will get the following usage screen.
*NOTE: view command does not use --fake or --token and they are ignored.

```bash
┌──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                               Metal Price Console v3.0                                               │
│                                               Written By Scott Glasgow                                               │
└──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
USAGE:
    MetalPriceConsole.dll [OPTIONS] <COMMAND>

EXAMPLES:
    MetalPriceConsole.dll price --start YYYY-MM-DD --end YYYY-MM-DD --currency USD --gold --palladium --platinum
--silver --fake --save --cache --debug --token <token>
    MetalPriceConsole.dll view --start YYYY-MM-DD --end YYYY-MM-DD --currency USD --gold --palladium --platinum --silver
--fake --cache --debug --token <token>
    MetalPriceConsole.dll account --fake --debug --hidden --token <token>
    MetalPriceConsole.dll acct --fake --debug --hidden --token <token>
    MetalPriceConsole.dll status --debug --hidden --token <token>
    MetalPriceConsole.dll restore --debug --hidden

OPTIONS:
    -h, --help       Prints help information
    -v, --version    Prints version information

COMMANDS:
    price         Get Metal Price
    view          Display From Cache File Or Database
    account       Retrieves account information
    status        Retrieves WebApi Status
    restore       Restores Cache File
    testdb        Tests The Database Connection
```