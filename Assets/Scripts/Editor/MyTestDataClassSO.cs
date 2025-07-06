using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Editor {
    [Serializable]
    public class MyTestDataClassSO : ScriptableObject {
        [HideLabel]
        public MyTestDataClass Data;
    }
}
