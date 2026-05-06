using Microsoft.Maui.Controls;
using System.Windows.Input;
using MobileSLI.Pages;
using MobileSLI.Services;

namespace MobileSLI.ViewModels;

public sealed class AccueilViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly SettingsService _settingsService;
    private readonly ConnectivityService _connectivityService;

    private string _apiBaseUrl;
    private string _connectionTitle = "Connexion non testée";
    private string _connectionMessage = "Testez la connexion au Wi-Fi du dépôt avant de continuer.";
    private bool _isConnected;

    public AccueilViewModel(ApiService apiService, SettingsService settingsService, ConnectivityService connectivityService)
    {
        _apiService = apiService;
        _settingsService = settingsService;
        _connectivityService = connectivityService;
        _apiBaseUrl = _settingsService.ApiBaseUrl;

        TestConnectionCommand = new Command(async () => await TestConnectionAsync(), () => !IsBusy);
        ContinueCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(IdentificationLivreurPage)));
        SaveApiUrlCommand = new Command(SaveApiUrl);
    }

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public string ConnectionTitle
    {
        get => _connectionTitle;
        set => SetProperty(ref _connectionTitle, value);
    }

    public string ConnectionMessage
    {
        get => _connectionMessage;
        set => SetProperty(ref _connectionMessage, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public ICommand TestConnectionCommand { get; }
    public ICommand ContinueCommand { get; }
    public ICommand SaveApiUrlCommand { get; }

    private void SaveApiUrl()
    {
        _settingsService.ApiBaseUrl = ApiBaseUrl;
        ConnectionTitle = "Adresse API enregistrée";
        ConnectionMessage = _settingsService.ApiBaseUrl;
    }

    private async Task TestConnectionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            SaveApiUrl();

            if (!_connectivityService.HasInternetOrLocalNetwork)
            {
                IsConnected = false;
                ConnectionTitle = "Aucun réseau";
                ConnectionMessage = "Le téléphone ne détecte pas de réseau. Vérifiez le Wi-Fi du dépôt.";
                return;
            }

            var result = await _apiService.TestConnectionAsync();
            IsConnected = result.Success;
            ConnectionTitle = result.Success ? "Connecté" : "Connexion impossible";
            ConnectionMessage = result.Success ? "Le téléphone peut contacter l'API." : result.Message;
        }
        finally
        {
            IsBusy = false;
            ((Command)TestConnectionCommand).ChangeCanExecute();
        }
    }
}
