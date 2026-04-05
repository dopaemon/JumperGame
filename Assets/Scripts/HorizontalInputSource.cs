using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public interface IHorizontalInputSource
{
    float GetHorizontal();
}

public sealed class CompositeHorizontalInputSource : IHorizontalInputSource
{
    private readonly bool preferAccelerometer;
    private readonly float accelerometerSensitivity;
    private readonly float accelerometerDeadZone;

    public CompositeHorizontalInputSource(bool preferAccelerometer, float accelerometerSensitivity, float accelerometerDeadZone)
    {
        this.preferAccelerometer = preferAccelerometer;
        this.accelerometerSensitivity = accelerometerSensitivity;
        this.accelerometerDeadZone = accelerometerDeadZone;
    }

    public float GetHorizontal()
    {
        float accelerometerInput = ReadAccelerometer();
        if (preferAccelerometer && Mathf.Abs(accelerometerInput) > accelerometerDeadZone)
        {
            return accelerometerInput;
        }

        float buttonInput = ReadButtonInput();
        if (Mathf.Abs(buttonInput) > 0.001f)
        {
            return buttonInput;
        }

        return accelerometerInput;
    }

    private float ReadAccelerometer()
    {
#if ENABLE_INPUT_SYSTEM
        if (Accelerometer.current != null)
        {
            float tilt = Mathf.Clamp(Accelerometer.current.acceleration.ReadValue().x * accelerometerSensitivity, -1f, 1f);
            if (Mathf.Abs(tilt) > accelerometerDeadZone)
            {
                return tilt;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        float legacyTilt = Mathf.Clamp(Input.acceleration.x * accelerometerSensitivity, -1f, 1f);
        if (Mathf.Abs(legacyTilt) > accelerometerDeadZone)
        {
            return legacyTilt;
        }
#endif

        return 0f;
    }

    private float ReadButtonInput()
    {
#if ENABLE_INPUT_SYSTEM
        float horizontal = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }
        }

        if (Mathf.Abs(horizontal) > 0.001f)
        {
            return Mathf.Clamp(horizontal, -1f, 1f);
        }

        if (Gamepad.current != null)
        {
            return Mathf.Clamp(Gamepad.current.leftStick.ReadValue().x, -1f, 1f);
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);
#else
        return 0f;
#endif
    }
}
