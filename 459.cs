using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace AllManagedSNITests
{
    class _459
    {
        private static readonly string script = File.ReadAllText(@"InsertSP.sql");
        private static CancellationToken token;

        public static void RunTest(string connString, int iterationCount)
        {
            try
            {
                Console.WriteLine("\n*************** Running issue #459 **************\n");
                Console.WriteLine("Connection String : " + connString + "\n");
                GC.Collect();
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                token = tokenSource.Token;
                RunAsync(iterationCount, connString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
        }

        private static void RunAsync(int counter, string connString)
        {
            while (counter > 0)
            {
                var items = Enumerable.Range(0, 10);
                Parallel.ForEach(items,
                     item =>
                     {
                         TestOpenConnectionAsync(token, connString).GetAwaiter().GetResult();
                     }
                     );

                Console.WriteLine(string.Format("counter: {0}, time:{1}", counter--, DateTime.Now));
            }
        }

        private static async Task TestOpenConnectionAsync(CancellationToken cancellationToken, string connString)
        {
            using (var connection = new SqlConnection(connString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {

                    //provide your local path to the sql file.
                    cmd.CommandText = script;// + " FOR XML AUTO, XMLDATA";

                    using (var r = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await r.ReadAsync(cancellationToken))
                        {
                            //Console.Write("CustomerId: {0}", await r.GetFieldValueAsync<string>(2));
                            await r.GetFieldValueAsync<string>(0);
                            await r.GetFieldValueAsync<string>(1);
                            await r.GetFieldValueAsync<string>(2);
                        }
                    }
                }
            }
        }
    }
}
