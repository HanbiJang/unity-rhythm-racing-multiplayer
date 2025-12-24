using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PickupScript : MonoBehaviour
{
    public bool bPicked;
    public int nodeType = 0; // 노드 타입 (0: ObjectA, 1: ObjectB, 2: ObjectC, 3: AFail, 4: BFail, 5: CFail)
    public abstract void OnPicked(Vector3 CrusherPosition);
    public abstract void OnMissed();
}