using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Db;
using DbUpdater.Csv;
using Domain;
using HashDepot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Readers;
using TinyCsvParser;

namespace DbUpdater
{
    class Program
    {
        private static IConfiguration _configuration;
        private static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;
        public const string ConfigConnectionString = "db";
        private static readonly string GeoLiteDir = AppDir + "/geolite2/";
        private static ConcurrentBag<IpRecord> _toAdd = new ConcurrentBag<IpRecord>();
        private static ConcurrentBag<IpRecord> _toUpdate = new ConcurrentBag<IpRecord>();
        private static ApplicationDbContext _dbContext;

        static async Task Main(string[] args)
        {
            var startTime = DateTime.Now;
            _configuration = BuildConfig();
            CreateLogger(_configuration);

            
            var fileToDecompress = new FileInfo(GeoLiteDir + "GeoLite2-City-CSV_20191105.zip");
            using FileStream originalFileStream = fileToDecompress.OpenRead();
            using var za = new ZipArchive(originalFileStream, ZipArchiveMode.Read);
            var res = za.Entries.First(x => x.Name.Equals("GeoLite2-City-Blocks-IPv4.csv"));
            
//            using var decompressed = new MemoryStream();
//            var qq = res.Open();
            
//            using GZipStream zipStream = new GZipStream(originalFileStream, CompressionMode.Decompress, true);
//            using var decompressed = new MemoryStream();
//            zipStream.CopyTo(decompressed);
//            decompressed.Seek(0, SeekOrigin.Begin);
//            using TarArchive tarArchive = TarArchive.Open(decompressed);
//            using var reader = tarArchive.ExtractAllEntries();
//            reader.WriteAllToDirectory(AppDir + "GeoLite2", options: new ExtractionOptions {Overwrite = true});


            

            _dbContext = GetDbContext();
            _dbContext.Database.Migrate();

            
            
            CsvParserOptions csvParserOptions = new CsvParserOptions(true, ',');
            var csvMapper = new CsvMapper();
            var csvParser = new CsvParser<IpRecord>(csvParserOptions, csvMapper);

            Log.Debug($"Try load data from csv");
            
            var csvRecords = csvParser
                .ReadFromStream(res.Open(), Encoding.UTF8)
//                .ReadFromFile(GeoLiteDir + "GeoLite2-City-Blocks-IPv4-lite.csv", Encoding.ASCII)
                .Where(x => x.IsValid)
                .Select(x => x.Result)
                .Select(x =>
                {
                    x.Hash = XXHash.Hash32(Encoding.UTF8.GetBytes(x.Network.ToString() + x.Latitude + x.Longitude));
                    return x;
                })
                .ToImmutableDictionary(x => x.Network, x => x);
            Log.Debug($"CsvRecords dictionary count: {csvRecords.Count}");


            var dbRecords = _dbContext.IpRecords
                .ToImmutableDictionary(x => x.Network, x => x);
            Log.Debug($"DbRecords dictionary count: {dbRecords.Count()}");

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

            var toAdd = csvDiff.Values
                .AsParallel()
                .Where(csv => !dbRecords.ContainsKey(csv.Network))
                .ToImmutableList();
            Log.Debug($"To add count: {toAdd.Count()}");
            await _dbContext.BulkInsertAsync(toAdd);
            csvDiff.RemoveRange(toAdd.Select(x => x.Network));

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

            var toDelete = dbRecords
                .Where(db => !csvRecords.ContainsKey(db.Key))
                .Select(db => db.Value)
                .ToImmutableList();

            Log.Debug($"To Delete count: {toDelete.Count()}");
            await _dbContext.BulkDeleteAsync(toDelete);


            Console.WriteLine($"Start time = {startTime}, current = {DateTime.Now}");
            Console.WriteLine($"Delta time = {(DateTime.Now - startTime).TotalSeconds}");
            Console.WriteLine("Completed");
        }

        private static ApplicationDbContext GetDbContext()
        {
            var builder = new DbContextOptionsBuilder();
            builder.UseNpgsql(_configuration.GetConnectionString(ConfigConnectionString));
            return new ApplicationDbContext(builder.Options);
        }

        private static void Decompress(FileInfo fileToDecompress)
        {
            using FileStream originalFileStream = fileToDecompress.OpenRead();
            using GZipStream zipStream = new GZipStream(originalFileStream, CompressionMode.Decompress, true);
            using var decompressed = new MemoryStream();
            zipStream.CopyTo(decompressed);
            decompressed.Seek(0, SeekOrigin.Begin);
            using TarArchive tarArchive = TarArchive.Open(decompressed);
            using var reader = tarArchive.ExtractAllEntries();
            reader.WriteAllToDirectory(AppDir + "GeoLite2", options: new ExtractionOptions {Overwrite = true});
        }

        private static string GetUrl(IConfiguration configuration)
        {
            var url = _configuration["Geoip:url"];
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("incorrect url in config file");
            }

            return url;
        }

        public static IConfiguration BuildConfig()
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