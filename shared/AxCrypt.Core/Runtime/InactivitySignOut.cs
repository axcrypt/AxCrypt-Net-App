using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Runtime
{
    public class InactivitySignOut : IDisposable
    {
        private DelayedAction _signOutDelayedAction;
        private readonly IdentityViewModel _identityViewModel;

        public InactivitySignOut(TimeSpan timeout, IdentityViewModel identityViewModel)
        {
            if (timeout == TimeSpan.Zero || !New<LicensePolicy>().Capabilities.Has(LicenseCapability.InactivitySignOut))
            {
                return;
            }

            _identityViewModel = identityViewModel;
            _signOutDelayedAction = new DelayedAction(New<IDelayTimer>(), timeout);
            _signOutDelayedAction.ActionAsync += async () => await SignOutAction(this, new EventArgs());
            _signOutDelayedAction.StartIdleTimer();
        }

        public void RestartInactivityTimer()
        {
            _signOutDelayedAction?.StartIdleTimer();
        }

        private async Task SignOutAction(object sender, EventArgs e)
        {
            if (!New<LicensePolicy>().Capabilities.Has(LicenseCapability.InactivitySignOut))
            {
                return;
            }

            if (await _identityViewModel.LogOffLogOn.CanExecuteAsync(null))
            {
                await _identityViewModel.LogOffLogOn.ExecuteAsync(null);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }
            if (_signOutDelayedAction != null)
            {
                _signOutDelayedAction.Dispose();
            }
            _signOutDelayedAction = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}