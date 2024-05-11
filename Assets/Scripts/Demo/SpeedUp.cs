using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicGrass;   
public class SpeedUp : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (GetComponent<InteractableObject>().Type != InteractMotionBufferController.GrassInteractorType.Burnning)
        {
            GetComponent<Renderer>().material.color = Random.ColorHSV();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (GetComponent<Rigidbody>().velocity.sqrMagnitude < 10f)
        {
            GetComponent<Rigidbody>().AddForce((Random.onUnitSphere) * 16f);
        }
    }
}
