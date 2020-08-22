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
        private static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            var isSetBreak = false;
            AutoResetEvent mainResetEvent = new AutoResetEvent(false);

            var ds = new DataSimulator();

            while (!isSetBreak)
            {

                mainResetEvent.WaitOne(100);

                if (DateTime.Now.Subtract(startTime).TotalSeconds > 10)
                    isSetBreak = true;
            }

            ds.Dispose();
            mainResetEvent.Dispose();
        }
    }

    public class DataSimulator : IDisposable
    {
        private bool disposedValue;
        private readonly int sleep = 0;
        private bool breakSignTriggered = false;
        private readonly AutoResetEvent mainResetEvent = null;
        private readonly AutoResetEvent writeDataResetEvent = null;

        private readonly ConcurrentQueue<string> somethingQueue = null;

        private readonly Thread mainThread = null;
        private readonly Thread dataWriteThread = null;

        public DataSimulator(int sleep = 1000)
        {
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
                seed = ++seed % 1000;
            }
        }

        protected void WriteData(string something)
        {
            somethingQueue.Enqueue(something);
            mainResetEvent.Set();
            writeDataResetEvent.Set();
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

                        File.AppendAllText($"./{nameof(MainApp)}.log", $"{result}{Environment.NewLine}");
                    }
                }
                else
                {
                    writeDataResetEvent.WaitOne();
                }

                writeDataResetEvent.WaitOne(1);
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