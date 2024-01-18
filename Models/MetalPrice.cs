using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetalPriceConsole.Models;

public class MetalPrice
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }
    [JsonPropertyName("metal")]
    public string Metal { get; set; }
    [JsonPropertyName("exchange")]
    public string Exchange { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; }
    [JsonPropertyName("price")]
    public double Price { get; set; }
    [JsonPropertyName("prev_close_price")]
    public double PrevClosePrice { get; set; }
    [JsonPropertyName("ch")]
    public double Change { get; set; }
    [JsonPropertyName("chp")]
    public double ChangePercent { get; set; }
    [JsonPropertyName("price_gram_24k")]
    public double PriceGram24k { get; set; }
    [JsonPropertyName("price_gram_22k")]
    public double PriceGram22k { get; set; }  
    [JsonPropertyName("price_gram_21k")]
    public double PriceGram21k { get; set; }  
    [JsonPropertyName("price_gram_20k")]
    public double PriceGram20k { get; set; }  
    [JsonPropertyName("price_gram_18k")]
    public double PriceGram18k { get; set; }  
}
public class ApiStatus
{
    [JsonPropertyName("result")]
    public bool Result { get; set; }
}
public class Account
{
    [JsonPropertyName("requests_today")]
    public int RequestsToday { get; set; }
    [JsonPropertyName("requests_yesterday")]
    public int RequestsYesterday { get; set; }
    [JsonPropertyName("requests_month")]
    public int RequestsMonth { get; set; }
    [JsonPropertyName("requests_last_month")]
    public int RequestsLastMonth { get; set; }
}
