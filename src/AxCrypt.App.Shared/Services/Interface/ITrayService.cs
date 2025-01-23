namespace AxCrypt.App.Shared.Services.Interface;

public interface ITrayService
{
    void Initialize();

    Action ClickHandler { get; set; }
}