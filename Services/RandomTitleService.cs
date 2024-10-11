using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;

public class RandomTitleService
{
    private static string[] _titles = [];
    private static readonly char[] separator = ['\r', '\n'];
    private static readonly Random random = new();


    public RandomTitleService()
    {
        if (_titles == null || _titles.Length == 0)
        {
            _titles = LoadTitlesFromResource();
        }
    }

    private string[] LoadTitlesFromResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "sl_img_prcr.resources.titles.txt";
        using Stream stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Resource '{resourceName}' not found in assembly.");
        using StreamReader reader = new(stream);
        var fileContent = reader.ReadToEnd();
        return fileContent.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }

    public string GetRandomTitle()
    {
        if (_titles == null || _titles.Length == 0)
        {
            throw new InvalidOperationException("Titles list is empty or failed to load.");
        }
        return _titles[random.Next(_titles.Length)];
    }
}
