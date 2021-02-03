using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AllManagedSNITests
{
    class _659_2
    {
        private static int _successes;
        private static int _networkErrors;
        private static int _invalidResults;
        private static int _missingResults;

        public static void RunTest(string connectionString, int iterationCount)
        {
            RunTestAsync(connectionString, iterationCount).Wait();
        }

        static async Task RunTestAsync(string connectionString, int iterationCount)
        {
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelExecution);

            await InitializeAsync(connectionString);

            await ExecuteTestAsync(cts.Token, connectionString, iterationCount);

            Console.WriteLine("Done!");
        }

        private static async Task ExecuteTestAsync(CancellationToken cancellationToken, string connectionString, int iterationCount)
        {
            using var monitorCts = new CancellationTokenSource();
            var monitorTask = MonitorAsync(monitorCts.Token);

            var tasks = new Task[iterationCount];
            Console.WriteLine($"Starting {tasks.Length} tasks...");

            for (var i = 0; i < tasks.Length; i++)
            {
                tasks[i] = ExecuteLoopAsync(i, cancellationToken, connectionString);
            }

            await Task.WhenAll(tasks);

            monitorCts.Cancel();
            await monitorTask;
        }

        private static async Task ExecuteLoopAsync(int id, CancellationToken cancellationToken, string connectionString)
        {
            await Task.Yield();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await ExecuteTransactionAsync(id, connectionString);
                    if (!result.HasValue)
                    {
                        Interlocked.Increment(ref _missingResults);
                    }
                    else if (result != id)
                    {
                        Interlocked.Increment(ref _invalidResults);
                    }
                    else
                    {
                        Interlocked.Increment(ref _successes);
                    }
                }
                catch (Exception exception) when (exception is SqlException || exception is InvalidOperationException)
                {
                    Interlocked.Increment(ref _networkErrors);
                }
            }
        }

        private static async Task<int?> ExecuteTransactionAsync(int id, string connectionString)
        {
            int? result = null;

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var tx = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            await using var command = new SqlCommand(@"select @Id as Id", connection, tx);
            command.CommandTimeout = 1;
            command.Parameters.AddWithValue("Id", id);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var columnIndex = reader.GetOrdinal("Id");
                    result = reader.GetInt32(columnIndex);
                    break;
                }
            }

            await tx.CommitAsync();

            return result;
        }


        private static async Task InitializeAsync(string connectionString)
        {
            //FlushFirewall();

            Console.WriteLine("Waiting for SQLServer...");
            await ReadinessCheckAsync(connectionString);
            Console.WriteLine("Smoke test passed!");

            //await TestFirewallAsync(connectionString);

            //SetupFirewall();
        }

        private static async Task ReadinessCheckAsync(string connectionString)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            Exception lastException = null;

            while (true)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine(lastException.ToString());
                    throw lastException;
                }

                try
                {
                    await ExecuteTransactionAsync(1, connectionString);
                    return;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task MonitorAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                var successes = _successes;
                var networkErrors = _networkErrors;
                var invalidResults = _invalidResults;
                var missingResults = _missingResults;
                int total = successes + networkErrors + invalidResults + missingResults;

                Console.WriteLine($"Processed: {total,6} - Network errors: {networkErrors,6} - Missing: {missingResults,6} - Invalid: {invalidResults,6}");
            }
        }


        private static void CancelExecution(object sender, ConsoleCancelEventArgs args)
        {
            // Set the Cancel property to true to prevent the process from terminating.
            args.Cancel = true;
            Console.WriteLine("Requested cancellation..");
        }
    }
}
