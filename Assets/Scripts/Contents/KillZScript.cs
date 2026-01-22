using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZScript : MonoBehaviour
{
    //[SerializeField]
    //Transform m_RoadRespawnTransform;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PickupItem")) 
        {
            PickupScript pickupScript = other.gameObject.GetComponent<PickupScript>();
            if (pickupScript != null && !pickupScript.bPicked)
            {
                Debug.Log(name + " : PickupItem trigger (Miss)");
                // 노트를 놓쳤을 때 OnMissed 호출하여 Miss 판정 처리
                pickupScript.OnMissed();
                // OnMissed에서 이미 Destroy를 호출하므로 여기서는 호출하지 않음
                return;
            }
            
            // 이미 처리된 노트는 그냥 파괴
            if (other.gameObject.GetComponent<FloatablePickupScript>() != null)
            {
                Destroy(other.gameObject);
            }
        }

        if (other.CompareTag("RoadBlock")) 
        {
            Debug.Log(name + " : RoadBlock trigger");
            //other.transform.position = m_RoadRespawnTransform.position;
        }
    }
}
