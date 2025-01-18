using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; private set; }

    private InputActions inputActions;

    private void Awake() {
        //Create a singleton for this, ensures that only one exists
        if(Instance != null) {
            Destroy(this);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        inputActions = new InputActions();
    }

    public InputActions GetInputActions() {
        if(inputActions != null) {
            return inputActions;
        }
        Debug.LogWarning("Input actions was not found!");
        return null;
    }

    private void OnEnable() {
        inputActions.Enable();
    }

    private void OnDisable() {
        inputActions.Disable();
    }
}