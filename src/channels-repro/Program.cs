using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;

namespace channels_repro
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var channel = Channel.Create<int>();

            Console.WriteLine("Press Ctrl-C to stop generating values");
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                cts.Cancel();
            };

            // Start a reading thread and a writing thread
            var reader = ReadItems(channel);
            var writer = WriteItems(channel, cts.Token);

            Task.WaitAll(reader, writer);
        }

        private static async Task WriteItems(IWritableChannel<int> channel, CancellationToken cancellationToken)
        {
            // Start generating items
            int counter = 0;
            while (!cancellationToken.IsCancellationRequested && counter < 5)
            {
                try
                {
                    var value = counter++;
                    Console.WriteLine($"Sending: {value}");
                    await channel.WriteAsync(value);
                    Console.WriteLine("Waiting 0.5sec");
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sender Threw: {ex}");
                    return;
                }
            }

            Console.WriteLine("Completing channel");
            channel.Complete();
        }

        private static async Task ReadItems(IReadableChannel<int> readEnd)
        {
            while (!readEnd.Completion.IsCompleted)
            {
                try
                {
                    var item = await readEnd.ReadAsync();
                    Console.WriteLine($"Received: {item}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Receiver Threw: {ex}");
                    return;
                }
            }
            Console.WriteLine("Channel completed");
        }
    }
}
