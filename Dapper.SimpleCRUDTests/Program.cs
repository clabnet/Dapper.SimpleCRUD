﻿using System;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Npgsql;

namespace Dapper.SimpleCRUDTests
{
    class Program
    {
        static void Main(string[] args)
        {
            // Setup();
            // RunTests();

            //PostgreSQL tests assume port 5432 with username postgres and password passw0rd
            //they are commented out by default since postgres setup is required to run tests
            SetupPG();
            RunTestsPG();

            // Please note : all tests using GUIDs are excluded !!!!

            // SetupSQLite();
            // RunTestsSQLite();
        }

        private static void Setup()
        {
            var projLoc = Assembly.GetAssembly(typeof(Program)).Location;
            var projFolder = Path.GetDirectoryName(projLoc);

            using (var connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;Initial Catalog=Master;Integrated Security=True"))
            {
                connection.Open();
                try
                {
                    connection.Execute(@" DROP DATABASE DapperSimpleCrudTestDb; ");
                }
                catch { }

                connection.Execute(@" CREATE DATABASE DapperSimpleCrudTestDb; ");
            }

            using (var connection = new SqlConnection(@"Data Source = (LocalDB)\v11.0;Initial Catalog=DapperSimpleCrudTestDb;Integrated Security=True"))
            {
                connection.Open();
                connection.Execute(@" create table Users (Id int IDENTITY(1,1) not null, Name nvarchar(100) not null, Age int not null, ScheduledDayOff int null, CreatedDate datetime DEFAULT(getdate())) ");
                connection.Execute(@" create table Car (CarId int IDENTITY(1,1) not null, Id int null, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
                connection.Execute(@" create table BigCar (CarId bigint IDENTITY(2147483650,1) not null, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
                connection.Execute(@" create table City (Name nvarchar(100) not null, Population int not null) ");
                connection.Execute(@" CREATE SCHEMA Log; ");
                connection.Execute(@" create table Log.CarLog (Id int IDENTITY(1,1) not null, LogNotes nvarchar(100) NOT NULL) ");
                connection.Execute(@" CREATE TABLE [dbo].[GUIDTest]([Id] [uniqueidentifier] NOT NULL,[name] [varchar](50) NOT NULL, CONSTRAINT [PK_GUIDTest] PRIMARY KEY CLUSTERED ([Id] ASC))");
                connection.Execute(@" create table StrangeColumnNames (ItemId int IDENTITY(1,1) not null Primary Key, word nvarchar(100) not null, colstringstrangeword nvarchar(100) not null) ");

            }
            Console.WriteLine("Created database");
        }

        private static void SetupPG()
        {

            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.PostgreSQL);

            using (var connection = new NpgsqlConnection(String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};", "localhost", "5432", "postgres", "passw0rd", "postgres")))
            {
                connection.Open();
                // drop  database 
                connection.Execute("DROP DATABASE IF EXISTS  testdbsimplecrud;");
                connection.Execute("CREATE DATABASE testdbsimplecrud  WITH OWNER = postgres ENCODING = 'UTF8' CONNECTION LIMIT = -1;");
            }
            System.Threading.Thread.Sleep(1000);

            using (var connection = new NpgsqlConnection(String.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};", "localhost", "5432", "postgres", "passw0rd", "testdbsimplecrud")))
            {
                connection.Open();
                connection.Execute(@" CREATE TABLE public.Users (Id serial PRIMARY KEY, Name varchar(50) NOT NULL, Age integer NOT NULL, ScheduledDayOff integer, createddate varchar(50) NOT NULL DEFAULT ('now'::text)::date) ");
                connection.Execute(@" create table Car (CarId SERIAL PRIMARY KEY, Id int null, Make varchar not null, Model varchar not null) ");
                connection.Execute(@" create table BigCar (CarId BIGSERIAL PRIMARY KEY, Make varchar not null, Model varchar not null) ");
                connection.Execute(@" alter sequence bigcar_carid_seq RESTART WITH 2147483650");
                connection.Execute(@" create table City (Name varchar not null, Population int not null) ");
                connection.Execute(@" CREATE SCHEMA Log; ");
                connection.Execute(@" create table Log.CarLog (Id SERIAL PRIMARY KEY, LogNotes varchar NOT NULL) ");
                connection.Execute(@" CREATE TABLE GUIDTest(Id uuid PRIMARY KEY,name varchar NOT NULL)");
                connection.Execute(@" create table StrangeColumnNames (ItemId Serial PRIMARY KEY, word varchar not null, colstringstrangeword varchar) ");
            }

        }

        private static void SetupSQLite()
        {
            System.IO.File.Delete(Directory.GetCurrentDirectory() + "\\MyDatabase.sqlite");
            SQLiteConnection.CreateFile("MyDatabase.sqlite");
            var connection = new SQLiteConnection("Data Source=MyDatabase.sqlite;Version=3;");
            using (connection)
            {
                connection.Open();

                connection.Execute(@" create table Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name nvarchar(100) not null, Age int not null, ScheduledDayOff int null, createddate datetime default current_timestamp ) ");
                connection.Execute(@" create table Car (CarId INTEGER PRIMARY KEY AUTOINCREMENT, Id INTEGER null, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
                connection.Execute(@" create table BigCar (CarId INTEGER PRIMARY KEY AUTOINCREMENT, Make nvarchar(100) not null, Model nvarchar(100) not null) ");
                connection.Execute(@" insert into BigCar (CarId,Make,Model) Values (2147483649,'car','car') ");
                connection.Execute(@" create table City (Name nvarchar(100) not null, Population int not null) ");
                connection.Execute(@" CREATE TABLE GUIDTest([Id] [uniqueidentifier] NOT NULL,[name] [varchar](50) NOT NULL, CONSTRAINT [PK_GUIDTest] PRIMARY KEY  ([Id] ASC))");
                connection.Execute(@" create table StrangeColumnNames (ItemId INTEGER PRIMARY KEY AUTOINCREMENT, word nvarchar(100) not null, colstringstrangeword nvarchar(100) not null) ");
            }
        }

        private static void RunTests()
        {
            var stopwatch = Stopwatch.StartNew();
            var sqltester = new Tests(SimpleCRUD.Dialect.SQLServer);
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var testwatch = Stopwatch.StartNew();
                Console.Write("Running " + method.Name + " in sql server");
                method.Invoke(sqltester, null);
                testwatch.Stop();
                System.Console.WriteLine(" - OK! {0}ms", testwatch.ElapsedMilliseconds);
            }
            stopwatch.Stop();

            // Write result
            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);

            using (var connection = new SqlConnection(@"Data Source=(LocalDB)\v11.0;Initial Catalog=Master;Integrated Security=True"))
            {
                connection.Open();
                try
                {
                    //drop any remaining connections, then drop the db.
                    connection.Execute(@" alter database DapperSimpleCrudTestDb set single_user with rollback immediate; DROP DATABASE DapperSimpleCrudTestDb; ");
                }
                catch { }
            }
            Console.Write("SQL Server testing complete.");
            Console.ReadKey();
        }

        private static void RunTestsPG()
        {

            var pgtester = new Tests(SimpleCRUD.Dialect.PostgreSQL);
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                Console.Write("Running " + method.Name + " in PostgreSQL");
                method.Invoke(pgtester, null);
                Console.WriteLine(" - OK!");
            }

            Console.Write("PostgreSQL testing complete.");
            Console.ReadKey();
        }

        private static void RunTestsSQLite()
        {


            var pgtester = new Tests(SimpleCRUD.Dialect.SQLite);
            foreach (var method in typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                //skip schema tests
                if (method.Name.Contains("Schema")) continue;

                Console.Write("Running " + method.Name + " in SQLite");
                method.Invoke(pgtester, null);
                Console.WriteLine(" - OK!");
            }

            Console.Write("SQLite testing complete.");
            Console.ReadKey();
        }

    }
}
