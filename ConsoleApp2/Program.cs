using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Clarifai.API;
using Clarifai.DTOs.Inputs;
using Clarifai.DTOs.Searches;
using Newtonsoft.Json.Linq;

namespace Index
{
    class Program
    {
        private static int testNumber = 10;
        private static Dictionary<string, decimal> _priceDictionary = new Dictionary<string,decimal>();
        private static string _imagesPath = @"C:\Users\ariciogull\Desktop\unic\";
        private static Dictionary<string, ClarifaiFileImage> _imageDictionary = new Dictionary<string, ClarifaiFileImage>();
        private static Dictionary<string, ClarifaiFileImage> _indexedImages = new Dictionary<string, ClarifaiFileImage>();
        private static Dictionary<string, ClarifaiFileImage> _testImages = new Dictionary<string, ClarifaiFileImage>();
        private static Random _random = new Random();
        private static string _indexPath = _imagesPath + "Index" + testNumber +".txt";
        private static string _testPath = _imagesPath + "Test" + testNumber +".txt";
        private static string _resultPath = _imagesPath + "Results" + testNumber + "\\";
        private static string _evalPath = _imagesPath + "Evaluations\\Evaluation" + testNumber + ".csv";
        private static ClarifaiClient _client = new ClarifaiClient("b8ea314c343744588b1151e598552a51");

        //static void Main(string[] args)
        //{
        //    Console.WriteLine("Hello World!");
        //    var client = new ClarifaiClient("b8ea314c343744588b1151e598552a51");
        //}

        //public static async Task Main()
        public static void Main(string[] args)
        {
           // Clear();
            Init();
            //GenerateIndexAndTestSet();
            //Index();

            Test();      
            Evaluate();

            Console.WriteLine("DONE!");
            Console.ReadLine();
        }

        public static void Init()
        {
            
            Console.WriteLine("Reading Price list");
            // read prices
            var csv = File.ReadLines(_imagesPath + "All.csv").Select(line => line.Split(',')).ToDictionary(line => line[0], line => line[1]);
            
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

            // load images
            foreach (var keyandvalue in csv)
            {
                try
                {
                    _imageDictionary.Add(keyandvalue.Key, new ClarifaiFileImage(File.ReadAllBytes(_imagesPath + keyandvalue.Key + ".jpg"), keyandvalue.Key));
                }
                catch
                {

                }
            }
        }

        public static void Index()
        {
            _indexedImages = _imageDictionary.Where(x => File.ReadAllLines(_indexPath).Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            Console.WriteLine($"--Indexing Started {DateTime.Now}");
            // clear existing indexing.
            

            var task = _client.AddInputs(_indexedImages.Values.Take(128)).ExecuteAsync();
                      
            task.Wait();
            Console.WriteLine($"--Indexing done for first 128 image, {DateTime.Now}, {task.Result.Status}");
            task = _client.AddInputs(_indexedImages.Values.TakeLast(_indexedImages.Values.Count - 128)).ExecuteAsync();
            task.Wait();
            Console.WriteLine($"--Indexing Finished {DateTime.Now}, {task.Result.Status}");
        }

        

        public static void Clear()
        {
            _priceDictionary.Clear();
            _imageDictionary.Clear();
            _indexedImages.Clear();
            _testImages.Clear();

            // Clear Index 
            var task = _client.DeleteAllInputs().ExecuteAsync();
            task.Wait();
            Console.WriteLine(task.Result);
        }

        public static void Test()
        {
            Console.WriteLine("Testing Started");
            _testImages = _imageDictionary.Where(x => File.ReadAllLines(_testPath).Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            Console.WriteLine($"--Test" + $" Started {DateTime.Now}");
            foreach(var testImage in _testImages)
            {
                var task = _client.SearchInputs(SearchBy.ImageVisually(testImage.Value)).PerPage(5)
                    .Page(1)
                    .ExecuteAsync();

                task.Wait();
                var jsonResult = JObject.Parse(task.Result.RawBody);
                File.WriteAllText(_resultPath + testImage.Key + ".txt",task.Result.RawBody);
            }

            Console.WriteLine($"--Testing Finished {DateTime.Now}");

        }

        public static void GenerateIndexAndTestSet()
        {
            // get random 90%
            _indexedImages = _imageDictionary.OrderBy(x => _random.Next()).Take((int)Math.Round(_imageDictionary.Count() * 0.90)).ToDictionary(x => x.Key, x => x.Value);
            _testImages = _imageDictionary.Where(x => !_indexedImages.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            File.WriteAllLines(_indexPath, _indexedImages.Keys.ToArray());
            File.WriteAllLines(_testPath, _testImages.Keys.ToArray());
        }

        public static void Evaluate()
        {
            using (System.IO.StreamWriter evalFile =
            new System.IO.StreamWriter(_evalPath))
            {

                evalFile.WriteLine($"Product Code,Original Price,Minimum Price,Avarage Price,Maximum Price,Fit");
                string[] resultFiles = Directory.GetFiles(_resultPath, "*.txt", SearchOption.AllDirectories);
            foreach(var file in resultFiles)
            {
                var modelNo = Path.GetFileName(file).Replace(".txt", "");
                var jsonResult = JObject.Parse(File.ReadAllText(file));
                var exactPrice = _priceDictionary[modelNo];
                var hits = jsonResult["hits"].Take(5);
                //Console.Write($"--Item Price:[{exactPrice}]");
                var similarPrices = new List<decimal>();
                similarPrices.Clear();
                foreach (var hit in hits)
                {
                    var priceOfHit = _priceDictionary[(string)hit["input"]["id"]];
                    similarPrices.Add(priceOfHit);
                    //Console.Write($"    S:[{Math.Round((double)hit["score"], 2)}]  P:[{priceOfHit}]");
                }

                    var correctOrNot = similarPrices.Min() <= exactPrice && exactPrice <= similarPrices.Max() ? true : false;
                Console.WriteLine($"Exact Price:{exactPrice} - Predictions min:{similarPrices.Min()} avg:{similarPrices.Average()} max:{similarPrices.Max()} ");
                    evalFile.WriteLine($"{modelNo},{exactPrice},{similarPrices.Min()},{similarPrices.Average()},{similarPrices.Max()},{correctOrNot}");

                }

            }
        }
    }
}
