# Dr. SQL, M.D.

**Dr. SQL, M.D. (DrSQLMD)** is a C# console application that exports a MySQL database schema (tables, views, and stored procedures) into a **Markdown file** formatted for **Obsidian** or any Markdown viewer.

## Features

- Generates a **Table of Contents** with nested links to every table, view, and stored procedure  
- **Alphabetically sorted** for quick navigation  
- Includes:
  - Drop statements
  - Create statements
  - Initial data insert statements (if available)
- **Obsidian-friendly links** for seamless navigation

## Requirements

- [.NET 6 or later](https://dotnet.microsoft.com/)  
- [MySQL Server](https://dev.mysql.com/)  
- `MySql.Data` NuGet package (automatically installed via `dotnet add package`)

## Installation

```bash
git clone <your-repo-url>
cd DrSQLMD
dotnet build
```

If you haven't already, install the MySQL connector:

```bash
dotnet add package MySql.Data
```

## Usage

You can run **DrSQLMD** in two ways:

### **1. Direct Command-Line Arguments**

```bash
dotnet run "<connectionString>" <databaseName>
```

Example (for a local DB with root and no password):

```bash
dotnet run "Server=localhost;Database=example_database;User ID=root;Password=;Port=3306;SslMode=None;" example_database
```

### **2. Config File (`config.txt`)**

Create a `config.txt` in the project root with two lines:

```
Server=localhost;Database=example_database;User ID=root;Password=;Port=3306;SslMode=None;
example_database
```

Then run:

```bash
dotnet run
```

## Output

The program creates a file:

```
Database Design.md
```

This file contains:

- A **TOC** with nested links  
- Sections for **Tables**, **Views**, and **Stored Procedures**  
- Each section has:
  - Drop statement
  - Create statement
  - Initial data inserts (if any)
  - “Back to Table of Contents” links

The file is ready to drop into your **Obsidian vault**.

## Example TOC (Excerpt)

```
## Table of Contents
- [[#Tables|Tables]]
    - [[#Table users|users]]
    - [[#Table posts|posts]]
- [[#Views|Views]]
    - [[#View user_summary|user_summary]]
- [[#Stored Procedures|Stored Procedures]]
    - [[#Procedure get_active_users|get_active_users]]
```

## License

This project is licensed under the **MIT License**. You are free to use, modify, and distribute it as you wish.
