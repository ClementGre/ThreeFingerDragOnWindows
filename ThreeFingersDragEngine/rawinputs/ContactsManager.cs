using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using ThreeFingersDragEngine.utils;
using WinRT.Interop;

namespace ThreeFingersDragEngine.rawinputs;

public class ContactsManager {
    private readonly List<TouchpadContact> _lastContacts = new();
    private readonly HandlerWindow _source;
    private HwndSource? _targetSource;
    
    private long _lastInput;

    public ContactsManager(HandlerWindow source){
        _source = source;
    }

    public void InitializeSource(){
        var touchpadExists = TouchpadHelper.Exists();
        Debug.WriteLine("Touchpad exists: " + touchpadExists);
        if(!touchpadExists){
            _source.OnTouchpadInitialized(false, false);
            return;
        }
        
        _targetSource = PresentationSource.FromVisual(_source) as HwndSource;
        _targetSource?.AddHook(WindowProcess);

        var success =  _targetSource != null && TouchpadHelper.RegisterInput(_targetSource.Handle);
        _source.OnTouchpadInitialized(true, success);
    }

    // WindowProc Listener
    private IntPtr WindowProcess(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled){
        switch(message){
            case TouchpadHelper.WM_INPUT:
                var contacts = TouchpadHelper.ParseInput(lParam);
                RegisterTouchpadContacts(contacts);
                break;
        }

        return IntPtr.Zero;
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