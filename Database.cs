using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text.Json;
using MySqlConnector;

using MetalPriceConsole.Commands;
using MetalPriceConsole.Models;
using System.Linq;
using MetalPriceConsole.Commands.Settings;

namespace MetalPriceConsole
{
    internal class Database
    {
        /// <summary>
        /// Saves collection of metalPrice basically looping through the 
        /// collection and called the single metalPrice method.
        /// </summary>
        /// <param name="metalPrices"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static bool Save(List<MetalPrice> metalPrices, string connectionString, string cacheFile)
        {
            int success = 0;
            foreach (MetalPrice metalPrice in metalPrices)
            {
                if (Save(metalPrice, connectionString))
                    success++;
            }
            if (success != metalPrices.Count)
                CacheData(metalPrices, cacheFile);
            return (success == metalPrices.Count);
        }
        /// <summary>
        /// Saves gold price for specific day.
        /// </summary>
        /// <param name="metalPrice">Type metalPrice</param>
        /// <param name="connectionString"Database connectionstring></param>
        /// <returns></returns>
        public static bool Save(MetalPrice metalPrice, string connectionString)
        {
            //metalPrice.Date = DateTimeOffset.Parse(metalPrice.Timestamp.ToString()).Date;
            if (metalPrice.Date.Year < 1900)
                return false;
            MySqlConnection sqlConnection = new(connectionString);
            MySqlCommand sqlCommand = new("usp_AddMetalPrice", sqlConnection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            DateTime timeStamp = DateTimeOffset.FromUnixTimeMilliseconds(metalPrice.Timestamp).UtcDateTime;
            try
            {
                sqlConnection.Open();
                sqlCommand.Parameters.AddWithValue("metal", metalPrice.Metal);
                sqlCommand.Parameters.AddWithValue("currency", metalPrice.Currency);
                sqlCommand.Parameters.AddWithValue("price", metalPrice.Price);
                sqlCommand.Parameters.AddWithValue("prev_price", metalPrice.PrevClosePrice);
                sqlCommand.Parameters.AddWithValue("ratedate", metalPrice.Date.ToString("yyyy/MM/dd"));
                sqlCommand.Parameters.AddWithValue("unixtime", timeStamp);
                sqlCommand.Parameters.AddWithValue("chg", metalPrice.Change);
                sqlCommand.Parameters.AddWithValue("chg_pct", metalPrice.ChangePercent);
                sqlCommand.Parameters.AddWithValue("price_gram_24k", metalPrice.PriceGram24k);
                sqlCommand.Parameters.AddWithValue("price_gram_22k", metalPrice.PriceGram22k);
                sqlCommand.Parameters.AddWithValue("price_gram_21k", metalPrice.PriceGram21k);
                sqlCommand.Parameters.AddWithValue("price_gram_20k", metalPrice.PriceGram20k);
                sqlCommand.Parameters.AddWithValue("price_gram_18k", metalPrice.PriceGram18k);
 
                var recs = sqlCommand.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Could not insert new metal rate.");
                Console.WriteLine("Exception: {0}", ex.Message);
                return false;
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                    sqlConnection.Close();
                sqlCommand.Dispose();
                sqlConnection.Dispose();
            }
            return true;
        }
        public static List<MetalPrice> GetData(string connectionString, string metal, PriceCommandSettings settings)
        {
            MySqlConnection sqlConnection = new(connectionString);
            MySqlCommand sqlCommand = new("usp_GetMetalPrices", sqlConnection);
            List<MetalPrice> metals = new();
            sqlCommand.CommandType = CommandType.StoredProcedure;
            try
            {
                sqlConnection.Open();
                sqlCommand.Parameters.AddWithValue("startDate", settings.StartDate);
                sqlCommand.Parameters.AddWithValue("endDate", settings.EndDate);
                sqlCommand.Parameters.AddWithValue("metalName", metal);
                sqlCommand.Parameters.AddWithValue("baseCurrency", settings.Currency);
                var reader = sqlCommand.ExecuteReader();
                while (reader.Read()) 
                {
                    metals.Add(new MetalPrice
                    {
                        Metal = reader["Metal"].ToString(),
                        Currency = reader["Currency"].ToString(),
                        Price = double.Parse(reader["Price"].ToString()),
                        PrevClosePrice = double.Parse(reader["PrevPriceClose"].ToString()),
                        Date = DateTime.Parse(reader["RateDate"].ToString()),
                        Change = double.Parse(reader["Chg"].ToString()),
                        ChangePercent = double.Parse(reader["ChgPct"].ToString()),
                        PriceGram24k = double.Parse(reader["Price_Gram_24k"].ToString()),
                        PriceGram22k = double.Parse(reader["Price_Gram_22k"].ToString()),
                        PriceGram21k = double.Parse(reader["Price_Gram_21k"].ToString()),
                        PriceGram20k = double.Parse(reader["Price_Gram_20k"].ToString()),
                        PriceGram18k = double.Parse(reader["Price_Gram_18k"].ToString()),
                    });
                }
                reader.Close();
                reader.Dispose();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Could not insert new metal rate.");
                Console.WriteLine("Exception: {0}", ex.Message);
                return null;
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                    sqlConnection.Close();
                sqlCommand.Dispose();
                sqlConnection.Dispose();
            }
            return metals;
        }
        public static bool CacheData(List<MetalPrice> metalPrices, string cacheFile)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string file = Path.Combine(path, cacheFile);
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                List<MetalPrice> cache = JsonSerializer.Deserialize<List<MetalPrice>>(json);
                foreach (var metalPrice in metalPrices)
                    cache.Add(metalPrice);
                string result = JsonSerializer.Serialize(cache);
                File.WriteAllText(file, result);
            }
            else
            {
                string result = JsonSerializer.Serialize(metalPrices);
                File.WriteAllText(file, result);
            }
            return true;
        }
    }
}
