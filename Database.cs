using System.Collections.Generic;
using MetalPriceConsole.Models;
using MySqlConnector;
using System.Data;
using System;
using System.IO;
using System.Text.Json;
using System.Reflection;

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
                if (Save(metalPrice, connectionString, cacheFile))
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
        public static bool Save(MetalPrice metalPrice, string connectionString, string cacheFile)
        {
            if (metalPrice.Date.Year < 1900)
                return false;
            MySqlConnection sqlConnection = new(connectionString);
            MySqlCommand sqlCommand = new("usp_AddMetalPrice", sqlConnection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            try
            {
                sqlConnection.Open();
                sqlCommand.Parameters.AddWithValue("metal", metalPrice.Metal);
                sqlCommand.Parameters.AddWithValue("currency", metalPrice.Currency);
                sqlCommand.Parameters.AddWithValue("price", metalPrice.Price);
                sqlCommand.Parameters.AddWithValue("prev_price", metalPrice.PrevClosePrice);
                sqlCommand.Parameters.AddWithValue("ratedate", metalPrice.Date.ToString("yyyy/MM/dd"));
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
        public static bool CacheData(List<MetalPrice> metalPrices, string cacheFile)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string file = Path.Combine(path, cacheFile);
            if (File.Exists(file))
            {
                var json = File.ReadAllText(file);
                List<MetalPrice> cache = JsonSerializer.Deserialize<List<MetalPrice>>(json);
                foreach (var metalPrice in cache)
                    cache.Add(metalPrice);
                string result = JsonSerializer.Serialize(metalPrices);
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
