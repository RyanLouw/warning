using CallQualityUITesting.Models;
using Microsoft.Playwright;

namespace CallQualityUITesting.Pages;

public class LoginPage
{
    private readonly IPage _page;
    private readonly string _baseUrl;


    public LoginPage(
        IPage page,
        string baseUrl)
    {
        _page = page;
        _baseUrl = baseUrl;
    }


    public async Task GoToAsync()
    {
        await _page.GotoAsync(_baseUrl);
    }


    public async Task LoginAsync(TestUser user)
    {
        await _page
            .GetByRole(
                AriaRole.Textbox,
                new()
                {
                    Name = "Enter your email, phone, or"
                })
            .FillAsync(user.Username);

        await _page
            .GetByRole(
                AriaRole.Button,
                new() { Name = "Next" })
            .ClickAsync();


        await _page
            .GetByRole(
                AriaRole.Textbox,
                new()
                {
                    Name = $"Enter the password for {user.Username}"
                })
            .FillAsync(user.Password);

        await _page
            .GetByRole(
                AriaRole.Button,
                new() { Name = "Sign in" })
            .ClickAsync();


        await _page
            .GetByRole(
                AriaRole.Button,
                new() { Name = "Yes" })
            .ClickAsync();
    }
}