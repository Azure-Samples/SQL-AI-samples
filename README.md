# SQL-AI-samples

## About this repo

This repo hosts samples meant to help design [AI applications built on data from an Azure SQL Database](https://aka.ms/sql-ai). We illustrate key technical concepts and demonstrate workflows that integrate Azure SQL data with other popular AI application components inside and outside of Azure.

- [AI Features Samples](#ai-features-samples)
    - [Azure SQL + Azure Cognitive Services](#azure-sql--azure-cognitive-services)
    - [Azure SQL + Azure Promptflow](#azure-sql--azure-promptflow)
    - [Azure SQL + Azure OpenAI](#azure-sql--azure-openai)
    - [Generating SQL for Azure SQL Database using Vanna.AI](#generating-sql-for-azure-sql-database-using-vannaai)
    - [Retrieval Augmented Generation (T-SQL Sample)](#retrieval-augmented-generation-t-sql-sample)
    - [Content Moderation](#content-moderation)
    - [LangChain and Azure SQL Database](#langchain-and-azure-sql-database)
- [End-To-End Samples](#end-to-end-samples)
    - [Similar Content Finder](#similar-content-finder)
    - [Session Conference Assistant](#session-conference-assistant)
    - [Chatbot on your own data with LangChain and Chainlit](#chatbot-on-your-own-data-with-langchain-and-chainlit)
    - [Chatbot on structured and unstructured data with Semantic Kernel](#chatbot-on-structured-and-unstructured-data-with-semantic-kernel)
    - [Azure SQL DB Vectorizer](#azure-sql-db-vectorizer)
    - [SQL Server Database Development using Prompts as T-SQL Development](#sql-server-database-development-using-prompts-as-t-sql-development)
    - [Redis Vector Search Demo Application using ACRE and Cache Prefetching from Azure SQL with Azure Functions](#redis-vector-search-demo-application-using-acre-and-cache-prefetching-from-azure-sql-with-azure-functions)
    - [Similarity Search with FAISS and Azure SQL](#similarity-search-with-faiss-and-azure-sql)
    - [Build your own IVFFlat index with KMeans](#build-your-own-ivfflat-index-with-kmeans)
- [Workshops](#workshops)
    - [Azure SQL Cryptozoology AI Embeddings Lab​](#azure-sql-cryptozoology-ai-embeddings-lab​)  
    - [Build an AI App GraphQL Endpoint with SQL DB in Fabric​](#build-an-ai-app-graphql-endpoint-with-sql-db-in-fabric​)

## AI Features Samples

### Azure SQL + Azure Cognitive Services

The [AzureSQL_CogSearch_IntegratedVectorization](https://github.com/Azure-Samples/SQL-AI-samples/blob/main/AzureSQLACSSamples/src/AzureSQL_CogSearch_IntegratedVectorization.ipynb) sample notebook shows a simple AI application that recommends products based on a database of user reviews, using Azure Cognitive Search to store and search the relevant data. It highlights new preview features of Azure Cognitive Search, including automatic chunking and integrated vectorization of user queries.

### Azure SQL + Azure Promptflow 

The [AzureSQL_Prompt_Flow](https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLPromptFlowSamples) sample shows an E2E example of how to build AI applications with Prompt Flow, Azure Cognitive Search, and your own data in Azure SQL database. It includes instructions on how to index your data with Azure Cognitive Search, a sample Prompt Flow local development that links everything together with Azure OpenAI connections, and also how to create an endpoint of the flow to an Azure ML workspace.

### Azure SQL + Azure OpenAI 

This example shows how to use Azure OpenAI from Azure SQL database to get the vector embeddings of any chosen text, and then calculate the cosine similarity against the Wikipedia articles (for which vector embeddings have been already calculated,) to find the articles that covers topics that are close - or similar - to the provided text.

https://github.com/Azure-Samples/azure-sql-db-openai

### Generating SQL for Azure SQL Database using Vanna.AI
This notebook runs through the process of using the `vanna` Python package to generate SQL using AI (RAG + LLMs) including connecting to a database and training.

https://github.com/Azure-Samples/SQL-AI-samples/blob/main/AzureSQLDatabase/Vanna.ai/vanna_and_sql.ipynb

### Retrieval Augmented Generation (T-SQL Sample)

In this repo you will find a step-by-step guide on how to use Azure SQL Database to do Retrieval Augmented Generation (RAG) using the data you have in Azure SQL and integrating with OpenAI, directly from the Azure SQL database itself. You'll be able to ask queries in natural language and get answers from the OpenAI GPT model, using the data you have in Azure SQL Database.

https://github.com/Azure-Samples/azure-sql-db-chatbot

### Content Moderation

In this folder are two T-SQL scripts that call Azure OpenAI Content Safety and Language AI. The Content Safety example will analyze a text string and return a severity in four categories: violence, sexual, self-harm, and hate. The Language AI script will analyze text and return what PII it found, what category of PII it is, and redact the results to obfuscate the PII in the original text string.

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLDatabase/ContentModeration

### LangChain and Azure SQL Database

This folder contains 2 python notebooks that use LangChain to create a NL2SQL agent against an Azure SQL Database. The notebooks use either Azure OpenAI or OpenAI for the LLM. To get started immedietly, you can create a codespace on this repository, use the terminal to change to the LangChain directory and follow one of the notebooks.

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLDatabase/LangChain

You can also use the Getting Started samples available on LangChain website, but using Azure SQL:

https://github.com/Azure-Samples/azure-sql-langchain

### MSSQL MCP Server

A Model Context Protocol (MCP) server for MSSQL Databases using the official [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk).

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/MssqlMcp

### Using Agent Mode to build and test SQL Database Projects

An example of how to use VSCode Agent Mode to carry out a multi-step chain-of-thought process to iteratively build and test SQL Database projects.

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AgentMode

## End-To-End Samples

### Similar Content Finder

OpenAI embeddings, and thus vectors, can be used to perform similarity search and create solution that provide customer with a better user experience, better search results and in general a more natural way to find relevant data in a reference dataset. Due to ability to provide an answer even when search request do not perfectly match a given content, similary search is ideal for creating recommenders. A fully working end-to-end sample is available here: 

https://github.com/Azure-Samples/azure-sql-db-session-recommender

###  Session Conference Assistant

This sample demonstrates how to build a session assistant using Jamstack, Retrieval Augmented Generation (RAG) and Event-Driven architecture, using Azure SQL DB to store and search vectors embeddings generated using OpenAI. The solution is built using Azure Static Web Apps, Azure Functions, Azure SQL Database, and Azure OpenAI. A fully working, production ready, version of this sample, that has been used at VS Live conferences, is available here: https://ai.microsofthq.vslive.com/

https://github.com/azure-samples/azure-sql-db-session-recommender-v2

### Chatbot on your own data with LangChain and Chainlit

Sample RAG pattern, with full UX, using Azure SQL DB, Langchain and Chainlit as demonstrated in the [#RAGHack](https://github.com/microsoft/RAG_Hack) conference. Full details and video recording available here: [RAG on Azure SQL Server](https://github.com/microsoft/RAG_Hack/discussions/53).

https://github.com/Azure-Samples/azure-sql-db-rag-langchain-chainlit

### Chatbot on structured and unstructured data with Semantic Kernel

A chatbot that can answer using RAG and using SQL Queries to answer any question you may want to ask it, be it on unstructured data (eg: what is the common issue raised for product XYZ) or on structured data (eg: how many customers from Canada called the support line?). Built using Semantic Kernel.

https://github.com/Azure-Samples/azure-sql-db-chat-sk

### Azure SQL DB Vectorizer

Quickly chunk text and generate embeddings at scale with data from Azure SQL. 

https://github.com/Azure-Samples/azure-sql-db-vectorizer

###  SQL Server Database Development using Prompts as T-SQL Development

In this notebook, we will learn how to use prompts as a way to develop and test Transact-SQL (T-SQL) code for SQL Server databases. Prompts are natural language requests that can be converted into T-SQL statements by using Generative AI models, such as GPT-4. This can help us write code faster, easier, and more accurately, as well as learn from the generated code examples.

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLDatabase/Prompt-Based%20T-SQL%20Database%20Development

### Redis Vector Search Demo Application using ACRE and Cache Prefetching from Azure SQL with Azure Functions

We based this project from our Product Search Demo which showcase how to use Redis as a Vector Db. We modified the demo by adding a Cache Prefetching pattern from Azure SQL to ACRE using Azure Functions. The Azure Function uses a SQL Trigger that will trigger for any updates that happen in the table.

https://github.com/AzureSQLDB/redis-azure-ai-demo

### Similarity Search with FAISS and Azure SQL

This contains Python notebooks that integrate Azure SQL Database with FAISS for efficient similarity search. The notebooks demonstrate how to store and query data in Azure SQL, leveraging FAISS for fast similarity search. We will be demonstrating it with Wikipedia movie plots data stored in Azure SQL. We’ll encode these movie plots into dense vectors using a pre-trained model and then create a FAISS index to perform similarity searches.
Learn more in the detail blog and video: https://aka.ms/azuresql-faiss

https://github.com/Azure-Samples/SQL-AI-samples/tree/main/AzureSQLFaiss

### Build your own IVFFlat index with KMeans

This sample demonstrates how to perform Approximate Nearest Neighbor (ANN) search on a vector column in Azure SQL DB using KMeans clustering, a technique known as IVFFlat or Cell-Probing. The project utilizes the SciKit Learn library for clustering, storing results in a SQL DB table to facilitate ANN search. This approach is beneficial for speeding up vector searches in Azure SQL DB. 

## Workshops

### Azure SQL Cryptozoology AI Embeddings Lab

Dive into a unique cryptozoology experience using Azure SQL and Azure AI to uncover intriguing (and true) connections among cryptids worldwide.

[https://github.com/Azure-Samples/sql-in-fabric-ai-embeddings-workshop](https://github.com/AzureSQLDB/azure-sql-cryptozoology-ai-embeddings-lab)

### Build an AI App GraphQL Endpoint with SQL DB in Fabric​

This lab will guide you through creating a set of GraphQL RAG application APIs that use relational data, Azure OpenAI, and SQL DB in Fabric.

https://github.com/Azure-Samples/sql-in-fabric-ai-embeddings-workshop

## Getting started

See the description in each sample for instructions (projects will have either a README file or instructions in the notebooks themselves.)

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
