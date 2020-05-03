using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ControlAction { Throttle, Steering, Reset, ResetToCheckpoint, Brake };

public class ControlManager : MonoBehaviour
{
    public InputMaster controls;

    public CarController cc;
    public GameObject buttonObject;

    InputActionRebindingExtensions.RebindingOperation currentRebind;

    void Awake()
    {
        controls = new InputMaster();

        EnableControls();
    }

    public void SetCar(CarController carController)
    {
        cc = carController;
    }

    public void EnableControls()
    {
        controls.CarControls.Throttle.performed += context => SetCarAccelerationInput(context);
        controls.CarControls.Brake.performed += context => SetCarBrakingInput(context);
        controls.CarControls.Steering.performed += context => SetCarSteeringInput(context);

        controls.CarControls.Reset.performed += context => ActivateCarReset(context);
        controls.CarControls.ResetToCheckpoint.performed += context => ActivateCarResetToCheckpoint(context);
    }


    public void RebindThrottle(GameObject g)
    {
        if (currentRebind != null) return;
        
        currentRebind = RebindStart(controls.CarControls.Throttle, g);
    }

    public void RebindBrake(GameObject g)
    {
        if (currentRebind != null) return;

        currentRebind = RebindStart(controls.CarControls.Brake, g);
    }

    public void RebindSteeringLeft(GameObject g)
    {
        if (currentRebind != null) return;

        currentRebind = RebindStart(controls.CarControls.Steering, g, 1);
    }

    public void RebindSteeringRight(GameObject g)
    {
        if (currentRebind != null) return;

        currentRebind = RebindStart(controls.CarControls.Steering, g, 3);
    }

    public void RebindRest(GameObject g)
    {
        if (currentRebind != null) return;

        currentRebind = RebindStart(controls.CarControls.Reset, g);
    }

    public void RebindResetToCheckpoint(GameObject g)
    {
        if (currentRebind != null) return;

        currentRebind = RebindStart(controls.CarControls.ResetToCheckpoint, g);
    }

    public InputActionRebindingExtensions.RebindingOperation RebindStart(InputAction inputAction, GameObject buttonObject)
    {
        return RebindStart(inputAction, buttonObject, 0);
    }

    public InputActionRebindingExtensions.RebindingOperation RebindStart(InputAction inputAction, GameObject buttonObject, int bindingID)
    {
        MenuController.SetOthersStateAtLevel(false, buttonObject);
        inputAction.Disable();

        InputActionRebindingExtensions.RebindingOperation ro = inputAction.PerformInteractiveRebinding().WithTargetBinding(bindingID);

        TextMeshProUGUI txt = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        txt.text = txt.text.Split(new string[] { " -" }, StringSplitOptions.RemoveEmptyEntries)[0] + " - ?";

        ro.Start();

        ro.OnComplete((x) =>
        {
            ro.Dispose();
            ro.action.Enable();

            txt.text = GetUserDisplay(inputAction, bindingID);

            MenuController.SetOthersStateAtLevel(true, buttonObject);

            currentRebind = null;
        });

        return ro;
    }

    string GetUserDisplay(InputAction inputAction)
    {
        return GetUserDisplay(inputAction, 0);
    }

    string GetUserDisplay(InputAction inputAction, int bindingID)
    {
        string keyNames = "";

        List<InputBinding> sameNameBindings = new List<InputBinding>();

        foreach (InputBinding ib in inputAction.bindings)
        {
            if (ib.name == inputAction.bindings[bindingID].name)
            {
                sameNameBindings.Add(ib);
            }
        }

        for (int i = 0; i < sameNameBindings.Count; ++i)
        {
            keyNames += sameNameBindings[i].ToDisplayString();

            if (i != sameNameBindings.Count - 1)
            {
                keyNames += ", ";
            }
        }

        string steer_dir = "";

        if(inputAction.name == "Steering")
        {
            if(inputAction.bindings[bindingID].name.ToLower() == "positive")
            {
                steer_dir = " Right";
            } else
            {
                steer_dir = " Left";
            }
        }

        string actionName = inputAction.name;

        if (actionName == "Throttle")
        {
            actionName = "Accelerate";
        }

        if (actionName == "Reset")
        {
            actionName = "Flip Car";
        }

        if (actionName == "ResetToCheckpoint")
        {
            actionName = "Reset";
        }

        return actionName + steer_dir + " - " + keyNames;
    }

    public void UpdateTextToControlName(TextMeshProUGUI ui, ControlAction ca, int bindingID)
    {

        InputAction action = new InputAction();

        switch(ca)
        {
            case ControlAction.Throttle:
                action = controls.CarControls.Throttle;
                break;
            case ControlAction.Brake:
                action = controls.CarControls.Brake;
                break;
            case ControlAction.Steering:
                action = controls.CarControls.Steering;
                break;
            case ControlAction.Reset:
                action = controls.CarControls.Reset;
                break;
            case ControlAction.ResetToCheckpoint:
                action = controls.CarControls.ResetToCheckpoint;
                break;
        }

        ui.text = GetUserDisplay(action, bindingID);
    }

    void SetCarAccelerationInput(InputAction.CallbackContext context)
    {
        if (cc == null) return;
        cc.accelerationInput = context.ReadValue<float>();
    }

    void SetCarBrakingInput(InputAction.CallbackContext context)
    {
        if (cc == null) return;
        cc.brakingInput = context.ReadValue<float>();
    }

    void SetCarSteeringInput(InputAction.CallbackContext context)
    {
        if (cc == null) return;
        cc.steeringInput = context.ReadValue<float>();
    }

    void ActivateCarReset(InputAction.CallbackContext context)
    {
        if (cc == null) return;
        cc.Reset();
    }

    void ActivateCarResetToCheckpoint(InputAction.CallbackContext context)
    {
        if (cc == null) return;
        cc.ResetToCheckpoint();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisabled()
    {
        controls.Disable();
    }
}
