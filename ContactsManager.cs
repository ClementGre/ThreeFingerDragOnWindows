using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;

namespace ThreeFingersDragOnWindows; 

public class ContactsManager<T> where T : Window, IContactsManager {

    private HwndSource _targetSource;
    private readonly T _source;
    public ContactsManager(T  source){
        _source = source;
    }
    
    public void InitializeSource(){
        _targetSource = PresentationSource.FromVisual(_source) as HwndSource;
        _targetSource?.AddHook(WndProc);

        var touchpadExists = TouchpadHelper.Exists();
        var success = touchpadExists && _targetSource != null && TouchpadHelper.RegisterInput(_targetSource.Handle);
        
        _source.OnTouchpadInitialized(touchpadExists, success);
    }
    
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled){
        switch(msg){
            case TouchpadHelper.WM_INPUT:
                var contacts = TouchpadHelper.ParseInput(lParam);
                RegisterTouchpadContacts(contacts);
                break;
        }
        return IntPtr.Zero;
    }
    
    private void RegisterTouchpadContacts(TouchpadContact[] contacts){
        foreach(var contact in contacts) RegisterTouchpadContact(contact);
    }
    private readonly List<TouchpadContact> _lastContacts = new();
    private void RegisterTouchpadContact(TouchpadContact contact){
        foreach(var lastContact in _lastContacts)
            if(lastContact.ContactId == contact.ContactId){
                // A contact is registered twice: send the event with the list of all contacts
                _source.OnTouchpadContact(_lastContacts.ToArray());
                _lastContacts.Clear();
                break;
            }

        // Add the contact to the list
        _lastContacts.Add(contact);
    }
}

public interface IContactsManager {
    // Called when the touchpad is detected and the events handlers are registered (or not)
    public void OnTouchpadInitialized(bool touchpadExists, bool touchpadRegistered);
    // Called when a new set of contacts has been registered
    public void OnTouchpadContact(TouchpadContact[] contacts);
}