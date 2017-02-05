using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AzureBootCamp.DataPump
{
    public class Tag
    {
        public string Module { get; set; }
        public string Mill { get; set; }
        public string TagName { get; set; }
        public double Value { get; set; }
        public DateTime TimeStamp { get; set; }

        public Tag()
        {

        }
    }

    public class FlowControl : Tag
    {
        public FlowControl(string mill, string module, int num, double val)
        {
            this.Mill = mill;
            this.Module = module;
            this.TagName = $"FC{num}:con";
            this.Value = val;
            this.TimeStamp = DateTime.Now;
        }
    }

    public static class RandomExtensions
    {
        public static double NextDouble(
            this Random random,
            double minValue,
            double maxValue)
        {
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }
    }

    public class ValmetDataPump
    {
        string hubname = "valmetioteventhub";
        string connstring = "secret";
        private object locker = new object();
        public List<FlowControl> Data { get; set; }
        
        public async Task Run()
        {

            var drifter = -1;
            var drifters = 1;
            var driftdelay = 20;
            var driftduration = 60;
            var rnd = new Random();
            Data = new List<FlowControl>(8);

            //setup startvalues
            for (var i = 236; i <= 243; i++)
            {
                Data.Add(new FlowControl("Stendal", "TwinRoll", i, rnd.NextDouble(49, 55)));
            }

            //fake a drifter... 1 sensor between 1-8 will have drifting values during a "driftduration"
            var _d = Task.Run(async () =>
            {
                // keep track of drifters...
                var now = DateTime.Now;
                
                while (true)
                {
                    Console.WriteLine("In loop");
                    if (drifter == -1)
                    {
                        //wait
                        await Task.Delay(driftdelay * 1000);
                    }
                    //assign new drifter 
                    drifter = rnd.Next(0, 7);
                    Console.WriteLine("New drifter is " + drifter);
                    //wait driftduration
                    await Task.Delay(driftduration * 1000);
                    Console.WriteLine("No drifter");
                    drifter = -1;
                }
            });

            //update data with new values... also set drifter value if there is one...
            var _1 = Task.Run(async () =>
            {
                while (true)
                {
                    lock (locker)
                    {
                        var now = DateTime.Now;
                        for (var i = 0; i <= 7; i++)
                        {

                            Data[i].TimeStamp = now;
                            if(i == drifter)
                            {
                                Console.WriteLine("Drifter is " + i);
                                Data[i].Value = rnd.NextDouble(59, 61);
                                Console.WriteLine("No drifter with value " + Data[i].Value);
                            }
                            else
                            {
                                Data[i].Value = rnd.NextDouble(49, 51);
                            }

                        }
                    }
                    await Task.Delay(5000);
                }
            });

            // Send batched data every x seconds...
            var _2 = Task.Run(async () =>
            {
                var eventhubclient = Microsoft.ServiceBus.Messaging.EventHubClient.CreateFromConnectionString(connstring,hubname);
                
                while (true)
                {                    
                    await Task.Delay(5000);
                    lock (locker)
                    {
                        if (Data.Count == 0) continue;
                        // send all data to blob storage
                        Console.WriteLine("Valmet DataPump DATA SENDING");
                        
                        // send notification to storage queue or service bus                        
                        var json = JsonConvert.SerializeObject(Data);
                        eventhubclient.Send(new Microsoft.ServiceBus.Messaging.EventData(Encoding.UTF8.GetBytes(json)));
                        
                        Console.WriteLine("Valmet DataPump DATA SENT");                        
                    }
                }
            });
        }
    }
}
