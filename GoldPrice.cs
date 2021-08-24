using System;
namespace GoldPriceConsole
{
    public class GoldPrice
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
    }
    public class Status
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
}
