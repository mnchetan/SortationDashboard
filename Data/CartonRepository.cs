using SortationDashboard.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace SortationDashboard.Data
{
    public class CartonRepository
    {
        private readonly string _connectionString;

        public CartonRepository(string connectionString)
        {
            _connectionString = connectionString;

            // Initiate the self-provisioning sequence the moment this class is created
            EnsureDatabaseAndTableExist();
        }

        private void EnsureDatabaseAndTableExist()
        {
            // 1. Figure out exactly where the .exe is running from (your bin/Debug folder)
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string mdfFilePath = Path.Combine(appDirectory, "SortationDB.mdf");
            string ldfFilePath = Path.Combine(appDirectory, "SortationDB_log.ldf");

            // 2. Connect to the master SQL database first to issue server-level commands
            string masterConnString = "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";

            try
            {
                using (SqlConnection masterConn = new SqlConnection(masterConnString))
                {
                    masterConn.Open();

                    // 3. Ask SQL Server if 'SortationDB' already exists in its registry
                    using (SqlCommand checkDbCmd = new SqlCommand("SELECT db_id('SortationDB')", masterConn))
                    {
                        object result = checkDbCmd.ExecuteScalar();

                        // 4. If it doesn't exist, create it physically on the hard drive
                        if (result == null || result == DBNull.Value)
                        {
                            Console.WriteLine("[Provisioning] Database not found. Creating new .mdf file...");

                            string createDbSql = $@"
                                CREATE DATABASE [SortationDB]
                                ON PRIMARY (
                                    NAME = SortationDB_Data,
                                    FILENAME = '{mdfFilePath}'
                                )
                                LOG ON (
                                    NAME = SortationDB_Log,
                                    FILENAME = '{ldfFilePath}'
                                )";

                            using (SqlCommand createDbCmd = new SqlCommand(createDbSql, masterConn))
                            {
                                createDbCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // 5. Now connect to our newly created database and ensure the table exists
                using (SqlConnection targetConn = new SqlConnection(_connectionString))
                {
                    targetConn.Open();

                    const string createTableSql = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CartonEvents' and xtype='U')
                        BEGIN
                            CREATE TABLE [dbo].[CartonEvents] (
                                [EventId] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                                [MessageId] VARCHAR(50) NOT NULL UNIQUE,
                                [EventType] VARCHAR(20) NOT NULL,
                                [LabelNumber] VARCHAR(50) NOT NULL,
                                [TargetChute] VARCHAR(20) NOT NULL,
                                [ProcessedTimestamp] DATETIME NOT NULL
                            );
                        END";

                    using (SqlCommand createTableCmd = new SqlCommand(createTableSql, targetConn))
                    {
                        createTableCmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fatal Provisioning Error] {ex.Message}");
            }
        }

        // --- 1. WRITE TO DATABASE ---
        public async Task<bool> InsertEventAsync(ParsedCartonEvent cartonEvent)
        {
            const string sql = @"
                INSERT INTO CartonEvents (MessageId, EventType, LabelNumber, TargetChute, ProcessedTimestamp)
                VALUES (@MessageId, @EventType, @LabelNumber, @TargetChute, @ProcessedTimestamp)";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@MessageId", SqlDbType.VarChar, 50).Value = cartonEvent.MessageId;
                    cmd.Parameters.Add("@EventType", SqlDbType.VarChar, 20).Value = cartonEvent.EventType;
                    cmd.Parameters.Add("@LabelNumber", SqlDbType.VarChar, 50).Value = cartonEvent.LabelNumber;
                    cmd.Parameters.Add("@TargetChute", SqlDbType.VarChar, 20).Value = cartonEvent.TargetChute;
                    cmd.Parameters.Add("@ProcessedTimestamp", SqlDbType.DateTime).Value = cartonEvent.ProcessedTimestamp;

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627) return false; // Ignored: Duplicate MessageId (Idempotency)
                throw;
            }
        }

        // --- 2. READ FROM DATABASE ---
        public async Task<List<ParsedCartonEvent>> GetRecentEventsAsync()
        {
            List<ParsedCartonEvent> eventsList = new List<ParsedCartonEvent>();

            const string sql = @"
                SELECT TOP 50 MessageId, EventType, LabelNumber, TargetChute, ProcessedTimestamp 
                FROM CartonEvents 
                ORDER BY ProcessedTimestamp DESC";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                await conn.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        eventsList.Add(new ParsedCartonEvent
                        {
                            MessageId = reader.GetString(0),
                            EventType = reader.GetString(1),
                            LabelNumber = reader.GetString(2),
                            TargetChute = reader.GetString(3),
                            ProcessedTimestamp = reader.GetDateTime(4)
                        });
                    }
                }
            }
            return eventsList;
        }
    }
}