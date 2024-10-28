namespace AxCrypt.App.Components.Services.Interface;

public interface ITrayService
{
    void Initialize();

    Action ClickHandler { get; set; }
}
