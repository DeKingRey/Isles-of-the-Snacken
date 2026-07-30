using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class objectposition : MonoBehaviour
{
    void Start()
    {

    }

    void Update()
    {
        Shader.SetGlobalVector("_objectposition", new Vector4(
            transform.position.x,
            transform.position.y,
            transform.position.z,
            transform.localScale.x
        ));
    
    }
}