# This section explains how to configure your environment to work with this notebook.

- **bicycle_data_prompt.json**: is designed to house all our prompts. While it’s set up to work as is, feel free to tailor it to your needs. Customization is always an option
- **.env**: file is the secure location for storing sensitive details such as your Azure OpenAI endpoint, keys, and more. It’s crucial to update this file with your information. Without these updates, the notebook won’t function unless you manually input the values directly into the notebook. Please handle with care!
- **tsql**: folder is where all T-SQL scripts are stored. Remember to update it within the notebook and/or bicycle_data_prompt.json as needed.
- **csv**: folder is where we keep the CSV sales data. Don’t forget to update it within the notebook as necessary.

# SQL Server Database Development using Prompts as T-SQL Development
In this notebook, we will learn how to use prompts as a way to develop and test Transact-SQL (T-SQL) code for SQL Server databases. Prompts are natural language requests that can be converted into T-SQL statements by using Generative AI models, such as GPT-4. This can help us write code faster, easier, and more accurately, as well as learn from the generated code examples.

## **Database Development Stages**

Database development is the process of designing, creating, and maintaining databases that store and manage data for various applications. Database development typically involves the following stages:
- **Requirements analysis:** This is the stage where we define the purpose, scope, and objectives of the database, as well as the data sources, users, and business rules. We also identify the data entities, attributes, and relationships that will form the logical structure of the database.

- **Database design:** This is the stage where we translate the logical structure of the database into a physical structure that can be implemented by a specific database management system (DBMS), such as SQL Server. We also define the data types, constraints, indexes, views, stored procedures, functions, triggers, and security settings for the database objects.

- **Database implementation:** This is the stage where we create the database and its objects by using a data definition language (DDL), such as T-SQL. We also populate the database with data by using a data manipulation language (DML), such as T-SQL or bulk import tools.

- **Database testing:** This is the stage where we verify that the database meets the requirements and performs as expected. We also check the data quality, integrity, and security of the database. We can use various tools and techniques to test the database, such as unit testing, integration testing, regression testing, performance testing, and data analysis.

- **Database deployment:** This is the stage where we make the database available for use by the intended users and applications. We can use various methods and tools to deploy the database, such as backup and restore, detach and attach, copy database, or SQL Server Data Tools (SSDT).

- **Database maintenance:** This is the stage where we monitor, update, and optimize the database to ensure its availability, reliability, and performance. We can use various tools and tasks to maintain the database, such as backup and recovery, error handling, logging and auditing, indexing and statistics, fragmentation and defragmentation, or SQL Server 

# Goal of this notebook

This notebook is here to show you something amazing. It's about how a smart AI can help you in ways you might not have thought of before. Whether you've worked with T-SQL database development or not, it doesn't matter. This guide will show you how this smart AI can be a helpful tool for you. Let's get started!

This notebook focuses on simplicity, so we're only dealing with a single table and we'll be excluding primary/foreign keys and identifiers. However, don't let this simplicity fool you. You can leverage the flow of this notebook to create complex T-SQL database developments. It's all about how you use the tools at your disposal! Enjoy exploring. 😊

After you've used this notebook, your feedback would be greatly appreciated.

# Leverage the Generative AI as a Knowledgeable Peer
Generative AI is a form of artificial intelligence that can create new and original content, such as text, images, audio, and video, by using generative models, such as GPT-4. Generative AI can learn from existing data and generate new data that has similar characteristics, but does not repeat it. Generative AI can also respond to natural language requests, such as prompts, and produce relevant and realistic outputs.

Generative AI can be a knowledgeable peer for database developers, as it can help us with various tasks, such as:
- **Code generation:** Generative AI can generate T-SQL code for us based on our prompts, which can save us time and effort, and help us learn from the code examples. For instance, we can ask generative AI to create a table, a view, a stored procedure, or a query for us, and it will produce the corresponding T-SQL code.

- **Code suggestion:** Generative AI can suggest T-SQL code for us based on our partial or incomplete code, which can help us complete our code faster and easier, and avoid errors and mistakes. For instance, we can ask generative AI to suggest a column name, a data type, a constraint, or a join condition for us, and it will provide the appropriate T-SQL code.

- **Code explanation:** Generative AI can explain T-SQL code for us based on our questions, which can help us understand the code better and improve our coding skills. For instance, we can ask generative AI to explain the purpose, the logic, or the output of a T-SQL code, and it will provide a clear and concise explanation.

- **Code optimization:** Generative AI can optimize T-SQL code for us based on our goals, which can help us improve the performance and efficiency of our code. For instance, we can ask generative AI to optimize a T-SQL code for speed, memory, or readability, and it will provide a better or alternative T-SQL code.
