using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
//using System.Windows.Interop;
using ThreeFingersDragOnWindows.src.utils;
using ThreeFingersDragOnWindows.src.Utils;

namespace ThreeFingersDragOnWindows.src.touchpad;

public class ContactsManager<T> where T : Window, IContactsManager
{
    private readonly List<TouchpadContact> _lastContacts = new();
    private readonly T _source;
    //private HwndSource _targetSource;

    private long _lastInput;

    public ContactsManager(T source)
    {
        _source = source;
    }

    public void InitializeSource()
    {
        //_targetSource = PresentationSource.FromVisual(_source) as HwndSource;
        //_targetSource?.AddHook(WndProc);

        var touchpadExists = TouchpadHelper.Exists();
        //var success = touchpadExists && _targetSource != null && TouchpadHelper.RegisterInput(_targetSource.Handle);

        _source.OnTouchpadInitialized(touchpadExists, /*success*/false);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case TouchpadHelper.WM_INPUT:
                var contacts = TouchpadHelper.ParseInput(lParam);
                RegisterTouchpadContacts(contacts);
                break;
        }

        return IntPtr.Zero;
    }

    private void RegisterTouchpadContacts(TouchpadContact[] contacts)
    {
        foreach (var contact in contacts) RegisterTouchpadContact(contact);
    }

    private void RegisterTouchpadContact(TouchpadContact contact)
    {
        foreach (var lastContact in _lastContacts)
            if (lastContact.ContactId == contact.ContactId)
            {
                // A contact is registered twice: send the event with the list of all contacts
                if (Ctms() - _lastInput < 50)
                { // If contacts have all been released for a long time, cancel the last contact list
                    _source.OnTouchpadContact(_lastContacts.ToArray());
                }
                _lastContacts.Clear();
                break;
            }

        _lastInput = Ctms();
        // Add the contact to the list
        _lastContacts.Add(contact);
    }

    private long Ctms()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}

public interface IContactsManager
{
    // Called when the touchpad is detected and the events handlers are registered (or not)
    public void OnTouchpadInitialized(bool touchpadExists, bool touchpadRegistered);

    // Called when a new set of contacts has been registered
    public void OnTouchpadContact(TouchpadContact[] contacts);
}