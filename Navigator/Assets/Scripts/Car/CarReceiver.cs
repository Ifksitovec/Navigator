using UnityEngine;
using TMPro;

public class CarReceiver : MonoBehaviour
{
    [SerializeField] private GameObject _problemField;

    #region PRIVATE_FIELDS
    private GameObject _car;
    private CharacterController _controller;
    #endregion

    #region PROPERTIES
    public CharacterController Controller
    {
        get
        {
            if (_controller == null)
            {
                _controller = _car.GetComponent<CharacterController>();
            }
            return _controller;
        }
        set
        {
            _controller = value;
        }
    }
    #endregion

    public static CarReceiver Instance;
    
    #region MONOEVENTS
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        _car = GameObject.FindGameObjectWithTag("Car");
    }
    #endregion

    #region PUBLIC_METHODS
    public void SetNewPosition(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (_car != null)
        {
            _car.transform.position = position;
            _car.transform.rotation = rotation;
            _car.transform.localScale = scale;
        }
        else 
        {
            Debug.Log("Car is Null");
        }
    }

    public void ProblemInfo(TypeProblem typeProblem, bool status)
    {
        Debug.Log(typeProblem);
        if (status)
        {
            _problemField?.SetActive(true);
            switch (typeProblem)
            {
                case TypeProblem.PressT:
                    _problemField.GetComponent<TextMeshProUGUI>().text = "Нажата клавиша T!";
                    
                    break;
                default:
                    _problemField.GetComponent<TextMeshProUGUI>().text = "Неизвестная неисправность!";
                    break;
            }
        }
        else
        {
            _problemField?.SetActive(false);
        }
    }
    #endregion
}
