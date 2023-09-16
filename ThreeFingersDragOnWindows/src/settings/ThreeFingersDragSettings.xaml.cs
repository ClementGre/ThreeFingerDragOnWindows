using System.Collections.Generic;

namespace ThreeFingersDragOnWindows.settings;

public sealed partial class ThreeFingersDragSettings {

    public ThreeFingersDragSettings(){
        InitializeComponent();
    }

    public void UpdateContactsText(string text){
        ContactsDebug.Text = text;
    }

}