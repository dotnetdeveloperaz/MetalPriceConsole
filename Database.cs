using System.Collections.Generic;
using RestSharp;
using GoldPriceConsole.Models;
using MySqlConnector;
using System.Data;
using System;
using Newtonsoft.Json;
using System.IO;

namespace GoldPriceConsole
{
    internal class Database
    {
        /// <summary>
        /// Saves collection of GoldPrice basically looping through the 
        /// collection and called the single GoldPrice method.
        /// </summary>
        /// <param name="goldPrices"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static bool Save(List<GoldPrice> goldPrices, string connectionString)
        {
            int success = 0;
            foreach (GoldPrice goldPrice in goldPrices)
            {
                if (Save(goldPrice, connectionString))
                    success++;
            }
            return (success == goldPrices.Count);
        }
        /// <summary>
        /// Saves gold price for specific day.
        /// </summary>
        /// <param name="goldPrice">Type GoldPrice</param>
        /// <param name="connectionString"Database connectionstring></param>
        /// <returns></returns>
        public static bool Save(GoldPrice goldPrice, string connectionString)
        {
            if (goldPrice.date.Year < 1900)
                return false;
            MySqlConnection sqlConnection = new MySqlConnection(connectionString);
            MySqlCommand sqlCommand = new MySqlCommand("usp_AddGoldPrice", sqlConnection);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            try
            {
 
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
                var recs = sqlCommand.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Could not insert new gold rate.");
                Console.WriteLine("Exception: {0}", ex.Message);
                List<GoldPrice> goldPrices = new List<GoldPrice>();
                if (File.Exists("GoldPrice.cache"))
                {
                    var file = File.ReadAllText("GoldPrice.cache");
                    goldPrices = JsonConvert.DeserializeObject<List<GoldPrice>>(file);
                }
                goldPrices.Add(goldPrice);
                string result = JsonConvert.SerializeObject(goldPrices, Formatting.Indented);
                File.WriteAllText($"GoldPrice.cache", result);

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
