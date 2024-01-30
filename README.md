# SQL-AI-samples

## About this repo

This repo hosts samples meant to help design AI applications built on data from an Azure SQL Database. We illustrate key technical concepts and demonstrate workflows that integrate Azure SQL data with other popular AI application components inside and outside of Azure. 

## Description of samples

### Azure SQL ACS Samples

The [AzureSQL_CogSearch_IntegratedVectorization](https://github.com/Azure-Samples/SQL-AI-samples/blob/main/AzureSQLACSSamples/src/AzureSQL_CogSearch_IntegratedVectorization.ipynb) sample notebook shows a simple AI application that recommends products based on a database of user reviews, using Azure Cognitive Search to store and search the relevant data. It highlights new preview features of Azure Cognitive Search, including automatic chunking and integrated vectorization of user queries.

### Azure SQL Promptflow Samples

The [AzureSQL_Prompt_Flow](https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLPromptFlowSamples) sample shows an E2E example of how to build AI applications with Prompt Flow, Azure Cognitive Search, and your own data in Azure SQL database. It includes instructions on how to index your data with Azure Cognitive Search, a sample Prompt Flow local development that links everything together with Azure OpenAI connections, and also how to create an endpoint of the flow to an Azure ML workspace.

### Similarity Search Samples

OpenAI embeddings, and thus vectors, can be used to perform similarity search and create solution that provide customer with a better user experience, better search results and in general a more natural way to find relevant data in a reference dataset. Due to ability to provide an answer even when search request do not perfectly match a given content, similary search is ideal for creating recommenders. A fully working end-to-end sample is available here: https://github.com/Azure-Samples/azure-sql-db-session-recommender

## Getting started

See the description in each sample for instructions (projects will have either a README file or instructions in the notebooks themselves.)

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
