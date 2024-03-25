# SQL-AI-samples

## About this repo

This repo hosts samples meant to help design [AI applications built on data from an Azure SQL Database](aka.ms/sql-ai). We illustrate key technical concepts and demonstrate workflows that integrate Azure SQL data with other popular AI application components inside and outside of Azure.

## Available Samples

### Azure SQL + Azure Cognitive Services

The [AzureSQL_CogSearch_IntegratedVectorization](https://github.com/Azure-Samples/SQL-AI-samples/blob/main/AzureSQLACSSamples/src/AzureSQL_CogSearch_IntegratedVectorization.ipynb) sample notebook shows a simple AI application that recommends products based on a database of user reviews, using Azure Cognitive Search to store and search the relevant data. It highlights new preview features of Azure Cognitive Search, including automatic chunking and integrated vectorization of user queries.

### Azure SQL + Azure Promptflow 

The [AzureSQL_Prompt_Flow](https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLPromptFlowSamples) sample shows an E2E example of how to build AI applications with Prompt Flow, Azure Cognitive Search, and your own data in Azure SQL database. It includes instructions on how to index your data with Azure Cognitive Search, a sample Prompt Flow local development that links everything together with Azure OpenAI connections, and also how to create an endpoint of the flow to an Azure ML workspace.

### Azure SQL + Azure OpenAI 

This example shows how to use Azure OpenAI from Azure SQL database to get the vector embeddings of any choosen text, and then calculate the cosine similarity against the Wikipedia articles (for which vector embeddings have been already calculated,) to find the articles that covers topics that are close - or similar - to the provided text.

https://github.com/Azure-Samples/azure-sql-db-openai

### Retrieval Augmented Generation (T-SQL Sample)

In this repo you will find a step-by-step guide on how to use Azure SQL Database to do Retrieval Augmented Generation (RAG) using the data you have in Azure SQL and integrating with OpenAI, directly from the Azure SQL database itself. You'll be able to ask queries in natural language and get answers from the OpenAI GPT model, using the data you have in Azure SQL Database.

https://github.com/Azure-Samples/azure-sql-db-chatbot

### Content Moderation

In this folder are two T-SQL scripts that call Azure OpenAI Content Safety and Language AI. The Content Safety example will analyze a text string and return a severity in four categories: violence, sexual, self-harm, and hate. The Language AI script will analyze text and return what PII it found, what category of PII it is, and redact the results to obfuscate the PII in the original text string.

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLDatabase/ContentModeration

### LangChain and Azure SQL Database

This folder contains 2 python notebooks that use LangChain to create a NL2SQL agent against an Azure SQL Database. The notebooks use either Azure OpenAI or OpenAI for the LLM. To get started immedietly, you can create a codespace on this repository, use the terminal to change to the LangChain directory and follow one of the notebooks.

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLDatabase/LangChain

## End-To-End Samples

### Similar Content Finder

OpenAI embeddings, and thus vectors, can be used to perform similarity search and create solution that provide customer with a better user experience, better search results and in general a more natural way to find relevant data in a reference dataset. Due to ability to provide an answer even when search request do not perfectly match a given content, similary search is ideal for creating recommenders. A fully working end-to-end sample is available here: 

https://github.com/Azure-Samples/azure-sql-db-session-recommender

###  Session Conference Assistant

This sample demonstrates how to build a session assistant using Jamstack, Retrieval Augmented Generation (RAG) and Event-Driven architecture, using Azure SQL DB to store and search vectors embeddings generated using OpenAI. The solution is built using Azure Static Web Apps, Azure Functions, Azure SQL Database, and Azure OpenAI. A fully working, production ready, version of this sample, that has been used at VS Live conferences, is available here: https://ai.lasvegas.vslive.com/

https://github.com/azure-samples/azure-sql-db-session-recommender-v2

###  SQL Server Database Development using Prompts as T-SQL Development
In this notebook, we will learn how to use prompts as a way to develop and test Transact-SQL (T-SQL) code for SQL Server databases. Prompts are natural language requests that can be converted into T-SQL statements by using Generative AI models, such as GPT-4. This can help us write code faster, easier, and more accurately, as well as learn from the generated code examples.

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLDatabase/Prompt-Based%20T-SQL%20Database%20Development

## Getting started

See the description in each sample for instructions (projects will have either a README file or instructions in the notebooks themselves.)

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
