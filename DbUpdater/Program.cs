using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Db;
using DbUpdater.Csv;
using Domain;
using HashDepot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Serilog;
using TinyCsvParser;

namespace DbUpdater
{
    class Program
    {
        private static IConfiguration _configuration;
        private const string ConfigConnectionString = "db";
        private static ApplicationDbContext _dbContext;

        // ReSharper disable once UnusedParameter.Local
        static async Task Main(string[] args)
        {
            var startTime = DateTime.Now;
            _configuration = BuildConfig();
            CreateLogger(_configuration);
            _dbContext = GetDbContext();
            _dbContext.Database.Migrate();

            var response = DownloadUpdate();

            await using var zipData = new MemoryStream(response);
            using var zipArchive = new ZipArchive(zipData, ZipArchiveMode.Read);
            var data = zipArchive.Entries.First(x => x.Name.Equals(GetIpv4FileName()));

            var csvRecords = ParseCsvRecords(data);
            var dbRecords = GetAllDbRecords();
            var csvDiff = GetCsvDiff(csvRecords, dbRecords);

            await AddNewRecords(csvDiff, dbRecords);
            await UpdateRecords(csvDiff, dbRecords);
            await DeleteOldRecords(dbRecords, csvRecords);

            var totalWorkTime = (DateTime.Now - startTime).TotalSeconds;
            Log.Information("Completed in {0} seconds", totalWorkTime);
        }

        private static async Task DeleteOldRecords(ImmutableDictionary<(IPAddress, int), IpRecord> dbRecords,
            ImmutableDictionary<(IPAddress, int), IpRecord> csvRecords)
        {
            var toDelete = dbRecords
                .Where(db => !csvRecords.ContainsKey(db.Key))
                .Select(db => db.Value)
                .ToImmutableList();

            Log.Debug($"To Delete count: {toDelete.Count()}");
            await _dbContext.BulkDeleteAsync(toDelete);
        }

        private static async Task UpdateRecords(ImmutableDictionary<(IPAddress, int), IpRecord> csvDiff,
            ImmutableDictionary<(IPAddress, int), IpRecord> dbRecords)
        {
            var toUpdate = csvDiff
                .Where(csv => dbRecords.ContainsKey(csv.Key))
                .Select(csv =>
                {
                    var (key, value) = csv;
                    if (!dbRecords.TryGetValue(key, out var dbRecord))
                    {
                        throw new InvalidOperationException("Can't find key in db dictionary");
                    }

                    dbRecord.Latitude = value.Latitude;
                    dbRecord.Longitude = value.Longitude;
                    dbRecord.Hash = value.Hash;
                    return dbRecord;
                })
                .ToImmutableList();
            Log.Debug($"To update count: {toUpdate.Count()}");
            await _dbContext.BulkUpdateAsync(entities: toUpdate);
        }

        private static async Task AddNewRecords(ImmutableDictionary<(IPAddress, int), IpRecord> csvDiff,
            ImmutableDictionary<(IPAddress, int), IpRecord> dbRecords)
        {
            var toAdd = csvDiff.Values
                .AsParallel()
                .Where(csv => !dbRecords.ContainsKey(csv.Network))
                .ToImmutableList();
            Log.Debug($"To add count: {toAdd.Count()}");
            await _dbContext.BulkInsertAsync(toAdd);
            csvDiff.RemoveRange(toAdd.Select(x => x.Network));
        }

        private static ImmutableDictionary<(IPAddress, int), IpRecord> GetAllDbRecords()
        {
            var dbRecords = _dbContext.IpRecords
                .ToImmutableDictionary(x => x.Network, x => x);
            Log.Debug($"DbRecords dictionary count: {dbRecords.Count()}");
            return dbRecords;
        }

        private static ImmutableDictionary<(IPAddress, int), IpRecord> GetCsvDiff(ImmutableDictionary<(IPAddress, int), IpRecord> csvRecords,
            ImmutableDictionary<(IPAddress, int), IpRecord> dbRecords)
        {
            var csvDiff = csvRecords.Values
                .Where(csv =>
                {
                    if (dbRecords.TryGetValue(csv.Network, out var dbValue))
                    {
                        return !dbValue.Hash.Equals(csv.Hash);
                    }

                    return true;
                })
                .ToImmutableDictionary(x => x.Network, x => x);
            Log.Debug($"CsvDiff count: {csvDiff.Count()}");
            return csvDiff;
        }

        private static ImmutableDictionary<(IPAddress, int), IpRecord> ParseCsvRecords(ZipArchiveEntry res)
        {
            Log.Debug($"Try load data from csv");

            var csvParserOptions = new CsvParserOptions(true, ',');
            var csvMapper = new CsvMapper();
            var csvParser = new CsvParser<IpRecord>(csvParserOptions, csvMapper);

            var csvRecords = csvParser
                .ReadFromStream(res.Open(), Encoding.UTF8)
                .Where(x => x.IsValid)
                .Select(x => x.Result)
                .Select(x =>
                {
                    x.Hash = XXHash.Hash32(Encoding.UTF8.GetBytes(x.Network.ToString() + x.Latitude + x.Longitude));
                    return x;
                })
                .ToImmutableDictionary(x => x.Network, x => x);
            Log.Debug($"CsvRecords dictionary count: {csvRecords.Count}");
            return csvRecords;
        }

        private static byte[] DownloadUpdate()
        {
            var url = GetUrl();
            var rest = new RestClient(url);
            var request = new RestRequest();
            var response = rest.DownloadData(request);

            if (response == null)
            {
                Log.Error($"Can't download update from {url}");
                throw new InvalidOperationException("data buffer null");
            }

            return response;
        }

        private static ApplicationDbContext GetDbContext()
        {
            var builder = new DbContextOptionsBuilder();
            builder.UseNpgsql(_configuration.GetConnectionString(ConfigConnectionString));
            return new ApplicationDbContext(builder.Options);
        }

        private static string GetUrl()
        {
            var url = _configuration["Geoip:url"];
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("incorrect url in config file");
            }

            return url;
        }

        private static string GetIpv4FileName()
        {
            var item = _configuration["Geoip:ipv4FileName"];
            if (string.IsNullOrWhiteSpace(item))
            {
                throw new ArgumentException("incorrect fileName in config file");
            }

            return item;
        }

        private static IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        private static void CreateLogger(IConfiguration configuration)
        {
            Console.Write("Creating logger");
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
            Console.WriteLine(" OK");
        }
    }
}