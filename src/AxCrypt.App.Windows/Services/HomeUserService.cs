namespace AxCrypt.App.Windows.Services;

public class HomeUserService
{
    //public event Action<bool>? OnUserSignedInStateChanged;

    //private bool _isSignedIn;

    //public bool IsSignedIn
    //{
    //    get => _isSignedIn;
    //    set
    //    {
    //        _isSignedIn = value;
    //        //OnUserSignedInStateChanged?.Invoke(_isSignedIn);
    //    }
    //}

    //public void ShowSignIn()
    //{
    //    IsSignedIn = false;
    //}

    //public void ShowMainPage()
    //{
    //    IsSignedIn = true;
    //}

    public event Action<bool>? OnVisibilityOfUserSignUpChanged;

    private bool _showSignUp;

    public bool ShowSignUp
    {
        get => _showSignUp;
        set
        {
            _showSignUp = value;
            OnVisibilityOfUserSignUpChanged?.Invoke(_showSignUp);
        }
    }

    public void ShowSignUpPage()
    {
        ShowSignUp = true;
    }

    public void ShowFilePasswordDialog()
    {

    }
}
