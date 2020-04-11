// GENERATED AUTOMATICALLY FROM 'Assets/Scripts/InputSetups/InputMaster.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @InputMaster : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @InputMaster()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""InputMaster"",
    ""maps"": [
        {
            ""name"": ""CarControls"",
            ""id"": ""b936cc16-adeb-4540-87b9-5138444331a5"",
            ""actions"": [
                {
                    ""name"": ""Throttle"",
                    ""type"": ""Button"",
                    ""id"": ""3a65521f-1f48-42a9-a6bc-e56159076ef4"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": ""Press(behavior=2)""
                },
                {
                    ""name"": ""Steering"",
                    ""type"": ""Button"",
                    ""id"": ""c469739e-1957-4d29-8694-bc301e1462e6"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Reset"",
                    ""type"": ""Button"",
                    ""id"": ""ddc3f4ef-0261-41f2-9298-08ac3268ba9a"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Brake"",
                    ""type"": ""Button"",
                    ""id"": ""781c6b96-2e0b-473c-9ad3-305a79788c8d"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""2234b421-75c2-40fb-8e7c-e84f44d850e9"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Throttle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e7f5d46c-611d-492d-b494-fef97031ad0a"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Throttle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""11c95abd-9a67-42d9-b420-96c18108cc58"",
                    ""path"": ""<Keyboard>/r"",
                    ""interactions"": ""Hold"",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Reset"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""Steering"",
                    ""id"": ""59664bf0-4a43-4e86-84a4-29f731e90edf"",
                    ""path"": ""1DAxis"",
                    ""interactions"": ""Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Steering"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""40e7abee-8419-4659-b5cd-38ad31184c6d"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Steering"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""negative"",
                    ""id"": ""43f5f009-51b6-4b17-b365-5c8121b595dc"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Steering"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""8a64a986-f6ba-4cc0-9ebe-74fee521c4a4"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Steering"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""positive"",
                    ""id"": ""3dce43b7-29e8-414b-a28d-970b9a59daff"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Steering"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""cced2ef5-1948-4986-9484-e6e3558b82e8"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": ""Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Brake"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""34fc5d80-bae5-482f-82de-def0d435375c"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": ""Press(behavior=2)"",
                    ""processors"": """",
                    ""groups"": ""Keyboard"",
                    ""action"": ""Brake"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard"",
            ""bindingGroup"": ""Keyboard"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        },
        {
            ""name"": ""Gamepad"",
            ""bindingGroup"": ""Gamepad"",
            ""devices"": []
        }
    ]
}");
        // CarControls
        m_CarControls = asset.FindActionMap("CarControls", throwIfNotFound: true);
        m_CarControls_Throttle = m_CarControls.FindAction("Throttle", throwIfNotFound: true);
        m_CarControls_Steering = m_CarControls.FindAction("Steering", throwIfNotFound: true);
        m_CarControls_Reset = m_CarControls.FindAction("Reset", throwIfNotFound: true);
        m_CarControls_Brake = m_CarControls.FindAction("Brake", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // CarControls
    private readonly InputActionMap m_CarControls;
    private ICarControlsActions m_CarControlsActionsCallbackInterface;
    private readonly InputAction m_CarControls_Throttle;
    private readonly InputAction m_CarControls_Steering;
    private readonly InputAction m_CarControls_Reset;
    private readonly InputAction m_CarControls_Brake;
    public struct CarControlsActions
    {
        private @InputMaster m_Wrapper;
        public CarControlsActions(@InputMaster wrapper) { m_Wrapper = wrapper; }
        public InputAction @Throttle => m_Wrapper.m_CarControls_Throttle;
        public InputAction @Steering => m_Wrapper.m_CarControls_Steering;
        public InputAction @Reset => m_Wrapper.m_CarControls_Reset;
        public InputAction @Brake => m_Wrapper.m_CarControls_Brake;
        public InputActionMap Get() { return m_Wrapper.m_CarControls; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CarControlsActions set) { return set.Get(); }
        public void SetCallbacks(ICarControlsActions instance)
        {
            if (m_Wrapper.m_CarControlsActionsCallbackInterface != null)
            {
                @Throttle.started -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnThrottle;
                @Throttle.performed -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnThrottle;
                @Throttle.canceled -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnThrottle;
                @Steering.started -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnSteering;
                @Steering.performed -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnSteering;
                @Steering.canceled -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnSteering;
                @Reset.started -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnReset;
                @Reset.performed -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnReset;
                @Reset.canceled -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnReset;
                @Brake.started -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnBrake;
                @Brake.performed -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnBrake;
                @Brake.canceled -= m_Wrapper.m_CarControlsActionsCallbackInterface.OnBrake;
            }
            m_Wrapper.m_CarControlsActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Throttle.started += instance.OnThrottle;
                @Throttle.performed += instance.OnThrottle;
                @Throttle.canceled += instance.OnThrottle;
                @Steering.started += instance.OnSteering;
                @Steering.performed += instance.OnSteering;
                @Steering.canceled += instance.OnSteering;
                @Reset.started += instance.OnReset;
                @Reset.performed += instance.OnReset;
                @Reset.canceled += instance.OnReset;
                @Brake.started += instance.OnBrake;
                @Brake.performed += instance.OnBrake;
                @Brake.canceled += instance.OnBrake;
            }
        }
    }
    public CarControlsActions @CarControls => new CarControlsActions(this);
    private int m_KeyboardSchemeIndex = -1;
    public InputControlScheme KeyboardScheme
    {
        get
        {
            if (m_KeyboardSchemeIndex == -1) m_KeyboardSchemeIndex = asset.FindControlSchemeIndex("Keyboard");
            return asset.controlSchemes[m_KeyboardSchemeIndex];
        }
    }
    private int m_GamepadSchemeIndex = -1;
    public InputControlScheme GamepadScheme
    {
        get
        {
            if (m_GamepadSchemeIndex == -1) m_GamepadSchemeIndex = asset.FindControlSchemeIndex("Gamepad");
            return asset.controlSchemes[m_GamepadSchemeIndex];
        }
    }
    public interface ICarControlsActions
    {
        void OnThrottle(InputAction.CallbackContext context);
        void OnSteering(InputAction.CallbackContext context);
        void OnReset(InputAction.CallbackContext context);
        void OnBrake(InputAction.CallbackContext context);
    }
}
