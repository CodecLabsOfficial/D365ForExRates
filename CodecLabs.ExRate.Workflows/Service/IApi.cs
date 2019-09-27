using Microsoft.Xrm.Sdk;
using System;
using System.Net.Http;

namespace CodecLabs.ExRate.Workflows.Service
{
    interface IApi
    {
        HttpResponseMessage CallAPI(string ignoreCases, string baseCurrencyCode, string apiUrl, string apiKey);
        void RetrieveAndProcessD365Currencies(IOrganizationService service, HttpResponseMessage response, string ignoreCases, string baseCurrencyCode, Guid exchangeIntegrationId);
    }
}
