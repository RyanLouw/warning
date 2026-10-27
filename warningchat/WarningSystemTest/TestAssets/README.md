# Browser dependencies used by tests

BrowserAssets serves these pinned copies when the application's CDN URLs are
requested. This lets the actual Quill, Tom Select and date-picker controls run
without external requests or exposing test authentication headers to a CDN.
Production application asset loading is unchanged.

Sources (npm distributions via https://cdn.jsdelivr.net/npm/):

| Package | Version | Files relative to package |
|---|---|---|
| quill | 1.3.7 | dist/quill.min.js, dist/quill.snow.css, LICENSE |
| tom-select | 2.4.3 | dist/js/tom-select.complete.min.js, dist/css/tom-select.bootstrap5.min.css, LICENSE |
| bootstrap-datepicker | 1.10.0 | dist/js/bootstrap-datepicker.min.js, dist/css/bootstrap-datepicker.min.css, LICENSE |

License copies are named `<package>-LICENSE`. When upgrading an application CDN
version, update these copies and run the editor/calendar UI cases too. All
unrecognized third-party requests are blocked by BrowserAssets.
