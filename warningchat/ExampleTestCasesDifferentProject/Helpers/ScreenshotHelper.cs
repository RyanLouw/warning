
using Microsoft.Playwright;

namespace CallQualityUITesting.Helpers
{
    public static class ScreenshotHelper
    {
        public static async Task TakeScreenshotAsync(
            IPage page,
            string name)
        {
            var directory =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Screenshots");

            Directory.CreateDirectory(directory);

            var path =
                Path.Combine(
                    directory,
                    $"{name}.png");

            await page.ScreenshotAsync(new()
            {
                Path = path,
                FullPage = true
            });
        }
    }
}
