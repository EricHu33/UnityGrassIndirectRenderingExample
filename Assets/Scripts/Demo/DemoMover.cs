using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoMover : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        _targetPos = transform.position;
    }

    // Update is called once per frame
    private Vector3 _targetPos;
    private Vector3 _SmoothDamp;
    private float _currentHeight = 4f;
    void Update()
    {
        var targetHeight = transform.position.y;

        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit))
        {
            var terrain = hit.transform.GetComponent<Terrain>();
            if (terrain != null)
            {
                targetHeight = terrain.SampleHeight(hit.point) + _currentHeight;
                _targetPos = new Vector3(transform.position.x, targetHeight, transform.position.z);
            }
        }
    }
    void LateUpdate()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            _currentHeight += Time.deltaTime * 5f;
        }
        if (Input.GetKey(KeyCode.X))
        {
            _currentHeight -= Time.deltaTime * 5f;
        }
        _currentHeight = Mathf.Clamp(_currentHeight, 4f, 30f);
        transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _SmoothDamp, 0.3f);
        var forwardMovement = Input.GetAxisRaw("Vertical");
        if (Input.touchCount > 0)
        {
            forwardMovement = 1f;
        }
        transform.position += Quaternion.LookRotation(
            Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.up) *
            new Vector3(Input.GetAxisRaw("Horizontal"), 0, forwardMovement) * 20f * Time.deltaTime;
    }
}
