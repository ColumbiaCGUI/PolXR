using UnityEngine;

public class ConstrainAxes : MonoBehaviour
{
    [Header("Constrain Movement")]
    public bool constrainXPos;
    public bool constrainYPos;
    public bool constrainZPos;
    
    [Header("Constrain Rotation")]
    public bool constrainXRot;
    public bool constrainYRot;
    public bool constrainZRot;
    
    private Vector3 _cachedPosition;
    private Vector3 _cachedRotation;

    private bool _isConstraining = true;

    private void Start() => UpdateCachedTransform();

    public void StartConstraining() => _isConstraining = true;

    public void StopConstraining() => _isConstraining = false;

    private void UpdateCachedTransform()
    {
        _cachedPosition = transform.position;
        _cachedRotation = transform.rotation.eulerAngles;
    }

    private void ConstrainRotation()
    {
        Vector3 constrainedEulerRotation = _cachedRotation;
        
        if (!constrainXRot)
            constrainedEulerRotation.x = transform.eulerAngles.x;
        
        if (!constrainYRot)
            constrainedEulerRotation.y = transform.eulerAngles.y;
        
        if (!constrainZRot)
            constrainedEulerRotation.z = transform.eulerAngles.z;

        transform.rotation = Quaternion.Euler(constrainedEulerRotation);
    }

    private void ConstrainPosition()
    {
        Vector3 constrainedPosition = _cachedPosition;
        
        if (!constrainXPos)
            constrainedPosition.x = transform.position.x;
        
        if (!constrainYPos)
            constrainedPosition.y = transform.position.y;
        
        if (!constrainZPos)
            constrainedPosition.z = transform.position.z;
        
        transform.position = constrainedPosition;
    }
    
    private void LateUpdate()
    {
        if (!_isConstraining) return;
        
        ConstrainPosition();
        ConstrainRotation();
    }

    public void ConstrainToVerticalMovement()
    {
        constrainXPos = true;
        constrainYPos = false;
        constrainZPos = true;
    }
}
