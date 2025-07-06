using DefaultNamespace;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class WeakSerialize : MonoBehaviour {
    // public GameObject myAsset;

    // public SerializeByPath<GameObject> myGameObject;
    
    // public SerializeByPath<WeakSerialize> myself;
    
    [LabelText("hahaha")]
    [GUIColor(1f, 0f, 0f)]
    public SerializeByPath<TestScript> testScript;
    
    // public TestScript originTestScript;
}
