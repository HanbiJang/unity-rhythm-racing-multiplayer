using PathCreation.Examples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpwanerFollower : PathFollower
{
    [SerializeField]
    public float GapBetweenPlayer = 30f;
    private PlayerFollower playerFollower;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        playerFollower = FindObjectOfType<PlayerFollower>();
        if (playerFollower != null)
        {
            speed = playerFollower.speed;
        }
        if (pathCreator != null && playerFollower != null)
        {
            float playerDistance = pathCreator.path.GetClosestDistanceAlongPath(playerFollower.transform.position);
            distanceTravelled = playerDistance + GapBetweenPlayer;
            transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
            transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);
        }
        else
        {
            distanceTravelled += GapBetweenPlayer;
        }
    }

    protected override void Update()
    {
        if (playerFollower == null)
        {
            playerFollower = FindObjectOfType<PlayerFollower>();
            if (playerFollower != null)
            {
                speed = playerFollower.speed;
            }
        }

        if (pathCreator != null && playerFollower != null)
        {
            float playerDistance = pathCreator.path.GetClosestDistanceAlongPath(playerFollower.transform.position);
            distanceTravelled = playerDistance + GapBetweenPlayer;
            transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled, endOfPathInstruction);
            transform.rotation = pathCreator.path.GetRotationAtDistance(distanceTravelled, endOfPathInstruction);
            return;
        }

        base.Update();
    }

}
