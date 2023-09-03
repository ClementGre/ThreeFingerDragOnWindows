using System;
using System.Collections.Generic;
using System.Diagnostics;
using ThreeFingersDragOnWindows.src.utils;

namespace ThreeFingersDragOnWindows.src.touchpad;

public class ContactsManager
{
    private readonly List<TouchpadContact> _lastContacts = new();
    private readonly HandlerWindow _source;

    
    private IntPtr _oldWndProc;

    private long _lastInput;

    public ContactsManager(HandlerWindow source)
    {
        _source = source;
    }

    public void InitializeSource()
    {
       
        var touchpadExists = TouchpadHelper.Exists();
        Debug.WriteLine("Touchpad exists: " + touchpadExists);


        IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_source);
        _oldWndProc = Interop.SetWndProc(hwnd, WindowProcess);

        var success = touchpadExists && TouchpadHelper.RegisterInput(hwnd);

        _source.OnTouchpadInitialized(touchpadExists, success);
    }

    // WindowProc Registration
    private IntPtr WindowProcess(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        Debug.WriteLine("WindowProcess: " + message);
        switch (message)
        {
            case TouchpadHelper.WM_INPUT:
                Debug.WriteLine("WM_INPUT message: " + lParam);
                var contacts = TouchpadHelper.ParseInput(lParam);
                RegisterTouchpadContacts(contacts);
                break;
        }

        return Interop.CallWindowProc(_oldWndProc, hwnd, message, wParam, lParam); ;
    }


    // Contacts managements

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