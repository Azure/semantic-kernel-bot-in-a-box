using System;
using System.ComponentModel;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Azure.Identity;

namespace Plugins;

public class SQLPlugin
{
    private readonly string connectionString;
    private DefaultAzureCredential credential;
    public SQLPlugin(IConfiguration config) 
    {
        connectionString = config.GetValue<string>("SQL_CONNECTION_STRING");
        credential = new DefaultAzureCredential();
    }




    [SKFunction, Description("Obtain the table names in AdventureWorksLT, which contains customer and sales data. Always run this before running other queries instead of assuming the user mentioned the correct name.")]
    public string GetTables(SKContext context) {
        return QueryAsCSV($"SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;");
    }



    [SKFunction, Description("Obtain the database schema for a table in AdventureWorksLT.")]
    public string GetSchema(
        [Description("The table to get the schema for. Do not include the schema name.")] string tableName
    ) 
    {
        return QueryAsCSV($"SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}';");
    }



    [SKFunction, Description("Run SQL against the AdventureWorksLT database")]
    public string RunQuery(
        [Description("The query to run on SQL Server. When referencing tables, make sure to add the schema names.")] string query
    )
    {
        return QueryAsCSV(query);
    }




    private string QueryAsCSV(string query) 
    {

        var output = "[DATABASE RESULTS] \n";
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            // var token = credential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));
            // connection.AccessToken = token.Token;
            Console.WriteLine("Running SQL Plugin");

            SqlCommand command = new SqlCommand(query, connection);
            connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            Console.WriteLine("Connection Opened");
            try
            {
                Console.WriteLine("Reading...");
                for (int i = 0; i < reader.FieldCount; i++) {
                    output += reader.GetName(i);
                    if (i < reader.FieldCount - 1) 
                        output += ",";
                }
                output += "\n";
                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++) {
                        var columnName = reader.GetName(i);
                        output += reader[columnName].ToString();
                        if (i < reader.FieldCount - 1) 
                            output += ",";
                    }
                    output += "\n";
                }
            } catch (Exception e) {
                Console.WriteLine(e);
            }
            finally
            {
                reader.Close();
            }
        }
        return output;
    }

}