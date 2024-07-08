using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

public class GoogleFitService
{
    private static string[] Scopes = { FitnessService.Scope.FitnessActivityRead };
    private static string ApplicationName = "MyFitnessApp";
    private FitnessService service;
    private UserCredential credential;

    public async Task InitializeServiceAsync()
    {
        try
        {
            string credentialsFileName = "credentials.json";

            // Kolla i AppDataDirectory
            string credentialsPath = Path.Combine(FileSystem.AppDataDirectory, credentialsFileName);
            Console.WriteLine($"Attempting to load credentials from: {credentialsPath}");

            // Kasta ett undantag om filen inte hittas
            if (!File.Exists(credentialsPath))
            {
                Console.WriteLine("credentials.json not found.");
                throw new FileNotFoundException("credentials.json not found.");
            }

            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                string credPath = Path.Combine(FileSystem.AppDataDirectory, "token.json");
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }

            service = new FitnessService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Console.WriteLine("Google Fit service initialized successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Google Fit service: {ex.Message}");
            throw;
        }
    }

    public async Task<int> GetTotalStepsAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var dataSources = service.Users.DataSources.List("me");
            var response = await dataSources.ExecuteAsync();

            var stepDataSource = response.DataSource.FirstOrDefault(ds => ds.DataType.Name == "com.google.step_count.delta");

            if (stepDataSource == null)
            {
                throw new Exception("No step data source found.");
            }

            var datasetId = $"{startDate:yyyyMMdd}000000000-{endDate:yyyyMMdd}000000000";
            var dataSets = service.Users.DataSources.Datasets.Get("me", stepDataSource.DataStreamId, datasetId);
            var dataSetResponse = await dataSets.ExecuteAsync();

            int totalSteps = dataSetResponse.Point
                .Where(p => p.Value != null && p.Value.Count > 0)
                .Sum(p => p.Value[0].IntVal ?? 0);

            return totalSteps;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting total steps: {ex.Message}");
            throw;
        }
    }
}
