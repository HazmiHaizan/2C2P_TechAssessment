using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _2C2P_TechAssessment.Data;
using _2C2P_TechAssessment.Models;
using _2C2P_TechAssessment.Services;

namespace _2C2P_TechAssessment.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _appDbContext;
        private readonly ITransactionParserService _parser;
        private readonly ILogger<TransactionsController> _logger;
        private readonly IWebHostEnvironment _env;

        public TransactionsController (AppDbContext appDbContext, ITransactionParserService parser, ILogger<TransactionsController> logger, IWebHostEnvironment env)
        {
            _appDbContext = appDbContext;
            _parser = parser;
            _logger = logger;
            _env = env;
        }

        public IActionResult Index() => View();


        // /Transactions/Upload
        [HttpPost]
        [RequestSizeLimit(1 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("file is empty.");
            }

            if(file.Length > 1 * 1024 * 1024)
            {
                return BadRequest("File exceeds 1MB");
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var parseResult = await _parser.ParseAsync(ms, file.FileName);

            if (parseResult.Errors.Any())
            {
                var logsDir = Path.Combine(_env.ContentRootPath, "Logs");
                Directory.CreateDirectory(logsDir);
                var logFile = Path.Combine(logsDir, $"invalid_{DateTime.UtcNow:yyyyMMdd_HHmmss}.log");
                await System.IO.File.WriteAllLinesAsync(logFile, parseResult.Errors);

                _logger.LogWarning("Upload contained invalid records. Log saved to {LogFile}", logFile);

                return BadRequest(new { Errors = parseResult.Errors });
            }

            await _appDbContext.Transactions.AddRangeAsync(parseResult.Transactions);
            await _appDbContext.SaveChangesAsync();

            return Ok(new { Message = "File processed successfully", Count = parseResult.Transactions.Count });
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------         APIs for filters        -----------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------


        // /Transactions/ByCurrency?code=<currency>
        [HttpGet]
        public async Task<IActionResult> ByCurrency(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Currency code required");
            }
            
            code = code.ToUpperInvariant();
            var list = await _appDbContext.Transactions
                .Where(t => t.CurrencyCode == code)
                .Select(t => new TransactionDTO
                {
                    Id = t.TransactionId,
                    Payment = $"{t.Amount: 0.00} {t.CurrencyCode}",
                    Status = t.Status
                }).ToListAsync();

            return Json(list);
        }

        // /Transaction/ByStatus?status=<status>
        [HttpGet]
        public async Task<IActionResult> ByStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest("Status required");
            }

            var list = await _appDbContext.Transactions
                .Where(t => t.Status == status)
                .Select(t => new TransactionDTO
                {
                    Id = t.TransactionId,
                    Payment = $"{t.Amount: 0.00} {t.CurrencyCode}",
                    Status = t.Status
                }).ToListAsync();

            return Json(list);
        }

        // /Transaction/ByDateRange?start=<from>&end=<to>
        [HttpGet]
        public async Task<IActionResult> ByDateRange(DateTime start, DateTime end)
        {
            if (end < start)
            {
                return BadRequest("End date must be more than start date");
            }

            var list = await _appDbContext.Transactions
                .Where(t => t.TransactionDate >= start && t.TransactionDate <= end)
                .Select(t => new TransactionDTO
                {
                    Id = t.TransactionId,
                    Payment = $"{t.Amount: 0.00} {t.CurrencyCode}",
                    Status = t.Status
                }).ToListAsync();

            return Json(list);
        }
    }
}
