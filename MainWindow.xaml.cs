using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

namespace ThreeFingersDragOnWindows;

public partial class MainWindow {
    public static readonly DependencyProperty TouchpadExistsProperty =
        DependencyProperty.Register("TouchpadExists", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

    public static readonly DependencyProperty TouchpadContactsProperty =
        DependencyProperty.Register("TouchpadContacts", typeof(string), typeof(MainWindow), new PropertyMetadata(null));

    private HwndSource _targetSource;
    private readonly ThreeFingersDrag _threeFingersDrag;

    public MainWindow(){
        InitializeComponent();
        
        _threeFingersDrag = new ThreeFingersDrag();
        _threeFingersDrag.Calibrate();
    }

    public bool TouchpadExists{
        get => (bool) GetValue(TouchpadExistsProperty);
        set => SetValue(TouchpadExistsProperty, value);
    }

    public string TouchpadContacts{
        get => (string) GetValue(TouchpadContactsProperty);
        private set => SetValue(TouchpadContactsProperty, value);
    }

    protected override void OnSourceInitialized(EventArgs e){
        base.OnSourceInitialized(e);

        _targetSource = PresentationSource.FromVisual(this) as HwndSource;
        _targetSource?.AddHook(WndProc);

        TouchpadExists = TouchpadHelper.Exists();

        if(TouchpadExists){
            var success = _targetSource != null && TouchpadHelper.RegisterInput(_targetSource.Handle);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled){
        switch(msg){
            case TouchpadHelper.WM_INPUT:
                var contacts = TouchpadHelper.ParseInput(lParam);
                TouchpadContacts = string.Join(Environment.NewLine, contacts.Select(x => x.ToString()));

                _threeFingersDrag.RegisterTouchpadContacts(contacts);
                break;
        }

        return IntPtr.Zero;
    }

    private void Calibrate(object sender, RoutedEventArgs e){
        _threeFingersDrag.Calibrate();
    }
}