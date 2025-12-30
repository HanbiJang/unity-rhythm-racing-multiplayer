using PathCreation.Examples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpwanerFollower : PathFollower
{
    [SerializeField]
    public float GapBetweenPlayer = 30f;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        distanceTravelled += GapBetweenPlayer;
    }

}
