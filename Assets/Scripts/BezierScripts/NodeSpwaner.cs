using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeSpwaner : MonoBehaviour
{
    [SerializeField]
    List<GameObject> m_NodeList = new List<GameObject>();

    [SerializeField]
    Transform senpai;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) 
        {
            SpawnNodeCentre();
        }

        if (Input.GetKeyDown(KeyCode.J)) 
        {
            SpawnNodeLeft();
        }

        if (Input.GetKeyDown(KeyCode.L)) 
        {
            SpawnNodeRight();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            SpawnNodeCentre(1);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            SpawnNodeLeft(1);
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            SpawnNodeRight(1);
        }
    }

    public GameObject SpawnNodeCentre(int id = 0)
    {
        GameObject go = Instantiate(m_NodeList[id], senpai.position + senpai.rotation * Vector3.left * 0.5f, senpai.rotation);
        return go;
    }
    public GameObject SpawnNodeLeft(int id = 0)
    {
        GameObject go = Instantiate(m_NodeList[id], senpai.position + senpai.rotation * Vector3.left * 0.5f + senpai.rotation * Vector3.down * 3, senpai.rotation);
        return go;
    }
    public GameObject SpawnNodeRight(int id = 0)
    {
        GameObject go = Instantiate(m_NodeList[id], senpai.position + senpai.rotation * Vector3.left * 0.5f + senpai.rotation * Vector3.up * 3, senpai.rotation);
        return go;
    }
}
