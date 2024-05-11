using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SinMover : MonoBehaviour
{
    [FormerlySerializedAs("range")] public float Range = 10;
    private Vector3 _startPosition;
    private float _offset;
    // Start is called before the first frame update
    void Start()
    {
        _startPosition = transform.position;
        _offset = Random.Range(5, 10);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(_startPosition.x, _startPosition.y, _startPosition.z) + transform.forward * Mathf.Sin(_offset + Time.time * 2) * Range;
    }
}
