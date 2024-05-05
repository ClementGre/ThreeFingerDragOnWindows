using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.utils;
using WinRT.Interop;

namespace ThreeFingerDragOnWindows.touchpad;

public class ContactsManager{
    private readonly List<TouchpadContact> _lastContacts = new();
    private readonly HandlerWindow _source;

    private long _lastSendContacts;

    private readonly IntPtr _hwnd;
    private IntPtr _oldWndProc;

    public ContactsManager(HandlerWindow source){
        _source = source;

        _hwnd = WindowNative.GetWindowHandle(_source);
        _oldWndProc = Interop.SetWndProc(_hwnd, WindowProcess);
    }

    public void InitializeSource(){
        var touchpadExists = TouchpadHelper.Exists();
        Logger.Log("Touchpad exists: " + touchpadExists);

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
    public bool isSingleContactMode = true;

    private void ReceiveTouchpadContacts(TouchpadContact[] contacts){
        if(contacts == null || contacts.Length == 0){
            Logger.Log("Receiving empty contacts");
            return;
        }
        Logger.Log("Receiving contacts: " + string.Join(", ", contacts.Select(c => c.ToString())));

        if(isSingleContactMode){
            if(contacts.Length == 1){
                RegisterTouchpadContact(contacts[0]);
            } else{
                isSingleContactMode = false;
                SendLastContacts();
            }
        }
        if(!isSingleContactMode){
            _source.OnTouchpadContact(contacts);
        }
    }

    private void RegisterTouchpadContact(TouchpadContact contact){
        foreach(var lastContact in _lastContacts){
            if(lastContact.ContactId == contact.ContactId){
                // A contact is registered twice: send the event with the list of all contacts
                SendLastContacts();
                break;
            }
        }
        _lastContacts.Add(contact);
    }
    
    private void SendLastContacts(){
        // If contacts have all been released for a long time, cancel the last contact list
        if(Ctms() - _lastSendContacts < 50 && _lastContacts.Count > 0)
            _source.OnTouchpadContact(_lastContacts.ToArray());
        
        _lastContacts.Clear();
        _lastSendContacts = Ctms();
    }

    private long Ctms(){
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}
