using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region ENUMS
public enum TypeMove
{
    Arrows = 1,
    Navigator = 2
}
#endregion

public class RouteChecker : MonoBehaviour
{
    public TypeMove TMove = TypeMove.Arrows;

    public static RouteChecker Instance;

    #region SERIALIZE_FIELDS
    [SerializeField] private LineRenderer _route;
    [SerializeField] private GameObject _prefRouteDev;
    [SerializeField] private float _checkDist;
    #endregion

    #region PRIVATE_FIELDS
    private GameObject _car;
    private Dictionary<int, List<Vector3>> _smallPointsList;
    private float _stepRoute = 0.1f;
    private bool _isSpawnRouteDev = false;
    private int _currentPoint = 0;
    private int _currentSmallPoint = 0;
    private LineRenderer _currentNoRoute;
    #endregion

    #region MONOEVENTS
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void OnEnable()
    {
        StopAllCoroutines();
    }

    private void Start()
    {
        _car = GameObject.FindGameObjectWithTag("Car");
        SetSmallPoint();
    }
    #endregion

    #region COROUTINES
    private IEnumerator StartDistanceCheck()
    {
        FindNearestPoint();
        CheckDistance();
        yield return new WaitForSeconds(0.25f);
        StartCoroutine(StartDistanceCheck());
    }
    #endregion

    #region PRIVATE_METHODS
    private void SetSmallPoint()
    {
        _smallPointsList = new Dictionary<int, List<Vector3>>();
        for (int i = 0; i < _route.positionCount - 1; i++)
        {
            Vector3 startPoint = _route.GetPosition(i);
            Vector3 endPoint = _route.GetPosition(i + 1);
            Vector3 middlePoint = Vector3.Lerp(startPoint, endPoint, 0.5f);
            int countPoint = (int)((middlePoint - startPoint).magnitude / _stepRoute);

            for (int j = 0; j < countPoint; j++)
            {
                if (!_smallPointsList.ContainsKey(i))
                {
                    _smallPointsList.Add(i, new List<Vector3>());
                }

                if (!_smallPointsList.ContainsKey(i + 1))
                {
                    _smallPointsList.Add(i + 1, new List<Vector3>());
                }

                _smallPointsList[i].Add(Vector3.Lerp(startPoint, middlePoint, (float)j / (float)countPoint));
                _smallPointsList[i + 1].Add(Vector3.Lerp(middlePoint, endPoint, (float)j / (float)countPoint));
            }
        }

        if (_car != null && _route != null)
        {
            StartCoroutine(StartDistanceCheck());
        }
        else
        {
            Debug.LogError("_car == null or _route == null");
        }
    }

    private void FindNearestPoint()
    {
        int numPoint = 0;
        int numSmallPoint = 0;
        float distPoint = _stepRoute * 10;

        for (int i = 0; i < _smallPointsList.Count; i++)
        {
            int j = 0;
            foreach (Vector3 point in _smallPointsList[i])
            {
                float newDist = Vector3.Distance(_car.transform.position, point);

                if (((i == 0) & (j == 0)) || distPoint > newDist)
                {
                    numPoint = i;
                    distPoint = newDist;
                    numSmallPoint = j;
                }
                j++;
            } 
        }
        _currentSmallPoint = numSmallPoint;
        _currentPoint = numPoint;
    }

    private void CheckDistance()
    {
        float dist = 0f;
        List<Vector3> smallPoints = _smallPointsList[_currentPoint];
        Vector2 currentPoint = new Vector2(smallPoints[_currentSmallPoint].x, smallPoints[_currentSmallPoint].z);
        Vector2 carPoint = new Vector2(_car.transform.position.x, _car.transform.position.z);
        if (_currentSmallPoint < smallPoints.Count - 1)
        {
            Vector2 nextPoint = new Vector2(smallPoints[_currentSmallPoint + 1].x, smallPoints[_currentSmallPoint + 1].z); 

            Vector2 dirCurrentToNext =  nextPoint - currentPoint;
            Vector2 dirCurrentToCar =  carPoint - currentPoint;

            float angleBetweenNextAndCar = Vector2.Angle(dirCurrentToNext, dirCurrentToCar);
            angleBetweenNextAndCar *= Mathf.Deg2Rad;
            
            float cosAngleBetweenNextAndCar = Mathf.Cos(angleBetweenNextAndCar);
            float sinAngleBetweenNextAndCar = Mathf.Sin(angleBetweenNextAndCar);

            if (cosAngleBetweenNextAndCar > 0 && cosAngleBetweenNextAndCar < 1)
            {
                dist = sinAngleBetweenNextAndCar * dirCurrentToCar.magnitude;
            }
            else if (cosAngleBetweenNextAndCar < 0 && cosAngleBetweenNextAndCar > -1)
            {
                if (_currentSmallPoint != 0)
                {
                    Vector2 prevPoint = new Vector2(smallPoints[_currentSmallPoint - 1].x, smallPoints[_currentSmallPoint - 1].z);
                    Vector2 dirCurrentToPrev = prevPoint - currentPoint;

                    float angleBetweenPrevAndCar = Vector2.Angle(dirCurrentToPrev, dirCurrentToCar);
                    angleBetweenPrevAndCar *= Mathf.Deg2Rad;
                    float cosAngleBetweenPrevAndCar = Mathf.Cos(angleBetweenPrevAndCar);
                    float sinAngleBetweenPrevAndCar = Mathf.Sin(angleBetweenPrevAndCar);

                    if (cosAngleBetweenPrevAndCar > 0 && cosAngleBetweenPrevAndCar < 1)
                    {
                        dist = sinAngleBetweenPrevAndCar * dirCurrentToCar.magnitude;
                    }
                    else
                    {
                        dist = Vector2.Distance(carPoint, currentPoint);
                    }
                }
                else
                {
                    dist = Vector2.Distance(carPoint, currentPoint);
                }
            }
            else if (cosAngleBetweenNextAndCar == 0)
            {
                dist = Vector2.Distance(carPoint, currentPoint);
            }
        }
        else 
        {
            dist = Vector2.Distance(carPoint, currentPoint);
        }

        if (dist > _checkDist)
        {
            SpawnNoRoutePoint();
        }
        else
        {
            _isSpawnRouteDev = false;
        }
    }

    private void SpawnNoRoutePoint()
    {
        if (!_isSpawnRouteDev)
        {
            _isSpawnRouteDev = true;
            GameObject noRoutePoint = Instantiate(_prefRouteDev);
            _currentNoRoute = noRoutePoint.GetComponent<LineRenderer>();
        }

        _currentNoRoute.positionCount++;
        _currentNoRoute.SetPosition(_currentNoRoute.positionCount - 1, new Vector3(_car.transform.position.x, 0.2f, _car.transform.position.z));
    }
    #endregion

    #region PUBLIC_METHODS
    public Vector3 GetNextPoint(ref int number)
    {
        if (number >= _route.positionCount)
        {
            number = -1;
            return Vector3.zero;
        }
        else
        {
            if (number < _currentPoint)
            {
                number = _currentPoint;
            }
        } 
        
        return _route.GetPosition(number);
    }

    public void Horn_On_Off(bool status)
    {
        if (_car != null)
        {
            _car.GetComponent<Movement>().Horn(status);
        }
    }
    #endregion
}