using UnityEngine;
using UnityEngine.UI;

namespace OneClickLocalization.Demo.Scripts
{
	public class PopupMessage : MonoBehaviour {

		public Text message;

		// Use this for initialization
		void Start () {
	
		}
	
		// Update is called once per frame
		void Update () {
	
		}

		public void Close()
		{
			gameObject.SetActive(false);
		}
	}
}
