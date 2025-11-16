using System.IO;
using System.Threading.Tasks;

public class TemplateService
{
    private readonly string _basePath;

    public TemplateService(IWebHostEnvironment env)
    {
        _basePath = Path.Combine(env.ContentRootPath, "EmailTemplates");
    }

    public async Task<string> LoadTemplateAsync(string fileName)
    {
        string path = Path.Combine(_basePath, fileName);
        return await File.ReadAllTextAsync(path);
    }

    public string ReplacePlaceholders(string template, Dictionary<string, string> values)
    {
        foreach (var item in values)
            template = template.Replace($"{{{{{item.Key}}}}}", item.Value);

        return template;
    }
}
