# Azure SQL and Azure AI Services

This is an example of how to use build a simple chat application using your data in an Azure SQL Database using Azure AI Search, Azure OpenAI, and the Azure AI Foundry. In this example you will learn how to walk through the steps to:

- Deploy Azure OpenAI models using the Azure AI Foundry
- Build an Azure AI Search index and vectorize your data in the index based on a table in an Azure SQL Database
- Test our a chat application in the Chat Playground of Azure AI Studio using the Azure AI Search index as a data source along with Azure OpenAI models.
- Deploy a simple Azure Web application using Azure AI Studio based on the chat playground.

## Prerequisites

You will need an Azure subscription for this example and at minimum Contributor access to the subscription with the rights to create a resource group and all the resources listed in this example. You will also need to be a member of the Managed Identity Contributor role to create a user-assigned managed identity or have a user-assigned managed identity created for you.

1. Create a new resource group in your Azure subscription to hold all of your resources.

The region for the group does not matter. You can use the same region for all resources or different regions for each resource. Use the following documentation for instructions on how to create a resource group: [Create a resource group](https://learn.microsoft.com/azure/azure-resource-manager/management/manage-resource-groups-portal#create-resource-groups).

2. Create an Azure Entra User-Assigned Managed Identity

If you do not already have a user-assigned managed identity available to use you will need to create one or have one created for you. To create one you will need to be a member of the Managed Identity Contributor role. Create an Azure Managed Identity for your Azure subscription. To create a user-assigned managed identity, follow the instructions [here](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-manage-user-assigned-managed-identities).

- Use the resource group you created in the first steps for the resource group for the managed identity.
- You can choose the same region as the resource group or the region of your choice.

3. Deploy an Azure SQL Database

Deploy a new Azure SQL Database based on the sample AdventureWorksLT database per the documentation [here](https://docs.microsoft.com/azure/azure-sql/database/single-database-create-quickstart?tabs=azure-portal).
- Use the resource group you created earlier.
- Create a new logical server. Use the server name of your choice. Deploy the server in the same region as your resource group. Choose Entra Authentication only and select an Entra admin. This admin will be needed later to give permissions to the managed identity to access the database when you create an Azure AI Search index.
- You can use the free database offer for this database. If you choose this option your database will be a General Purpose, Gen5, 2 vCores, and 32Gb storage. You can use any service tier option provided you have at least 2 vCores and 32Gb storage (or with Hyperscale you don't have to choose a storage option).
- For networking choose Public endpoint and allow Azure services and resources to access this server. This will allow the Azure AI Search to access the database.
- Use the default security settings.
- Use the default Additional settings except be sure to select Sample for using existing data.

4. Assign permissions to the managed identity to access the logical server and database.

- Assign the managed identity you created earlier to the Reader role assignment for the logical server. To do this go to the logical server in the Azure portal and select Access control (IAM) and add a role assignment. Select the Reader role and the managed identity you created earlier.
- Assign access to the logical server for the managed identity. To do this go to the Logical Server in the Azure Portal and select Security and then Identity. Add the User-assigned managed identity you created earlier to the list of managed identities that can access the server. Also select this as the Primary managed identity for the server. Select Save to save the changes.
- Create a new database user as the managed identity and assign it access to read from the database. To do this go to the database in the Azure portal and select Query editor. Run the following SQL script to create a new user and assign it read access to the database:

```sql
CREATE USER [your-managed-identity-name] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [your-managed-identity-name];
ALTER ROLE db_datawriter ADD MEMBER [our-managed-identity-name];
```

5. Deploy an Azure AI Foundry project and AI models

6. Deploy an Azure AI Search service


## Create an Azure AI Search Index with vectorized data from the Azure SQL Database


## Deploy a Chat Playground session in Azure AI Studio

Note: Should I use the new AI Assistants playground instead?


## Test your Chat session


## Deploy a simple Azure Web Application based on the chat playground