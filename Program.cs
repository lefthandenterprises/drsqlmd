using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

class Program
{
    private static string _connectionString;
    private static string _databaseName;

    static void Main(string[] args)
    {
        if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help"))
        {
            Console.WriteLine("Usage: dotnet run <connectionString> <databaseName>");
            Console.WriteLine("Example: dotnet run \"Server=localhost;Database=mydb;User ID=myuser;Password=mypassword;Port=3306;SslMode=None;\" mydb");
            Console.WriteLine("If no arguments are provided, the program will attempt to read from config.txt in the current directory.");
            return;
        }

        if (args.Length < 2)
        {
            Console.WriteLine("No arguments provided. Attempting to read from config.txt...");
            if (!File.Exists("config.txt"))
            {
                Console.WriteLine("config.txt not found. Please create it or provide arguments.");
                Console.WriteLine("Expected format (two lines):\\n<connectionString>\\n<databaseName>");
                return;
            }

            var lines = File.ReadAllLines("config.txt");
            if (lines.Length < 2)
            {
                Console.WriteLine("config.txt is missing required lines. Expected two lines:");
                Console.WriteLine("Line 1: connection string\\nLine 2: database name");
                return;
            }

            _connectionString = lines[0].Trim();
            _databaseName = lines[1].Trim();
        }
        else
        {
            _connectionString = args[0];
            _databaseName = args[1];
        }

        var program = new Program();
        program.ExportDatabaseSchema();
    }

    public void ExportDatabaseSchema(bool useObsidianLinks = true)
    {
        string Link(string heading, string displayText)
        {
            return useObsidianLinks
                ? $"[[#{heading}|{displayText}]]"
                : $"[{displayText}](#{heading.ToLower().Replace(" ", "-")})";
        }

        var markdownBuilder = new StringBuilder();
        markdownBuilder.AppendLine($"# Database Schema: {_databaseName}");
        markdownBuilder.AppendLine($"## Generated on: {DateTime.Now}\n");

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        var tables = GetTables(connection).OrderBy(t => t).ToList();
        var views = GetViews(connection).OrderBy(v => v).ToList();
        var procedures = GetStoredProcedures(connection).OrderBy(p => p).ToList();

        markdownBuilder.AppendLine("## Table of Contents");
        markdownBuilder.AppendLine($"- {Link("Tables", "Tables")}");
        foreach (var t in tables)
            markdownBuilder.AppendLine($"    - {Link($"Table {t}", t)}");
        markdownBuilder.AppendLine($"- {Link("Views", "Views")}");
        foreach (var v in views)
            markdownBuilder.AppendLine($"    - {Link($"View {v}", v)}");
        markdownBuilder.AppendLine($"- {Link("Stored Procedures", "Stored Procedures")}");
        foreach (var p in procedures)
            markdownBuilder.AppendLine($"    - {Link($"Procedure {p}", p)}");

        markdownBuilder.AppendLine("\n## Tables");
        foreach (var table in tables)
        {
            markdownBuilder.AppendLine($"\n### Table {table}");
            markdownBuilder.AppendLine("#### Drop Statement");
            markdownBuilder.AppendLine($"```sql\nDROP TABLE IF EXISTS `{table}`;\n```");
            markdownBuilder.AppendLine("#### Create Statement");
            markdownBuilder.AppendLine($"```sql\n{GetTableCreateStatement(connection, table)}\n```");
            foreach (var insert in GetTableInsertStatements(connection, table))
                markdownBuilder.AppendLine($"```sql\n{insert}\n```");

            markdownBuilder.AppendLine($"\n{Link("Table of Contents", "Back to Table of Contents")}\n");
        }

        markdownBuilder.AppendLine("## Views");
        foreach (var view in views)
        {
            markdownBuilder.AppendLine($"\n### View {view}");
            markdownBuilder.AppendLine("#### Drop Statement");
            markdownBuilder.AppendLine($"```sql\nDROP VIEW IF EXISTS `{view}`;\n```");
            markdownBuilder.AppendLine("#### Create Statement");
            markdownBuilder.AppendLine($"```sql\n{GetViewCreateStatement(connection, view)}\n```");
            markdownBuilder.AppendLine($"\n{Link("Table of Contents", "Back to Table of Contents")}\n");
        }

        markdownBuilder.AppendLine("## Stored Procedures");
        foreach (var procedure in procedures)
        {
            markdownBuilder.AppendLine($"\n### Procedure {procedure}");
            markdownBuilder.AppendLine("#### Drop Statement");
            markdownBuilder.AppendLine($"```sql\nDROP PROCEDURE IF EXISTS `{procedure}`;\n```");
            markdownBuilder.AppendLine("#### Create Statement");
            markdownBuilder.AppendLine($"```sql\n{GetProcedureCreateStatement(connection, procedure)}\n```");
            markdownBuilder.AppendLine($"\n{Link("Table of Contents", "Back to Table of Contents")}\n");
        }

        File.WriteAllText("Database Design.md", markdownBuilder.ToString());
        Console.WriteLine("Database schema exported successfully!");
    }

    private List<string> GetTables(MySqlConnection connection)
    {
        var list = new List<string>();
        using var cmd = new MySqlCommand("SHOW FULL TABLES WHERE Table_type = 'BASE TABLE'", connection);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        r.Close();
        return list;
    }

    private List<string> GetViews(MySqlConnection connection)
    {
        var list = new List<string>();
        using var cmd = new MySqlCommand("SHOW FULL TABLES WHERE Table_type = 'VIEW'", connection);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString(0));
        r.Close();
        return list;
    }

    private List<string> GetStoredProcedures(MySqlConnection connection)
    {
        var list = new List<string>();
        using var cmd = new MySqlCommand($"SHOW PROCEDURE STATUS WHERE Db = '{_databaseName}'", connection);
        using var r = cmd.ExecuteReader();
        while (r.Read()) list.Add(r.GetString("Name"));
        r.Close();
        return list;
    }

    private string GetTableCreateStatement(MySqlConnection connection, string name)
    {
        using var cmd = new MySqlCommand($"SHOW CREATE TABLE `{name}`", connection);
        using var r = cmd.ExecuteReader();
        return r.Read() ? r.GetString("Create Table") : string.Empty;
    }

    private List<string> GetTableInsertStatements(MySqlConnection connection, string name)
    {
        var list = new List<string>();
        using var cmd = new MySqlCommand($"SELECT * FROM `{name}`", connection);
        using var r = cmd.ExecuteReader();
        var cols = new List<string>();
        for (int i = 0; i < r.FieldCount; i++) cols.Add(r.GetName(i));
        while (r.Read())
        {
            var c = string.Join(", ", cols.Select(col => $"`{col}`"));
            var v = string.Join(", ", cols.Select(col => r[col] == DBNull.Value ? "NULL" : $"'{r[col].ToString().Replace("'", "''")}'"));
            list.Add($"INSERT INTO `{name}` ({c}) VALUES ({v});");
        }
        r.Close();
        return list;
    }

    private string GetViewCreateStatement(MySqlConnection connection, string name)
    {
        using var cmd = new MySqlCommand($"SHOW CREATE VIEW `{name}`", connection);
        using var r = cmd.ExecuteReader();
        return r.Read() ? r.GetString("Create View") : string.Empty;
    }

    private string GetProcedureCreateStatement(MySqlConnection connection, string name)
    {
        using var cmd = new MySqlCommand($"SHOW CREATE PROCEDURE `{name}`", connection);
        using var r = cmd.ExecuteReader();
        return r.Read() ? r.GetString("Create Procedure") : string.Empty;
    }
}
