IF OBJECT_ID('prd.sales', 'U') IS NOT NULL
    DROP TABLE prd.sales;

CREATE TABLE prd.sales
(
    ProductId INT,
    ProductName VARCHAR(50),
    ProductType VARCHAR(30),
    Color VARCHAR(15),
    OrderQuantity INT,
    Size VARCHAR(15),
    Category VARCHAR(15),
    Country VARCHAR(30),
    Date DATE,
    PurchasePrice DECIMAL(18,2),
    SellingPrice DECIMAL(18,2)
);