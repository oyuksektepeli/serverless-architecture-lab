﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs.Host;
using TollBooth.Models;

namespace TollBooth
{
    internal class DatabaseMethods
    {
        private readonly string _endpointUrl = ConfigurationManager.AppSettings["cosmosDBEndPointUrl"];
        private readonly string _authorizationKey = ConfigurationManager.AppSettings["cosmosDBAuthorizationKey"];
        private readonly string _databaseId = ConfigurationManager.AppSettings["cosmosDBDatabaseId"];
        private readonly string _collectionId = ConfigurationManager.AppSettings["cosmosDBCollectionId"];
        private readonly TraceWriter _log;
        // Reusable instance of DocumentClient which represents the connection to a Cosmos DB endpoint.
        private DocumentClient _client;

        public DatabaseMethods(TraceWriter log)
        {
            _log = log;
        }

        /// <summary>
        /// Retrieves all license plate records (documents) that have not yet been exported.
        /// </summary>
        /// <returns></returns>
        public List<LicensePlateDataDocument> GetLicensePlatesToExport()
        {
            _log.Info("Retrieving license plates to export");
            int exportedCount = 0;
            var collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);
            List<LicensePlateDataDocument> licensePlates;

            using (_client = new DocumentClient(new Uri(_endpointUrl), _authorizationKey))
            {
                // MaxItemCount value tells the document query to retrieve 100 documents at a time until all are returned.
                // TODO 5: Retrieve a List of LicensePlateDataDocument objects from the collectionLink where the exported value is false.
                // COMPLETE: licensePlates = _client.CreateDocumentQuery ...
                licensePlates = _client.CreateDocumentQuery<LicensePlateDataDocument>(collectionLink,
            new FeedOptions() { MaxItemCount = 100 })
                .Where(l => l.exported == false)
                .ToList();

                // TODO 6: Remove the line below.
                //my Comments Here
            }

            exportedCount = licensePlates.Count();
            _log.Info($"{exportedCount} license plates found that are ready for export");
            return licensePlates;
        }

        /// <summary>
        /// Updates license plate records (documents) as exported. Call after successfully
        /// exporting the passed in license plates.
        /// In a production environment, it would be best to create a stored procedure that
        /// bulk updates the set of documents, vastly reducing the number of transactions.
        /// </summary>
        /// <param name="licensePlates"></param>
        /// <returns></returns>
        public async Task MarkLicensePlatesAsExported(IEnumerable<LicensePlateDataDocument> licensePlates)
        {
            _log.Info("Updating license plate documents exported values to true");
            var collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);

            using (_client = new DocumentClient(new Uri(_endpointUrl), _authorizationKey))
            {
                foreach (var licensePlate in licensePlates)
                {
                    licensePlate.exported = true;
                    var response = await _client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(_databaseId, _collectionId, licensePlate.Id), licensePlate);

                    var updated = response.Resource;
                    //_log.Info($"Exported value of updated document: {updated.GetPropertyValue<bool>("exported")}");
                }
            }
        }

    }
}