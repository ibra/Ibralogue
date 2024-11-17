using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ibralogue.UI
{
    public class ChoiceButton : MonoBehaviour
    {
        private Button _button;

        public string Name { get; set; }
        public string LeadingConversation { get; set; }

        public UnityEvent ClickEvent { get; set; }
        public UnityAction ClickCallback { get; set; }

        private void Start()
        {
            _button = GetComponent<Button>();
        }
    }
}