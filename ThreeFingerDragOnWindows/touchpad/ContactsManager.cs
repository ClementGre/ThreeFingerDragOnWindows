using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.utils;
using WinRT.Interop;

namespace ThreeFingerDragOnWindows.touchpad;

public class ContactsManager {
    private readonly List<TouchpadContact> _lastContacts = new();
    private readonly HandlerWindow _source;

    private long _lastInput;

    private readonly IntPtr _hwnd;
    private IntPtr _oldWndProc;

    public ContactsManager(HandlerWindow source){
        _source = source;
        
        _hwnd = WindowNative.GetWindowHandle(_source);
        _oldWndProc = Interop.SetWndProc(_hwnd, WindowProcess);
    }

    public void InitializeSource(){
        var touchpadExists = TouchpadHelper.Exists();
        Debug.WriteLine("Touchpad exists: " + touchpadExists);

        var success = touchpadExists && TouchpadHelper.RegisterInput(_hwnd);
        
        _source.OnTouchpadInitialized(touchpadExists, success);
    }

    // WindowProc Listener
    private IntPtr WindowProcess(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam){
        switch(message){
            case TouchpadHelper.WM_INPUT:
                var contacts = TouchpadHelper.ParseInput(lParam);
                ReceiveTouchpadContacts(contacts);
                break;
        }

        return Interop.CallWindowProc(_oldWndProc, hwnd, message, wParam, lParam);
    }


    // Contacts managements
    private void ReceiveTouchpadContacts(TouchpadContact[] contacts){
        Debug.WriteLine("Receiving contacts: " + string.Join(", ", contacts.Select(c => c.ToString())));
        
        if (contacts.Length == 1){
            // On some touchpads, contacts are sent one by one
            RegisterTouchpadContact(contacts[0]);
        }else{
            _source.OnTouchpadContact(contacts);
        }
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