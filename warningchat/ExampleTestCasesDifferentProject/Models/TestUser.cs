using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CallQualityUITesting.Models;

public class TestUser
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
public class TestUsers
{
    public TestUser Assessor { get; set; } = new();

    public TestUser Manager { get; set; } = new();

    public TestUser IT { get; set; } = new();
}
public enum ReassessmentAnswer
{
    Yes,
    No,
    NotApplicable
}
public static class TestConfiguration
{
    private static readonly IConfigurationRoot Configuration;

    static TestConfiguration()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    public static TestUsers Users =>
        Configuration
            .GetSection("TestUsers")
            .Get<TestUsers>()
        ?? throw new InvalidOperationException(
            "TestUsers configuration could not be loaded.");
}