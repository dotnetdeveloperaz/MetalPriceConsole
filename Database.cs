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
        private List<GoldPrice> _goldPrices;

        public static bool Save(List<GoldPrice> goldPrices, string connectionString)
        {
            int success = 0;
            foreach(GoldPrice goldPrice in goldPrices)
            {
                if (Save(goldPrice, connectionString))
                    success++;
            }
            return (success == goldPrices.Count);
        }

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
                var recs = sqlCommand.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Could not insert new gold rate.");
                Console.WriteLine("Exception: {0}", ex.Message);
                string result = JsonConvert.SerializeObject(goldPrice, Formatting.Indented);
                File.AppendAllText($"GoldPrice.cache", result);
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
