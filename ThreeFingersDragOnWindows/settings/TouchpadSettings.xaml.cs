namespace ThreeFingersDragOnWindows.settings;

public sealed partial class TouchpadSettings {

    public TouchpadSettings(){
        InitializeComponent();
        if(App.Instance.HandlerWindow == null || !App.Instance.HandlerWindow.TouchpadInitialized){
            TouchpadStatus.Title = "Registering touchpad...";
            TouchpadStatus.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Informational;
            ContactsDebug.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        } else{
            OnTouchpadInitialized();
        }
    }

    public void UpdateContactsText(string text){
        ContactsDebug.Title = "Inputs:\n" + text;
    }

    public void OnTouchpadInitialized(){
        if(App.Instance.HandlerWindow.TouchpadExists){
            if(App.Instance.HandlerWindow.TouchpadRegistered){
                TouchpadStatus.Title = "Touchpad exists and is registered !";
                TouchpadStatus.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                ContactsDebug.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            } else{
                TouchpadStatus.Title = "Touchpad exists, but can't be registered.";
                TouchpadStatus.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                ContactsDebug.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            } 
        } else{
            TouchpadStatus.Title = "Touchpad not detected, make sure to have a Windows Precision compatible touchpad.";
            TouchpadStatus.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
            ContactsDebug.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

}