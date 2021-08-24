# Gold Price Console

## Table of Contents

- [About](#about)
- [Getting Started](#getting_started)
- [Usage](#usage)
- [Contributing](../CONTRIBUTING.md)

## About <a name = "about"></a>

Simple console application that calls a third party web api to retrieve Gold prices.

## Getting Started <a name = "getting_started"></a>

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See [deployment](#deployment) for notes on how to deploy the project on a live system.

### Prerequisites

What things you need to install the software and how to install them.

```
Give examples
```

### Installing

A step by step series of examples that tell you how to get a development env running.

Say what the step will be

```
Give the example
```

And repeat

```
until finished
```

End with an example of getting some data out of the system or using it for a little demo.

## Usage <a name = "usage"></a>
/status - Gets the status of the third party web service.
/account - Gets details of your account at the third part web service.
/rate - Gets yesterdays Gold rate and saves to the configured database.
/backtrack <startDate> <numberOfDays> - Gets Gold prices from the date specified, and works backwards x number of days.
    eg: /backtrack 2021-07-31 30 - Will get the gold rates from July 31st, 2021, back 30 days or June 21st, 2021.
    <b>Only Week Days Are Processed</b>Only weekdays are counted and processed. Holidays when exchanges are closed are 
    currently not skipped, so they are included.
