using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostLights : MonoBehaviour
{
    public GameObject BaseLight;
    // Start is called before the first frame update
    void Start()
    {
        //InvokeRepeating("Clone", 0.1f, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        Clone();
    }

    void Clone()
    {
        GameObject clone = Object.Instantiate(BaseLight);
        clone.transform.position = transform.position;
        //clone.GetComponent<RayTracedMesh>().
        clone.SetActive(true);
    }
}
