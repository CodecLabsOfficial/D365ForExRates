using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;

namespace CodecLabs.ExRate.Workflows
{
    public sealed class UpdateCrmCurrency : CodeActivity
    {
        [RequiredArgument]
        [Input("Currency")]
        [ReferenceTarget("transactioncurrency")]
        public InArgument<EntityReference> InArgCurrency { get; set; }

        [RequiredArgument]
        [Input("ExchangeRateIntegration")]
        [ReferenceTarget("clabs_exrateintegration")]
        public InArgument<EntityReference> InArgExchangeRateIntegration { get; set; }

        [RequiredArgument]
        [Input("ExchangeRate")]
        public InArgument<decimal> InArgExchangeRate { get; set; }

        private static IWorkflowContext _context;
        private static IOrganizationServiceFactory _serviceFactory;
        private static IOrganizationService _service;

        protected override void Execute(CodeActivityContext executionContext)
        {
            InitWorkflow(executionContext);

            // Retrieve inputs
            EntityReference erCurrency = InArgCurrency.Get<EntityReference>(executionContext);
            EntityReference eRExchangeRateIntegration = InArgExchangeRateIntegration.Get<EntityReference>(executionContext);
            decimal ExchangeRate = InArgExchangeRate.Get<decimal>(executionContext);

            Entity eCurrency = UpdateCurrency(erCurrency, ExchangeRate);
            CreateHistoryRecord(erCurrency, ExchangeRate, eCurrency, eRExchangeRateIntegration);
        }

        private static void CreateHistoryRecord(EntityReference erCurrency, decimal dcExchangeRate, Entity eCurrency, EntityReference eRExchangeRateIntegration)
        {
            Entity exchangeRecord = new Entity("clabs_exchangerate");
            exchangeRecord.Attributes["clabs_currencyid"] = erCurrency;
            exchangeRecord.Attributes["clabs_currencycode"] = eCurrency.Attributes["isocurrencycode"];
            exchangeRecord.Attributes["clabs_exchangeratevalue"] = dcExchangeRate;
            exchangeRecord.Attributes["clabs_issuccessful"] = true;
            exchangeRecord.Attributes["clabs_name"] = erCurrency.Name + eCurrency.Attributes["isocurrencycode"];
            exchangeRecord.Attributes["clabs_exrateintegrationid"] = eRExchangeRateIntegration;

            TryCreateHistoryRecord(eRExchangeRateIntegration, exchangeRecord);
        }

        private static void TryCreateHistoryRecord(EntityReference eRExchangeRateIntegration, Entity exchangeRecord)
        {
            try
            {
                _service.Create(exchangeRecord);
            }
            catch (Exception e)
            {
                Entity ExchangeRateIntegration = new Entity("clabs_exrateintegration");
                ExchangeRateIntegration.Id = eRExchangeRateIntegration.Id;
                ExchangeRateIntegration.Attributes["clabs_issuccessful"] = false;
                ExchangeRateIntegration.Attributes["clabs_errormessage"] = e.Message;
                _service.Update(ExchangeRateIntegration);
            }
        }

        private Entity UpdateCurrency(EntityReference erCurrency, decimal ExchangeRate)
        {
            Entity eCurrency = RetrieveCurrency(erCurrency.Id);

            Entity updateCurrency = new Entity("transactioncurrency");
            updateCurrency.Id = erCurrency.Id;
            updateCurrency.Attributes["exchangerate"] = ExchangeRate;
            _service.Update(updateCurrency);
            return eCurrency;
        }

        private static void InitWorkflow(CodeActivityContext executionContext)
        {
            //CRM context and service
            _context = executionContext.GetExtension<IWorkflowContext>();
            _serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            _service = _serviceFactory.CreateOrganizationService(null);
        }

        private Entity RetrieveCurrency(Guid pCurrency)
        {
            return _service.Retrieve("transactioncurrency", pCurrency, new ColumnSet(true));
        }
    }
}
