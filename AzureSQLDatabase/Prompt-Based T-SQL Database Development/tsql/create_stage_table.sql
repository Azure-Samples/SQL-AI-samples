IF OBJECT_ID('stg.salestmp', 'U') IS NOT NULL
    DROP TABLE stg.salestmp;

CREATE TABLE stg.salestmp
(
    ProductId VARCHAR(255),
    ProductName VARCHAR(255),
    ProductType VARCHAR(255),
    Color VARCHAR(255),
    OrderQuantity VARCHAR(255),
    Size VARCHAR(255),
    Category VARCHAR(255),
    Country VARCHAR(255),
    Date VARCHAR(255),
    PurchasePrice VARCHAR(255),
    SellingPrice VARCHAR(255)
);