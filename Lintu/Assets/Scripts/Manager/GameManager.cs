﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourSingleton<GameManager>
{
    public PlayerControls GameInput;
    bool HasWon = false;

    public override void Awake()
    {
        base.Awake();
        GameInput = new PlayerControls();
        GameInput.Enable();
    }

    private void Update()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public bool Won
    {
        get { return HasWon; }
        set { HasWon = value; }
    }

    public PlayerControls Input
    {
        get { return GameInput; }
    }
}
