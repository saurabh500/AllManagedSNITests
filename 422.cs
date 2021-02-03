using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AllManagedSNITests
{
    class _422
    {
        static List<string> s_exceptions = new List<string>();

        static int iterationCount = 50;
        static int trips = 50;

        private static SqlConnectionStringBuilder _cs =
            new SqlConnectionStringBuilder("Data Source = localhost; Initial Catalog = Northwind; UID=sa; PWD=Moonshine4me;");

        private static SqlConnectionStringBuilder _cs_Mars =
            new SqlConnectionStringBuilder(_cs.ConnectionString)
            {
                MultipleActiveResultSets = true
            };

        private static SqlConnectionStringBuilder _cs_Encrypt =
            new SqlConnectionStringBuilder(_cs.ConnectionString)
            {
                Encrypt = true,
                TrustServerCertificate = true
            };

        private static SqlConnectionStringBuilder _cs_Mars_Encrypt =
            new SqlConnectionStringBuilder(_cs_Mars.ConnectionString)
            {
                Encrypt = true,
                TrustServerCertificate = true
            };

        void Main()
        {
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

            // Async
            RunTestAsync(_cs_Encrypt.ConnectionString, iterationCount, trips).Wait(); // No Errors
            RunTestAsync(_cs_Mars.ConnectionString, iterationCount, trips).Wait(); // No Errors
            RunTestAsync(_cs_Mars_Encrypt.ConnectionString, iterationCount, trips).Wait(); // No Errors

            // Sync
            RunTestSync(_cs_Encrypt.ConnectionString, iterationCount, trips); // No Errors
            RunTestSync(_cs_Mars.ConnectionString, iterationCount, trips); // ERROR!!
            RunTestSync(_cs_Mars_Encrypt.ConnectionString, iterationCount, trips); // ERROR!!
        }

        public static void RunTestSync(string connString, int iterationCount, int trips)
        {
            try
            {
                var total = Stopwatch.StartNew();

                PrepareData(connString);
                total.Restart();

                Enumerable.Range(0, iterationCount)
                    .AsParallel()
                    .WithDegreeOfParallelism(iterationCount)
                    .ForAll(n => Scenario4Sync(connString, n, trips));

                Console.WriteLine("******************************************************");

                Console.WriteLine("************** Completed Scenario4 Sync **************" +
                                $"\nConnection String: \t{connString}" +
                                $"\nTotal Iterations: \t{iterationCount}" +
                                $"\nTotal Trips: \t\t{trips}" +
                                $"\nTotal time elapsed: \t{total.Elapsed}" +
                                $"\nExceptions occurred: \t{s_exceptions.Count > 0}");

                s_exceptions.ForEach((string s) =>
                {
                    Console.WriteLine(s);
                });

                Console.WriteLine("******************************************************");
            }
            catch (Exception)
            {
                //Console.WriteLine(e.Message + e.StackTrace);
                throw;
            }
        }

        private static async Task RunTestAsync(string connString, int iterationCount, int trips)
        {
            try
            {
                var total = Stopwatch.StartNew();

                PrepareData(connString);
                total.Restart();

                async IAsyncEnumerable<int> RangeAsync()
                {
                    for (int i = 0; i < iterationCount; i++)
                    {
                        await Scenario4Async(connString, i, trips);
                        yield return i;
                    }
                }
                await foreach (var _ in RangeAsync())
                { }

                Console.WriteLine("******************************************************");

                Console.WriteLine("************** Completed Scenario4 Async **************" +
                                $"\nConnection String: \t{connString}" +
                                $"\nTotal Iterations: \t{iterationCount}" +
                                $"\nTotal Trips: \t\t{trips}" +
                                $"\nTotal time elapsed: \t{total.Elapsed}" +
                                $"\nExceptions occurred: \t{s_exceptions.Count > 0}");

                s_exceptions.ForEach((string s) =>
                {
                    Console.WriteLine(s);
                });

                Console.WriteLine("******************************************************");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }

        private static void Scenario4Sync(string connString, int number, int trips)
        {
            var userStopWatch = Stopwatch.StartNew();

            var buffer = new object[100];
            for (var i = 0; i < trips; i++)
            {
                var queryStopWatch = Stopwatch.StartNew();

                using (var connection = new SqlConnection(connString))
                {
                    try
                    {
                        connection.Open();
                        using (var command = new SqlCommand("SELECT * From TestTable", connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    reader.GetValues(buffer);
                                }
                            }
                        }

                        queryStopWatch.Stop();
                        userStopWatch.Stop();
                        Console.WriteLine($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed}");
                    }
                    catch (AggregateException ae)
                    {
                        s_exceptions.Add($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ae.Message}");
                        Console.WriteLine($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ae.Message}");
                    }
                    catch (Exception ex)
                    {
                        s_exceptions.Add($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ex.Message}");
                        Console.WriteLine($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ex.Message}");
                    }
                }
            }
        }

        private static async Task Scenario4Async(string connString, int number, int trips)
        {
            var userStopWatch = Stopwatch.StartNew();

            var buffer = new object[100];
            for (var i = 0; i < trips; i++)
            {
                var queryStopWatch = Stopwatch.StartNew();

                using (var connection = new SqlConnection(connString))
                {
                    try
                    {
                        await connection.OpenAsync().ConfigureAwait(false);
                        using (var command = new SqlCommand("SELECT * From TestTable", connection))
                        {
                            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                            {
                                while (await reader.ReadAsync().ConfigureAwait(false))
                                {
                                    reader.GetValues(buffer);
                                }
                            }
                        }

                        queryStopWatch.Stop();
                        userStopWatch.Stop();
                        Console.WriteLine($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed}");
                    }
                    catch (AggregateException ae)
                    {
                        s_exceptions.Add($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ae.Message}");
                        Console.WriteLine($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ae.Message}");
                    }
                    catch (Exception ex)
                    {
                        s_exceptions.Add($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ex.Message}");
                        Console.WriteLine($"Iteration: {number + 1} \tTrip: {i + 1} \tTime: {userStopWatch.Elapsed} \tException: {ex.Message}");
                    }
                }
            }
        }

        static void PrepareData(string connectionString)
        {
            var createTable = @"
                DROP TABLE IF EXISTS TestTable;
                CREATE TABLE TestTable
                (
                    [Id] [nvarchar](50) NOT NULL PRIMARY KEY,
                    [Name] [nvarchar](20) NOT NULL
                );";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(createTable, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
