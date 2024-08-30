using System;
using UnityEngine;


namespace Utility.UI
{
    public class CustomToggleGroup : MonoBehaviour
    {
        [SerializeField, Tooltip("If true, only allow one of the child values to be selected.")]
        bool _exclusive;
        [SerializeField, Tooltip("What object type do you want to control toggle on.")]
        ToggleGroupType _type = default(ToggleGroupType);

        private void Awake()
        {
            Debug.Log($"Default type: {_type}");
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

    [Serializable]
    public enum ToggleGroupType
    {
        Button, Other
    }
}
