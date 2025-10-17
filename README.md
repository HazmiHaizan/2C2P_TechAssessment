# 2C2P_TechAssessment

A web application for **uploading, validating, and querying transaction data** from **CSV** and **XML** files.  
Built with **ASP.NET Core MVC (.NET 8)** and **Entity Framework Core (SQLite)** made for 2C2P's Technical Assessment.

---

## 🚀 Features

✅ Upload transaction files in **CSV** or **XML** format  
✅ Validate mandatory fields — reject entire file if any record is invalid  
✅ Log invalid records automatically to `/Logs`  
✅ Store transactions in SQLite database  
✅ Query transactions:
- by **Currency**
- by **Status (A/R/D)**
- by **Date range**

---

## 🏗️ Tech Stack

| Layer | Technology |
|:------|:------------|
| **Frontend** | HTML5, JavaScript, [Bootstrap 5](https://getbootstrap.com/) |
| **Backend** | ASP.NET Core MVC (.NET 8) |
| **Database** | SQLite (via Entity Framework Core) |
| **ORM** | Entity Framework Core 8 |
| **CSV Parser** | [CsvHelper](https://joshclose.github.io/CsvHelper/) |
| **Logging** | Console + log files (invalid records saved to `/Logs`) |

---

## ⚙️ Setup Instructions

1. Clone the repository
```
git clone https://github.com/HazmiHaizan/2C2P_TechAssessment.git
cd 2C2P_TechAssessment/2C2P_TechAssessment
```

2. Install dependencies
```
dotnet restore
```

3. Apply database migrations
```
dotnet tool install --global dotnet-ef
dotnet ef database update
```

4. Run the application
```
dotnet run
```

Open your browser and navigate to:
👉 http://localhost:5000/Transactions

---

## 📂 File Format Examples (CSV & XML file examples are included in 'samples' folder)
CSV
```
"Invoice0000001","1,000.00","USD","20/02/2019 12:33:16","Approved"
"Invoice0000002","300.00","USD","21/02/2019 02:04:59","Failed"
```
XML
```
<Transactions>
  <Transaction id="Inv00001">
    <TransactionDate>2019-01-23T13:45:10</TransactionDate>
    <PaymentDetails>
      <Amount>200.00</Amount>
      <CurrencyCode>USD</CurrencyCode>
    </PaymentDetails>
    <Status>Done</Status>
  </Transaction>
</Transactions>
```

---

## 🧠 API Endpoints

| Method | Endpoint                                                    | Description            |
| :----- | :---------------------------------------------------------- | :--------------------- |
| `POST` | `/Transactions/Upload`                                      | Upload CSV or XML file |
| `GET`  | `/Transactions/ByCurrency?code=USD`                         | Get by currency        |
| `GET`  | `/Transactions/ByStatus?status=A`                           | Get by status          |
| `GET`  | `/Transactions/ByDateRange?start=2024-01-01&end=2024-12-31` | Get by date range      |

---

## Support

If you are unable to run the project, feel free to contact me for support
