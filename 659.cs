using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AllManagedSNITests
{
    class _659
    {
        public static void RunTest(string connString, int iterationCount)
        {
            try
            {
                Console.WriteLine("\n*************** Running issue #659 **************\n");
                Console.WriteLine("Connection String : " + connString + "\n");
                Stopwatch watch = new Stopwatch();
                watch.Start();

                // will count how many responses contain correct result
                var successCount = 0;
                // will count how many responses return incorrect result
                var wrongCount = 0;
                // will count the number of exceptions
                var exceptionCount = 0;

                // will contain the list of results returned from threads
                var resultsFromThreads = new Queue<QueryResult>();

                // create many threads. all of them execute the same query but with different index
                var threads = new List<(Thread, ThreadState)>();
                for (var i = 0; i < iterationCount; i++)
                {
                    // this object will be passed to every thread
                    var threadInitState = new ThreadState
                    {
                        ConnectionString = connString,
                        Index = i,
                        Finished = false,
                    };

                    var thread = new Thread((threadStateObj) =>
                    {
                    // in thread, pull the data out of init object
                    var threadState = (ThreadState)threadStateObj;
                        string threadConnectionString;
                        int threadIndex;
                        lock (threadState)
                        {
                            threadConnectionString = threadState.ConnectionString;
                            threadIndex = threadState.Index;
                        }

                    // perform the query
                    var queryResult = QueryAndVerify(threadIndex, threadConnectionString).Result;

                    // put result back into queue
                    lock (resultsFromThreads)
                        { resultsFromThreads.Enqueue(queryResult); }
                    // mark this thread as finished
                    lock (threadState)
                        { threadState.Finished = true; }
                    });

                    // add thread and thread state to list for monitoring completion
                    threads.Add((thread, threadInitState));
                    // start the thread
                    thread.Start(threadInitState);
                }
                // loop this until all threads are finished

                var finished = false;
                while (!finished)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));

                    // check for all finished

                    finished = threads.All(item =>
                    {
                        var state = item.Item2;
                        lock (state)
                        {
                            return state.Finished;
                        }
                    });

                    // pull all results from queue and print them

                    var newResults = new List<QueryResult>();
                    lock (resultsFromThreads)
                    {
                        newResults.AddRange(resultsFromThreads);
                        resultsFromThreads.Clear();
                    }

                    foreach (var result in newResults)
                    {
                        if (result.Success)
                        {
                            //Console.WriteLine($"{result.ConnectionId} \t{watch.ElapsedMilliseconds / 1000} secs \t{result.Index} \tSuccess");
                            Console.Write(".");
                            successCount += 1;
                        }
                        else if (result.Exception == null)
                        {
                            Console.WriteLine($"{result.ConnectionId} \t{watch.ElapsedMilliseconds / 1000} secs \t{result.Index} >>>> {result.Result} \tWrong result");
                            wrongCount += 1;
                        }
                        else
                        {
                            Console.WriteLine($"{result.ConnectionId} \t{watch.ElapsedMilliseconds / 1000} secs \t{result.Index} \t{result.Exception.Message}");
                            exceptionCount += 1;
                        }
                    }
                }

                // not required, but join all threads before quiting
                foreach (var (t, _) in threads)
                {
                    t.Join();
                }

                // print summary

                Console.WriteLine($"Total Time taken: {watch.ElapsedMilliseconds / 1000} secs");
                Console.WriteLine($"Successful results: " + successCount);
                Console.WriteLine($"Wrong results: " + wrongCount);
                Console.WriteLine($"Exceptions: " + exceptionCount);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// Perform a db query and verify if returned result matches.
        /// Return an object that contains success flag or an exception.
        /// </summary>
        private static async Task<QueryResult> QueryAndVerify(int index, string connectionString)
        {
            using (var cn = new SqlConnection(connectionString))
            {
                try
                {
                    await cn.OpenAsync();
                    var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted);
                    var sql = $"select {index} as Id;";

                    var command = new SqlCommand(sql, cn, tx);
                    command.Transaction = tx;
                    command.CommandTimeout = 10;

                    var result = -1;

                    // Problem is here. ExecuteScalarAsync works fine.
                    // result = (int) await command.ExecuteScalarAsync().ConfigureAwait(false);
                    // ExecuteReader | ExecuteReaderAsync

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var columnIndex = reader.GetOrdinal("Id");
                            result = reader.GetInt32(columnIndex);
                            break;
                        }
                    }

                    //using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    //{
                    //    while (await reader.ReadAsync().ConfigureAwait(false))
                    //    {
                    //        var columnIndex = reader.GetOrdinal("Id");
                    //        result = reader.GetInt32(columnIndex);
                    //        break;
                    //    }
                    //}

                    tx.Commit();

                    return new QueryResult
                    {
                        ConnectionId = cn.ClientConnectionId,
                        Success = result == index,
                        Index = index,
                        Result = result
                    };
                }
                catch (Exception e)
                {
                    return new QueryResult
                    {
                        ConnectionId = cn.ClientConnectionId,
                        Success = false,
                        Exception = e,
                        Index = index,
                    };
                }
            }
        }

        private class QueryResult
        {
            public Guid ConnectionId;
            public bool Success;
            public Exception Exception;
            public long Index;
            public long Result;
        }

        private class ThreadState
        {
            public bool Finished;
            public string ConnectionString;
            public int Index;
        }
    }
}
