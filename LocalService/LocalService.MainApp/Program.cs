using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LocalService.IncludeLibs.Helpers;

namespace LocalService.MainApp
{
    internal class Program
    {
        private static Dictionary<string, StartUpInstance> instances = null;

        private static Dictionary<string, StartUpInstance> Instances { get; set; }

        private static void Main(string[] args)
        {

            if (args.Length < 2)
            {
                return;
            }

            Console.WriteLine(string.Join(",", args));

            try
            {
                var arg0 = args[0]; // apId
                var arg1 = args[1]; // instanceId

                using (var mutex = new Mutex(false, arg0))
                {
                    var isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
                    if (isAnotherInstanceOpen)
                    {
                        //Console.WriteLine("one instance, ");
                        if (args.Length >= 2)
                        {
                            if (!Instances.ContainsKey(arg1))
                            {
                                Instances.Add(arg1, new StartUpInstance(arg1));
                                Instances[arg1].Run();
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        Instances = instances ?? new Dictionary<string, StartUpInstance>();
                        Instances.Add(arg1, new StartUpInstance(arg1));
                        Instances[arg1].Run();

                        Console.ReadLine();
                        mutex.ReleaseMutex();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

    }

    public class StartUpInstance
    {
        private readonly Thread thread = null;

        public string InstanceId { get; }

        public StartUpInstance(string instanceId)
        {
            InstanceId = instanceId;
            var startTime = DateTime.Now;
            var isSetBreak = false;
            AutoResetEvent mainResetEvent = new AutoResetEvent(false);

            var ds = new DataSimulator(instanceId);

            while (!isSetBreak)
            {
                mainResetEvent.WaitOne(100);

                if (DateTime.Now.Subtract(startTime).TotalSeconds > 60)
                    isSetBreak = true;
            }

            ds.Dispose();
            mainResetEvent.Dispose();
        }

        public void Run()
        {
            thread.Start();
        }
    }

    public class DataSimulator : IDisposable
    {
        private readonly string fileId = string.Empty;
        private bool disposedValue;
        private readonly int sleep = 0;
        private bool breakSignTriggered = false;
        private AutoResetEvent mainResetEvent = null;
        private AutoResetEvent writeDataResetEvent = null;

        private readonly ConcurrentQueue<string> somethingQueue = null;

        private readonly Thread mainThread = null;
        private readonly Thread dataWriteThread = null;

        public DataSimulator(string fileId, int sleep = 1000)
        {
            this.fileId = fileId;
            this.sleep = sleep;
            somethingQueue = new ConcurrentQueue<string>();
            mainResetEvent = new AutoResetEvent(false);
            writeDataResetEvent = new AutoResetEvent(false);

            mainThread = new Thread(Start)
            {
                IsBackground = true,
            };

            dataWriteThread = new Thread(writeDataQueueProcess)
            {
                IsBackground = true
            };

            dataWriteThread.Start();
            mainThread.Start();
        }

        private void Start()
        {
            int seed = 1;
            while (true && !breakSignTriggered)
            {
                var data = GenSomethingString(seed);
                data = data.InsertSymbol(2);
                WriteData(data);

                mainResetEvent.WaitOne(sleep);
                seed = ++seed % sleep;
            }
        }

        protected void WriteData(string something)
        {
            somethingQueue.Enqueue(something);
            writeDataResetEvent?.Set();
        }

        private string GenSomethingString(int seed)
        {
            return new Random(seed).Next(int.MaxValue).ToString().PadLeft(30, '0');
        }

        private void writeDataQueueProcess()
        {
            while (true && !breakSignTriggered)
            {
                if (!somethingQueue.IsEmpty)
                {
                    if (somethingQueue.TryDequeue(out string result))
                    {
                        if (string.IsNullOrWhiteSpace(result))
                            continue;

                        Console.WriteLine($"{fileId}:{result}");
                        //File.AppendAllText($"./{nameof(MainApp)}.log", $"{result}{Environment.NewLine}");
                    }
                }
                else
                {
                    writeDataResetEvent.WaitOne();
                }

                writeDataResetEvent.WaitOne(100);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    breakSignTriggered = true;
                    writeDataResetEvent?.Dispose();
                    mainResetEvent?.Dispose();
                }

                writeDataResetEvent = null;
                mainResetEvent = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}