using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerEventManager : MonoBehaviour
{
    public static PlayerEventManager Instance;

    public static event Action MoveEvent;
    public static event Action RevealEvent;
    public static event Action FlagEvent;
    public static event Action ExplodeEvent;
    public static event Action InteractEvent;    

    private void Awake()
    {
        Instance = this;
    }

    public void TriggerMoveEvent()
    {
        MoveEvent?.Invoke();
    }

    public static void TriggerRevealEvent(Vector3 cellPosition)
    {
        RevealEvent?.Invoke();
    }
    public static void TriggerFlagEvent() { FlagEvent?.Invoke(); }
    public static void TriggerExplodeEvent()
    {
        ExplodeEvent?.Invoke(); 
    }
    public static void TriggerInteractEvent() { InteractEvent?.Invoke(); }
}
