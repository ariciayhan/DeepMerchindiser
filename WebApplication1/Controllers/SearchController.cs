using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clarifai.API;
using Clarifai.DTOs.Inputs;
using Clarifai.DTOs.Searches;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Dynamic;


// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApplication1.Controllers
{
    public class SearchController : Controller
    {
        private static string _imagesPath = @"C:\Users\ariciogull\Desktop\unic\";
        private static Dictionary<string, decimal> _priceDictionary = new Dictionary<string, decimal>();
        private static ClarifaiClient _client = new ClarifaiClient("b8ea314c343744588b1151e598552a51");
        // GET: /<controller>/
        public ActionResult Index(string productcode)
        {
            init();
            var task = _client.SearchInputs(SearchBy.ImageVisually(new ClarifaiFileImage(System.IO.File.ReadAllBytes(_imagesPath + productcode + ".jpg"))))
                    .PerPage(5)
                   .Page(1)
                   .ExecuteAsync();

            task.Wait();
            var jsonResult = JObject.Parse(task.Result.RawBody);
            
            
            var hits = jsonResult["hits"].Take(6);
            //Console.Write($"--Item Price:[{exactPrice}]");

            dynamic data = new ExpandoObject();
            var products = new List<System.Dynamic.ExpandoObject>();
            var prices = new List<decimal>();

            foreach (var hit in hits)
            {
                dynamic result = new System.Dynamic.ExpandoObject();
                if (productcode != (string)hit["input"]["id"])
                {
                    result.productcode = (string)hit["input"]["id"];                   
                    result.score = Math.Truncate(decimal.Parse((string)hit["score"]) * 100); ;
                                        result.price =  _priceDictionary[result.productcode];
                    prices.Add(_priceDictionary[result.productcode]);
                    products.Add(result);
                }                
            }
            data.products = products;
            data.originalPrice = _priceDictionary[productcode]; ;
            data.min = prices.Min();
            data.max = prices.Max();
            data.avg = prices.Average();
            Response.StatusCode = 200;
            return Json(data);
        }

        private void init()
        {
            var csv = System.IO.File.ReadLines(_imagesPath + "All.csv").Select(line => line.Split(',')).ToDictionary(line => line[0], line => line[1]);

            foreach (var keyandvalue in csv)
            {
                try
                {
                    _priceDictionary.Add(keyandvalue.Key, Convert.ToDecimal(keyandvalue.Value));
                    //Console.WriteLine($"{keyandvalue.Key} {keyandvalue.Value}");
                }
                catch
                {

                }

            }
        }
    }

}
