using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Xml.Linq;
using _2C2P_TechAssessment.Models;
using System.Diagnostics.CodeAnalysis;

namespace _2C2P_TechAssessment.Services
{
    public class ParseResult
    {
        public List<TransactionEntity> Transactions { get; } = new();
        public List<string> Errors { get; } = new();
    }

    public interface ITransactionParserService
    {
        Task<ParseResult> ParseAsync(Stream inputStream, string fileName, CancellationToken ct = default);
    }

    public class TransactionParserService : ITransactionParserService
    {
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public async Task<ParseResult> ParseAsync(Stream inputStream, string fileName, CancellationToken ct = default)
        {
            var result = new ParseResult();
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (ext == ".csv")
            {
                using var reader = new StreamReader(inputStream, leaveOpen: true);
                result = ParseCsv(reader);
            }
            else if (ext == ".xml")
            {
                using var reader = new StreamReader(inputStream, leaveOpen: true);
                var xml = await reader.ReadToEndAsync();
                result = ParseXml(xml);
            }
            else{
                result.Errors.Add("Unknown file format");
            }

            return result;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private ParseResult ParseCsv(TextReader reader)
        {
            var result = new ParseResult();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            };

            using var csv = new CsvReader(reader, config);

            while(csv.Read())
            {
                try
                {
                    var fields = new string[5];
                    for (int i = 0; i < 5; i++)
                    {
                        fields[i] = csv.GetField(i)?.Trim() ?? string.Empty;
                    }

                    ValidateAndMapCsv(fields, result);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"CSV parse error: { ex.Message }");
                }
            }

            if (result.Errors.Any()) result.Transactions.Clear();
            return result;
        }

        private void ValidateAndMapCsv(string[] fields, ParseResult result)
        {
            string id = TrimQuotes(fields[0]);
            string amountRaw = TrimQuotes(fields[1]);
            string currency = TrimQuotes(fields[2]);
            string dateRaw = TrimQuotes(fields[3]);
            string statusRaw = TrimQuotes(fields[4]);

            var errorPrefix = $"CSV record (id ='{id}')";

            if (string.IsNullOrWhiteSpace(id)) 
            {
                result.Errors.Add(errorPrefix + "TransactionId missing");
                return;
            }

            if (id.Length > 50)
            {
                result.Errors.Add(errorPrefix + "TransactionId length is more than 50");
                return;
            }

            if(string.IsNullOrWhiteSpace(amountRaw))
            {
                result.Errors.Add(errorPrefix + "Amount missing");
                return;
            }

            if (!decimal.TryParse(amountRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            {
                result.Errors.Add(errorPrefix + $"Amount invalid ('{amountRaw}')");
                return;
            }

            if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3 || !currency.All(char.IsLetter))
            {
                result.Errors.Add(errorPrefix + $"Currency invalid ('{currency}')");
                return;
            }
            currency = currency.ToUpperInvariant();

            if (!DateTime.TryParseExact(dateRaw, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var txDate))
            {
                result.Errors.Add(errorPrefix + $"TransactionDate invalid ('{dateRaw}')");
                return;
            }

            string status = statusRaw switch
            {
                "Approved" => "A",
                "Failed" => "R",
                "Finished" => "D",
                _ => null
            };

            if (status == null)
            {
                result.Errors.Add(errorPrefix + $"Status invalid ('{statusRaw}')");
                return;
            }

            result.Transactions.Add(new TransactionEntity
            {
                TransactionId = id,
                Amount = amount,
                CurrencyCode = currency,
                TransactionDate = txDate,
                Status = status
            });
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private ParseResult ParseXml(string xml)
        {
            var result = new ParseResult();
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                result.Errors.Add("XML parse failed: " + ex.Message);
                return result;
            }

            var transactions = doc.Descendants("Transaction");
            int i = 0;
            foreach (var tx in transactions)
            {
                i++;
                try
                {
                    var idAttr = tx.Attribute("id")?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(idAttr))
                    {
                        result.Errors.Add($"XML record #{i}: id attribute missing");
                        continue;
                    }
                    if (idAttr.Length > 50) { result.Errors.Add($"XML record '{idAttr}': id too long (>50)"); continue; }

                    var dateEl = tx.Element("TransactionDate")?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(dateEl)) { result.Errors.Add($"XML record '{idAttr}': TransactionDate missing"); continue; }
                    if (!DateTime.TryParseExact(dateEl, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var txDate))
                    {
                        result.Errors.Add($"XML record '{idAttr}': TransactionDate invalid ('{dateEl}')");
                        continue;
                    }

                    var amountEl = tx.Element("PaymentDetails")?.Element("Amount")?.Value?.Trim();
                    var currencyEl = tx.Element("PaymentDetails")?.Element("CurrencyCode")?.Value?.Trim();

                    if (string.IsNullOrWhiteSpace(amountEl)) { result.Errors.Add($"XML record '{idAttr}': Amount missing"); continue; }
                    if (!decimal.TryParse(amountEl, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                    {
                        result.Errors.Add($"XML record '{idAttr}': Amount invalid ('{amountEl}')");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(currencyEl) || currencyEl.Length != 3 || !currencyEl.All(char.IsLetter))
                    {
                        result.Errors.Add($"XML record '{idAttr}': Currency invalid ('{currencyEl}')");
                        continue;
                    }
                    var currency = currencyEl.ToUpperInvariant();

                    var statusRaw = tx.Element("Status")?.Value?.Trim();
                    string status = statusRaw switch
                    {
                        "Approved" => "A",
                        "Rejected" => "R",
                        "Done" => "D",
                        _ => null
                    };
                    if (status == null) { result.Errors.Add($"XML record '{idAttr}': Status invalid ('{statusRaw}')"); continue; }

                    result.Transactions.Add(new TransactionEntity
                    {
                        TransactionId = idAttr,
                        Amount = amount,
                        CurrencyCode = currency,
                        TransactionDate = txDate,
                        Status = status
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"XML record #{i} unexpected error: {ex.Message}");
                }
            }

            if (result.Errors.Any()) result.Transactions.Clear();
            return result;
        }

        private static string TrimQuotes(string s) => s?.Trim().Trim('"').Trim() ?? string.Empty;
    }
}
