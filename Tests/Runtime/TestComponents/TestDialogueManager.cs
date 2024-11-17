using Ibralogue.Parser;
using Ibralogue.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ibralogue.Tests
{
    public class TestDialogueManager : DialogueManagerBase
    {
        public float ScrollSpeed {  get => scrollSpeed; set => scrollSpeed = value; }
        public TextMeshProUGUI NameText { get => nameText; set => nameText = value; }
        public TextMeshProUGUI SentenceText { get => sentenceText; set => sentenceText = value; }
        public Image SpeakerPortrait { get => speakerPortrait; set => speakerPortrait = value; }
        public Transform ChoiceButtonHolder { get => choiceButtonHolder; set => choiceButtonHolder = value; }
        public ChoiceButton ChoiceButton { get => choiceButton.GetComponent<ChoiceButton>(); set => choiceButton = value.gameObject; }
    }
}

