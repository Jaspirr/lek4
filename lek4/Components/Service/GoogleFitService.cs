using Google.Apis.Auth.OAuth2;
using Google.Apis.Fitness.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class GoogleFitService
{
    private static string[] Scopes = { FitnessService.Scope.FitnessActivityRead };
    private static string ApplicationName = "Your Application Name";
    private FitnessService service;

    public GoogleFitService()
    {
        InitializeService().Wait();
    }

    private async Task InitializeService()
    {
        try
        {
            UserCredential credential;

            string credentialsPath = Path.Combine(Environment.CurrentDirectory, "Resources", "credentials.json");
            Console.WriteLine($"Attempting to load credentials from: {Path.GetFullPath(credentialsPath)}");

            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                string credPath = Path.Combine(Environment.CurrentDirectory, "token.json");
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Google Fit service: {ex.Message}");
            throw;
        }
    }

    public async Task<int> GetTotalStepsAsync(DateTime startDate, DateTime endDate)
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
}
