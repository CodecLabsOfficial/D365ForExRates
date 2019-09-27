using CodecLabs.ExRate.Workflows.Helper;
using CodecLabs.ExRate.Workflows.Models;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CodecLabs.ExRate.Workflows.Service
{
    internal class FoatRatesAPI : BaseAPI, IApi
    {
        public HttpResponseMessage CallAPI(string ignoreCases, string baseCurrencyCode, string apiUrl, string apiKey = null)
        {
            apiUrl = $"{apiUrl}/daily/{baseCurrencyCode}.json";
            HttpClient client = new HttpClient();

            // Add an Accept header
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // List data response
            HttpResponseMessage response = client.GetAsync(apiUrl).Result;
            return response;
        }

        public void RetrieveAndProcessD365Currencies(IOrganizationService service, HttpResponseMessage response, string ignoreCases, string baseCurrencyCode, Guid exchangeIntegrationId)
        {
            // Parse the response body
            var json = response.Content.ReadAsStringAsync().Result;
            json = FormatJSON(json);

            var exr = JsonConvert.DeserializeObject<List<FloatRateItem>>(json);

            if (exr == null)
            {
                base.UpdateIntegrationRecord(service, exchangeIntegrationId, false, "Couldn't return any rate" + response.RequestMessage);
                throw new InvalidWorkflowException("Couldn't return any rate" + response.RequestMessage);
            }

            //Retrieve CRM Currencies
            EntityCollection ecCurrencies = SDKHelper.RetrieveAllRecords(service, "transactioncurrency");

            foreach (Entity item in ecCurrencies.Entities)
            {
                CallCurrencyActionFloatRate(service, item, exr, ignoreCases, baseCurrencyCode, exchangeIntegrationId);
            }
        }

        private static string FormatJSON(string json)
        {
            //TODO: Use Newtonsoft instead - this is ugly
            var splitJson = json.Split('{', '}').Where(x => x.Contains("code"));

            json = "[";

            foreach (var item in splitJson)
            {
                json += "{" + item + "},";
            }

            json += "]";
            json = json.Replace(",]", "]");
            return json;
        }

        internal void CallCurrencyActionFloatRate(IOrganizationService service, Entity item, List<FloatRateItem> exr, string ignoreCases, string baseCurrencyCode, Guid exchangeIntegrationId)
        {
            List<string> lstIgnoreCases = new List<String>();
            lstIgnoreCases = String.IsNullOrEmpty(ignoreCases) ? new List<string>() : new List<string>(ignoreCases.Replace(" ", "").Split(';'));

            //If currency code contains in the ignore cases list, skip
            if (lstIgnoreCases.Contains(item.Attributes["isocurrencycode"].ToString())) { return; }
            if (baseCurrencyCode.Equals(item.Attributes["isocurrencycode"].ToString())) { return; }

            if (exr.Any(x => x.code.ToLower().Equals(item.Attributes["isocurrencycode"].ToString().ToLower())))
            {
                var value = exr.Where(x => x.code.ToLower().Equals(item.Attributes["isocurrencycode"].ToString().ToLower())).FirstOrDefault().rate;

                item.Attributes["exchangerate"] = value;

                // Call action
                Dictionary<string, object> actionParams = GetActionParams(item, exchangeIntegrationId, value);

                OrganizationResponse resp = SDKHelper.CallAction(service, "clabs_ExchangeIntegrationAction", actionParams);
            }
        }

        private static Dictionary<string, object> GetActionParams(Entity item, Guid exchangeIntegrationId, decimal value)
        {
            return new Dictionary<string, object>() {
                    { "Currency",  new EntityReference(item.LogicalName, item.Id) },
                    { "ExchangeRate",  Convert.ToDecimal(value) },
                    { "ExchangeRateIntegration", new EntityReference("clabs_exrateintegration", exchangeIntegrationId) } };
        }
    }
}
