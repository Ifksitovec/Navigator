using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VEXNetwork
{
    public class UnityThread : MonoBehaviour
    {
        #region PRIVATE_FIELDS
        //our (singleton) instance
        private static UnityThread _instance = null;

        ////////////////////////////////////////////////UPDATE IMPL////////////////////////////////////////////////////////
        //Holds actions received from another Thread. Will be coped to actionCopiedQueueUpdateFunc then executed from there
        private static List<System.Action> _actionQueuesUpdateFunc = new List<Action>();

        //holds Actions copied from actionQueuesUpdateFunc to be executed
        List<System.Action> _actionCopiedQueueUpdateFunc = new List<System.Action>();

        // Used to know if whe have new Action function to execute. This prevents the use of the lock keyword every frame
        private volatile static bool _noActionQueueToExecuteUpdateFunc = true;
        #endregion

        #region PUBLIC_METHODS
        //Used to initialize UnityThread. Call once before any function here
        public static void InitUnityThread(bool visible = false)
        {
            if (_instance != null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                // add an invisible game object to the scene
                GameObject obj = new GameObject("MainThreadExecuter");
                if (!visible)
                {
                    obj.hideFlags = HideFlags.HideAndDontSave;
                }

                DontDestroyOnLoad(obj);
                _instance = obj.AddComponent<UnityThread>();
            }
        }

        //////////////////////////////////////////////COROUTINE IMPL//////////////////////////////////////////////////////
        public static void ExecuteCoroutine(IEnumerator action)
        {
            if (_instance != null)
            {
                ExecuteInUpdate(() => _instance.StartCoroutine(action));
            }
        }

        ////////////////////////////////////////////UPDATE IMPL////////////////////////////////////////////////////
        public static void ExecuteInUpdate(System.Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            lock (_actionQueuesUpdateFunc)
            {
                _actionQueuesUpdateFunc.Add(action);
                _noActionQueueToExecuteUpdateFunc = false;
            }
        }
        #endregion

        #region MONOEVENTS
        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Update()
        {
            if (_noActionQueueToExecuteUpdateFunc)
            {
                return;
            }

            //Clear the old actions from the actionCopiedQueueUpdateFunc queue
            _actionCopiedQueueUpdateFunc.Clear();
            lock (_actionQueuesUpdateFunc)
            {
                //Copy actionQueuesUpdateFunc to the actionCopiedQueueUpdateFunc variable
                _actionCopiedQueueUpdateFunc.AddRange(_actionQueuesUpdateFunc);
                //Now clear the actionQueuesUpdateFunc since we've done copying it
                _actionQueuesUpdateFunc.Clear();
                _noActionQueueToExecuteUpdateFunc = true;
            }

            // Loop and execute the functions from the actionCopiedQueueUpdateFunc
            for (int i = 0; i < _actionCopiedQueueUpdateFunc.Count; i++)
            {
                _actionCopiedQueueUpdateFunc[i].Invoke();
            }
        }

        public void OnDisable()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion
    }
}
