using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using File = System.IO.File;

namespace WebApplication1.Pages
{
    public class EvalModel : PageModel
    {
        private static int testNumber = 9;
        private static Dictionary<string, decimal> _priceDictionary = new Dictionary<string, decimal>();
        private static string _imagesPath = @"C:\Users\ariciogull\Desktop\unic\";
        private static Random _random = new Random();
        private static string _indexPath = _imagesPath + "Index" + testNumber + ".txt";
        private static string _testPath = _imagesPath + "Test" + testNumber + ".txt";
        private static string _resultPath = _imagesPath + "Results" + testNumber + "\\";
        private static string _evalPath = _imagesPath + "Evaluations\\Evaluation" + testNumber + ".csv";


        
        public Dictionary<Item, List<Item>> TestResults = new Dictionary<Item, List<Item>>();

        public class Item {
            public string ModelNo { get; set; }
            public string Price { get; set; }
            public string Score { get; set;  }
        }

        private void Init()
        {
            ViewData["Title"] = "Test Results " + testNumber;
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

        public void OnGet()
        {
            Init();
            string[] resultFiles = Directory.GetFiles(_resultPath, "*.txt", SearchOption.AllDirectories);
            foreach (var file in resultFiles)
            {
                var modelNo = Path.GetFileName(file).Replace(".txt", "");
                var jsonResult = JObject.Parse(System.IO.File.ReadAllText(file));
                var exactPrice = _priceDictionary[modelNo];
                var hits = jsonResult["hits"].Take(5);
                var item = new Item { ModelNo = modelNo, Price = exactPrice.ToString() };

                var hitList = new List<Item>();
            
                
                
                foreach (var hit in hits)
                {
                    var id = (string)hit["input"]["id"];
                    var score = Math.Round((double)hit["score"], 2).ToString();
                    var priceOfHit = _priceDictionary[id];
                    hitList.Add(new Item { ModelNo = id, Price = priceOfHit.ToString(), Score = score });
                    //Console.Write($"    S:[{Math.Round((double)hit["score"], 2)}]  P:[{priceOfHit}]");
                }

                TestResults.Add(item, hitList);
                
                
            }

        }
    }
    
}
