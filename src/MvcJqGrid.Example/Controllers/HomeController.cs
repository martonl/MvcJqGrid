using MvcJqGrid.Example.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Linq.Dynamic;
using System.Web;
using System.Web.Mvc;

namespace MvcJqGrid.Example.Controllers
{
    [HandleError]
    public class HomeController : Controller
    {
        private readonly Repository _repo;

        public HomeController()
        {
            _repo = new Repository();
        }

        public ActionResult Basic()
        {
            return View();
        }

        public ActionResult Search()
        {
            ViewData["CompanyNames"] = _repo.GetCompanyNames();
            return View();
        }

        public ActionResult DefaultSearchValue()
        {
            ViewData["CompanyNames"] = _repo.GetCompanyNames();
            return View();
        }

        public ActionResult Toolbar()
        {
            return View();
        }

        public ActionResult Multiselect()
        {
            return View();
        }

        public ActionResult Formatters()
        {
            return View();
        }

        public ActionResult Events()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        /// <summary>
        /// AJAX request, retrieves data for basic grid
        /// </summary>
        /// <param name="gridSettings">Settings received from jqGrid request</param>
        /// <returns>JSON view containing data for basic grid</returns>
        public ActionResult GridDataBasic(GridSettings gridSettings)
        {
            var customers = _repo.GetCustomers(gridSettings);
            var totalCustomers = _repo.CountCustomers(gridSettings);

            var jsonData = new
            {
                total = totalCustomers / gridSettings.PageSize + 1,
                page = gridSettings.PageIndex,
                records = totalCustomers,
                rows = (
                    from c in customers
                    select new
                    {
                        id = c.CustomerID,
                        cell = new[] 
                    { 
                        c.CustomerID.ToString(), 
                        string.Format("{0} {1}", c.FirstName, c.LastName),
                        c.CompanyName,
                        c.EmailAddress,
                        c.ModifiedDate.ToShortDateString(),
                        c.Phone
                    }
                    }).ToArray()
            };

            return Json(jsonData, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// AJAX request, retrieves data for basic grid
        /// </summary>
        /// <param name="gridSettings">Settings received from jqGrid request</param>
        /// <returns>JSON view containing data for basic grid</returns>
        public ActionResult GridDataBasicGeneric(GridSettings gridSettings)
        {
            var customers = _repo.GetCustomersGeneric();
            return new GridResult<MvcJqGrid.Example.Models.Repository.CustomerViewModel>(customers, gridSettings);
        }

        public ActionResult TreeGrid()
        {
            return View();
        }


        public ActionResult TreeGridData()
        {
            Response.ContentType = "text/xml";
            return View();
        }

        public class GridResult<T> : ActionResult
        {
            public IQueryable<T> Query;
            public object Model;
            public GridSettings Settings;
            public JsonSerializerSettings JsonSettings;
            public Formatting Formatting { get; set; }

            public GridResult(IQueryable<T> _query, GridSettings _settings, JsonSerializerSettings _jsonSettings = null)
            {
                Query = _query;
                Settings = _settings;
                if (_jsonSettings == null)
                {
                    JsonSettings = new JsonSerializerSettings();
                }
                else
                {
                    JsonSettings = _jsonSettings;
                }
            }

            private int SearchDateParse(string[] DatePart, int PartIndex)
            {
                int PartResult;
                try
                {
                    Int32.TryParse(DatePart[PartIndex], out PartResult);
                    if (PartResult == 0) PartResult = 1;
                }
                catch (Exception)
                {
                    PartResult = 1;
                }
                return PartResult;
            }

            public override void ExecuteResult(ControllerContext context)
            {
                if (Settings.Where != null)
                {
                    foreach (var rule in Settings.Where.rules)
                    {
                        System.Reflection.PropertyInfo p = typeof(T).GetProperty(rule.field);
                        string pname;
                        Type columnType = p.PropertyType;
                        if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            // If it is NULLABLE, then get the underlying type. eg if "Nullable<int>" then this will return just "int"
                            columnType = p.PropertyType.GetGenericArguments()[0];
                            pname = columnType.Name;
                        }
                        else
                        {
                            pname = p.PropertyType.Name;
                        }
                        switch (pname)
                        {
                            case "String":
                                Query = Query.Where(rule.field + ".ToLower().Contains(@0)", rule.data.ToLower());
                                break;
                            case "Int32":
                                Query = Query.Where(rule.field + "== @0", Convert.ToInt32(rule.data));
                                break;
                            case "DateTime":
                                string[] datum = rule.data.Split('.');
                                int honap = SearchDateParse(datum, 1);
                                int nap = SearchDateParse(datum, 2);
                                DateTime teszt = new DateTime(Convert.ToInt32(datum[0]), honap, nap);
                                Query = Query.Where(rule.field + " >= @0", teszt);
                                break;
                        }
                    }
                }
                if (Settings.SortColumn != "")
                {
                    Query = Query.OrderBy(Settings.SortColumn + " " + Settings.SortOrder.ToUpper()); 
                }
                int records = Query.Count();
                int total = (int)Math.Ceiling((float)records / (float)Settings.PageSize);
                if (Settings.PageIndex > 1)
                {
                    Query = Query.Skip((Settings.PageIndex - 1) * Settings.PageSize).Take(Settings.PageSize);
                }
                else
                {
                    Query = Query.Take(Settings.PageSize);
                }
                var results = new
                {
                    total = total, //number of pages
                    page = Settings.PageIndex, //current page
                    records = records, //total item number
                    rows = Query //real datas
                };

                HttpResponseBase response = context.HttpContext.Response;
                response.ContentType = "application/json";
                JsonTextWriter writer = new JsonTextWriter(response.Output) { Formatting = this.Formatting };
                JsonSerializer serializer = JsonSerializer.Create(JsonSettings);
                serializer.Serialize(writer, results);
                writer.Flush();
            }
        }
    }
}
