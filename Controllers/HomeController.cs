using CsvHelper;
using CsvToSqlConverter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static CsvToSqlConverter.Function.CSVReader;

namespace CsvToSqlConverter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHostEnvironment Environment;

        public HomeController(ILogger<HomeController> logger, IHostEnvironment _environment)
        {
            _logger = logger;
            Environment = _environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(IFormFile formFile)
        {
            try
            {
                //Throw exception if the file are not found
                if (formFile is null)
                    throw new NullReferenceException("No CSV file are import");

                //throw exception if is not CSV format
                if (!formFile.FileName.Contains(".csv", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException(@"This file '" + formFile.FileName + "' is not CSV format");

                //Create a uploads folder
                string path = Path.Combine(Environment.ContentRootPath, "Uploads");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                //get the uploaded CSV file path
                string fileName = Path.GetFileName(formFile.FileName);
                string filePath = Path.Combine(path, DateTime.Now.Ticks + fileName);
                using (FileStream stream = new(filePath, FileMode.Create))
                    formFile.CopyTo(stream);

                //Create a temp table name with the CSV file name
                string tempTableName = "#" + await RemoveSpecialCharacters(fileName.Trim().Replace(".", "").Replace(" ", string.Empty));

                //Read CSV path and get the Csv header row
                string[] csvHeader = await GetHeader(filePath);
                string queryHeader = await GetSqlHeader(csvHeader, tempTableName);

                //Read CSV path and get the csv data row
                List<object> sqlRow = await GetsqlRecord(filePath);
                string insertIntoTable = await GetDataRecord(sqlRow, tempTableName);

                //Full SQL query
                string fullQuery = queryHeader + "\n" + insertIntoTable + "\n"; //+ queryname.Query;

                //ViewBag.queryList = queryList;
                ViewBag.data = fullQuery;
                TempData["complete"] = "Import successful";
            }
            catch (ArgumentException ex)
            {
                TempData["error"] = ex.Message;
            }
            catch (CsvHelperException ex)
            {
                TempData["error"] = ex.InnerException.Message;
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return await Task.FromResult(View());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}