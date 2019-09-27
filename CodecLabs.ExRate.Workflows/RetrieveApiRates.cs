using CodecLabs.ExRate.Workflows.Enums;
using CodecLabs.ExRate.Workflows.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Net.Http;

namespace CodecLabs.ExRate.Workflows
{
    public sealed class RetrieveApiRates : CodeActivity
    {
        #region Input
        [RequiredArgument]
        [Input("ExRateAPI")]
        [ReferenceTarget("clabs_exrateapi")]
        public InArgument<EntityReference> InArgExRateAPI { get; set; }

        [RequiredArgument]
        [Input("ExRateIntegration")]
        [ReferenceTarget("clabs_exrateintegration")]
        public InArgument<EntityReference> InArgExRateIntegration { get; set; }

        [RequiredArgument]
        [Input("IgnoreCases")]
        public InArgument<string> InArgIgnoreCases { get; set; }

        [Output("NextRunDate")]
        public OutArgument<DateTime> OutArgNextRunDate { get; set; }
        #endregion

        #region Output
        [Output("Success")]
        public OutArgument<bool> Success { get; set; }

        [Output("Error Message")]
        public OutArgument<string> ErrorMessage { get; set; }
        #endregion

        private static CodeActivityContext _executionContext;
        private static IWorkflowContext _context;
        private static IOrganizationServiceFactory _serviceFactory;
        private static IOrganizationService _service;
        private Guid _exchangeIntegrationId;
        private BaseAPI _baseAPI;

        //public const string URL = "http://data.fixer.io/api/latest";
        //public static string urlParameters = "?access_key=4862f4eab19851f8f37afc97a8c34acc&format=1";

        protected override void Execute(CodeActivityContext executionContext)
        {
            InitWorkflow(executionContext);

            try
            {
                // Retrieve inputs
                string ignoreCases = InArgIgnoreCases.Get<string>(executionContext);
                EntityReference exRateAPI = InArgExRateAPI.Get<EntityReference>(executionContext);
                EntityReference exRateIntegration = InArgExRateIntegration.Get<EntityReference>(executionContext);
                _exchangeIntegrationId = exRateIntegration.Id;

                //Retrieve ExRate fields
                RetrieveAPIConfig(exRateAPI, out string apiUrl, out string apiKey, out string d365BaseCurrency, out OptionSetValue api);

                ProcessCurrencies(ignoreCases, apiUrl, apiKey, d365BaseCurrency, api);

                UpdateIntegrationRecord();

                Success.Set(_executionContext, true);
                ErrorMessage.Set(_executionContext, "");
            }
            catch (Exception ex)
            {
                Success.Set(_executionContext, false);
                ErrorMessage.Set(_executionContext, ex.Message);
            }
            finally
            {
                UpdateNextRunDate(_executionContext, _exchangeIntegrationId);
            }
        }

        private void UpdateIntegrationRecord()
        {
            try
            {
                _baseAPI.UpdateIntegrationRecord(_service, _exchangeIntegrationId, true, "");
            }
            catch (Exception e)
            {
                _baseAPI.UpdateIntegrationRecord(_service, _exchangeIntegrationId, false, e.Message);
            }
        }

        private static void ValidateAPI(OptionSetValue api)
        {
            if (api == null)
            {
                throw new IndexOutOfRangeException("API is not valid.");
            }
        }

        private void ProcessCurrencies(string ignoreCases, string apiUrl, string apiKey, string d365BaseCurrency, OptionSetValue apiValue)
        {
            IApi api;
            ExRateAPI apiType = (ExRateAPI)apiValue.Value;

            api = GetAPIObject(apiType);

            HttpResponseMessage resp = api.CallAPI(ignoreCases, d365BaseCurrency, apiUrl, apiKey);
            ProcessCurrenciesRate(ignoreCases, d365BaseCurrency, resp, api);
        }

        private static IApi GetAPIObject(ExRateAPI apiType)
        {
            IApi api;
            switch (apiType)
            {
                case ExRateAPI.Fixer:
                    api = new FixerAPI();
                    break;
                case ExRateAPI.ExchangeRateApi:
                    api = new ExchangeRateAPI();
                    break;
                case ExRateAPI.FloatRates:
                    api = new FoatRatesAPI();
                    break;
                default:
                    throw new Exception("API type not found!");
            }

            return api;
        }

        private void ProcessCurrenciesRate(string ignoreCases, string baseCurrencyCode, HttpResponseMessage response, IApi api)
        {
            if (response.IsSuccessStatusCode)
            {
                api.RetrieveAndProcessD365Currencies(_service, response, ignoreCases, baseCurrencyCode, _exchangeIntegrationId);
            }
            else
            {
                _baseAPI.UpdateIntegrationRecord(_service, _exchangeIntegrationId, false, "Couldn't return any rate" + response.RequestMessage);
            }
        }

        private void RetrieveAPIConfig(EntityReference ExRateAPI, out string apiUrl, out string apiKey, out string baseCurrencyCode, out OptionSetValue api)
        {
            Entity crmExRateAPI = RetrieveExchangeAPI(ExRateAPI.Id);
            apiUrl = crmExRateAPI.GetAttributeValue<string>("clabs_url");
            apiKey = crmExRateAPI.GetAttributeValue<string>("clabs_key");
            baseCurrencyCode = crmExRateAPI["clabs_basecurrencycode"].ToString();
            api = crmExRateAPI.GetAttributeValue<OptionSetValue>("clabs_api");

            ValidateAPI(api);
        }

        private void InitWorkflow(CodeActivityContext executionContext)
        {
            //CRM context and service
            _executionContext = executionContext;
            _context = executionContext.GetExtension<IWorkflowContext>();
            _serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            _service = _serviceFactory.CreateOrganizationService(null);
            _baseAPI = new BaseAPI();
        }

        private Entity RetrieveExchangeAPI(Guid pExchangeAPI)
        {
            return _service.Retrieve("clabs_exrateapi", pExchangeAPI, new ColumnSet(true));
        }

        private void UpdateNextRunDate(CodeActivityContext executionContext, Guid exchangeIntegrationId)
        {
            Entity ExchangeRateIntegration = _service.Retrieve("clabs_exrateintegration", exchangeIntegrationId, new ColumnSet(true));
            //DateTime nexrundate = ExchangeRateIntegration.GetAttributeValue<DateTime>("clabs_nextrundate");
            DateTime nextrundate = DateTime.Now;
            nextrundate = nextrundate.ToUniversalTime();
            //DateTime lastrundate = ExchangeRateIntegration.GetAttributeValue<DateTime>("clabs_lastrundate");
            int frequencyValue = ExchangeRateIntegration.GetAttributeValue<int>("clabs_frequencyvalue");
            OptionSetValue frequencyType = ExchangeRateIntegration.GetAttributeValue<OptionSetValue>("clabs_frequencytype");

            nextrundate = GetNextRunDate(nextrundate, frequencyValue, frequencyType);

            OutArgNextRunDate.Set(executionContext, nextrundate);
            return;
        }

        private static DateTime GetNextRunDate(DateTime nextrundate, int frequencyValue, OptionSetValue frequencyType)
        {
            switch (frequencyType.Value) //day 1; hour 2; minute 3
            {
                case 1:
                    nextrundate = nextrundate.AddDays(frequencyValue);
                    break;
                case 2:
                    nextrundate = nextrundate.AddHours(frequencyValue);
                    break;
                case 3:
                    nextrundate = nextrundate.AddMinutes(frequencyValue);
                    break;
            }

            return nextrundate;
        }
    }
}
