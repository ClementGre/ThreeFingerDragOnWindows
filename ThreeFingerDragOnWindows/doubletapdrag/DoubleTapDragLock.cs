using System;
using System.Collections.Generic;
using System.Timers;
using ThreeFingerDragEngine.utils;
using ThreeFingerDragOnWindows.drag;
using ThreeFingerDragOnWindows.settings;
using ThreeFingerDragOnWindows.touchpad;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.doubletapdrag;

/// <summary>
/// Recognizes a one-finger double-tap drag-lock gesture while Windows continues
/// to provide the native one-finger pointer movement.
/// </summary>
public sealed class DoubleTapDragLock : IDisposable{
    private const int MaximumTapDurationMs = 250;
    private const int DoubleTapIntervalMs = 500;
    private const float MaximumTapMovePixels = 8;
    private const float MaximumTapMoveRaw = 80;
    private const float DragStartMovePixels = 4;
    private const float DragStartMoveRaw = 40;

    private readonly object _stateLock = new();
    private readonly Dictionary<IntPtr, DeviceState> _deviceStates = new();
    private readonly DragButtonCoordinator _dragButtonCoordinator;
    private readonly Timer _stateTimer;
    private bool _enabled;
    private bool _disposed;

    public DoubleTapDragLock(DragButtonCoordinator dragButtonCoordinator, bool enabled){
        _dragButtonCoordinator = dragButtonCoordinator;
        _enabled = enabled;
        _stateTimer = new Timer(20){ AutoReset = true };
        _stateTimer.Elapsed += OnStateTimerElapsed;
        _stateTimer.Start();
        Logger.Log($"DTD: initialized enabled={enabled}");
    }

    public void OnTouchpadContact(IntPtr currentDevice, TouchpadContact[] contacts){
        long now = Environment.TickCount64;

        lock(_stateLock){
            if(_disposed || !_enabled) return;

            var deviceState = GetDeviceState(currentDevice);
            if(contacts.Length == 0){
                HandleContactReleased(currentDevice, deviceState, now, "explicit-zero-contact");
                return;
            }

            if(contacts.Length != 1){
                SuppressForMultipleContacts(currentDevice, deviceState, contacts.Length, now);
                return;
            }

            // A short no-report gap is how some touchpads signal that the old
            // contact ended. The periodic timer can miss a gap that is only a
            // few milliseconds longer than the shared release threshold when the
            // next report arrives between timer ticks. Resolve that release
            // before treating this report as part of the existing contact.
            if(deviceState.ContactActive &&
               now - deviceState.LastContactAt >= TouchpadTiming.ContactReleaseThresholdMs){
                HandleContactReleased(
                    currentDevice,
                    deviceState,
                    deviceState.LastContactAt,
                    "report-gap");
            }

            if(deviceState.State == GestureState.SuppressedUntilRelease){
                deviceState.ContactActive = true;
                deviceState.LastContactAt = now;
                return;
            }

            TouchpadContact contact = contacts[0];
            if(!deviceState.ContactActive){
                BeginContact(currentDevice, deviceState, contact, now);
            } else if(deviceState.ContactId != contact.ContactId){
                Logger.Log($"DTD: CONTACT_ID_CHANGED device={FormatDevice(currentDevice)} old={deviceState.ContactId} new={contact.ContactId}");
                HandleContactReleased(currentDevice, deviceState, now, "contact-id-changed");
                BeginContact(currentDevice, deviceState, contact, now);
            }

            deviceState.LastContactAt = now;
            UpdateMovement(deviceState, contact);

            if(deviceState.State == GestureState.TouchingSecondTap &&
               (deviceState.MaximumPixelDistance >= DragStartMovePixels ||
                deviceState.MaximumRawDistance >= DragStartMoveRaw)){
                if(_dragButtonCoordinator.TryAcquire(
                       DragOwner.DoubleTap,
                       SettingsData.ThreeFingerDragButtonType.LEFT)){
                    deviceState.State = GestureState.Dragging;
                    Logger.Log(
                        $"DTD: DRAG_START device={FormatDevice(currentDevice)} " +
                        $"movePx={deviceState.MaximumPixelDistance:F1} moveRaw={deviceState.MaximumRawDistance:F1}");
                } else{
                    Logger.Log($"DTD: DRAG_START_BLOCKED device={FormatDevice(currentDevice)}");
                    Reset(deviceState);
                }
            }
        }
    }

    public void OnTouchpadReleased(IntPtr currentDevice){
        long now = Environment.TickCount64;
        lock(_stateLock){
            if(_disposed || !_enabled) return;
            HandleContactReleased(currentDevice, GetDeviceState(currentDevice), now, "explicit-zero-contact");
        }
    }

