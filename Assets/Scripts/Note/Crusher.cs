using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crusher : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("PickupItem")) 
        {
            collision.gameObject.GetComponent<PickupScript>().OnPicked(transform.position);
        }
    }
}
