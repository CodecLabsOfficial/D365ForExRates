using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodecLabs.ExRate.Workflows.Helper
{
    internal static class SDKHelper
    {
        internal static EntityCollection RetrieveAllRecords(IOrganizationService service, string logicalName)
        {
            Entity entity = null;

            QueryExpression query = new QueryExpression();
            query.EntityName = logicalName;

            query.ColumnSet = new ColumnSet(true);

            EntityCollection collection;

            collection = service.RetrieveMultiple(query);
            return collection;
        }

        internal static EntityCollection RetrieveEntityCollection(IOrganizationService service, string logicalName, string[] entitySearchField, object[] entitySearchFieldValue, ColumnSet columnSet, ConditionOperator op = ConditionOperator.Equal, string orderDescendingByfield = null)
        {
            QueryExpression query = new QueryExpression();
            query.EntityName = logicalName;

            FilterExpression filter = new FilterExpression();

            for (int i = 0; i < entitySearchField.Length; i++)
            {
                ConditionExpression condition = new ConditionExpression();

                condition.AttributeName = entitySearchField[i];
                condition.Operator = op;

                if (entitySearchFieldValue[i] == null)
                {
                    condition.Operator = ConditionOperator.Null;
                }
                else
                {
                    condition.Values.Add(entitySearchFieldValue[i]);
                }

                filter.FilterOperator = LogicalOperator.And;

                filter.AddCondition(condition);
            }

            query.ColumnSet = columnSet;
            query.Criteria = filter;

            if (!String.IsNullOrEmpty(orderDescendingByfield))
            {
                query.Orders.Add(new OrderExpression(orderDescendingByfield, OrderType.Descending));
            }

            EntityCollection collection;

            collection = service.RetrieveMultiple(query);

            return collection;
        }

        internal static Guid Create(IOrganizationService service, Entity entityTBC)
        {
            return service.Create(entityTBC);
        }

        public static void Update(IOrganizationService service, Entity objTBU, string logicalName)
        {
            objTBU.LogicalName = logicalName;
            service.Update(objTBU);
        }

        internal static OrganizationResponse CallAction(IOrganizationService service, string actionName, Dictionary<string, object> actionParams)
        {
            OrganizationRequest req = new OrganizationRequest(actionName);

            foreach (var item in actionParams)
            {
                req[item.Key] = item.Value;
            }

            // Execute the Action
            OrganizationResponse response = service.Execute(req);
            return response;
        }

        internal static EntityMetadata GetEntityMetadata(IOrganizationService service, string entityName)
        {
            RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = EntityFilters.All,
                LogicalName = entityName
            };
            RetrieveEntityResponse retrieveAccountEntityResponse = (RetrieveEntityResponse)service.Execute(retrieveEntityRequest);
            EntityMetadata entityMetadata = retrieveAccountEntityResponse.EntityMetadata;

            return entityMetadata;
        }

        internal static bool AttributeExists(AttributeMetadata[] attributes, string attributeName)
        {
            if (attributes == null || attributes.Length < 1)
            {
                return false;
            }

            foreach (var item in attributes)
            {
                if (item.LogicalName.Equals(attributeName))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool AttributeExists(AttributeCollection attributes, string attributeName)
        {
            if (attributes == null || attributes.Count < 1)
            {
                return false;
            }

            foreach (var item in attributes)
            {
                if (item.Key.Equals(attributeName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
