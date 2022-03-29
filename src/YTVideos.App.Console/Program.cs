using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YTVidoes.Core;

bool endApp = false;

Console.WriteLine("Youtube Videos From Channel To CSV\r");
Console.WriteLine("------------------------\n");

var appSettings = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build().Get<AppSettings>();

var services = new ServiceCollection().AddLogging();
services.AddMemoryCache();
services.AddTransient<YoutubeService>();
services.AddSingleton(s => appSettings);
var sp = services.BuildServiceProvider();

var youtubeApiKey = appSettings.YouTubeAPIKey;
var channelUrl = "";
while (!endApp)
{
    if (string.IsNullOrEmpty(appSettings.ExportPath))
    {
        appSettings.ExportPath = AppDomain.CurrentDomain.BaseDirectory;
    }
    while (string.IsNullOrEmpty(youtubeApiKey))
    {
        Console.Write("Please enter your youtube api key : ");
        youtubeApiKey= appSettings.YouTubeAPIKey  = Console.ReadLine();
        var jsonWriteOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
        
        var newJson = JsonSerializer.Serialize(appSettings, jsonWriteOptions);

        var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        File.WriteAllText(appSettingsPath, newJson);
    }
    
    while (string.IsNullOrEmpty(channelUrl))
    {
        Console.Write("Please enter youtube channel url: ");
        channelUrl = Console.ReadLine();
        var progress = new Progress<string>(x => Console.Write("\r{0}   ", x));
        var youtubeService = sp.GetService<YoutubeService>();
        var result = await youtubeService.GetYoutubeVideosInAChannel(channelUrl, progress, true);
        if (result.IsValid)
        {
            Console.WriteLine("Exported successfull to the path " + appSettings.ExportPath);
        }
        else
        {
            Console.WriteLine("There was a problem : " + result.GetAllErrors());
        }
        Console.Write(channelUrl);
    }

    // Wait for the user to respond before closing.
    Console.Write("Press 'n' and Enter to close the app, or press any other key and Enter to continue: ");
    if (Console.ReadLine() == "n")
    {
        endApp = true;

    }
    else
    {
        channelUrl = "";
    }
    
}

Console.WriteLine("\n"); 