    public void SetEnabled(bool enabled){
        lock(_stateLock){
            if(_disposed || _enabled == enabled) return;
            _enabled = enabled;
            if(!enabled) CancelAll("feature-disabled");
            Logger.Log($"DTD: enabled={enabled}");
        }
    }

    public void Cancel(string reason){
        lock(_stateLock){
            if(_disposed) return;
            CancelAll(reason);
        }
    }

    private void BeginContact(IntPtr currentDevice, DeviceState deviceState, TouchpadContact contact, long now){
        deviceState.ContactActive = true;
        deviceState.ContactId = contact.ContactId;
        deviceState.ContactStartedAt = now;
        deviceState.LastContactAt = now;
        deviceState.StartRawContact = contact;
        deviceState.StartCursorPosition = MouseOperations.GetCursorPosition();
        deviceState.MaximumPixelDistance = 0;
        deviceState.MaximumRawDistance = 0;

        switch(deviceState.State){
            case GestureState.WaitingForSecondTap when now - deviceState.FirstTapReleasedAt <= DoubleTapIntervalMs:
                deviceState.State = GestureState.TouchingSecondTap;
                Logger.Log(
                    $"DTD: SECOND_TOUCH device={FormatDevice(currentDevice)} " +
                    $"gapMs={now - deviceState.FirstTapReleasedAt}");
                break;

            case GestureState.LockedWaiting when now <= deviceState.LockDeadline:
                deviceState.State = GestureState.Dragging;
                Logger.Log(
                    $"DTD: RESUME device={FormatDevice(currentDevice)} " +
                    $"gapMs={now - deviceState.FingersReleasedAt}");
                break;

            case GestureState.LockedWaiting:
                Logger.Log(
                    $"DTD: TIMEOUT device={FormatDevice(currentDevice)} " +
                    $"elapsedMs={now - deviceState.FingersReleasedAt}");
                _dragButtonCoordinator.Release(DragOwner.DoubleTap, "double-tap-timeout-before-contact");
                deviceState.State = GestureState.TouchingFirstTap;
                break;

            default:
                deviceState.State = GestureState.TouchingFirstTap;
                break;
        }
    }

    private static void UpdateMovement(DeviceState deviceState, TouchpadContact contact){
        IntMousePoint cursorPosition = MouseOperations.GetCursorPosition();
        float pixelDistance = Distance(
            cursorPosition.x - deviceState.StartCursorPosition.x,
            cursorPosition.y - deviceState.StartCursorPosition.y);
        float rawDistance = contact.GetDist2D(deviceState.StartRawContact);

        deviceState.MaximumPixelDistance = Math.Max(deviceState.MaximumPixelDistance, pixelDistance);
        deviceState.MaximumRawDistance = Math.Max(deviceState.MaximumRawDistance, rawDistance);
    }

    private void HandleContactReleased(
        IntPtr currentDevice,
        DeviceState deviceState,
        long now,
        string source){
        if(!deviceState.ContactActive) return;

        deviceState.ContactActive = false;
        long duration = Math.Max(0, now - deviceState.ContactStartedAt);

        switch(deviceState.State){
            case GestureState.TouchingFirstTap:
                if(duration <= MaximumTapDurationMs &&
                   deviceState.MaximumPixelDistance <= MaximumTapMovePixels &&
                   deviceState.MaximumRawDistance <= MaximumTapMoveRaw){
                    deviceState.State = GestureState.WaitingForSecondTap;
                    deviceState.FirstTapReleasedAt = now;
                    Logger.Log(
                        $"DTD: FIRST_TAP device={FormatDevice(currentDevice)} source={source} " +
                        $"durationMs={duration} movePx={deviceState.MaximumPixelDistance:F1} " +
                        $"moveRaw={deviceState.MaximumRawDistance:F1}");
                } else{
                    Logger.Log(
                        $"DTD: FIRST_TOUCH_REJECTED device={FormatDevice(currentDevice)} source={source} " +
                        $"durationMs={duration} movePx={deviceState.MaximumPixelDistance:F1} " +
                        $"moveRaw={deviceState.MaximumRawDistance:F1}");
                    Reset(deviceState);
                }
                break;

            case GestureState.TouchingSecondTap:
                Logger.Log(
                    $"DTD: DOUBLE_TAP_NO_DRAG device={FormatDevice(currentDevice)} source={source} " +
                    $"durationMs={duration} movePx={deviceState.MaximumPixelDistance:F1} " +
                    $"moveRaw={deviceState.MaximumRawDistance:F1}");
                Reset(deviceState);
                break;

            case GestureState.Dragging:
                deviceState.State = GestureState.LockedWaiting;
                deviceState.FingersReleasedAt = now;
                int releaseDelay = Math.Max(
                    App.SettingsData.DoubleTapDragLockReleaseDelay,
                    TouchpadTiming.ContactReleaseThresholdMs);
                deviceState.LockDeadline = now + releaseDelay;
                Logger.Log(
                    $"DTD: FINGERS_RELEASED device={FormatDevice(currentDevice)} source={source} " +
                    $"lockMs={releaseDelay}");
                break;

            default:
                Reset(deviceState);
                break;
        }
    }

