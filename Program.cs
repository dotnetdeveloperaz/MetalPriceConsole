using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Newtonsoft.Json;
using RestSharp;

namespace GoldRateConsole
{
    class Program
    {
        static bool getStatus = false;
        static bool getAccount = false;
        static bool getRate = false;
/*
        static string url = "https://www.goldapi.io/api/";
        static string gold = "XAU/USD/";
        static string token = "goldapi-3m42qaukpd1dudi-io";
*/
        static string stat = "stat";
        static string status = "status";
        static string rateDate = DateTime.Now.AddDays(-1).ToString("yyyyMMdd");
        static string connectionString;

        public static void Main(string[] args)
        {
            Account _account;
            Status _status;
            GoldPrice _goldPrice;
            string url;
            string gold;
            string token;
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                //.AddUserSecrets<Program>()
                .Build();

            connectionString = config.GetConnectionString("DefaultDB");
            //Console.WriteLine("DB String: {0}", connectionString);
            token = config.GetSection("Token").Value;
            url = config.GetSection("BaseURL").Value;
            gold = config.GetSection("DefaultMetal").Value;
            
            if(args[0] == "/backtrack")
            {
                int cnt = 211;
                DateTime dt = DateTime.Parse(args[1]);
                Console.WriteLine("Getting rates starting from {0} to {1}", args[1], dt.AddDays(-211).ToString("yyyy-MM-dd"));
                while(cnt != 0)
                {
                    dt = dt.AddDays(-1);
                    var client = new RestClient(url + gold + dt.ToString("yyyyMMdd"));
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("x-access-token", token);
                    request.AddHeader("Content-Type", "application/json");
                    IRestResponse response = client.Execute(request);
                    _goldPrice = JsonConvert.DeserializeObject<GoldPrice>(response.Content);
                    Save(_goldPrice);
                    cnt--;
                }
                return;
            }

            if(!GetParameters(args))
            {
                Console.WriteLine("No parameters specified, quitting.");
                return;
            }
            else
            {
                if(getStatus)
                    url += status;
                else if(getAccount)
                    url += stat;
                else if(getRate)
                    url += gold + rateDate;
                Console.WriteLine("Calling {0}", url);
                var client = new RestClient(url);
                var request = new RestRequest(Method.GET);
                //Console.WriteLine("Token is {0}", token);
                request.AddHeader("x-access-token", token);
                request.AddHeader("Content-Type", "application/json");
                IRestResponse response = client.Execute(request);
                Console.WriteLine("Received\r\n{0}", response.Content);
                if(getAccount)
                {
                    _account = JsonConvert.DeserializeObject<Account>(response.Content);
                    Console.WriteLine("Requests Left: {0}", (300 - _account.requests_month));
                    Console.WriteLine("Requests This Month: {0}", _account.requests_month);
                    Console.WriteLine("Requests Last Month: {0}", _account.requests_last_month);
                }
                else if(getStatus)
                {
                    _status = JsonConvert.DeserializeObject<Status>(response.Content);
                    Console.WriteLine("Service Up: {0}", _status.result);
                }
                else if(getRate)
                {
                    _goldPrice = JsonConvert.DeserializeObject<GoldPrice>(response.Content);
                    Console.WriteLine("Gold Prices for {0}", _goldPrice.date.ToString("MM/dd/yyyy"));
                    Console.WriteLine("Price: {0}\r\nPrevious Price: {1}\r\nChange: {2}\r\nPercent: {3}%", 
                        _goldPrice.price, _goldPrice.prev_close_price, _goldPrice.ch, _goldPrice.chp);
                    Save(_goldPrice);
                }
            }
        }

        static void Save(GoldPrice goldPrice)
        {
            MySqlConnection sqlConnection = new MySqlConnection(connectionString);
            MySqlCommand sqlCommand = new MySqlCommand("sp_AddGoldRate", sqlConnection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            try
            {
                sqlConnection.Open();
                sqlCommand.Parameters.AddWithValue("price", goldPrice.price);
                sqlCommand.Parameters.AddWithValue("prev_price", goldPrice.prev_close_price);
                sqlCommand.Parameters.AddWithValue("ratedate",  goldPrice.date.ToString("yyyy/MM/dd"));
                sqlCommand.Parameters.AddWithValue("chg",  goldPrice.ch);
                sqlCommand.Parameters.AddWithValue("chg_pct",  goldPrice.chp);
                var recs = sqlCommand.ExecuteNonQuery();
                Console.WriteLine("Records Inserted: {0}", recs);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Could not insert new gold rate.");
                Console.WriteLine("Exception: {0}", ex.Message);
            }
            finally
            {
                if(sqlConnection.State == ConnectionState.Open)
                    sqlConnection.Close();
                sqlCommand.Dispose();
                sqlConnection.Dispose();
            }            
        }

        static bool GetParameters(string[] args)
        {
            bool canRun = false;
            foreach (string arg in args)
            {
                int cnt = 1;
                switch (arg.ToLower())
                {
                    case "/status":
                        canRun = true;
                        getStatus = true;
                        break;
                    case "/account":
                        canRun = true;
                        getAccount = true;
                        break;
                    case "/rate":
                        canRun = true;
                        getRate = true;
                        break;
                    case "/date":
                        rateDate = args[cnt];
                        break;
                }
                cnt++;
            }
            return canRun;
        }
    }
}
