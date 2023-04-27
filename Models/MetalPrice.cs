using System;

namespace MetalPriceConsole.Models;

public class MetalPrice
{
    public DateTime date { get; set; }
    public long timestamp { get; set; }
    public string metal { get; set; }
    public string exchange { get; set; }
    public string currency { get; set; }
    public double price { get; set; }
    public double prev_close_price { get; set; }
    public double ch { get; set; }
    public double chp { get; set; }
    public double price_gram_24k { get; set; }
    public double price_gram_22k { get; set; }  
    public double price_gram_21k { get; set; }  
    public double price_gram_20k { get; set; }  
    public double price_gram_18k { get; set; }  
}
public class ApiStatus
{
    public bool result { get; set; }
}
public class Account
{
    public int requests_today { get; set; }
    public int requests_yesterday { get; set; }
    public int requests_month { get; set; }
    public int requests_last_month { get; set; }
}