    private void SuppressForMultipleContacts(
        IntPtr currentDevice,
        DeviceState deviceState,
        int contactCount,
        long now){
        if(deviceState.State is GestureState.Dragging or GestureState.LockedWaiting){
            _dragButtonCoordinator.Release(DragOwner.DoubleTap, "double-tap-multitouch");
        }
        if(deviceState.State != GestureState.SuppressedUntilRelease){
            Logger.Log(
                $"DTD: CANCEL_MULTITOUCH device={FormatDevice(currentDevice)} contacts={contactCount}");
        }
        deviceState.State = GestureState.SuppressedUntilRelease;
        deviceState.ContactActive = true;
        deviceState.LastContactAt = now;
    }

    private void OnStateTimerElapsed(object source, ElapsedEventArgs e){
        long now = Environment.TickCount64;

        lock(_stateLock){
            if(_disposed) return;

            if(_dragButtonCoordinator.ReconcileExternalRelease(DragOwner.DoubleTap)){
                Logger.Log("DTD: EXTERNAL_RELEASE");
                foreach(var (device, deviceState) in _deviceStates){
                    if(deviceState.ContactActive){
                        deviceState.State = GestureState.SuppressedUntilRelease;
                        deviceState.MaximumPixelDistance = 0;
                        deviceState.MaximumRawDistance = 0;
                        Logger.Log(
                            $"DTD: SUPPRESS_UNTIL_RELEASE device={FormatDevice(device)} reason=external-release");
                    } else{
                        Reset(deviceState);
                    }
                }
            }

            foreach(var (device, deviceState) in _deviceStates){
                if(deviceState.ContactActive && now - deviceState.LastContactAt >= TouchpadTiming.ContactReleaseThresholdMs){
                    HandleContactReleased(device, deviceState, deviceState.LastContactAt, "silence-timeout");
                }

                if(deviceState.State == GestureState.WaitingForSecondTap &&
                   now - deviceState.FirstTapReleasedAt > DoubleTapIntervalMs){
                    Reset(deviceState);
                } else if(deviceState.State == GestureState.LockedWaiting && now >= deviceState.LockDeadline){
                    Logger.Log(
                        $"DTD: TIMEOUT device={FormatDevice(device)} " +
                        $"elapsedMs={now - deviceState.FingersReleasedAt}");
                    _dragButtonCoordinator.Release(DragOwner.DoubleTap, "double-tap-timeout");
                    Reset(deviceState);
                }
            }
        }
    }

    private DeviceState GetDeviceState(IntPtr currentDevice){
        if(!_deviceStates.TryGetValue(currentDevice, out var deviceState)){
            deviceState = new DeviceState();
            _deviceStates.Add(currentDevice, deviceState);
        }
        return deviceState;
    }

    private static void Reset(DeviceState deviceState){
        deviceState.State = GestureState.Idle;
        deviceState.ContactActive = false;
        deviceState.MaximumPixelDistance = 0;
        deviceState.MaximumRawDistance = 0;
    }

    private void CancelAll(string reason){
        _dragButtonCoordinator.Release(DragOwner.DoubleTap, reason);
        foreach(var deviceState in _deviceStates.Values) Reset(deviceState);
    }

    private static float Distance(int x, int y){
        return (float)Math.Sqrt((double)x * x + (double)y * y);
    }

    private static string FormatDevice(IntPtr device){
        return $"0x{device.ToInt64():X}";
    }

    public void Dispose(){
        lock(_stateLock){
            if(_disposed) return;
            CancelAll("double-tap-dispose");
            _disposed = true;
            _stateTimer.Stop();
            _stateTimer.Dispose();
            _deviceStates.Clear();
        }
    }

    private sealed class DeviceState{
        public GestureState State = GestureState.Idle;
        public bool ContactActive;
        public int ContactId;
        public long ContactStartedAt;
        public long LastContactAt;
        public long FirstTapReleasedAt;
        public long FingersReleasedAt;
        public long LockDeadline;
        public TouchpadContact StartRawContact;
        public IntMousePoint StartCursorPosition;
        public float MaximumPixelDistance;
        public float MaximumRawDistance;
    }

    private enum GestureState{
        Idle,
        TouchingFirstTap,
        WaitingForSecondTap,
        TouchingSecondTap,
        Dragging,
        LockedWaiting,
        SuppressedUntilRelease,
    }
}
