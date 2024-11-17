using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ibralogue.UI
{
    public class ChoiceButton : MonoBehaviour
    {
        private Button _button;

        public UnityEvent OnChoiceClick { get; set; } = new UnityEvent();

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnChoiceClick.Invoke);

            // TODO: In the future, all choice button handling (including setting values)
            // will be done through this class.
        }
    }
}