using Microsoft.Xrm.Sdk;
using System;

namespace CodecLabs.ExRate.Workflows.Service
{
    internal class BaseAPI
    {
        internal void UpdateIntegrationRecord(IOrganizationService service, Guid exchangeIntegrationId, bool pIssucessfull, string errormessage)
        {
            Entity exchangeintegration = new Entity("clabs_exrateintegration", exchangeIntegrationId);
            exchangeintegration.Id = exchangeIntegrationId;
            exchangeintegration.Attributes["clabs_issuccessful"] = pIssucessfull;
            exchangeintegration.Attributes["clabs_errormessage"] = errormessage;

            service.Update(exchangeintegration);
        }
    }
}