using System;
using ThreeFingerDragOnWindows.settings;
using ThreeFingerDragOnWindows.utils;

namespace ThreeFingerDragOnWindows.drag;

public sealed class DragButtonCoordinator{
    private readonly object _stateLock = new();
    private DragOwner _owner = DragOwner.None;
    private SettingsData.ThreeFingerDragButtonType _button = SettingsData.ThreeFingerDragButtonType.NONE;
    private long _acquiredAt;

    public bool TryAcquire(DragOwner owner, SettingsData.ThreeFingerDragButtonType button){
        lock(_stateLock){
            if(button == SettingsData.ThreeFingerDragButtonType.NONE) return false;
            if(_owner != DragOwner.None){
                Logger.Log($"DRAG-BUTTON: acquire blocked owner={owner} activeOwner={_owner}");
                return false;
            }

            MouseOperations.MouseClick(GetButtonDownFlag(button));
            _owner = owner;
            _button = button;
            _acquiredAt = Environment.TickCount64;
            Logger.Log($"DRAG-BUTTON: acquired owner={owner} button={button}");
            return true;
        }
    }

    public bool ReconcileExternalRelease(DragOwner owner){
        lock(_stateLock){
            if(_owner != owner) return false;
            if(Environment.TickCount64 - _acquiredAt < 50) return false;
            if(MouseOperations.IsMouseButtonDown(_button)) return false;

            Logger.Log($"DRAG-BUTTON: external release owner={_owner} button={_button}");
            _owner = DragOwner.None;
            _button = SettingsData.ThreeFingerDragButtonType.NONE;
            _acquiredAt = 0;
            return true;
        }
    }

    public void Release(DragOwner owner, string reason){
        lock(_stateLock){
            if(_owner != owner) return;
            ReleaseCore(reason);
        }
    }

    public void ForceRelease(string reason){
        lock(_stateLock){
            if(_owner == DragOwner.None) return;
            ReleaseCore(reason);
        }
    }

    private void ReleaseCore(string reason){
        MouseOperations.MouseClick(GetButtonUpFlag(_button));
        Logger.Log($"DRAG-BUTTON: released owner={_owner} button={_button} reason={reason}");
        _owner = DragOwner.None;
        _button = SettingsData.ThreeFingerDragButtonType.NONE;
        _acquiredAt = 0;
    }

    private static int GetButtonDownFlag(SettingsData.ThreeFingerDragButtonType button){
        return button switch{
            SettingsData.ThreeFingerDragButtonType.LEFT => MouseOperations.MOUSEEVENTF_LEFTDOWN,
            SettingsData.ThreeFingerDragButtonType.RIGHT => MouseOperations.MOUSEEVENTF_RIGHTDOWN,
            SettingsData.ThreeFingerDragButtonType.MIDDLE => MouseOperations.MOUSEEVENTF_MIDDLEDOWN,
            _ => 0,
        };
    }

    private static int GetButtonUpFlag(SettingsData.ThreeFingerDragButtonType button){
        return button switch{
            SettingsData.ThreeFingerDragButtonType.LEFT => MouseOperations.MOUSEEVENTF_LEFTUP,
            SettingsData.ThreeFingerDragButtonType.RIGHT => MouseOperations.MOUSEEVENTF_RIGHTUP,
            SettingsData.ThreeFingerDragButtonType.MIDDLE => MouseOperations.MOUSEEVENTF_MIDDLEUP,
            _ => 0,
        };
    }
}

public enum DragOwner{
    None,
    ThreeFinger,
    DoubleTap,
}
