using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ChangeTracker.Client.Models;
using Newtonsoft.Json;

namespace ChangeTracker.Client
{
    public class ChangeTrackerService
    {
        private readonly ConcurrentBag<ChangeTrackerStoreItem> _items = new ConcurrentBag<ChangeTrackerStoreItem>();
        private readonly object _lockObject = new object();
        private bool _isInSend;
        
        public ChangeTrackerService(string changeTrackerAccountName, string apiSecretGet, string apiSecretPost, int tokenMinuteDuration)
        {
            var changeTrackerGateway = new ChangeTrackerGateway(
                changeTrackerAccountName,
                apiSecretGet, apiSecretPost, tokenMinuteDuration
            );

            const int delay = 1000;
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    while (!_items.IsEmpty)
                    {
                        var item = new ChangeTrackerStoreItem();
                        var notEmptyList = _items.Count > 0;
                        if (notEmptyList)
                        {
                            lock (_lockObject)
                            {
                                if (!_isInSend)
                                {
                                    _isInSend = true;

                                    _items.TryTake(out item);
                                }
                            }

                            try
                            {
                                // QUI dà errore perché nella libreria ChangeTracker c'è una libreria per la serializzazione json piu vecchia di quella che c'è qui
                                // newtonsof json
                                await changeTrackerGateway.StoreChangesAsync(
                                    item.TableName,
                                    item.UserName,
                                    item.RowDescription,
                                    item.Prev,
                                    item.Next,
                                    item.IpAddress);

                                try
                                {
                                    var json = JsonConvert.SerializeObject(item);
                                    Debug.WriteLine(json);
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine(e.Message);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.Message);
                            }

                            _isInSend = false;

                        }
                    }

                    Thread.Sleep(delay);
                    if (token.IsCancellationRequested)
                        break;
                }
            }, token, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public bool IsEmpty()
        {
            return _items.Count <= 0 && !_isInSend;
        }

        // ipAddress deve essere reperito dentro il Service e non può stare dento il sistema che fa lo storing perché altrimeti ritorna null
        public void Store(string tableName, string userName, string rowDescription, Row prev, Row next, string ipAddress = "")
        {
            var item = new ChangeTrackerStoreItem
            {
                TableName = tableName,
                UserName = userName,
                RowDescription = rowDescription,
                Prev = prev,
                Next = next,
                IpAddress = ipAddress
            };

            _items.Add(item);
        }

        private class ChangeTrackerStoreItem
        {
            public string TableName { get; set; }
            public string UserName { get; set; }
            public string RowDescription { get; set; }
            public Row Prev { get; set; }
            public Row Next { get; set; }
            public string IpAddress { get; set; }
        }
    }

    public static class PeriodicTask
    {
        public static async Task Run(
            Func<Task> action,
            TimeSpan period,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            while (!cancellationToken.IsCancellationRequested)
            {

                Stopwatch stopwatch = Stopwatch.StartNew();

                if (!cancellationToken.IsCancellationRequested)
                    await action();

                stopwatch.Stop();

                await Task.Delay(period - stopwatch.Elapsed, cancellationToken);
            }
        }
    }

}
