﻿using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Cosmos.DataTransfer.Interfaces;
using Cosmos.DataTransfer.MongoExtension.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Cosmos.DataTransfer.MongoExtension;
[Export(typeof(IDataSourceExtension))]
internal class MongoDataSourceExtension : IDataSourceExtensionWithSettings
{
    public string DisplayName => "MongoDB";

    public async IAsyncEnumerable<IDataItem> ReadAsync(IConfiguration config, ILogger logger, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = config.Get<MongoSourceSettings>();
        settings.Validate();

        if (!string.IsNullOrEmpty(settings.ConnectionString) && !string.IsNullOrEmpty(settings.DatabaseName))
        {
            var context = new Context(settings.ConnectionString, settings.DatabaseName);

            var collectionNames = !string.IsNullOrEmpty(settings.Collection)
                ? new List<string> { settings.Collection }
                : await context.GetCollectionNamesAsync();

            foreach (var collection in collectionNames)
            {
                await foreach (var item in EnumerateCollectionAsync(context, collection))
                {
                    yield return item;
                }
            }
        }
    }

    public async IAsyncEnumerable<IDataItem> EnumerateCollectionAsync(Context context, string collectionName)
    {
        var collection = context.GetRepository<BsonDocument>(collectionName);
        foreach (var record in collection.AsQueryable())
        {
            yield return new MongoDataItem(record);
        }
    }

    public IEnumerable<IDataExtensionSettings> GetSettings()
    {
        yield return new MongoSourceSettings();
    }
}
