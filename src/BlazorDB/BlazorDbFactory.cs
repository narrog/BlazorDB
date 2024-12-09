using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Threading;

namespace BlazorDB
{
    public class BlazorDbFactory : IBlazorDbFactory
    {
        readonly IJSRuntime _jsRuntime;
        readonly IServiceProvider _serviceProvider;
        readonly IDictionary<string, IndexedDbManager> _dbs = new Dictionary<string, IndexedDbManager>();
        readonly SemaphoreSlim _semaphore = new(1, 1);

        public BlazorDbFactory(IServiceProvider serviceProvider, IJSRuntime jSRuntime)
        {
            _serviceProvider = serviceProvider;
            _jsRuntime = jSRuntime;
        }

        public async Task<IndexedDbManager> GetDbManager(string dbName)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_dbs.Any())
                    await BuildFromServices();
            }
            finally
            {
                _semaphore.Release();
            }
            if(_dbs.ContainsKey(dbName))
                return _dbs[dbName];
            
            return null;
        }

        public Task<IndexedDbManager> GetDbManager(DbStore dbStore)
            => GetDbManager(dbStore.Name);

        async Task BuildFromServices()
        {
            var dbStores = _serviceProvider.GetServices<DbStore>();
            if(dbStores != null)
            {
                foreach(var dbStore in dbStores)
                {
                    Console.WriteLine($"{dbStore.Name}{dbStore.Version}{dbStore.StoreSchemas.Count}");
                    var db = new IndexedDbManager(dbStore, _jsRuntime);
                    await db.OpenDb();
                    _dbs.Add(dbStore.Name, db);
                }
            }
        }
    }
}