using System;
using System.Diagnostics.Tracing;
using Microsoft.Data.SqlClient;

namespace AllManagedSNITests
{
    class Program
    {
        private static SqlConnectionStringBuilder _cs =
            new SqlConnectionStringBuilder("Data Source = tcp:localhost; Integrated Security=true; Connection Timeout = 180;");

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

        static void Main(string[] args)
        {
            AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);
            if (args == null || args.Length == 0) args = new string[] { "659" };
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "85":
                            _085.RunTest(_cs_Encrypt.ConnectionString, 100);// Currently passing
                            _085.RunTest(_cs_Mars.ConnectionString, 100);// Currently passing
                            _085.RunTest(_cs_Mars_Encrypt.ConnectionString, 100);// Currently passing
                            break;
                        case "459":
                            _459.RunTest(_cs_Encrypt.ConnectionString, 100);
                            _459.RunTest(_cs_Mars_Encrypt.ConnectionString, 100);
                            break;

                        // Run below tests standalone, not after above tests as it may not reproduce error.
                        case "422":
                            //_422.RunTestSync(_cs_Encrypt.ConnectionString, 100, 100); // Currently passing
                            _422.RunTestSync(_cs_Mars.ConnectionString, 100, 100);
                            _422.RunTestSync(_cs_Mars_Encrypt.ConnectionString, 100, 100);
                            break;
                        case "601":
                            _601.RunTest(_cs_Encrypt.ConnectionString, 100);
                            //_601.RunTest(_cs_Mars.ConnectionString, 100);
                            //_601.RunTest(_cs_Mars_Encrypt.ConnectionString, 100);
                            break;
                        case "659":
                            //_659.RunTest(_cs_Encrypt.ConnectionString, 10000); // Currently passing
                            //_659.RunTest(_cs_Mars.ConnectionString, 6000); // Wrong Data errors
                            //_659.RunTest(_cs_Mars.ConnectionString, 10); // Wrong Data errors
                            //_659.RunTest(_cs_Mars_Encrypt.ConnectionString, 10000); // Wrong Data errors
                            //_659_2.RunTest(_cs_Encrypt.ConnectionString, 6000); // Currently passing
                            _659_2.RunTest(_cs_Mars.ConnectionString, 1000); // Wrong Data errors
                            //_659_2.RunTest(_cs_Mars_Encrypt.ConnectionString, 10000); // Wrong Data errors
                            break;
                        case "808":
                            //_808.RunTest(_cs_Encrypt.ConnectionString, 1);
                            _808.RunTest(_cs_Mars.ConnectionString, 1);
                            _808.RunTest(_cs_Mars_Encrypt.ConnectionString, 1);
                            break;
                        default:
                            Console.WriteLine("Provide a valid test number to run as arg: 85 | 459 | 422 | 601 | 659 | 808");
                            break;
                    }
                }
            }
            else
                Console.WriteLine("Provide a test number to run as arg: 85 | 459 | 422 | 601 | 659 | 808");
        }
    }
    public class SqlClientListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // Only enable events from SqlClientEventSource.
            if (eventSource.Name.Equals("Microsoft.Data.SqlClient.EventSource"))
            {
                // Use EventKeyWord 2 to capture basic application flow events.
                // See the above table for all available keywords.
                EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)2);
            }
        }

        // This callback runs whenever an event is written by SqlClientEventSource.
        // Event data is accessed through the EventWrittenEventArgs parameter.
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // Print event data.
            Console.WriteLine(eventData.Payload[0]);
        }
    }
}
