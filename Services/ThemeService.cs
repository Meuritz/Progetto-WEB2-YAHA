using Microsoft.JSInterop;

namespace Progetto_Web_2_IoT_Auth.Services;

public sealed class ThemeService
{
    private const string StorageKey = "theme.dark";
    private bool _initialized;

    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync(IJSRuntime js)
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            var raw = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (bool.TryParse(raw, out var value))
                IsDarkMode = value;
        }
        catch
        {
        }

        OnChange?.Invoke();
    }

    public async Task SetDarkModeAsync(bool value, IJSRuntime js)
    {
        if (IsDarkMode == value)
            return;

        IsDarkMode = value;

        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, value.ToString().ToLowerInvariant());
        }
        catch
        {
        }

        OnChange?.Invoke();
    }

    public Task ToggleAsync(IJSRuntime js) => SetDarkModeAsync(!IsDarkMode, js);
}
