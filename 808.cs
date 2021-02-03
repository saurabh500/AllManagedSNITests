using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;

namespace AllManagedSNITests
{
    public class _808
    {
        public static void RunTest(string connString, int iteration)
        {
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(connString))
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("[dbo].[Sp_GetAll]", con))
                {
                    cmd.Parameters.Clear();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@TenantCode", SqlDbType.VarChar, 10).Value = "0";

                    //Below code not working - when I inspect dataset it shows blank
                    SqlDataAdapter adp = new SqlDataAdapter(cmd);
                    adp.Fill(ds);

                    //Below code working  - when I inspect dataReader object I am able to see data fields
                    SqlDataReader dataReader = cmd.ExecuteReader();
                }
            }
        }
    }
}
