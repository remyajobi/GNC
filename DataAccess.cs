using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;


/// <summary>
/// Summary description for DataAccess
/// </summary>
/// 
namespace GNCService
{
    public class DataAccess
    {
        public static string destinationTbName = string.Empty;
        public static string destinationTbAsSubBeat = string.Empty;
        public List<int> IdsOfJournalists = new List<int>();
        public static List<int> IdsOfSubBeats = new List<int>();
      
        public DataAccess()
        {
            //
            // TODO: Add constructor logic here
            //


        }


        public static SqlConnection objsqlConnection = new SqlConnection();


        public void setSqlconnectionstring(string strConnectionstring)   //accept connctnstrng
        {
            objsqlConnection.ConnectionString = strConnectionstring;
        }

        //Query Execution

        public bool sqlExecute(DataTable Dttable, string Servertable)
        {
            try
            {
                objsqlConnection.Open();



                // take note of SqlBulkCopyOptions.KeepIdentity , you may or may not want to use this for your situation.  

                using (var bulkCopy = new SqlBulkCopy(objsqlConnection.ConnectionString, SqlBulkCopyOptions.KeepNulls & SqlBulkCopyOptions.KeepIdentity))
                {

                    bulkCopy.ColumnMappings.Clear();
                    bulkCopy.BatchSize = Dttable.Rows.Count;
                    // my DataTable column names match my SQL Column names, so I simply made this loop. However if your column names don't match, just pass in which datatable name matches the SQL column name in Column Mappings
                    foreach (DataColumn col in Dttable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = Servertable;
                    destinationTbName = Servertable;
                    bulkCopy.SqlRowsCopied +=
                      new SqlRowsCopiedEventHandler(OnSqlRowsTransfer);
                    bulkCopy.NotifyAfter = 1;
                    bulkCopy.WriteToServer(Dttable);
                  
                    return true;
                }

            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                objsqlConnection.Close();
            }

           
        }

        private static void OnSqlRowsTransfer(object sender,
         SqlRowsCopiedEventArgs e)
        {
           // Console.WriteLine("Copied {0} so far...", e.RowsCopied);

            int id = Library.GetIdsOfInsertedRecord(destinationTbName);
           int NoOfRowCopied =Convert.ToInt32( e.RowsCopied);

           for (int i = id; i >id-NoOfRowCopied; i--)
           {
               DataAccess objDataAccess = new DataAccess(); 
               objDataAccess.IdsOfJournalists.Add(i);
           }

        }

        public bool sqlExecuteSubBeat(DataTable Dttable, string Servertable)
        {
            try
            {
                objsqlConnection.Open();



                // take note of SqlBulkCopyOptions.KeepIdentity , you may or may not want to use this for your situation.  

                using (var bulkCopy = new SqlBulkCopy(objsqlConnection.ConnectionString, SqlBulkCopyOptions.KeepIdentity))
                {

                    bulkCopy.ColumnMappings.Clear();
                    bulkCopy.BatchSize = Dttable.Rows.Count;
                    // my DataTable column names match my SQL Column names, so I simply made this loop. However if your column names don't match, just pass in which datatable name matches the SQL column name in Column Mappings
                    foreach (DataColumn col in Dttable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    bulkCopy.BulkCopyTimeout = 600;
                    bulkCopy.DestinationTableName = Servertable;

                    bulkCopy.SqlRowsCopied +=
                      new SqlRowsCopiedEventHandler(OnSqlRowsTransfer);
                    bulkCopy.NotifyAfter =1;
                    bulkCopy.WriteToServer(Dttable);

                    return true;
                }

            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                objsqlConnection.Close();
            }


        }

        //Reading
        public DataSet getdata(string tbname, string query)
        {
            using (SqlDataAdapter objsqlDataAdapter = new SqlDataAdapter())
            {
                using (objsqlDataAdapter.SelectCommand = new SqlCommand(query, objsqlConnection))
                {
                    DataSet objDataSet = new DataSet();
                    if (!string.IsNullOrEmpty(tbname))
                    {
                        objDataSet.Tables.Add(tbname);
                        objsqlDataAdapter.Fill(objDataSet.Tables[tbname]);
                    }
                    else
                    {
                        objsqlDataAdapter.Fill(objDataSet);
                    }
                    return objDataSet;

                }
            }
        }

    }
}








