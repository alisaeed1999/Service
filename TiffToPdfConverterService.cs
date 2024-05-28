using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ImageMagick;

public class TiffToPdfConverterService : BackgroundService
{
    private readonly ILogger<TiffToPdfConverterService> _logger;
    // private readonly FileSystemWatcher _watcher;
    private Timer? _timer;
    private readonly string _inputDirectory;
    private readonly string _outputDirectory;

    public TiffToPdfConverterService(ILogger<TiffToPdfConverterService> logger)
    {
        _logger = logger;
        _inputDirectory = "TiffFiles";
        _outputDirectory = "PdfFiles";

        if (!Directory.Exists(_inputDirectory))
            Directory.CreateDirectory(_inputDirectory);

        if (!Directory.Exists(_outputDirectory))
            Directory.CreateDirectory(_outputDirectory);

        // _watcher = new FileSystemWatcher(_inputDirectory, "*.tiff")
        // {
        //     NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        // };
        // _watcher.Created += OnCreated;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        _logger.LogInformation("Tiff to PDF Converter Service started.");

        // Initial check for existing TIFF files
        await Task.Run(() => DoWork(null), stoppingToken);

        // Temporary manual trigger for testing
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        DoWork(null);

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));

    }

    private void DoWork(object? state)
    {
        _logger.LogInformation("Checking for TIFF files to convert at {time}", DateTimeOffset.Now);

        // _logger.LogInformation("Checking for TIFF files to convert.");
        try
        {
            var tiffFiles = Directory.GetFiles(_inputDirectory, "*.tiff");

            if (tiffFiles.Length == 0)
            {
                _logger.LogInformation("No TIFF files found at {time}", DateTimeOffset.Now);
                return;
            }

            _logger.LogInformation("{count} TIFF files found.", tiffFiles.Length);


            foreach (var filePath in tiffFiles)
            {
                _logger.LogInformation("Processing file: {file}", filePath);

                ConvertTiffToPdf(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while checking for TIFF files: {message}", ex.Message);
        }


        // var tiffFiles = Directory.GetFiles(_inputDirectory, "*.tiff");

        // foreach (var filePath in tiffFiles)
        // {
        //     ConvertTiffToPdf(filePath);
        // }
    }

    // private void OnCreated(object sender, FileSystemEventArgs e)
    // {
    //     _logger.LogInformation($"New file detected: {e.FullPath}");
    //     ConvertTiffToPdf(e.FullPath);
    // }

    private void ConvertTiffToPdf(string filePath)
    {

        try
        {
            var fileModifiedTime = File.GetLastWriteTime(filePath);
            var formattedDate = fileModifiedTime.ToString("yyyyMMdd_HHmmss");
            var outputFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{formattedDate}.pdf";
            var outputFilePath = Path.Combine(_outputDirectory, outputFileName);

            using (var images = new MagickImageCollection(filePath))
            {
                images.Write(Path.Combine(_outputDirectory, Path.GetFileNameWithoutExtension(filePath) + ".pdf"));
            }
            _logger.LogInformation($"Converted {filePath} to PDF.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error converting {filePath} to PDF: {ex.Message}");
        }
    }

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        // _watcher.EnableRaisingEvents = false;
        // _watcher.Dispose();

        _timer?.Change(Timeout.Infinite, 0);
        _logger.LogInformation("Tiff to PDF Converter Service stopped.");
        return base.StopAsync(stoppingToken);
    }


    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
