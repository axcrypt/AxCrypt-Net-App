using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AxCrypt.Api.Model;
using AxCrypt.Core;
using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.UI;

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Components.Models;
using AxCrypt.Mono;

namespace AxCrypt.App.Components.Services
{
    public class LoginService : ISignIn
    {
        private CommandLine _commandLine;
        private ApiVersion _apiVersion;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navManager;
        public string UserEmail { get; set; }
        public bool StopAndExit { get; set; } = true;
        public ApiVersion Version { get; set; }
        public bool IsSigningIn { get; set; }

        public LoginService(IJSRuntime jsRuntime, NavigationManager navManager)
        {
            _jsRuntime = jsRuntime;
            _navManager = navManager;
        }
        private bool _loading { get; set; } = false;

        private Action _onStateChange;

        public void SetOnStateChange(Action onStateChange)
        {
            _onStateChange = onStateChange;
        }

        public bool Loading
        {
            get => _loading;
            set
            {
                if (_loading != value)
                {
                    _loading = value;
                    _onStateChange?.Invoke();
                }
            }
        }

        public async Task HandleValidSubmit(LoginModel login)
        {
            Loading = true;
            try
            {
                if (New<UserSettings>().RememberMe != login.RememberMe)
                {
                    New<UserSettings>().RememberMe = login.RememberMe;
                }

                EmailAddress userEmail = ValidUserEmail(login);
                if (userEmail == EmailAddress.Empty) {
                    Loading = false;
                    return;
                }

                AccountStatus status = AccountStatus.Verified;
                //AccountStatus status = await New<MainHomeViewModel>().SignIn(userEmail, login.Password);
                switch (status)
                {
                    case AccountStatus.Verified:
                    case AccountStatus.DefinedByServer:
                        NavigateToHomePage();
                        break;
                    default:
                        login.ErrorMessage = Texts.LoginError;
                        break;
                }
                
               Loading = false;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        private void NavigateToHomePage()
        {
            DeviceIdiom deviceIdiom = DeviceInfo.Idiom;
            if (deviceIdiom == DeviceIdiom.Desktop || deviceIdiom == DeviceIdiom.Tablet)
            {
                _navManager.NavigateTo("/mainpage");
                return;
            }

            if (deviceIdiom == DeviceIdiom.Phone)
            {
                _navManager.NavigateTo("/mainpagemobile");
                return;
            }

            throw new PlatformNotSupportedException($"device is {deviceIdiom}");
        }

        private EmailAddress ValidUserEmail(LoginModel loginModel)
        {
            loginModel.Email = loginModel.Email.Trim();
            if (string.IsNullOrEmpty(loginModel.Email)) 
            {
                loginModel.ErrorMessage = Texts.InvalidEmail;
                return EmailAddress.Empty;
            }

            if(!EmailAddress.TryParse(loginModel.Email, out EmailAddress parsedEmail))
            {
                loginModel.ErrorMessage = Texts.InvalidEmail;
                return EmailAddress.Empty;
            }

            if (New<UserSettings>().RememberMe)
            {
                New<UserSettings>().UserEmail = parsedEmail.Address;
            }

            return parsedEmail;
        }

        public Task<bool> ValidateLogin(string username)
        {
            throw new NotImplementedException();
        }

        private static void StartupProcessMonitor()
        {
            TypeMap.Register.Singleton(() => new ProcessMonitor());
            New<ProcessMonitor>();
        }

        private void ExecuteCommandLine()
        {
            if (!_commandLine.CommandItems.Any() || _commandLine.IsOfflineCommand || _commandLine.IsStartCommand)
            {
                return;
            }
            Task.Run(() =>
            {
                _commandLine.Execute();
                //new ExplorerRefresh().Notify();
            });
        }

        public Task SignIn()
        {
            throw new NotImplementedException();
        }
    }
}