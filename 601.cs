using Microsoft.Data.SqlClient;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AllManagedSNITests
{
    public class _601
    {
        public static void RunTest(string connString, int iterationCount)
        {
            TestConnections(connString, iterationCount).Wait();
            TestConnections(connString, iterationCount).Wait();
        }

        static async Task TestConnections(string connString, int iterationCount)
        {
            // Low latency connection
            string lowLatencyConnectionString = connString;
            // High latency connection
            string highLatencyConnectionString = connString;


            string[] connectionStrings = new string[] { lowLatencyConnectionString, highLatencyConnectionString };

            string connectionType = "LowLatency";
            string csv = "";
            foreach (string connectionString in connectionStrings)
            {
                TimeSpan queryTime = TimeSpan.FromSeconds(0.5);
                for (int i = 0; i < 7; i++)
                {
                    (TimeSpan pooledTime, TimeSpan nonPooledTime) = await SingleRun(connectionString, queryTime, iterationCount);
                    csv += $"{connectionType},{queryTime.TotalSeconds},{pooledTime.TotalSeconds},{nonPooledTime.TotalSeconds}{Environment.NewLine}";
                    queryTime += TimeSpan.FromSeconds(0.5);
                }
                connectionType = "HighLatency";
            }
            File.WriteAllText("ConnectionTest.csv", csv);
            Console.WriteLine("\nTesting finished");
            Console.ReadLine();
        }

        private static async Task<(TimeSpan pooledTime, TimeSpan nonPooledTime)> SingleRun(string connectionString, TimeSpan queryTime, int iterationCount)
        {
            connectionString += ";Min Pool Size=200;Max Pool Size=500;";
            Console.WriteLine("WARM UP");
            await MeasureSingleConnectionAndReuse(connectionString);
            ClearPools();

            await MeasureSingleConnectionAndReuse(connectionString);
            ClearPools();

            await MeasureSingleConnectionAndReuse(connectionString);
            ClearPools();

            Console.WriteLine("\n\nCONCURRENT POOLED CONNECTIONS");
            TimeSpan pooledTime = MeasureParallelConnections(connectionString + ";Pooling=true;", queryTime, iterationCount);
            ClearPools();

            Console.WriteLine("\n\nCONCURRENT NON-POOLED CONNECTIONS");
            TimeSpan nonPooledTime = MeasureParallelConnections(connectionString + ";Pooling=false;", queryTime, iterationCount);
            ClearPools();
            return (pooledTime, nonPooledTime);
        }

        private static void ClearPools()
        {
            SqlConnection.ClearAllPools();
            Console.WriteLine("ALL POOLS CLEARED");
        }
        static ConcurrentDictionary<Guid, object> _connectionIDs = new ConcurrentDictionary<Guid, object>();
        
        private static TimeSpan MeasureParallelConnections(string connectionString, TimeSpan queryTime, int iterationCount)
        {
            Console.WriteLine("Start delay \t\tOpenAsync time \t\tIndex \tConnection ID \t\t\tReusedFromPool");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Task[] tasks = new Task[iterationCount];
            Stopwatch start = new Stopwatch();
            start.Start();
            for (int i = 0; i < iterationCount; i++)
            {
                tasks[i] = MeasureSingleConnection(i, start, connectionString, queryTime);
            }
            Task.WaitAll(tasks);
            start.Stop();
            Console.WriteLine($"{sw.Elapsed} \t{iterationCount} connections opened in paralel");
            return start.Elapsed;
        }

        private static async Task MeasureSingleConnection(int index, Stopwatch start, string connectionString, TimeSpan queryTime)
        {
            TimeSpan startDelay = start.Elapsed;
            Stopwatch sw = new Stopwatch();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                sw.Start();
                await connection.OpenAsync();
                Console.WriteLine($"{startDelay} \t{sw.Elapsed} \t{index} \t{connection.ClientConnectionId} \t{IsReuse(connection)}");
                //await Task.Delay(4000);
                ExecuteQuery(connection, queryTime);
            }
        }

        private static async Task MeasureSingleConnectionAndReuse(string connectionString)
        {
            Stopwatch sw = new Stopwatch();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                sw.Start();
                await connection.OpenAsync();
                Console.WriteLine($"{sw.Elapsed} \t{connection.ClientConnectionId} \t{IsReuse(connection)} Single open time ");
            }
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                sw.Restart();
                await connection.OpenAsync();
                Console.WriteLine($"{sw.Elapsed} \t{connection.ClientConnectionId} \t{IsReuse(connection)} Single open time with one previously opened connection");
            }
        }

        private static bool IsReuse(SqlConnection connection)
        {
            return !_connectionIDs.TryAdd(connection.ClientConnectionId, null);
        }

        private static void ExecuteQuery(SqlConnection connection, TimeSpan queryTime)
        {
            SqlCommand command = connection.CreateCommand();
            command.CommandText = $"WAITFOR DELAY '{queryTime:hh\\:mm\\:ss\\:fff}';";
            command.ExecuteNonQuery();
        }
    }
}
