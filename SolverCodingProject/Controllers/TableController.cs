// Crystal MacDonald
// 05/2022


using Microsoft.AspNetCore.Mvc;
using SolverCodingProject.Data;
using System.Data.Entity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SolverCodingProject.Controllers
{
    public class TableController : Controller
    {
        // For my testing, I am storing the connection strings in appsettings.json and reading different db information from there
        private readonly IConfiguration _config;
        public TableController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        [Route("/{databaseId}/{tableName}")]
        public IActionResult GetTableData([FromRoute] string databaseId, [FromRoute] string tableName, [FromQuery] int NumItems, [FromQuery] int PageNum)
        {
            // Check to see if we got the database from the user or not
            if (databaseId != null)
            {
                // Checking the appsettings.json file for a matching connection string
                var dbConnectionString = _config.GetConnectionString(databaseId);
                // If we are not able to locate a connection string, give an error message
                if (string.IsNullOrEmpty(dbConnectionString))
                {
                    return BadRequest("Can not find matching connectionstring in appsettings.json");
                }

                var returnval = new List<List<object>>();
                using (SqlConnection newconnection = new SqlConnection(dbConnectionString))
                {
                    newconnection.Open();
                    // Here we could add some checks to see if the table we're being passed
                    // exists in the db we are connected to

                    // We could also take the opportunity to follow foreign keys for more or different data.
                    // To do that, one option is to check which keys are on the table using SqlConnection.GetSchema() 

                    var sqlRequest = $"SELECT * FROM {tableName}";

                    using (SqlCommand newcommand = new SqlCommand(sqlRequest, newconnection))
                    {
                        SqlDataReader reader = newcommand.ExecuteReader();
                        if (reader.HasRows)
                        {
                            var rows = new List<object>();
                            // If we wanted to reduce the number of columns in the JSON Object we are returning
                            // One possibility would be to adjust the values here
                            Object[] values = new Object[reader.FieldCount];
                            while (reader.Read())
                            {
                                int fieldCount = reader.GetValues(values);

                                for (int i = 0; i < fieldCount; i++)
                                {
                                    try
                                    {
                                        rows.Add(values[i]);
                                    }
                                    catch (Exception ex)
                                    {
                                        return BadRequest(ex.Message);
                                    }
                                }

                                // Clear out the list so we only add each row once
                                returnval.Add(rows);
                                rows = new List<object>();
                            }
                        }
                    }
                }

                // Add paging if the page number and number of items per page are passed in
                if (PageNum != 0 && NumItems != 0)
                {
                    returnval = returnval.Skip(NumItems * PageNum).Take(NumItems).ToList();
                }

                // convert to json object we can return nicely
                var retval = Json(returnval);

                return Ok(retval);
            }

            // No connection string passed in, send an error
            else
                return BadRequest("Missing Database ID");
        }

        // This is how I would normally access the database and table info. Includes pagination and table connection using IDs
        // Not shown here: Dynamically access db and table based on values passed in.
        [HttpGet]
        [Route("GetKnownTable")]
        public IActionResult GetKnownTable([FromQuery] int NumItems, [FromQuery] int PageNum, [FromQuery] int supplierId)
        {
            using (var db = new WideWorldImportersFullContext())
            {
                // Get the info from one table and return it
                var SupplierInfo = db.Suppliers.ToList();
                SupplierInfo = SupplierInfo.OrderBy(key => key.SupplierId).ToList();

                if (supplierId > 0)
                {
                    SupplierInfo = SupplierInfo.Where(x => x.SupplierId == supplierId).ToList();
                }
                //var returnInfo = SupplierInfo.Skip(NumItems * PageNum).Take(NumItems);

                var supplierCat = new List<SupplierCategory>();
                var catNameList = new List<string>();
                foreach (var supplier in SupplierInfo)
                {
                    var supplierCatObj = db.SupplierCategories.Where(x => x.SupplierCategoryId == supplier.SupplierCategoryId).FirstOrDefault();
                    var supplierCatName = supplierCatObj.SupplierCategoryName;

                    if (supplierCatName != null)
                    {
                        catNameList.Add(supplierCatName);
                    }
                }
                var returnData = new List<string>();
                if (NumItems > 0 && PageNum > 0)
                { returnData = catNameList.Skip(NumItems * PageNum).Take(NumItems).ToList(); }
                else returnData = catNameList;

                return Ok(returnData);
            }
        }
    }
}
