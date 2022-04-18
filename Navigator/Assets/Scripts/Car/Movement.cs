using UnityEngine;
using UnityEngine.AI;

public class Movement : MonoBehaviour
{
    public float Speed = 2f;

    #region SERIALIZE_FIELDS
    [SerializeField] private GameObject _hornLight;
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [SerializeField] private CharacterController _characterController;
    #endregion

    private int _currentNumPoint = -1;

    // Update is called once per frame
    void Update()
    {
        switch (RouteChecker.Instance.TMove)
        {
            case TypeMove.Arrows:
                _currentNumPoint = -1;
                Quaternion rotation = transform.rotation;
                rotation.eulerAngles = Vector3.zero;
                transform.rotation = rotation;
                Moving();
                break;
            case TypeMove.Navigator:
                if ((Input.GetAxis("Horizontal") != 0f) || (Input.GetAxis("Vertical") != 0f))
                {
                    RouteChecker.Instance.TMove = TypeMove.Arrows;
                    GetComponent<CarSend>().SendInfoMoving(TypeMove.Arrows);
                    _navMeshAgent.isStopped = true;
                    break;
                }
                Autopilot();
                break;
        }
    }

    #region PRIVATE_METHODS
    private void Moving()
    {
        float x = Input.GetAxis("Horizontal") * Speed;
        float z = Input.GetAxis("Vertical") * Speed;

        Vector3 dir = transform.right * x + transform.forward * z;
        _characterController?.Move(dir * Time.deltaTime);
    }

    private void Autopilot()
    {
        _navMeshAgent.isStopped = false;
        if (!_navMeshAgent.hasPath)
        {
            _currentNumPoint++;
            Vector3 dir = RouteChecker.Instance.GetNextPoint(ref _currentNumPoint);
            if (_currentNumPoint != -1)
            {
                dir.y = transform.position.y;

                _navMeshAgent.SetDestination(dir);
            }
        }
    }
    #endregion

    public void Horn(bool status)
    {
        _hornLight.SetActive(status);
    }
}