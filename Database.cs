using System.Collections.Generic;
using RestSharp;
using MetalPriceConsole.Models;
using MySqlConnector;
using System.Data;
using System;
using Newtonsoft.Json;
using System.IO;

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
        public static bool Save(List<MetalPrice> metalPrices, string connectionString)
        {
            int success = 0;
            foreach (MetalPrice metalPrice in metalPrices)
            {
                if (Save(metalPrice, connectionString))
                    success++;
            }
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
            if (metalPrice.date.Year < 1900)
                return false;
            MySqlConnection sqlConnection = new MySqlConnection(connectionString);
            MySqlCommand sqlCommand = new MySqlCommand("usp_AddMetalPrice", sqlConnection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            try
            {
<<<<<<< HEAD
                sqlConnection.Open();
                sqlCommand.Parameters.AddWithValue("metal", metalPrice.metal);
                sqlCommand.Parameters.AddWithValue("currency", metalPrice.currency);
                sqlCommand.Parameters.AddWithValue("price", metalPrice.price);
                sqlCommand.Parameters.AddWithValue("prev_price", metalPrice.prev_close_price);
                sqlCommand.Parameters.AddWithValue("ratedate", metalPrice.date.ToString("yyyy/MM/dd"));
                sqlCommand.Parameters.AddWithValue("chg", metalPrice.ch);
                sqlCommand.Parameters.AddWithValue("chg_pct", metalPrice.chp);
                sqlCommand.Parameters.AddWithValue("price_gram_24k", metalPrice.price_gram_24k);
                sqlCommand.Parameters.AddWithValue("price_gram_22k", metalPrice.price_gram_22k);
                sqlCommand.Parameters.AddWithValue("price_gram_21k", metalPrice.price_gram_21k);
                sqlCommand.Parameters.AddWithValue("price_gram_20k", metalPrice.price_gram_20k);
                sqlCommand.Parameters.AddWithValue("price_gram_18k", metalPrice.price_gram_18k);
=======
 
               sqlConnection.Open();
                sqlCommand.Parameters.AddWithValue("price", goldPrice.price);
                sqlCommand.Parameters.AddWithValue("prev_price", goldPrice.prev_close_price);
                sqlCommand.Parameters.AddWithValue("ratedate", goldPrice.date.ToString("yyyy/MM/dd"));
                sqlCommand.Parameters.AddWithValue("chg", goldPrice.ch);
                sqlCommand.Parameters.AddWithValue("chg_pct", goldPrice.chp);
                sqlCommand.Parameters.AddWithValue("price_gram_24k", goldPrice.price_gram_24k);
                sqlCommand.Parameters.AddWithValue("price_gram_22k", goldPrice.price_gram_22k);
                sqlCommand.Parameters.AddWithValue("price_gram_21k", goldPrice.price_gram_21k);
                sqlCommand.Parameters.AddWithValue("price_gram_20k", goldPrice.price_gram_20k);
                sqlCommand.Parameters.AddWithValue("price_gram_18k", goldPrice.price_gram_18k);
>>>>>>> main
                var recs = sqlCommand.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Could not insert new gold rate.");
                Console.WriteLine("Exception: {0}", ex.Message);
                List<MetalPrice> metalPrices = new List<MetalPrice>();
                if (File.Exists("MetalPrice.cache"))
                {
                    var file = File.ReadAllText("MetalPrice.cache");
                    metalPrices = JsonConvert.DeserializeObject<List<MetalPrice>>(file);
                }
                metalPrices.Add(metalPrice);
                string result = JsonConvert.SerializeObject(metalPrices, Formatting.Indented);
                File.WriteAllText($"MetalPrice.cache", result);

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
    }
}
