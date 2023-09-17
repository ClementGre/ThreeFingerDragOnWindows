using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThreeFingersDragEngine.utils;
using ThreeFingersDragOnWindows.utils;
using WinRT.Interop;

namespace ThreeFingersDragOnWindows.touchpad;

public class ContactsManager {
    private readonly List<TouchpadContact> _lastContacts = new();
    private readonly HandlerWindow _source;

    private long _lastInput;


    private IntPtr _oldWndProc;

    public ContactsManager(HandlerWindow source){
        _source = source;
    }

    public void InitializeSource(){
        var touchpadExists = TouchpadHelper.Exists();
        Debug.WriteLine("Touchpad exists: " + touchpadExists);


        var hwnd = WindowNative.GetWindowHandle(_source);
        _oldWndProc = Interop.SetWndProc(hwnd, WindowProcess);

        var success = touchpadExists && TouchpadHelper.RegisterInput(hwnd);

        _source.OnTouchpadInitialized(touchpadExists, success);
    }

    // WindowProc Listener
    private IntPtr WindowProcess(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam){
        switch(message){
            case TouchpadHelper.WM_INPUT:
                var contacts = TouchpadHelper.ParseInput(lParam);
                RegisterTouchpadContacts(contacts);
                break;
        }

        return Interop.CallWindowProc(_oldWndProc, hwnd, message, wParam, lParam);
    }


    // Contacts managements
    private void RegisterTouchpadContacts(TouchpadContact[] contacts){
        //_source.OnTouchpadContact(contacts);
        // On some touchpads, contacts are sent one by one
        foreach(var contact in contacts) RegisterTouchpadContact(contact);
    }

    private void RegisterTouchpadContact(TouchpadContact contact){
        foreach(var lastContact in _lastContacts){
            if(lastContact.ContactId == contact.ContactId){
                // A contact is registered twice: send the event with the list of all contacts
                if(Ctms() - _lastInput < 50)
                    // If contacts have all been released for a long time, cancel the last contact list
                    _source.OnTouchpadContact(_lastContacts.ToArray());
                _lastContacts.Clear();
                break;
            }
        }

        _lastInput = Ctms();
        // Add the contact to the list
        _lastContacts.Add(contact);
    }

    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}