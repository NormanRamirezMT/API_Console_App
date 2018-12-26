using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Configuration;
namespace HttpClientSample
{
    class Program
    {
    
        //Class that mirrors response when getting a token
        class tokenDetails
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }

        }

        //Results and Workflows are classes to helo extract the desired templateID from the GET response. 
        class Results
        {
            public Workflows[] Items { get; set; }
            public string NextPageLink { get; set; }
            public int? Count { get; set; }
        }
        class Workflows
        {
            public string ID { get; set; }
            public string WorkflowName { get; set; }
            public string InitialStage { get; set; }
            public string DateCreated { get; set; }
        }

        //Class that mirrors the format of csv file containing workflows to be initiated. 
        class WorkflowData
        {
            public string TextField { get; set; }
            public string DropDown { get; set; }
            public string Date { get; set; }
            public string FilePath { get; set; }

        }

        //Obtain the bearer token
        static async Task<string> CreateProductAsync(IDictionary<string,string> product)
        {
            HttpResponseMessage response = await client.PostAsync(
                "auth/identity/connect/token", new FormUrlEncodedContent(product));
            response.EnsureSuccessStatusCode(); //If status code is not a success, prints out what it was/why

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        //Get form by template ID
        static async Task<string> CreateProductAsync(MultipartFormDataContent product, string templateID)
        {
            HttpResponseMessage response = await client.PostAsync(
                $"api/v1/workflows/{templateID}/form", product);
            response.EnsureSuccessStatusCode();

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        //Filter and find the testing workflow to be used.
        static async Task<string> GetProductAsync()
        {
            HttpResponseMessage response = await client.GetAsync(
                "api/v1/templates/dashboard?$filter=WorkflowName eq 'NR_API_Test'"); // eq: name of workflow to be intitated, must be exact match
            response.EnsureSuccessStatusCode();

            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }

        //Create HttpClient
        static HttpClient client = new HttpClient();

        static void Main()
        {
            RunAsync().GetAwaiter().GetResult();
        }

        static async Task RunAsync()
        {
            //update base address to relevant tenant (dev-tap/automation)
            client.BaseAddress = new Uri("https://default.dev-tap.thinksmart.com/automation/"); 
            try
            {
                //Create a dictionary that holds information necessary to retrieve a token.
                Dictionary<string, string> body = new Dictionary<string, string>()
                {
                {"grant_type","password"},
                {"scope", "api"},
                {"redirct_uri","testuri"},
                {"username", "Norman.Ramirez@mitratech.com"},                       //your email for tenant 
                {"password", "FCBayern96!"},                                        //your password for tentant
                {"client_id", "b6d2d905285746cd973146dd4fdd3232"},                  
                { "client_secret", "bC87n6ic+3kK8Pegd/Pd4vSIHC1NC8tHNkJwFSBMOZA="}
                };

                //staging client id: ae0e8243936644cda1e2f21a71ba2b91
                //staging client secret: isb2vzPHOYk6hd2h0C2XUZ1ePxVyQFVcpAmk0X1IdLM=

                //Send POST request with required info to receive a response that contains a token. 
                var response = await CreateProductAsync(body);
                Console.WriteLine($"Response1: {response}");
                tokenDetails token = JsonConvert.DeserializeObject<tokenDetails>(response); //Deserialize response into a defined tokenDetails objects
                Console.WriteLine(token.access_token);

                //add newly retrieved token to HttpClient headers as a bearer token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.access_token);

                //Use GET request to retrieve the information (TemplateID) for the desired template
                var response2 = await GetProductAsync();
                Console.WriteLine($"Response2: {response2}");
                Results workflow = JsonConvert.DeserializeObject<Results>(response2);
                string templateID = workflow.Items.FirstOrDefault().ID;
                Console.WriteLine($"ID:{templateID} ");

                //Open and read the records in the csv fille containing the information for workflows to be initiated.
                string csvFile = ConfigurationManager.AppSettings["csvPath"];
                var records = new CsvReader(new StreamReader(System.IO.File.OpenRead(csvFile))).GetRecords<WorkflowData>();
                    foreach (var record in records)
                    {
                        using (MultipartFormDataContent formdata = new MultipartFormDataContent())
                        {
                            Console.WriteLine(record.TextField);
                            formdata.Add(new StringContent(record.TextField), "element1");
                            Console.WriteLine(record.DropDown);
                            formdata.Add(new StringContent(record.DropDown), "element3");
                            Console.WriteLine(record.Date);
                            formdata.Add(new StringContent(record.Date), "element4");
                            Console.WriteLine(record.FilePath);
                            StreamContent streamContent = new StreamContent(System.IO.File.OpenRead(ConfigurationManager.AppSettings["filePath"]));
                            formdata.Add(streamContent, "element5", ConfigurationManager.AppSettings["filePath"]);
                            var response4 = await CreateProductAsync(formdata, templateID);
                            Console.WriteLine(response4);
                        }
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadLine();
        }
    }
}