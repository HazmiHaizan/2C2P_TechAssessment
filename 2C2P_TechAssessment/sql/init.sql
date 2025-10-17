CREATE TABLE Transactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TransactionId TEXT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    CurrencyCode TEXT NOT NULL,
    TransactionDate DATETIME NOT NULL,
    Status TEXT NOT NULL
);