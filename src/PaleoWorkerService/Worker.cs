using HtmlAgilityPack;
using System.Net;
using System.Net.Mail;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace UrlMonitorWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _pollingInterval;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _pollingInterval = TimeSpan.FromSeconds(_configuration.GetValue<int>("MonitorSettings:PollingIntervalSeconds"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("URL Monitor Worker Service started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorUrlAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring URL");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task MonitorUrlAsync(CancellationToken cancellationToken)
    {
        var monitorUrl = _configuration["MonitorSettings:Url"];
        var localFilePath = _configuration["MonitorSettings:LocalFilePath"];
        //var searchStrings = _configuration.GetSection("MonitorSettings:SearchStrings").Get<string[]>();

        if (string.IsNullOrEmpty(monitorUrl) && string.IsNullOrEmpty(localFilePath))
        {
            _logger.LogWarning("Monitor URL or local file path not configured");
            return;
        }

        //if (searchStrings == null || searchStrings.Length == 0)
        //{
        //    _logger.LogWarning("Search strings not configured");
        //    return;
        //}

        string content;

        if (!string.IsNullOrEmpty(localFilePath))
        {
            _logger.LogInformation("Reading local HTML file: {path}", localFilePath);
            content = await File.ReadAllTextAsync(localFilePath, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Polling URL with Selenium: {url}", monitorUrl);
            content = await FetchWithSeleniumAsync(monitorUrl);
        }

        var products = ParseProductBoxes(content);

        if (products.Any())
        {
            _logger.LogInformation("Found {count} products with 'kaufen' available", products.Count);
            await SendEmailNotificationAsync(products);
        }
        else
        {
            _logger.LogInformation("On {time}, no buyable products found", DateTimeOffset.Now);
        }
    }

    private async Task<string> FetchWithSeleniumAsync(string url)
    {
        return await Task.Run(() =>
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl(url);

            // Wait for page to load (adjust as needed)
            System.Threading.Thread.Sleep(3000);

            var pageSource = driver.PageSource;
            driver.Quit();

            return pageSource;
        });
    }

    private List<ProductInfo> ParseProductBoxes(string htmlContent)
    {
        var products = new List<ProductInfo>();
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        var productBoxes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'stx-ProductBox')]");

        if (productBoxes == null)
        {
            _logger.LogInformation("No stx-ProductBox elements found");
            return products;
        }

        _logger.LogInformation("Found {count} ProductBox elements", productBoxes.Count);

        foreach (var productBox in productBoxes)
        {
            try
            {
                var dateNode = productBox.SelectSingleNode(".//*[contains(@class, 'stx-ProductCardTitle')]");
                var eventDate = dateNode?.InnerText?.Trim();

                var productActionsDiv = productBox.SelectSingleNode(".//div[contains(@class, 'stx-ProductActions')]");
                if (productActionsDiv != null)
                {
                    var kaufenSpan = productActionsDiv.SelectSingleNode(".//a//span")?.InnerText;
                    if (!string.IsNullOrEmpty(kaufenSpan))
                    {
                        _logger.LogInformation("Found product available for purchase: {date}", eventDate);
                        products.Add(new ProductInfo
                        {
                            EventDate = eventDate ?? "Unknown",
                            HasKaufenOption = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing individual product box");
            }
        }

        return products;
    }

    private async Task SendEmailNotificationAsync(List<ProductInfo> products)
    {
        var smtpSettings = _configuration.GetSection("EmailSettings");
        var smtpHost = smtpSettings["SmtpHost"];
        var smtpPort = smtpSettings.GetValue<int>("SmtpPort");
        var smtpUsername = smtpSettings["Username"];
        var smtpPassword = smtpSettings["Password"];
        var fromEmail = smtpSettings["FromEmail"];
        var toEmail = smtpSettings["ToEmail"];
        var enableSsl = smtpSettings.GetValue<bool>("EnableSsl");

        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(toEmail))
        {
            _logger.LogWarning("Email settings not properly configured");
            return;
        }

        try
        {
            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? smtpUsername),
                Subject = "Paleo tickets available for Purchase",
                Body = $"On {DateTimeOffset.Now}, tickets are available for the following dates:\n\n" +
                       string.Join("\n", products.Select(p => $"- Event Date: {p.EventDate}")),
                IsBodyHtml = false
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email notification sent successfully to {email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification");
        }
    }
}

public class ProductInfo
{
    public string EventDate { get; set; } = string.Empty;
    public bool HasKaufenOption { get; set; }
}