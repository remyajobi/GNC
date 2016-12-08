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
        public static int NoOfRowCopied = 0;
       
        /// <summary>
        /// 
        /// </summary>
        public DataAccess()
        {
           
        }

        /// <summary>
        /// 
        /// </summary>
        public static SqlConnection objsqlConnection = new SqlConnection();
      
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strConnectionstring"></param>
        public void setSqlconnectionstring(string strConnectionstring) 
        {
            try
            {
                objsqlConnection.ConnectionString = strConnectionstring;
            }
            catch (Exception ex)
            {
                Library.CrawlerEventErrorLog("DB Connection Error : " + objsqlConnection.ConnectionString + " : " + ex.InnerException.ToString());

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Dttable"></param>
        /// <param name="Servertable"></param>
        /// <returns></returns>
        public bool sqlExecute(DataTable Dttable, string Servertable, string[] MappingColumns)
        {
            try
            {
                objsqlConnection.Open();
                
                // take note of SqlBulkCopyOptions.KeepIdentity , you may or may not want to use this for your situation.  
                using (var bulkCopy = new SqlBulkCopy(objsqlConnection.ConnectionString, SqlBulkCopyOptions.KeepNulls & SqlBulkCopyOptions.KeepIdentity))
                {
                    try
                    {
                        bulkCopy.ColumnMappings.Clear();
                        bulkCopy.BatchSize = Dttable.Rows.Count;
                        // my DataTable column names match my SQL Column names, so I simply made this loop. 
                        //However if your column names don't match, just pass in which datatable name matches the SQL column name in Column Mappings
                        foreach (string  column in MappingColumns)
                        {
                            bulkCopy.ColumnMappings.Add(column, column);
                        }
                   
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.DestinationTableName = Servertable;
                        destinationTbName = Servertable;

                        bulkCopy.SqlRowsCopied +=
                                      new SqlRowsCopiedEventHandler(OnSqlRowsTransfer);

                        bulkCopy.NotifyAfter = Dttable.Rows.Count;
                        bulkCopy.WriteToServer(Dttable);
                        Library.CrawlerEventInfoLog("Bulk copy " + Dttable.Rows.Count);
                    }
                    catch (Exception ex)
                    {
                        Library.CrawlerEventErrorLog("Bulk copying failed. " + ex.InnerException.ToString());
                        return false;
                    }
                    return true;
                }

            }
            catch (Exception ex)
            {
                Library.CrawlerEventErrorLog("Bulk copying failed. " + ex.InnerException.ToString());
                return false;
            }
            finally
            {
                objsqlConnection.Close();
            }
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnSqlRowsTransfer(object sender, SqlRowsCopiedEventArgs e)
        {
            DataAccess.NoOfRowCopied = 0;
            try
            {
                DataAccess.NoOfRowCopied = Convert.ToInt32(e.RowsCopied);
                Library.CrawlerEventInfoLog("RowCopied event " + DataAccess.NoOfRowCopied);
            }
            catch (Exception ex)
            {
                Library.CrawlerEventErrorLog("Bulk copying failed. " + ex.InnerException.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tbname"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public DataSet getdata(string tbname, string query)
        {
            DataSet objDataSet = new DataSet();
            try
            {
                using (SqlDataAdapter objsqlDataAdapter = new SqlDataAdapter())
                {
                    using (objsqlDataAdapter.SelectCommand = new SqlCommand(query, objsqlConnection))
                    {   
                        if (!string.IsNullOrEmpty(tbname))
                        {
                            objDataSet.Tables.Add(tbname);
                            objsqlDataAdapter.Fill(objDataSet.Tables[tbname]);
                        }
                        else
                        {
                            objsqlDataAdapter.Fill(objDataSet);
                        }
                        //return objDataSet;

                    }
                }
            }
            catch (Exception ex)
            {
                Library.CrawlerEventErrorLog("DB Error : " + objsqlConnection.ConnectionString + " : " + ex.InnerException.ToString());
                
            }
            return objDataSet;
        }

        public bool insert(string qry)
        {
            try
            {
                objsqlConnection.Open();
                using (SqlCommand objsqlcommand = new SqlCommand(qry, objsqlConnection))
                {
                    objsqlcommand.ExecuteNonQuery();
                    objsqlConnection.Close();
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

    }
}








